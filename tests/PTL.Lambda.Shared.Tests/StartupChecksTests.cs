using Microsoft.Extensions.Configuration;
using PTL.Lambda.Shared;

namespace PTL.Lambda.Shared.Tests;

public class StartupChecksTests
{
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void RequireDatabaseOptions_ReturnsOptions_WhenAllValuesPresent()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Host"] = "db-host",
            ["Database:Name"] = "PtlLambda",
            ["Database:User"] = "svc-user",
            ["Database:Password"] = "svc-password"
        });

        var options = StartupChecks.RequireDatabaseOptions(configuration);

        Assert.Equal("db-host", options.Host);
        Assert.Equal("PtlLambda", options.Name);
        Assert.False(options.TrustServerCertificate);
    }

    [Fact]
    public void RequireDatabaseOptions_Throws_WhenPasswordMissing()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Host"] = "db-host",
            ["Database:Name"] = "PtlLambda",
            ["Database:User"] = "svc-user"
        });

        Assert.Throws<InvalidOperationException>(() => StartupChecks.RequireDatabaseOptions(configuration));
    }

    [Fact]
    public void RequireDatabaseOptions_Throws_WhenHostMissing()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Name"] = "PtlLambda",
            ["Database:User"] = "svc-user",
            ["Database:Password"] = "svc-password"
        });

        Assert.Throws<InvalidOperationException>(() => StartupChecks.RequireDatabaseOptions(configuration));
    }

    [Fact]
    public void RequireDatabaseOptions_Throws_WhenNameMissing()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Host"] = "db-host",
            ["Database:User"] = "svc-user",
            ["Database:Password"] = "svc-password"
        });

        Assert.Throws<InvalidOperationException>(() => StartupChecks.RequireDatabaseOptions(configuration));
    }

    [Fact]
    public void RequireDatabaseOptions_Throws_WhenUserMissing()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Host"] = "db-host",
            ["Database:Name"] = "PtlLambda",
            ["Database:Password"] = "svc-password"
        });

        Assert.Throws<InvalidOperationException>(() => StartupChecks.RequireDatabaseOptions(configuration));
    }
}
