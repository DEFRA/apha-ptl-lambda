using Amazon.Lambda.TestUtilities;
using PTL.Lambda.DeleteAttachments;

namespace PTL.Lambda.DeleteAttachments.Tests;

public class FunctionTests
{
    public FunctionTests()
    {
        Environment.SetEnvironmentVariable("Database__Host", "db-host,1433");
        Environment.SetEnvironmentVariable("Database__Name", "PtlLambda");
        Environment.SetEnvironmentVariable("Database__User", "svc-user");
        Environment.SetEnvironmentVariable("Database__Password", "svc-password");
        Environment.SetEnvironmentVariable("Database__TrustServerCertificate", "true");
    }

    [Fact]
    public async Task FunctionHandler_ReturnsStubResult()
    {
        var context = new TestLambdaContext();

        var result = await DeleteAttachmentsFunction.FunctionHandler(new { }, context);

        Assert.Equal("DeleteAttachments", result.Service);
        Assert.Equal("Stub", result.Status);
        Assert.Contains("PtlLambda", result.Message);
    }
}
