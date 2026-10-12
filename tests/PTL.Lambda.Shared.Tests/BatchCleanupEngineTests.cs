using PTL.Lambda.TestKit;

namespace PTL.Lambda.Shared.Tests;

public class BatchCleanupEngineTests
{
    private static readonly DateTime ReferenceDate = new(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RunAsync_ReturnsEmptyResult_WhenNoRecordsFound()
    {
        var repository = new FakeBatchCleanupRepository<FakeCleanupRecord>([]);
        var auditLogger = new FakeAuditLogger();
        var engine = new BatchCleanupEngine<FakeCleanupRecord>("Test Cleanup", repository, auditLogger);

        var result = await engine.RunAsync(new FakeDbConnection(), batchSize: 1000, dryRun: false, testDate: ReferenceDate);

        Assert.Empty(result.Counts);
        Assert.Equal(0, result.TotalProcessed);
        Assert.False(result.DeferredRemainingWork);
        Assert.Contains(auditLogger.Entries, e => e.Message.Contains("Starting", StringComparison.Ordinal));
        Assert.Contains(auditLogger.Entries, e => e.Message.Contains("Total records cleaned: 0", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunAsync_ProcessesSingleBatch_AndCommitsTransaction()
    {
        var batch = new List<FakeCleanupRecord>
        {
            new(Guid.NewGuid(), ReferenceDate.AddYears(-8), "Internal"),
            new(Guid.NewGuid(), ReferenceDate.AddYears(-9), "External")
        };
        var repository = new FakeBatchCleanupRepository<FakeCleanupRecord>([batch]);
        var auditLogger = new FakeAuditLogger();
        var connection = new FakeDbConnection();
        var engine = new BatchCleanupEngine<FakeCleanupRecord>("Test Cleanup", repository, auditLogger);

        var result = await engine.RunAsync(connection, batchSize: 1000, dryRun: false, testDate: ReferenceDate);

        Assert.Equal(1, result.Counts["Internal"]);
        Assert.Equal(1, result.Counts["External"]);
        Assert.Equal(2, result.TotalProcessed);
        Assert.False(result.DeferredRemainingWork);
        Assert.Single(connection.Transactions);
        Assert.True(connection.Transactions[0].Committed);
        Assert.False(connection.Transactions[0].RolledBack);
    }

    [Fact]
    public async Task RunAsync_ProcessesMultipleBatches_UntilEmpty_AndExcludesEarlierIds()
    {
        var firstId = Guid.NewGuid();
        var firstBatch = new List<FakeCleanupRecord> { new(firstId, ReferenceDate.AddYears(-8), "Internal") };
        var secondBatch = new List<FakeCleanupRecord> { new(Guid.NewGuid(), ReferenceDate.AddYears(-8), "Internal") };
        var repository = new FakeBatchCleanupRepository<FakeCleanupRecord>([firstBatch, secondBatch]);
        var engine = new BatchCleanupEngine<FakeCleanupRecord>("Test Cleanup", repository, new FakeAuditLogger());

        var result = await engine.RunAsync(new FakeDbConnection(), batchSize: 1000, dryRun: false, testDate: ReferenceDate);

        Assert.Equal(2, result.Counts["Internal"]);
        Assert.Equal(3, repository.GetBatchCallCount); // two batches + the final empty call that stops the loop
        Assert.Empty(repository.ExcludedIdsSeenPerCall[0]);
        Assert.Contains(firstId, repository.ExcludedIdsSeenPerCall[1]);
    }

    [Fact]
    public async Task RunAsync_DryRun_NeverAppliesOrCommits_ButCountsAndSamplesRecords()
    {
        var batch = Enumerable.Range(0, 7)
            .Select(i => new FakeCleanupRecord(Guid.NewGuid(), ReferenceDate.AddYears(-7 - i), "Internal"))
            .ToList();
        var repository = new FakeBatchCleanupRepository<FakeCleanupRecord>([batch]);
        var auditLogger = new FakeAuditLogger();
        var connection = new FakeDbConnection();
        var engine = new BatchCleanupEngine<FakeCleanupRecord>("Test Cleanup", repository, auditLogger);

        var result = await engine.RunAsync(connection, batchSize: 1000, dryRun: true, testDate: ReferenceDate);

        Assert.Equal(7, result.Counts["Internal"]);
        Assert.Equal(0, repository.ApplyBatchCallCount);
        Assert.Empty(connection.Transactions);
        Assert.Contains(auditLogger.Entries, e => e.Message.Contains("...and 2 more records", StringComparison.Ordinal));
        Assert.Contains(auditLogger.Entries, e => e.Message.Contains("Breakdown by years inactive", StringComparison.Ordinal));
        Assert.Contains(auditLogger.Entries, e => e.Message.Contains("dry run", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunAsync_RollsBackTransaction_AndRethrows_WhenApplyFails()
    {
        var batch = new List<FakeCleanupRecord> { new(Guid.NewGuid(), ReferenceDate.AddYears(-8), "Internal") };
        var repository = new FakeBatchCleanupRepository<FakeCleanupRecord>([batch]) { ThrowOnApply = true };
        var connection = new FakeDbConnection();
        var engine = new BatchCleanupEngine<FakeCleanupRecord>("Test Cleanup", repository, new FakeAuditLogger());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => engine.RunAsync(connection, batchSize: 1000, dryRun: false, testDate: ReferenceDate));

        Assert.Equal("Simulated apply failure.", exception.Message);
        Assert.Single(connection.Transactions);
        Assert.True(connection.Transactions[0].RolledBack);
        Assert.False(connection.Transactions[0].Committed);
    }

    [Fact]
    public async Task RunAsync_StillRethrowsOriginalException_WhenRollbackItselfFails()
    {
        var batch = new List<FakeCleanupRecord> { new(Guid.NewGuid(), ReferenceDate.AddYears(-8), "Internal") };
        var repository = new FakeBatchCleanupRepository<FakeCleanupRecord>([batch]) { ThrowOnApply = true };
        var connection = new FakeDbConnection(throwOnRollback: true);
        var engine = new BatchCleanupEngine<FakeCleanupRecord>("Test Cleanup", repository, new FakeAuditLogger());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => engine.RunAsync(connection, batchSize: 1000, dryRun: false, testDate: ReferenceDate));

        Assert.Equal("Simulated apply failure.", exception.Message);
    }

    [Fact]
    public async Task RunAsync_StopsEarly_AndDefersRemainingWork_WhenTimeBudgetIsExceeded()
    {
        var repository = new FakeBatchCleanupRepository<FakeCleanupRecord>([[new FakeCleanupRecord(Guid.NewGuid(), ReferenceDate, "Internal")]]);
        var auditLogger = new FakeAuditLogger();
        var engine = new BatchCleanupEngine<FakeCleanupRecord>("Test Cleanup", repository, auditLogger, timeBudget: TimeSpan.Zero);

        var result = await engine.RunAsync(new FakeDbConnection(), batchSize: 1000, dryRun: false, testDate: ReferenceDate);

        Assert.True(result.DeferredRemainingWork);
        Assert.Equal(0, repository.GetBatchCallCount);
        Assert.Contains(auditLogger.Entries, e => e.Level == AuditLogLevel.Warning && e.Message.Contains("deferred", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunAsync_SkipsEntirely_WhenTestDateIsFutureAndNotDryRun()
    {
        var repository = new FakeBatchCleanupRepository<FakeCleanupRecord>([[new FakeCleanupRecord(Guid.NewGuid(), ReferenceDate, "Internal")]]);
        var auditLogger = new FakeAuditLogger();
        var engine = new BatchCleanupEngine<FakeCleanupRecord>("Test Cleanup", repository, auditLogger);

        var result = await engine.RunAsync(
            new FakeDbConnection(), batchSize: 1000, dryRun: false, testDate: DateTime.UtcNow.AddDays(1));

        Assert.Empty(result.Counts);
        Assert.Equal(0, repository.GetBatchCallCount);
        Assert.Empty(auditLogger.Entries);
    }

    [Fact]
    public async Task RunAsync_RunsNormally_WhenTestDateIsFutureButDryRun()
    {
        var repository = new FakeBatchCleanupRepository<FakeCleanupRecord>([[new FakeCleanupRecord(Guid.NewGuid(), ReferenceDate, "Internal")]]);
        var engine = new BatchCleanupEngine<FakeCleanupRecord>("Test Cleanup", repository, new FakeAuditLogger());

        var result = await engine.RunAsync(
            new FakeDbConnection(), batchSize: 1000, dryRun: true, testDate: DateTime.UtcNow.AddDays(1));

        Assert.Equal(1, result.Counts["Internal"]);
    }

    [Fact]
    public async Task RunAsync_Throws_WhenConnectionIsNull()
    {
        var engine = new BatchCleanupEngine<FakeCleanupRecord>(
            "Test Cleanup", new FakeBatchCleanupRepository<FakeCleanupRecord>([]), new FakeAuditLogger());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => engine.RunAsync(null!, batchSize: 1000, dryRun: false));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RunAsync_Throws_WhenBatchSizeIsNotPositive(int batchSize)
    {
        var engine = new BatchCleanupEngine<FakeCleanupRecord>(
            "Test Cleanup", new FakeBatchCleanupRepository<FakeCleanupRecord>([]), new FakeAuditLogger());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => engine.RunAsync(new FakeDbConnection(), batchSize, dryRun: false));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Throws_WhenCleanupNameIsNullOrWhiteSpace(string? cleanupName) =>
        Assert.ThrowsAny<ArgumentException>(
            () => new BatchCleanupEngine<FakeCleanupRecord>(cleanupName!, new FakeBatchCleanupRepository<FakeCleanupRecord>([]), new FakeAuditLogger()));

    [Fact]
    public void Constructor_Throws_WhenRepositoryIsNull() =>
        Assert.Throws<ArgumentNullException>(
            () => new BatchCleanupEngine<FakeCleanupRecord>("Test Cleanup", null!, new FakeAuditLogger()));

    [Fact]
    public void Constructor_Throws_WhenAuditLoggerIsNull() =>
        Assert.Throws<ArgumentNullException>(
            () => new BatchCleanupEngine<FakeCleanupRecord>("Test Cleanup", new FakeBatchCleanupRepository<FakeCleanupRecord>([]), null!));
}

