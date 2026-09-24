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
    public static Task<CleanupResult> FunctionHandler(object input, ILambdaContext context) =>
        FunctionHandler(input, context, connectionFactory: null);

    // Internal overload lets tests inject a fake IDbConnectionFactory instead of opening a real
    // SQL Server socket (see PTL.Lambda.DeleteAttachments.Tests, InternalsVisibleTo below).
    internal static Task<CleanupResult> FunctionHandler(
        object input, ILambdaContext context, IDbConnectionFactory? connectionFactory) =>
        LambdaLogging.InvokeAsync(nameof(DeleteAttachmentsFunction), context, () =>
        {
            var configuration = LambdaConfiguration.Build();
            var options = StartupChecks.RequireDatabaseOptions(configuration);

            DatabaseConnectivity.VerifyConnection(connectionFactory ?? new SqlConnectionFactory(configuration), options);

            return Task.FromResult(new CleanupResult(
                Service: "DeleteAttachments",
                Status: "Stub",
                Message: $"DeleteAttachments Lambda verified its database connection to '{options.Name}' on '{options.Host}', " +
                         "but the cleanup business logic has not been migrated yet. Replace this stub in Function.cs.",
                InvokedAtUtc: DateTime.UtcNow));
        });
}
