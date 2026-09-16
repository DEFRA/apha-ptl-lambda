using Amazon.Lambda.Core;
using PTL.Lambda.Shared;

namespace PTL.Lambda.DeleteAttachments;

/// <summary>
/// Scaffold handler for the DeleteAttachments cleanup Lambda. Business logic is not yet
/// migrated - see DeleteAttachmentsService.ProcessRecords in
/// proficiency-testing-2026-08-17/ProficiencyTestingDeleteAttachmentsService for the source to port.
/// </summary>
public static class DeleteAttachmentsFunction
{
    public static Task<CleanupResult> FunctionHandler(object input, ILambdaContext context)
    {
        context.Logger.LogInformation("DeleteAttachments Lambda invoked.");

        var configuration = LambdaConfiguration.Build();
        var options = StartupChecks.RequireDatabaseOptions(configuration);

        // Proves the DB env vars are wired correctly without performing any network I/O -
        // constructing a SqlConnection does not open a socket, only .Open() would.
        var connectionFactory = new SqlConnectionFactory(configuration);
        using var connection = connectionFactory.CreateConnection();

        var result = new CleanupResult(
            Service: "DeleteAttachments",
            Status: "Stub",
            Message: $"DeleteAttachments Lambda scaffold is wired up to database '{options.Name}' on '{options.Host}', " +
                     "but the cleanup business logic has not been migrated yet. Replace this stub in Function.cs.",
            InvokedAtUtc: DateTime.UtcNow);

        return Task.FromResult(result);
    }
}
