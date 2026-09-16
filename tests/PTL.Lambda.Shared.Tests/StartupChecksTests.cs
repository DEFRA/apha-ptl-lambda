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
    public void RequireGraphApiOptions_ReturnsOptions_WhenAllValuesPresent()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["GraphApi:TenantId"] = "tenant-id",
            ["GraphApi:ClientId"] = "client-id",
            ["GraphApi:ClientSecret"] = "client-secret",
            ["GraphApi:SenderUserId"] = "sender-id"
        });

        var options = StartupChecks.RequireGraphApiOptions(configuration);

        Assert.Equal("tenant-id", options.TenantId);
        Assert.Equal("sender-id", options.SenderUserId);
    }

    [Fact]
    public void RequireGraphApiOptions_Throws_WhenClientSecretMissing()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["GraphApi:TenantId"] = "tenant-id",
            ["GraphApi:ClientId"] = "client-id",
            ["GraphApi:SenderUserId"] = "sender-id"
        });

        Assert.Throws<InvalidOperationException>(() => StartupChecks.RequireGraphApiOptions(configuration));
    }
}
