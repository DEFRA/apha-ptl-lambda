using PTL.Lambda.TestKit;

namespace PTL.Lambda.Shared.Tests;

public class DatabaseConnectivityTests
{
    private static readonly DatabaseOptions Options = new("db-host", "PtlLambda", "svc-user", "svc-password", false);

    [Fact]
    public void VerifyConnection_Succeeds_WhenConnectionOpens()
    {
        var connectionFactory = new FakeDbConnectionFactory();

        DatabaseConnectivity.VerifyConnection(connectionFactory, Options);
    }

    [Fact]
    public void VerifyConnection_Throws_WhenConnectionFails()
    {
        var connectionFactory = new FakeDbConnectionFactory(throwOnOpen: true);

        var exception = Assert.Throws<InvalidOperationException>(
            () => DatabaseConnectivity.VerifyConnection(connectionFactory, Options));

        Assert.Contains(Options.Name, exception.Message, StringComparison.Ordinal);
        Assert.Contains(Options.Host, exception.Message, StringComparison.Ordinal);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public void VerifyConnection_Throws_WhenConnectionFactoryIsNull() =>
        Assert.Throws<ArgumentNullException>(() => DatabaseConnectivity.VerifyConnection(null!, Options));

    [Fact]
    public void VerifyConnection_Throws_WhenOptionsIsNull() =>
        Assert.Throws<ArgumentNullException>(() => DatabaseConnectivity.VerifyConnection(new FakeDbConnectionFactory(), null!));
}

