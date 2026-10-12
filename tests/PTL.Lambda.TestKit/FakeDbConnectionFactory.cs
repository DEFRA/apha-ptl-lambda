using System.Data;
using PTL.Lambda.Shared;

namespace PTL.Lambda.TestKit;

/// <summary>Hands out FakeDbConnections and remembers the most recent one so tests can inspect it (e.g. its transactions) after a handler runs.</summary>
public sealed class FakeDbConnectionFactory(bool throwOnOpen = false, bool throwOnRollback = false) : IDbConnectionFactory
{
    public FakeDbConnection? LastConnection { get; private set; }

    public IDbConnection CreateConnection()
    {
        LastConnection = new FakeDbConnection(throwOnOpen, throwOnRollback);
        return LastConnection;
    }
}
