using PTL.Lambda.TestKit;

namespace PTL.Lambda.Shared.Tests;

public class CleanupFunctionRunnerTests
{
    private static readonly DateTime ReferenceDate = new(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc);
    private const LegacyServiceType ServiceType = LegacyServiceType.DeleteAttachmentsService;

    public CleanupFunctionRunnerTests()
    {
        Environment.SetEnvironmentVariable("Database__Host", "db-host,1433");
        Environment.SetEnvironmentVariable("Database__Name", "PtlLambda");
        Environment.SetEnvironmentVariable("Database__User", "svc-user");
        Environment.SetEnvironmentVariable("Database__Password", "svc-password");
        Environment.SetEnvironmentVariable("Database__TrustServerCertificate", "true");
    }

    [Fact]
    public async Task RunAsync_ReturnsCompleted_WithExpectedMessage_OnSuccessfulRun()
    {
        var batch = new List<FakeCleanupRecord> { new(Guid.NewGuid(), ReferenceDate.AddYears(-8), "Internal") };
        var connectionFactory = new FakeDbConnectionFactory();
        var repository = new FakeBatchCleanupRepository<FakeCleanupRecord>([batch]);
        var auditLogger = new FakeAuditLogger();

        var result = await CleanupFunctionRunner.RunAsync(
            new CleanupDescriptor("TestCleanup", ServiceType, "widget", "deleted"), connectionFactory, repository, auditLogger,
            new CleanupInvocationRequest());

        Assert.Equal("TestCleanup", result.Service);
        Assert.Equal("Completed", result.Status);
        Assert.Matches(@"^deleted 1 widget\(s\) in \d+\.\ds\.$", result.Message);
        Assert.Single(connectionFactory.LastConnection!.Transactions);
        Assert.True(connectionFactory.LastConnection.Transactions[0].Committed);
        Assert.All(auditLogger.Entries, e => Assert.Equal(ServiceType, e.ServiceType));
    }

    [Fact]
    public async Task RunAsync_ReturnsDryRunMessage_WithoutCommittingAnyTransaction()
    {
        var batch = new List<FakeCleanupRecord> { new(Guid.NewGuid(), ReferenceDate.AddYears(-8), "Internal") };
        var connectionFactory = new FakeDbConnectionFactory();
        var repository = new FakeBatchCleanupRepository<FakeCleanupRecord>([batch]);

        var result = await CleanupFunctionRunner.RunAsync(
            new CleanupDescriptor("TestCleanup", ServiceType, "widget", "deleted"), connectionFactory, repository, new FakeAuditLogger(),
            new CleanupInvocationRequest(DryRun: true));

        Assert.Matches(@"^\[DRY RUN\] identified 1 widget\(s\) in \d+\.\ds\.$", result.Message);
        Assert.Empty(connectionFactory.LastConnection!.Transactions);
    }

    [Fact]
    public async Task RunAsync_Throws_WhenConnectionFails()
    {
        var connectionFactory = new FakeDbConnectionFactory(throwOnOpen: true);
        var repository = new FakeBatchCleanupRepository<FakeCleanupRecord>([]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CleanupFunctionRunner.RunAsync(
                new CleanupDescriptor("TestCleanup", ServiceType, "widget", "deleted"), connectionFactory, repository, new FakeAuditLogger(),
                new CleanupInvocationRequest()));
    }

    [Fact]
    public async Task RunAsync_Throws_AndRollsBack_WhenApplyFails()
    {
        var batch = new List<FakeCleanupRecord> { new(Guid.NewGuid(), ReferenceDate.AddYears(-8), "Internal") };
        var connectionFactory = new FakeDbConnectionFactory();
        var repository = new FakeBatchCleanupRepository<FakeCleanupRecord>([batch]) { ThrowOnApply = true };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CleanupFunctionRunner.RunAsync(
                new CleanupDescriptor("TestCleanup", ServiceType, "widget", "deleted"), connectionFactory, repository, new FakeAuditLogger(),
                new CleanupInvocationRequest()));

        Assert.True(connectionFactory.LastConnection!.Transactions[0].RolledBack);
    }
}
