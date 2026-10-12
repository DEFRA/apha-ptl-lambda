using System.Data;
using PTL.Lambda.Shared;

namespace PTL.Lambda.TestKit;

public sealed class FakeAuditLogger : IAuditLogger
{
    public List<(LegacyServiceType ServiceType, string Message, AuditLogLevel Level)> Entries { get; } = [];

    public Task LogAsync(
        IDbConnection connection, LegacyServiceType serviceType, string message, AuditLogLevel level,
        CancellationToken cancellationToken = default)
    {
        Entries.Add((serviceType, message, level));
        return Task.CompletedTask;
    }
}
