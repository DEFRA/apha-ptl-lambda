namespace PTL.Lambda.Shared.Tests;

internal sealed record FakeCleanupRecord(Guid Id, DateTime InactiveSince, string Category) : ICleanupRecord;
