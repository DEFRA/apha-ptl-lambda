using Amazon.Lambda.TestUtilities;
using PTL.Lambda.DeleteConsultant;
using PTL.Lambda.Shared;
using PTL.Lambda.TestKit;

namespace PTL.Lambda.DeleteConsultant.Tests;

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

    [Fact]
    public async Task FunctionHandler_DeletesConsultants_OnDefaultScheduledInvocation()
    {
        var batch = new List<ConsultantToDelete>
        {
            new() { Id = Guid.NewGuid(), ConsultantType = ConsultantType.Internal, InactiveDate = ReferenceDate.AddYears(-8), Name = "Alice" },
            new() { Id = Guid.NewGuid(), ConsultantType = ConsultantType.External, InactiveDate = ReferenceDate.AddYears(-9), Name = "Bob", SsoId = Guid.NewGuid() }
        };
        var connectionFactory = new FakeDbConnectionFactory();
        var repository = new FakeBatchCleanupRepository<ConsultantToDelete>([batch]);
        var context = new TestLambdaContext();

        var result = await DeleteConsultantFunction.FunctionHandler(input: null, context, connectionFactory, repository, new FakeAuditLogger());

        Assert.Equal("DeleteConsultant", result.Service);
        Assert.Equal("Completed", result.Status);
        Assert.Contains("deleted 2 consultant(s)", result.Message, StringComparison.Ordinal);
        Assert.Single(connectionFactory.LastConnection!.Transactions);
        Assert.True(connectionFactory.LastConnection.Transactions[0].Committed);
    }

    [Fact]
    public async Task FunctionHandler_ReportsDryRun_WithoutCommittingAnyTransaction()
    {
        var batch = new List<ConsultantToDelete>
        {
            new() { Id = Guid.NewGuid(), ConsultantType = ConsultantType.Internal, InactiveDate = ReferenceDate.AddYears(-8) }
        };
        var connectionFactory = new FakeDbConnectionFactory();
        var repository = new FakeBatchCleanupRepository<ConsultantToDelete>([batch]);
        var context = new TestLambdaContext();

        var result = await DeleteConsultantFunction.FunctionHandler(
            new CleanupInvocationRequest(DryRun: true), context, connectionFactory, repository, new FakeAuditLogger());

        Assert.Equal("Completed", result.Status);
        Assert.StartsWith("[DRY RUN] identified 1 consultant(s)", result.Message, StringComparison.Ordinal);
        Assert.Empty(connectionFactory.LastConnection!.Transactions);
    }

    [Fact]
    public async Task FunctionHandler_Throws_WhenConnectionFails()
    {
        var context = new TestLambdaContext();
        var connectionFactory = new FakeDbConnectionFactory(throwOnOpen: true);

        await Assert.ThrowsAsync<LambdaInvocationException>(
            () => DeleteConsultantFunction.FunctionHandler(
                input: null, context, connectionFactory, new FakeBatchCleanupRepository<ConsultantToDelete>([]), new FakeAuditLogger()));
    }

    [Fact]
    public async Task FunctionHandler_Throws_WhenApplyFails()
    {
        var batch = new List<ConsultantToDelete>
        {
            new() { Id = Guid.NewGuid(), ConsultantType = ConsultantType.Internal, InactiveDate = ReferenceDate.AddYears(-8) }
        };
        var connectionFactory = new FakeDbConnectionFactory();
        var repository = new FakeBatchCleanupRepository<ConsultantToDelete>([batch]) { ThrowOnApply = true };
        var context = new TestLambdaContext();

        await Assert.ThrowsAsync<LambdaInvocationException>(
            () => DeleteConsultantFunction.FunctionHandler(input: null, context, connectionFactory, repository, new FakeAuditLogger()));

        Assert.True(connectionFactory.LastConnection!.Transactions[0].RolledBack);
    }
}

