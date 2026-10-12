using System.Data;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using PTL.Lambda.Shared;

namespace PTL.Lambda.DeleteAttachments;

/// <summary>
/// Real stored-procedure calls ported from DeleteAttachmentsService.ProcessRecords
/// (proficiency-testing-2026-08-17/ProficiencyTestingDeleteAttachmentsService). Proc names,
/// parameter names and the two attachment categories are preserved verbatim.
/// </summary>
public sealed class SqlAttachmentCleanupRepository : IBatchCleanupRepository<AttachmentToDelete>
{
    [ExcludeFromCodeCoverage] // Real Dapper/stored-procedure call - needs a live SQL Server to exercise.
    public async Task<IReadOnlyList<AttachmentToDelete>> GetBatchAsync(
        IDbConnection connection, int batchSize, DateTime referenceDate, IReadOnlySet<Guid> excludedIds,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            "spgAttachmentsToDelete",
            new
            {
                BatchSize = batchSize,
                ReferenceDate = referenceDate,
                ProcessedIds = GuidTableValuedParameter.Create(excludedIds)
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<AttachmentToDelete>(command).ConfigureAwait(false);
        return rows.ToList();
    }

    [ExcludeFromCodeCoverage] // Real Dapper/stored-procedure call - needs a live SQL Server to exercise.
    public async Task<IReadOnlyDictionary<string, int>> ApplyBatchAsync(
        IDbConnection connection, IDbTransaction transaction, IReadOnlyList<AttachmentToDelete> batch,
        CancellationToken cancellationToken = default)
    {
        var (internalIds, publishedIds) = SplitByAttachmentType(batch);

        var parameters = new DynamicParameters();
        parameters.Add("InternalAttachmentIds", GuidTableValuedParameter.Create(internalIds));
        parameters.Add("PublishedAttachmentIds", GuidTableValuedParameter.Create(publishedIds));
        parameters.Add("InternalDeletedCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("PublishedDeletedCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var command = new CommandDefinition(
            "spdAttachments", parameters, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command).ConfigureAwait(false);

        return new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [nameof(AttachmentType.InternalAttachment)] = parameters.Get<int>("InternalDeletedCount"),
            [nameof(AttachmentType.PublishedAttachment)] = parameters.Get<int>("PublishedDeletedCount")
        };
    }

    /// <summary>Pure categorisation logic, kept separate from the Dapper call above so it can be unit tested directly.</summary>
    internal static (IReadOnlyList<Guid> InternalIds, IReadOnlyList<Guid> PublishedIds) SplitByAttachmentType(
        IReadOnlyList<AttachmentToDelete> batch) =>
        (
            batch.Where(r => r.AttachmentType == AttachmentType.InternalAttachment).Select(r => r.Id).ToList(),
            batch.Where(r => r.AttachmentType == AttachmentType.PublishedAttachment).Select(r => r.Id).ToList()
        );
}
