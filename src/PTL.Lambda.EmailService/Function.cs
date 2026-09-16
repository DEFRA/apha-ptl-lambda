using Amazon.Lambda.Core;
using PTL.Lambda.Shared;

namespace PTL.Lambda.EmailService;

/// <summary>
/// Scaffold handler for the PT-LIMS email Lambda (Microsoft Graph API), replacing the legacy
/// SMTP-based ProficiencyTestingEmailService Windows Service. Template loading (23 templates)
/// and send logic is not yet migrated - see
/// proficiency-testing-2026-08-17/ProficiencyTestingEmailService for the source to port.
/// </summary>
public static class EmailServiceFunction
{
    public static Task<CleanupResult> FunctionHandler(object input, ILambdaContext context)
    {
        context.Logger.LogInformation("EmailService Lambda invoked.");

        var configuration = LambdaConfiguration.Build();
        var options = StartupChecks.RequireGraphApiOptions(configuration);

        var result = new CleanupResult(
            Service: "EmailService",
            Status: "Stub",
            Message: $"EmailService Lambda scaffold is wired up to Microsoft Graph API for tenant '{options.TenantId}' " +
                     $"(sender '{options.SenderUserId}'), but template loading and send logic has not been migrated " +
                     "yet. Replace this stub in Function.cs.",
            InvokedAtUtc: DateTime.UtcNow);

        return Task.FromResult(result);
    }
}
