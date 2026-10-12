using System.Data;

namespace PTL.Lambda.Shared;

/// <summary>
/// The only part of a cleanup Lambda that is specific to one legacy service: the "get" and "apply"
/// stored procedure calls (TVP-paged query, transactional apply with per-category output counts).
/// Everything else - pagination, dry-run reporting, transaction lifecycle, the timeout guard, and
/// audit logging - lives once in <see cref="BatchCleanupEngine{TRecord}"/>.
/// </summary>
public interface IBatchCleanupRepository<TRecord> where TRecord : ICleanupRecord
{
    /// <summary>Pages the next batch, excluding IDs already processed earlier in this run.</summary>
    Task<IReadOnlyList<TRecord>> GetBatchAsync(
        IDbConnection connection,
        int batchSize,
        DateTime referenceDate,
        IReadOnlySet<Guid> excludedIds,
        CancellationToken cancellationToken = default);

    /// <summary>Applies the batch (delete or anonymize) within <paramref name="transaction"/>, returning the actual per-category count the database reports.</summary>
    Task<IReadOnlyDictionary<string, int>> ApplyBatchAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        IReadOnlyList<TRecord> batch,
        CancellationToken cancellationToken = default);
}
