namespace PTL.Lambda.Shared;

public sealed record DatabaseOptions(string Host, string Name, string User, string Password, bool TrustServerCertificate);
