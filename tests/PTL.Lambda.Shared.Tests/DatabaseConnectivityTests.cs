using System.Data;
using System.Diagnostics.CodeAnalysis;
using PTL.Lambda.Shared;

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

    private sealed class FakeDbConnectionFactory(bool throwOnOpen = false) : IDbConnectionFactory
    {
        public IDbConnection CreateConnection() => new FakeDbConnection(throwOnOpen);

        private sealed class FakeDbConnection(bool throwOnOpen) : IDbConnection
        {
            private string _connectionString = string.Empty;

            // [AllowNull] matches IDbConnection.ConnectionString's own property-level annotation.
            [AllowNull]
            public string ConnectionString
            {
                get => _connectionString;
                set => _connectionString = value ?? string.Empty;
            }

            public int ConnectionTimeout => 0;
            public string Database => string.Empty;
            public ConnectionState State { get; private set; } = ConnectionState.Closed;

            public void Open()
            {
                if (throwOnOpen)
                {
                    throw new InvalidOperationException("Simulated connection failure.");
                }

                State = ConnectionState.Open;
            }

            public void Close() => State = ConnectionState.Closed;
            public IDbTransaction BeginTransaction() => throw new NotSupportedException();
            public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotSupportedException();
            public void ChangeDatabase(string databaseName) => throw new NotSupportedException();
            public IDbCommand CreateCommand() => throw new NotSupportedException();
            public void Dispose()
            {
            }
        }
    }
}
