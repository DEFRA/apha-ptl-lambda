using System.Diagnostics.CodeAnalysis;
using Amazon.Lambda.Core;
using PTL.Lambda.Shared;

namespace PTL.Lambda.DeleteConsultant;

/// <summary>
/// Removes inactive internal/external test consultant accounts, ported from
/// DeleteConsultantService.ProcessRecords (proficiency-testing-2026-08-17).
/// </summary>
public static class DeleteConsultantFunction
{
    private const string CleanupName = "DeleteConsultant";

    // Excluded from coverage: pure delegation to the tested overload below with real collaborators,
    // which would require an actual DB connection to exercise (see README's Program.cs exclusion
    // rationale for the same class of non-testable wiring).
    [ExcludeFromCodeCoverage]
    public static Task<CleanupResult> FunctionHandler(CleanupInvocationRequest? input, ILambdaContext context) =>
        FunctionHandler(
            input,
            context,
            new SqlConnectionFactory(LambdaConfiguration.Build()),
            new SqlConsultantCleanupRepository(),
            new SqlAuditLogger());

    // Internal overload lets tests inject fakes for every collaborator that would otherwise need a
    // real SQL Server socket (see PTL.Lambda.DeleteConsultant.Tests, InternalsVisibleTo below).
    internal static Task<CleanupResult> FunctionHandler(
        CleanupInvocationRequest? input,
        ILambdaContext context,
        IDbConnectionFactory connectionFactory,
        IBatchCleanupRepository<ConsultantToDelete> repository,
        IAuditLogger auditLogger) =>
        LambdaLogging.InvokeAsync(nameof(DeleteConsultantFunction), context, () =>
            CleanupFunctionRunner.RunAsync(
                CleanupName, LegacyServiceType.DeleteConsultantService, "consultant", "deleted",
                connectionFactory, repository, auditLogger, input ?? new CleanupInvocationRequest()));
}

