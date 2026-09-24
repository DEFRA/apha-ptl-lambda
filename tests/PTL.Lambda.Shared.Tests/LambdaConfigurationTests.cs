using PTL.Lambda.Shared;

namespace PTL.Lambda.Shared.Tests;

public class LambdaConfigurationTests
{
    [Fact]
    public void Build_ReadsEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("Database__Host", "env-host");

        var configuration = LambdaConfiguration.Build();

        Assert.Equal("env-host", configuration["Database:Host"]);
    }
}
