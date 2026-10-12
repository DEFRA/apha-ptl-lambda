using System.Data;
using System.Data.Common;
using PTL.Lambda.TestKit;

namespace PTL.Lambda.Shared.Tests;

public class SqlAuditLoggerTests
{
    private static readonly FakeDbConnection Connection = new();
    private const LegacyServiceType ServiceType = LegacyServiceType.DeleteAttachmentsService;

    [Fact]
    public async Task LogAsync_InvokesInsert_WithExpectedArguments()
    {
        IDbConnection? seenConnection = null;
        LegacyServiceType? seenServiceType = null;
        string? seenMessage = null;
        AuditLogLevel? seenLevel = null;

        var logger = new SqlAuditLogger((connection, serviceType, message, level, _, _) =>
        {
            seenConnection = connection;
            seenServiceType = serviceType;
            seenMessage = message;
            seenLevel = level;
            return Task.CompletedTask;
        });

        await logger.LogAsync(Connection, ServiceType, "Starting cleanup", AuditLogLevel.Information);

        Assert.Same(Connection, seenConnection);
        Assert.Equal(ServiceType, seenServiceType);
        Assert.Equal("Starting cleanup", seenMessage);
        Assert.Equal(AuditLogLevel.Information, seenLevel);
    }

    [Fact]
    public async Task LogAsync_SwallowsException_WhenInsertFailsWithDbException()
    {
        var logger = new SqlAuditLogger((_, _, _, _, _, _) => throw new TestDbException());

        await logger.LogAsync(Connection, ServiceType, "Starting cleanup", AuditLogLevel.Error);
        // No exception propagates - matches the legacy EventLogger's SqlException fallback behaviour.
    }

    [Fact]
    public async Task LogAsync_Throws_WhenConnectionIsNull() =>
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => new SqlAuditLogger((_, _, _, _, _, _) => Task.CompletedTask)
                .LogAsync(null!, ServiceType, "message", AuditLogLevel.Information));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task LogAsync_Throws_WhenMessageIsNullOrWhiteSpace(string? message) =>
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => new SqlAuditLogger((_, _, _, _, _, _) => Task.CompletedTask)
                .LogAsync(Connection, ServiceType, message!, AuditLogLevel.Information));

    [Fact]
    public void DefaultConstructor_DoesNotThrow() => _ = new SqlAuditLogger();

    private sealed class TestDbException() : DbException("Simulated SQL failure.");
}
