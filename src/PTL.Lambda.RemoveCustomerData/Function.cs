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
        LambdaLogging.InvokeAsync(nameof(RemoveCustomerDataFunction), context, async () =>
        {
            var configuration = LambdaConfiguration.Build();
            var options = StartupChecks.RequireDatabaseOptions(configuration);

            // Proves the DB env vars are wired correctly without performing any network I/O -
            // constructing a SqlConnection does not open a socket, only .Open() would.
            var connectionFactory = new SqlConnectionFactory(configuration);
            using var connection = connectionFactory.CreateConnection();

            return new CleanupResult(
                Service: "RemoveCustomerData",
                Status: "Stub",
                Message: $"RemoveCustomerData Lambda scaffold is wired up to database '{options.Name}' on '{options.Host}', " +
                         "but the GDPR cleanup business logic has not been migrated yet. Replace this stub in Function.cs.",
                InvokedAtUtc: DateTime.UtcNow);
        });
}
