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
    private readonly Func<IDbConnection, LegacyServiceType, string, AuditLogLevel, DateTime, CancellationToken, Task> _insert;

    public SqlAuditLogger() : this(InsertAsync)
    {
    }

    // Lets tests replace the real Dapper/stored-procedure call with a fake, the same seam each
    // Lambda's FunctionHandler overload already uses for IDbConnectionFactory.
    internal SqlAuditLogger(Func<IDbConnection, LegacyServiceType, string, AuditLogLevel, DateTime, CancellationToken, Task> insert) =>
        _insert = insert;

    public async Task LogAsync(
        IDbConnection connection,
        LegacyServiceType serviceType,
        string message,
        AuditLogLevel level,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        try
        {
            await _insert(connection, serviceType, message, level, DateTime.UtcNow, cancellationToken).ConfigureAwait(false);
        }
        catch (DbException exception)
        {
            // Matches the legacy EventLogger: an audit-logging failure is swallowed (never blocks
            // the cleanup run itself) after being logged for operators to see.
            Log.Error(exception, "Failed to write audit log entry for {ServiceType}: {Message}", serviceType, message);
        }
    }

    [ExcludeFromCodeCoverage] // Real Dapper/stored-procedure call - needs a live SQL Server to exercise, same rationale as Program.cs.
    private static Task InsertAsync(
        IDbConnection connection, LegacyServiceType serviceType, string message, AuditLogLevel level, DateTime createdAtUtc,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition(
            "spInsertDeletionLog",
            // tblDeletionLog.fldService/@Service is INT (the legacy ServiceTypeEnum's underlying
            // value) - passing the free-text cleanup name here caused "Error converting data type
            // nvarchar to int" (SQL Error 8114) in production; cast the enum, never the display name.
            new { Message = message, Type = level.ToString(), Service = (int)serviceType, CreatedAt = createdAtUtc },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));
}
