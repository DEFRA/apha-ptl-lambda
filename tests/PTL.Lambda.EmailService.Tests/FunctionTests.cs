using Amazon.Lambda.TestUtilities;
using PTL.Lambda.EmailService;

namespace PTL.Lambda.EmailService.Tests;

public class FunctionTests
{
    public FunctionTests()
    {
        Environment.SetEnvironmentVariable("Notify__ApiKey", "test-notify-api-key");
    }

    [Fact]
    public async Task FunctionHandler_ReturnsStubResult()
    {
        var context = new TestLambdaContext();

        var result = await EmailServiceFunction.FunctionHandler(new { }, context);

        Assert.Equal("EmailService", result.Service);
        Assert.Equal("Stub", result.Status);
        Assert.Contains("GOV.UK Notify", result.Message);
    }
}
