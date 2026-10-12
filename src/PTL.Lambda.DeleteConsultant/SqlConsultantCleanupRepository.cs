using System.Data;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using PTL.Lambda.Shared;

namespace PTL.Lambda.DeleteConsultant;

/// <summary>
/// Real stored-procedure calls ported from DeleteConsultantService.ProcessRecords
/// (proficiency-testing-2026-08-17/ProficiencyTestingDeleteConsultantService). Proc names,
/// parameter names and the two consultant categories are preserved verbatim. Proc name confirmed
/// against ProficiencyTestingDatabase/Update Scripts/20250219_1-remove-customer-data-updates.sql -
/// that script (the latest) explicitly drops the older spdDeleteInactiveTestConsultants name before
/// creating the current spdInactiveTestConsultants, so the legacy C# code's literal name is correct.
/// </summary>
public sealed class SqlConsultantCleanupRepository : IBatchCleanupRepository<ConsultantToDelete>
{
    [ExcludeFromCodeCoverage] // Real Dapper/stored-procedure call - needs a live SQL Server to exercise.
    public async Task<IReadOnlyList<ConsultantToDelete>> GetBatchAsync(
        IDbConnection connection, int batchSize, DateTime referenceDate, IReadOnlySet<Guid> excludedIds,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            "spgInactiveTestConsultants",
            new
            {
                BatchSize = batchSize,
                ReferenceDate = referenceDate,
                ProcessedIds = GuidTableValuedParameter.Create(excludedIds)
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<ConsultantToDelete>(command).ConfigureAwait(false);
        return rows.ToList();
    }

    [ExcludeFromCodeCoverage] // Real Dapper/stored-procedure call - needs a live SQL Server to exercise.
    public async Task<IReadOnlyDictionary<string, int>> ApplyBatchAsync(
        IDbConnection connection, IDbTransaction transaction, IReadOnlyList<ConsultantToDelete> batch,
        CancellationToken cancellationToken = default)
    {
        var (internalIds, externalIds) = SplitByConsultantType(batch);

        var parameters = new DynamicParameters();
        parameters.Add("InternalConsultantIds", GuidTableValuedParameter.Create(internalIds));
        parameters.Add("ExternalConsultantIds", GuidTableValuedParameter.Create(externalIds));
        parameters.Add("InternalDeletedCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("ExternalDeletedCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var command = new CommandDefinition(
            "spdInactiveTestConsultants", parameters, transaction, commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command).ConfigureAwait(false);

        return new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [nameof(ConsultantType.Internal)] = parameters.Get<int>("InternalDeletedCount"),
            [nameof(ConsultantType.External)] = parameters.Get<int>("ExternalDeletedCount")
        };
    }

    /// <summary>Pure categorisation logic, kept separate from the Dapper call above so it can be unit tested directly.</summary>
    internal static (IReadOnlyList<Guid> InternalIds, IReadOnlyList<Guid> ExternalIds) SplitByConsultantType(
        IReadOnlyList<ConsultantToDelete> batch) =>
        (
            batch.Where(r => r.ConsultantType == ConsultantType.Internal).Select(r => r.Id).ToList(),
            batch.Where(r => r.ConsultantType == ConsultantType.External).Select(r => r.Id).ToList()
        );
}
