using Amazon.Lambda.Core;
using PTL.Lambda.Shared;

namespace PTL.Lambda.RemoveCustomerData;

/// <summary>
/// Scaffold handler for the RemoveCustomerData (GDPR cleanup) Lambda. Business logic is not
/// yet migrated - see RemoveCustomerDataService.ProcessRecords in
/// proficiency-testing-2026-08-17/ProficiencyTestingRemoveCustomerDataService for the source to port.
/// </summary>
public static class RemoveCustomerDataFunction
{
    public static Task<CleanupResult> FunctionHandler(object input, ILambdaContext context) =>
        FunctionHandler(input, context, connectionFactory: null);

    // Internal overload lets tests inject a fake IDbConnectionFactory instead of opening a real
    // SQL Server socket (see PTL.Lambda.RemoveCustomerData.Tests, InternalsVisibleTo below).
    internal static Task<CleanupResult> FunctionHandler(
        object input, ILambdaContext context, IDbConnectionFactory? connectionFactory) =>
        LambdaLogging.InvokeAsync(nameof(RemoveCustomerDataFunction), context, () =>
        {
            var configuration = LambdaConfiguration.Build();
            var options = StartupChecks.RequireDatabaseOptions(configuration);

            DatabaseConnectivity.VerifyConnection(connectionFactory ?? new SqlConnectionFactory(configuration), options);

            return Task.FromResult(new CleanupResult(
                Service: "RemoveCustomerData",
                Status: "Stub",
                Message: $"RemoveCustomerData Lambda verified its database connection to '{options.Name}' on '{options.Host}', " +
                         "but the GDPR cleanup business logic has not been migrated yet. Replace this stub in Function.cs.",
                InvokedAtUtc: DateTime.UtcNow));
        });
}
