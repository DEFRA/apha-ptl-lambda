namespace PTL.Lambda.Shared;

/// <summary>
/// Shape of the EventBridge Scheduler invocation payload for every cleanup Lambda - mirrors the
/// legacy CleanupOptions (ProficiencyTestingServiceObjects/CleanupOptions.cs). A normal scheduled
/// invocation passes an empty object ("{}"), giving the same defaults as the legacy Windows Service's
/// timer tick (BaseCleanupWindowsService.GetCleanupOptions). DryRun/TestDate allow the same ad-hoc,
/// no-mutation verification the legacy console mode offered, via a manual EventBridge/console test
/// invocation instead of an interactive prompt.
/// </summary>
public sealed record CleanupInvocationRequest(int BatchSize = 1000, bool DryRun = false, DateTime? TestDate = null);
