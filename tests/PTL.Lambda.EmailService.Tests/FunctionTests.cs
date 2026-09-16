using Amazon.Lambda.TestUtilities;
using PTL.Lambda.EmailService;

namespace PTL.Lambda.EmailService.Tests;

public class FunctionTests
{
    public FunctionTests()
    {
        Environment.SetEnvironmentVariable("GraphApi__TenantId", "tenant-id");
        Environment.SetEnvironmentVariable("GraphApi__ClientId", "client-id");
        Environment.SetEnvironmentVariable("GraphApi__ClientSecret", "client-secret");
        Environment.SetEnvironmentVariable("GraphApi__SenderUserId", "sender-id");
    }

    [Fact]
    public async Task FunctionHandler_ReturnsStubResult()
    {
        var context = new TestLambdaContext();

        var result = await EmailServiceFunction.FunctionHandler(new { }, context);

        Assert.Equal("EmailService", result.Service);
        Assert.Equal("Stub", result.Status);
        Assert.Contains("tenant-id", result.Message);
    }
}
