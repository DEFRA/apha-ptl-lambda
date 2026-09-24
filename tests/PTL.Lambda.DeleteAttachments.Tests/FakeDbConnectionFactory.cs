using System.Data;
using System.Diagnostics.CodeAnalysis;
using PTL.Lambda.Shared;

namespace PTL.Lambda.DeleteAttachments.Tests;

/// <summary>Avoids opening a real SQL Server socket in tests while still exercising the Open() call path.</summary>
internal sealed class FakeDbConnectionFactory(bool throwOnOpen = false) : IDbConnectionFactory
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
