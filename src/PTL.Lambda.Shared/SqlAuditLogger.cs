using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using Serilog;

namespace PTL.Lambda.Shared;

/// <summary>
/// Writes an audit trail row via spInsertDeletionLog (tblDeletionLog) - the same SQL insert the
/// legacy EventLogger performed. On failure it logs via Serilog instead of falling back to
/// System.Diagnostics.EventLog: EventLog is Windows-only and has no Lambda equivalent.
/// </summary>
public sealed class SqlAuditLogger : IAuditLogger
{
    private readonly Func<IDbConnection, string, string, AuditLogLevel, DateTime, CancellationToken, Task> _insert;

    public SqlAuditLogger() : this(InsertAsync)
    {
    }

    // Lets tests replace the real Dapper/stored-procedure call with a fake, the same seam each
    // Lambda's FunctionHandler overload already uses for IDbConnectionFactory.
    internal SqlAuditLogger(Func<IDbConnection, string, string, AuditLogLevel, DateTime, CancellationToken, Task> insert) =>
        _insert = insert;

    public async Task LogAsync(
        IDbConnection connection,
        string serviceName,
        string message,
        AuditLogLevel level,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        try
        {
            await _insert(connection, serviceName, message, level, DateTime.UtcNow, cancellationToken).ConfigureAwait(false);
        }
        catch (DbException exception)
        {
            // Matches the legacy EventLogger: an audit-logging failure is swallowed (never blocks
            // the cleanup run itself) after being logged for operators to see.
            Log.Error(exception, "Failed to write audit log entry for {Service}: {Message}", serviceName, message);
        }
    }

    [ExcludeFromCodeCoverage] // Real Dapper/stored-procedure call - needs a live SQL Server to exercise, same rationale as Program.cs.
    private static Task InsertAsync(
        IDbConnection connection, string serviceName, string message, AuditLogLevel level, DateTime createdAtUtc,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition(
            "spInsertDeletionLog",
            new { Message = message, Type = level.ToString(), Service = serviceName, CreatedAt = createdAtUtc },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));
}
