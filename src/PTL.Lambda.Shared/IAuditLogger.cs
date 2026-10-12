using System.Data;

namespace PTL.Lambda.Shared;

/// <summary>Writes the audit trail the legacy services wrote to tblDeletionLog via spInsertDeletionLog.</summary>
public interface IAuditLogger
{
    Task LogAsync(
        IDbConnection connection,
        string serviceName,
        string message,
        AuditLogLevel level,
        CancellationToken cancellationToken = default);
}
