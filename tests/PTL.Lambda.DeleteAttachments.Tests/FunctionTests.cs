using Amazon.Lambda.TestUtilities;
using PTL.Lambda.DeleteAttachments;
using PTL.Lambda.Shared;
using PTL.Lambda.TestKit;

namespace PTL.Lambda.DeleteAttachments.Tests;

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
    public async Task FunctionHandler_DeletesAttachments_OnDefaultScheduledInvocation()
    {
        var batch = new List<AttachmentToDelete>
        {
            new() { Id = Guid.NewGuid(), AttachmentType = AttachmentType.InternalAttachment, ResultsDate = ReferenceDate.AddYears(-8), Filename = "a.pdf", ContentType = "application/pdf" },
            new() { Id = Guid.NewGuid(), AttachmentType = AttachmentType.PublishedAttachment, ResultsDate = ReferenceDate.AddYears(-9), Filename = "b.pdf", ContentType = "application/pdf" }
        };
        var connectionFactory = new FakeDbConnectionFactory();
        var repository = new FakeBatchCleanupRepository<AttachmentToDelete>([batch]);
        var context = new TestLambdaContext();

        var result = await DeleteAttachmentsFunction.FunctionHandler(input: null, context, connectionFactory, repository, new FakeAuditLogger());

        Assert.Equal("DeleteAttachments", result.Service);
        Assert.Equal("Completed", result.Status);
        Assert.Contains("deleted 2 attachment(s)", result.Message, StringComparison.Ordinal);
        Assert.Single(connectionFactory.LastConnection!.Transactions);
        Assert.True(connectionFactory.LastConnection.Transactions[0].Committed);
    }

    [Fact]
    public async Task FunctionHandler_ReportsDryRun_WithoutCommittingAnyTransaction()
    {
        var batch = new List<AttachmentToDelete>
        {
            new() { Id = Guid.NewGuid(), AttachmentType = AttachmentType.InternalAttachment, ResultsDate = ReferenceDate.AddYears(-8) }
        };
        var connectionFactory = new FakeDbConnectionFactory();
        var repository = new FakeBatchCleanupRepository<AttachmentToDelete>([batch]);
        var context = new TestLambdaContext();

        var result = await DeleteAttachmentsFunction.FunctionHandler(
            new CleanupInvocationRequest(DryRun: true), context, connectionFactory, repository, new FakeAuditLogger());

        Assert.Equal("Completed", result.Status);
        Assert.StartsWith("[DRY RUN] identified 1 attachment(s)", result.Message, StringComparison.Ordinal);
        Assert.Empty(connectionFactory.LastConnection!.Transactions);
    }

    [Fact]
    public async Task FunctionHandler_Throws_WhenConnectionFails()
    {
        var context = new TestLambdaContext();
        var connectionFactory = new FakeDbConnectionFactory(throwOnOpen: true);

        await Assert.ThrowsAsync<LambdaInvocationException>(
            () => DeleteAttachmentsFunction.FunctionHandler(
                input: null, context, connectionFactory, new FakeBatchCleanupRepository<AttachmentToDelete>([]), new FakeAuditLogger()));
    }

    [Fact]
    public async Task FunctionHandler_Throws_WhenApplyFails()
    {
        var batch = new List<AttachmentToDelete>
        {
            new() { Id = Guid.NewGuid(), AttachmentType = AttachmentType.InternalAttachment, ResultsDate = ReferenceDate.AddYears(-8) }
        };
        var connectionFactory = new FakeDbConnectionFactory();
        var repository = new FakeBatchCleanupRepository<AttachmentToDelete>([batch]) { ThrowOnApply = true };
        var context = new TestLambdaContext();

        await Assert.ThrowsAsync<LambdaInvocationException>(
            () => DeleteAttachmentsFunction.FunctionHandler(input: null, context, connectionFactory, repository, new FakeAuditLogger()));

        Assert.True(connectionFactory.LastConnection!.Transactions[0].RolledBack);
    }
}

