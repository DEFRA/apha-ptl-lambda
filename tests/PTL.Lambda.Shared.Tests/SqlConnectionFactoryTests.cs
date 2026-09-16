using Microsoft.Extensions.Configuration;
using PTL.Lambda.Shared;

namespace PTL.Lambda.Shared.Tests;

public class SqlConnectionFactoryTests
{
    [Fact]
    public void CreateConnection_BuildsConnectionString_FromDatabaseOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Host"] = "db-host,1433",
                ["Database:Name"] = "PtlLambda",
                ["Database:User"] = "svc-user",
                ["Database:Password"] = "svc-password",
                ["Database:TrustServerCertificate"] = "true"
            })
            .Build();

        var factory = new SqlConnectionFactory(configuration);

        using var connection = factory.CreateConnection();

        Assert.Contains("db-host", connection.ConnectionString);
        Assert.Contains("PtlLambda", connection.ConnectionString);
    }
}
