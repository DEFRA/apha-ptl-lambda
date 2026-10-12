using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace PTL.Lambda.TestKit;

/// <summary>
/// Covers every IDbConnection member BatchCleanupEngine or a FunctionHandler's connectivity check
/// actually calls (Open, BeginTransaction). CreateCommand/ChangeDatabase are never faked - real
/// Dapper/stored-procedure execution is covered by a live database, not a unit test, same rationale
/// as the <see cref="PTL.Lambda.Shared.SqlAuditLogger"/>'s excluded insert call.
/// </summary>
public sealed class FakeDbConnection(bool throwOnOpen = false, bool throwOnRollback = false) : IDbConnection
{
    private string _connectionString = string.Empty;

    public List<FakeDbTransaction> Transactions { get; } = [];

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

    public IDbTransaction BeginTransaction()
    {
        var transaction = new FakeDbTransaction(this) { ThrowOnRollback = throwOnRollback };
        Transactions.Add(transaction);
        return transaction;
    }

    public IDbTransaction BeginTransaction(IsolationLevel il) => BeginTransaction();

    public void ChangeDatabase(string databaseName) => throw new NotSupportedException();
    public IDbCommand CreateCommand() => throw new NotSupportedException();

    public void Dispose()
    {
    }
}
