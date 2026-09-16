namespace PTL.Lambda.Shared;

public sealed record CleanupResult(string Service, string Status, string Message, DateTime InvokedAtUtc);
