using System.Diagnostics.CodeAnalysis;
using Amazon.Lambda.Core;
using PTL.Lambda.Shared;

namespace PTL.Lambda.DeleteAttachments;

/// <summary>
/// Deletes expired internal/published result attachments, ported from
/// DeleteAttachmentsService.ProcessRecords (proficiency-testing-2026-08-17).
/// </summary>
public static class DeleteAttachmentsFunction
{
    private const string CleanupName = "DeleteAttachments";

    // Excluded from coverage: pure delegation to the tested overload below with real collaborators,
    // which would require an actual DB connection to exercise (see README's Program.cs exclusion
    // rationale for the same class of non-testable wiring).
    [ExcludeFromCodeCoverage]
    public static Task<CleanupResult> FunctionHandler(CleanupInvocationRequest? input, ILambdaContext context) =>
        FunctionHandler(
            input,
            context,
            new SqlConnectionFactory(LambdaConfiguration.Build()),
            new SqlAttachmentCleanupRepository(),
            new SqlAuditLogger());

    // Internal overload lets tests inject fakes for every collaborator that would otherwise need a
    // real SQL Server socket (see PTL.Lambda.DeleteAttachments.Tests, InternalsVisibleTo below).
    internal static Task<CleanupResult> FunctionHandler(
        CleanupInvocationRequest? input,
        ILambdaContext context,
        IDbConnectionFactory connectionFactory,
        IBatchCleanupRepository<AttachmentToDelete> repository,
        IAuditLogger auditLogger) =>
        LambdaLogging.InvokeAsync(nameof(DeleteAttachmentsFunction), context, () =>
            CleanupFunctionRunner.RunAsync(
                new CleanupDescriptor(CleanupName, LegacyServiceType.DeleteAttachmentsService, "attachment", "deleted"),
                connectionFactory, repository, auditLogger, input ?? new CleanupInvocationRequest()));
}

