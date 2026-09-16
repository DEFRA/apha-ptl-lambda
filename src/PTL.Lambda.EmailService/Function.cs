using Amazon.Lambda.Core;
using PTL.Lambda.Shared;

namespace PTL.Lambda.EmailService;

/// <summary>
/// Scaffold handler for the PT-LIMS email Lambda (GOV.UK Notify), replacing the legacy SMTP-based
/// ProficiencyTestingEmailService Windows Service. Template loading (23 templates) and send logic
/// is not yet migrated - see proficiency-testing-2026-08-17/ProficiencyTestingEmailService for the
/// source to port, and the GOV.UK Notify .NET client (Notify.Client NuGet package) for sending.
/// </summary>
public static class EmailServiceFunction
{
    public static Task<CleanupResult> FunctionHandler(object input, ILambdaContext context)
    {
        context.Logger.LogInformation("EmailService Lambda invoked.");

        var configuration = LambdaConfiguration.Build();
        // Discarded - only the fail-fast validation matters until send logic is migrated.
        _ = StartupChecks.RequireNotifyOptions(configuration);

        var result = new CleanupResult(
            Service: "EmailService",
            Status: "Stub",
            Message: "EmailService Lambda scaffold is wired up to GOV.UK Notify, but template loading and " +
                     "send logic has not been migrated yet. Replace this stub in Function.cs.",
            InvokedAtUtc: DateTime.UtcNow);

        return Task.FromResult(result);
    }
}
