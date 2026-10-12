using System.Data;

namespace PTL.Lambda.TestKit;

/// <summary>Tracks Commit/Rollback calls so tests can assert transaction outcome without a real database.</summary>
public sealed class FakeDbTransaction(IDbConnection connection) : IDbTransaction
{
    public bool ThrowOnRollback { get; init; }
    public bool Committed { get; private set; }
    public bool RolledBack { get; private set; }
    public IDbConnection? Connection { get; } = connection;
    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

    public void Commit() => Committed = true;

    public void Rollback()
    {
        if (ThrowOnRollback)
        {
            throw new InvalidOperationException("Simulated rollback failure.");
        }

        RolledBack = true;
    }

    public void Dispose()
    {
    }
}
