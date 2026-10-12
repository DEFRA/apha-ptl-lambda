using System.Data;
using System.Diagnostics;
using Serilog;

namespace PTL.Lambda.Shared;

/// <summary>
/// Generic replacement for the shared batch-paging/dry-run/transaction/audit pattern that was
/// duplicated across DeleteAttachmentsService, DeleteConsultantService and RemoveCustomerDataService
/// (proficiency-testing-2026-08-17, all via BaseCleanupService.ProcessRecords). Each Lambda supplies
/// only an <see cref="IBatchCleanupRepository{TRecord}"/> (its own stored procedure calls) - the
/// paging loop, dry-run sampling/years-breakdown reporting, transactional apply with
/// commit/rollback, the elapsed-time guard, and the audit trail are implemented once, here.
/// </summary>
public sealed class BatchCleanupEngine<TRecord> where TRecord : ICleanupRecord
{
    private const int SampleSize = 5;
    private static readonly TimeSpan DefaultTimeBudget = TimeSpan.FromMinutes(12);

    private readonly string _cleanupName;
    private readonly IBatchCleanupRepository<TRecord> _repository;
    private readonly IAuditLogger _auditLogger;
    private readonly TimeSpan _timeBudget;

    public BatchCleanupEngine(
        string cleanupName,
        IBatchCleanupRepository<TRecord> repository,
        IAuditLogger auditLogger,
        TimeSpan? timeBudget = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cleanupName);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(auditLogger);

        _cleanupName = cleanupName;
        _repository = repository;
        _auditLogger = auditLogger;
        _timeBudget = timeBudget ?? DefaultTimeBudget;
    }

    /// <summary>
    /// Runs one cleanup pass. Mirrors BaseCleanupService.RunCleanup(batchSize, dryRun, testDate):
    /// a future <paramref name="testDate"/> outside dry-run mode is a deliberate no-op (matches the
    /// legacy guard verbatim), dry-run never mutates data and reports counts only, and a real run
    /// commits each batch independently so a mid-run stop simply defers the remainder.
    /// </summary>
    public async Task<CleanupRunResult> RunAsync(
        IDbConnection connection,
        int batchSize,
        bool dryRun,
        DateTime? testDate = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Batch size must be greater than zero.");
        }

        if (testDate.HasValue && testDate.Value.Date > DateTime.UtcNow.Date && !dryRun)
        {
            return new CleanupRunResult(new Dictionary<string, int>(), 0, TimeSpan.Zero, DeferredRemainingWork: false);
        }

        var referenceDate = testDate ?? DateTime.UtcNow;
        var effectiveBatchSize = dryRun ? int.MaxValue : batchSize;
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var processedIds = new HashSet<Guid>();
        var stopwatch = Stopwatch.StartNew();
        var deferredRemainingWork = false;

        await LogAsync(
            connection,
            dryRun ? $"Starting {_cleanupName} cleanup process (DRY RUN)" : $"Starting {_cleanupName} cleanup process",
            AuditLogLevel.Information,
            cancellationToken).ConfigureAwait(false);

        while (true)
        {
            if (stopwatch.Elapsed >= _timeBudget)
            {
                deferredRemainingWork = true;
                await LogAsync(
                    connection,
                    $"Stopping {_cleanupName} early after {stopwatch.Elapsed.TotalMinutes:N1} minutes; " +
                    "remaining records are deferred to the next scheduled invocation.",
                    AuditLogLevel.Warning,
                    cancellationToken).ConfigureAwait(false);
                break;
            }

            var batch = await _repository.GetBatchAsync(connection, effectiveBatchSize, referenceDate, processedIds, cancellationToken)
                .ConfigureAwait(false);

            if (batch.Count == 0)
            {
                break;
            }

            if (dryRun)
            {
                await LogDryRunDetailAsync(connection, batch, referenceDate, cancellationToken).ConfigureAwait(false);
                AddCounts(counts, batch.GroupBy(r => r.Category, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal));
            }
            else
            {
                var applied = await ApplyBatchWithTransactionAsync(connection, batch, cancellationToken).ConfigureAwait(false);
                AddCounts(counts, applied);
            }

            foreach (var record in batch)
            {
                processedIds.Add(record.Id);
            }
        }

        var totalProcessed = counts.Values.Sum();
        await LogAsync(
            connection,
            BuildSummary(dryRun, counts, totalProcessed, stopwatch.Elapsed),
            AuditLogLevel.Information,
            cancellationToken).ConfigureAwait(false);

        return new CleanupRunResult(counts, totalProcessed, stopwatch.Elapsed, deferredRemainingWork);
    }

    private async Task<IReadOnlyDictionary<string, int>> ApplyBatchWithTransactionAsync(
        IDbConnection connection, IReadOnlyList<TRecord> batch, CancellationToken cancellationToken)
    {
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            var applied = await _repository.ApplyBatchAsync(connection, transaction, batch, cancellationToken).ConfigureAwait(false);
            transaction.Commit();
            return applied;
        }
        catch (Exception exception)
        {
            RollbackSafely(transaction, exception);
            throw;
        }
    }

    private void RollbackSafely(IDbTransaction transaction, Exception originalException)
    {
        try
        {
            transaction.Rollback();
            Log.Warning(originalException, "{CleanupName}: transaction rolled back after an apply failure", _cleanupName);
        }
        catch (Exception rollbackException)
        {
            Log.Error(
                rollbackException,
                "{CleanupName}: failed to roll back transaction. Original error: {OriginalError}",
                _cleanupName, originalException.Message);
        }
    }

    private async Task LogDryRunDetailAsync(
        IDbConnection connection, IReadOnlyList<TRecord> batch, DateTime referenceDate, CancellationToken cancellationToken)
    {
        var lines = new List<string> { $"Batch of {batch.Count} records:" };
        lines.AddRange(batch.Take(SampleSize).Select(r =>
            $"- {r.Category}: {r.Id} (inactive since {r.InactiveSince:dd/MM/yyyy})"));

        if (batch.Count > SampleSize)
        {
            lines.Add($"...and {batch.Count - SampleSize} more records");
        }

        lines.Add("Breakdown by years inactive:");
        lines.AddRange(batch
            .GroupBy(r => referenceDate.Year - r.InactiveSince.Year)
            .OrderBy(g => g.Key)
            .Select(g => $"{g.Count():N0} records inactive for {g.Key} years"));

        await LogAsync(connection, string.Join('\n', lines), AuditLogLevel.Information, cancellationToken).ConfigureAwait(false);
    }

    private static void AddCounts(Dictionary<string, int> counts, IReadOnlyDictionary<string, int> toAdd)
    {
        foreach (var (category, amount) in toAdd)
        {
            counts[category] = counts.GetValueOrDefault(category) + amount;
        }
    }

    private static string BuildSummary(bool dryRun, Dictionary<string, int> counts, int totalProcessed, TimeSpan duration)
    {
        var action = dryRun ? "identified" : "cleaned";
        var lines = new List<string>
        {
            $"Cleanup {(dryRun ? "dry run" : "process")} completed:",
            $"Total records {action}: {totalProcessed:N0}"
        };
        lines.AddRange(counts.Select(kvp => $"- {kvp.Key}: {kvp.Value:N0}"));
        lines.Add($"Total duration: {duration.TotalMinutes:N1} minutes");

        if (dryRun)
        {
            lines.Add("NOTE: This was a dry run - no records were actually changed.");
        }

        return string.Join('\n', lines);
    }

    private async Task LogAsync(IDbConnection connection, string message, AuditLogLevel level, CancellationToken cancellationToken)
    {
        switch (level)
        {
            case AuditLogLevel.Error:
                Log.Error("{CleanupName}: {Message}", _cleanupName, message);
                break;
            case AuditLogLevel.Warning:
                Log.Warning("{CleanupName}: {Message}", _cleanupName, message);
                break;
            default:
                Log.Information("{CleanupName}: {Message}", _cleanupName, message);
                break;
        }

        await _auditLogger.LogAsync(connection, _cleanupName, message, level, cancellationToken).ConfigureAwait(false);
    }
}
