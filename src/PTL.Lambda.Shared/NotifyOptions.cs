namespace PTL.Lambda.Shared;

/// <summary>
/// Credentials for sending mail through GOV.UK Notify, replacing the legacy SMTP-based
/// ProficiencyTestingEmailService Windows Service.
/// </summary>
public sealed record NotifyOptions(string ApiKey);
