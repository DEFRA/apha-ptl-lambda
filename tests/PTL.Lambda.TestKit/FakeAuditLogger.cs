using System.Data;
using PTL.Lambda.Shared;

namespace PTL.Lambda.TestKit;

public sealed class FakeAuditLogger : IAuditLogger
{
    public List<(string ServiceName, string Message, AuditLogLevel Level)> Entries { get; } = [];

    public Task LogAsync(
        IDbConnection connection, string serviceName, string message, AuditLogLevel level,
        CancellationToken cancellationToken = default)
    {
        Entries.Add((serviceName, message, level));
        return Task.CompletedTask;
    }
}
