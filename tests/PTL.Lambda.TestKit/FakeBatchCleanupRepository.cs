using System.Data;
using PTL.Lambda.Shared;

namespace PTL.Lambda.TestKit;

/// <summary>Generic over any ICleanupRecord so both BatchCleanupEngine's own tests and each Lambda's FunctionHandler tests can reuse one fake instead of hand-rolling a per-record-type repository.</summary>
public sealed class FakeBatchCleanupRepository<TRecord> : IBatchCleanupRepository<TRecord> where TRecord : ICleanupRecord
{
    private readonly Queue<IReadOnlyList<TRecord>> _batches;
    private readonly Func<IReadOnlyList<TRecord>, IReadOnlyDictionary<string, int>>? _applyOverride;

    public FakeBatchCleanupRepository(
        IEnumerable<IReadOnlyList<TRecord>> batches,
        Func<IReadOnlyList<TRecord>, IReadOnlyDictionary<string, int>>? applyOverride = null)
    {
        _batches = new Queue<IReadOnlyList<TRecord>>(batches);
        _applyOverride = applyOverride;
    }

    public bool ThrowOnApply { get; init; }
    public int GetBatchCallCount { get; private set; }
    public int ApplyBatchCallCount { get; private set; }
    public List<IReadOnlySet<Guid>> ExcludedIdsSeenPerCall { get; } = [];

    public Task<IReadOnlyList<TRecord>> GetBatchAsync(
        IDbConnection connection, int batchSize, DateTime referenceDate, IReadOnlySet<Guid> excludedIds,
        CancellationToken cancellationToken = default)
    {
        GetBatchCallCount++;
        // Snapshot rather than keep the live reference - the engine reuses and mutates the same
        // HashSet instance across iterations, so capturing the reference would make every entry
        // here reflect only the final state once the run completes.
        ExcludedIdsSeenPerCall.Add(new HashSet<Guid>(excludedIds));
        var batch = _batches.Count > 0 ? _batches.Dequeue() : (IReadOnlyList<TRecord>)Array.Empty<TRecord>();
        return Task.FromResult(batch);
    }

    public Task<IReadOnlyDictionary<string, int>> ApplyBatchAsync(
        IDbConnection connection, IDbTransaction transaction, IReadOnlyList<TRecord> batch,
        CancellationToken cancellationToken = default)
    {
        ApplyBatchCallCount++;

        if (ThrowOnApply)
        {
            throw new InvalidOperationException("Simulated apply failure.");
        }

        var result = _applyOverride?.Invoke(batch)
            ?? batch.GroupBy(r => r.Category, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        return Task.FromResult<IReadOnlyDictionary<string, int>>(result);
    }
}
