using Amazon.Lambda.TestUtilities;
using PTL.Lambda.DeleteConsultant;
using PTL.Lambda.Shared;

namespace PTL.Lambda.DeleteConsultant.Tests;

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
    public async Task FunctionHandler_ReturnsStubResult_WhenConnectionOpensSuccessfully()
    {
        var context = new TestLambdaContext();

        var result = await DeleteConsultantFunction.FunctionHandler(new { }, context, new FakeDbConnectionFactory());

        Assert.Equal("DeleteConsultant", result.Service);
        Assert.Equal("Stub", result.Status);
        Assert.Contains("PtlLambda", result.Message);
    }

    [Fact]
    public async Task FunctionHandler_Throws_WhenConnectionFails()
    {
        var context = new TestLambdaContext();
        var connectionFactory = new FakeDbConnectionFactory(throwOnOpen: true);

        await Assert.ThrowsAsync<LambdaInvocationException>(
            () => DeleteConsultantFunction.FunctionHandler(new { }, context, connectionFactory));
    }
}
