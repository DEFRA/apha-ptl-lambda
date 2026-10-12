namespace PTL.Lambda.Shared;

/// <summary>
/// Outcome of one <see cref="BatchCleanupEngine{TRecord}.RunAsync"/> call. <paramref name="DeferredRemainingWork"/>
/// is true when the elapsed-time guard stopped the run before all available records were processed -
/// the next scheduled EventBridge invocation will pick up where this one left off.
/// </summary>
public sealed record CleanupRunResult(
    IReadOnlyDictionary<string, int> Counts,
    int TotalProcessed,
    TimeSpan Duration,
    bool DeferredRemainingWork);
