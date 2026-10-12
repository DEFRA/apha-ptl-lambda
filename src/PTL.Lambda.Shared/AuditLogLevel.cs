namespace PTL.Lambda.Shared;

/// <summary>Mirrors the legacy EventLogEntryType values actually used by the cleanup services (Information/Warning/Error).</summary>
public enum AuditLogLevel
{
    Information,
    Warning,
    Error
}
