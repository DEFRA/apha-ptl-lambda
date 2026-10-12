namespace PTL.Lambda.Shared;

/// <summary>
/// Runs a BatchCleanupEngine end-to-end for a single-pass cleanup Lambda and maps its
/// CleanupRunResult into the Lambda's CleanupResult shape - the tail that was identical across
/// DeleteAttachmentsFunction and DeleteConsultantFunction (only the generic record type, cleanup
/// name, and result wording differ per function). RemoveCustomerDataFunction runs two passes and
/// combines them itself, so it does not use this helper.
/// </summary>
public static class CleanupFunctionRunner
{
    public static async Task<CleanupResult> RunAsync<TRecord>(
        string cleanupName,
        string resultNoun,
        string actionVerb,
        IDbConnectionFactory connectionFactory,
        IBatchCleanupRepository<TRecord> repository,
        IAuditLogger auditLogger,
        CleanupInvocationRequest request)
        where TRecord : ICleanupRecord
    {
        var configuration = LambdaConfiguration.Build();
        var options = StartupChecks.RequireDatabaseOptions(configuration);

        DatabaseConnectivity.VerifyConnection(connectionFactory, options);

        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        var engine = new BatchCleanupEngine<TRecord>(cleanupName, repository, auditLogger);
        var result = await engine.RunAsync(connection, request.BatchSize, request.DryRun, request.TestDate)
            .ConfigureAwait(false);

        return new CleanupResult(
            cleanupName,
            result.DeferredRemainingWork ? "PartiallyCompleted" : "Completed",
            BuildMessage(request, result, resultNoun, actionVerb),
            DateTime.UtcNow);
    }

    private static string BuildMessage(CleanupInvocationRequest request, CleanupRunResult result, string resultNoun, string actionVerb)
    {
        var action = request.DryRun ? "identified" : actionVerb;
        var deferredNote = result.DeferredRemainingWork
            ? " Stopped early - the remainder is deferred to the next scheduled invocation."
            : string.Empty;

        return $"{(request.DryRun ? "[DRY RUN] " : string.Empty)}{action} {result.TotalProcessed} {resultNoun}(s) " +
               $"in {result.Duration.TotalSeconds:N1}s.{deferredNote}";
    }
}
