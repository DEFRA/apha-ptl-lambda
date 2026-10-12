using Amazon.Lambda.TestUtilities;
using PTL.Lambda.RemoveCustomerData;
using PTL.Lambda.Shared;
using PTL.Lambda.TestKit;

namespace PTL.Lambda.RemoveCustomerData.Tests;

public class FunctionTests
{
    private static readonly DateTime ReferenceDate = new(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc);

    public FunctionTests()
    {
        Environment.SetEnvironmentVariable("Database__Host", "db-host,1433");
        Environment.SetEnvironmentVariable("Database__Name", "PtlLambda");
        Environment.SetEnvironmentVariable("Database__User", "svc-user");
        Environment.SetEnvironmentVariable("Database__Password", "svc-password");
        Environment.SetEnvironmentVariable("Database__TrustServerCertificate", "true");
    }

    private static List<RecordToUpdate> CustomerBatch() =>
    [
        new() { Id = Guid.NewGuid(), Type = RecordType.Customer, InactiveDate = ReferenceDate.AddYears(-8), Identifier = "CUST-1" }
    ];

    private static List<RecordToUpdate> ParticipantBatch() =>
    [
        new() { Id = Guid.NewGuid(), Type = RecordType.Participant, InactiveDate = ReferenceDate.AddYears(-9), Identifier = "PART-1" }
    ];

    [Fact]
    public async Task FunctionHandler_AnonymizesBothPasses_OnDefaultScheduledInvocation()
    {
        var connectionFactory = new FakeDbConnectionFactory();
        var customerRepository = new FakeBatchCleanupRepository<RecordToUpdate>([CustomerBatch()]);
        var participantRepository = new FakeBatchCleanupRepository<RecordToUpdate>([ParticipantBatch()]);
        var context = new TestLambdaContext();

        var result = await RemoveCustomerDataFunction.FunctionHandler(
            input: null, context, connectionFactory, customerRepository, participantRepository, new FakeAuditLogger());

        Assert.Equal("RemoveCustomerData", result.Service);
        Assert.Equal("Completed", result.Status);
        Assert.Contains("anonymized 2 record(s)", result.Message, StringComparison.Ordinal);
        Assert.Equal(2, connectionFactory.LastConnection!.Transactions.Count);
        Assert.All(connectionFactory.LastConnection.Transactions, t => Assert.True(t.Committed));
    }

    [Fact]
    public async Task FunctionHandler_ReportsDryRun_WithoutCommittingAnyTransaction()
    {
        var connectionFactory = new FakeDbConnectionFactory();
        var customerRepository = new FakeBatchCleanupRepository<RecordToUpdate>([CustomerBatch()]);
        var participantRepository = new FakeBatchCleanupRepository<RecordToUpdate>([ParticipantBatch()]);
        var context = new TestLambdaContext();

        var result = await RemoveCustomerDataFunction.FunctionHandler(
            new CleanupInvocationRequest(DryRun: true), context, connectionFactory, customerRepository, participantRepository, new FakeAuditLogger());

        Assert.Equal("Completed", result.Status);
        Assert.StartsWith("[DRY RUN] identified 2 record(s)", result.Message, StringComparison.Ordinal);
        Assert.Empty(connectionFactory.LastConnection!.Transactions);
    }

    [Fact]
    public async Task FunctionHandler_Throws_WhenConnectionFails()
    {
        var context = new TestLambdaContext();
        var connectionFactory = new FakeDbConnectionFactory(throwOnOpen: true);

        await Assert.ThrowsAsync<LambdaInvocationException>(
            () => RemoveCustomerDataFunction.FunctionHandler(
                input: null, context, connectionFactory,
                new FakeBatchCleanupRepository<RecordToUpdate>([]),
                new FakeBatchCleanupRepository<RecordToUpdate>([]),
                new FakeAuditLogger()));
    }

    [Fact]
    public async Task FunctionHandler_Throws_AndNeverRunsParticipantPass_WhenCustomerPassFails()
    {
        var connectionFactory = new FakeDbConnectionFactory();
        var customerRepository = new FakeBatchCleanupRepository<RecordToUpdate>([CustomerBatch()]) { ThrowOnApply = true };
        var participantRepository = new FakeBatchCleanupRepository<RecordToUpdate>([ParticipantBatch()]);
        var context = new TestLambdaContext();

        await Assert.ThrowsAsync<LambdaInvocationException>(
            () => RemoveCustomerDataFunction.FunctionHandler(
                input: null, context, connectionFactory, customerRepository, participantRepository, new FakeAuditLogger()));

        Assert.True(connectionFactory.LastConnection!.Transactions[0].RolledBack);
        Assert.Equal(0, participantRepository.GetBatchCallCount);
    }
}

