namespace PTL.Lambda.Shared;

/// <summary>
/// Credentials for sending mail through Microsoft Graph (app-only client-credentials flow),
/// replacing the legacy SMTP-based ProficiencyTestingEmailService Windows Service.
/// </summary>
public sealed record GraphApiOptions(string TenantId, string ClientId, string ClientSecret, string SenderUserId);
