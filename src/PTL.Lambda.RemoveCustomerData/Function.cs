using System.Diagnostics.CodeAnalysis;
using Amazon.Lambda.Core;
using PTL.Lambda.Shared;

namespace PTL.Lambda.RemoveCustomerData;

/// <summary>
/// GDPR-style anonymization (UPDATE, blanks PII columns - never deletes the row) of inactive
/// customers then participants, ported from RemoveCustomerDataService.ProcessRecords
/// (proficiency-testing-2026-08-17). Runs the Customer pass, then - preserving the legacy's
/// deliberate pause - waits 300ms before running the Participant pass.
/// </summary>
public static class RemoveCustomerDataFunction
{
    private const string CleanupName = "RemoveCustomerData";
    private static readonly TimeSpan PassDelay = TimeSpan.FromMilliseconds(300);

    // Excluded from coverage: pure delegation to the tested overload below with real collaborators,
    // which would require an actual DB connection to exercise (see README's Program.cs exclusion
    // rationale for the same class of non-testable wiring).
    [ExcludeFromCodeCoverage]
    public static Task<CleanupResult> FunctionHandler(CleanupInvocationRequest? input, ILambdaContext context) =>
        FunctionHandler(
            input,
            context,
            new SqlConnectionFactory(LambdaConfiguration.Build()),
            new SqlRecordUpdateCleanupRepository("spgInactiveCustomerData", "spdInactiveCustomerData", "Customers"),
            new SqlRecordUpdateCleanupRepository("spgInactiveParticipantData", "spdInactiveParticipantData", "Participants"),
            new SqlAuditLogger());

    // Internal overload lets tests inject fakes for every collaborator that would otherwise need a
    // real SQL Server socket (see PTL.Lambda.RemoveCustomerData.Tests, InternalsVisibleTo below).
    internal static Task<CleanupResult> FunctionHandler(
        CleanupInvocationRequest? input,
        ILambdaContext context,
        IDbConnectionFactory connectionFactory,
        IBatchCleanupRepository<RecordToUpdate> customerRepository,
        IBatchCleanupRepository<RecordToUpdate> participantRepository,
        IAuditLogger auditLogger) =>
        LambdaLogging.InvokeAsync(nameof(RemoveCustomerDataFunction), context, async () =>
        {
            var request = input ?? new CleanupInvocationRequest();
            var configuration = LambdaConfiguration.Build();
            var options = StartupChecks.RequireDatabaseOptions(configuration);

            DatabaseConnectivity.VerifyConnection(connectionFactory, options);

            using var connection = connectionFactory.CreateConnection();
            connection.Open();

            var customerEngine = new BatchCleanupEngine<RecordToUpdate>(
                $"{CleanupName}-Customers", LegacyServiceType.RemoveCustomerDataService, customerRepository, auditLogger);
            var customerResult = await customerEngine.RunAsync(connection, request.BatchSize, request.DryRun, request.TestDate)
                .ConfigureAwait(false);

            await Task.Delay(PassDelay).ConfigureAwait(false);

            var participantEngine = new BatchCleanupEngine<RecordToUpdate>(
                $"{CleanupName}-Participants", LegacyServiceType.RemoveCustomerDataService, participantRepository, auditLogger);
            var participantResult = await participantEngine.RunAsync(connection, request.BatchSize, request.DryRun, request.TestDate)
                .ConfigureAwait(false);

            var combined = Combine(customerResult, participantResult);

            return new CleanupResult(
                CleanupName,
                combined.DeferredRemainingWork ? "PartiallyCompleted" : "Completed",
                BuildMessage(request, combined),
                DateTime.UtcNow);
        });

    private static CleanupRunResult Combine(CleanupRunResult customerResult, CleanupRunResult participantResult)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (category, count) in customerResult.Counts.Concat(participantResult.Counts))
        {
            counts[category] = count;
        }

        return new CleanupRunResult(
            counts,
            customerResult.TotalProcessed + participantResult.TotalProcessed,
            customerResult.Duration + participantResult.Duration,
            customerResult.DeferredRemainingWork || participantResult.DeferredRemainingWork);
    }

    private static string BuildMessage(CleanupInvocationRequest request, CleanupRunResult result)
    {
        var action = request.DryRun ? "identified" : "anonymized";
        var deferredNote = result.DeferredRemainingWork
            ? " Stopped early - the remainder is deferred to the next scheduled invocation."
            : string.Empty;

        return $"{(request.DryRun ? "[DRY RUN] " : string.Empty)}{action} {result.TotalProcessed} record(s) " +
               $"in {result.Duration.TotalSeconds:N1}s.{deferredNote}";
    }
}

