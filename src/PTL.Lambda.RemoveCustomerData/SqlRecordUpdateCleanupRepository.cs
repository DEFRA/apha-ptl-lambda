using System.Data;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using PTL.Lambda.Shared;

namespace PTL.Lambda.RemoveCustomerData;

/// <summary>
/// Real stored-procedure calls ported from RemoveCustomerDataService.ProcessRecords
/// (proficiency-testing-2026-08-17/ProficiencyTestingRemoveCustomerDataService). This is an
/// anonymization (UPDATE, blanks PII columns), not a deletion - the row itself is kept.
/// One instance is configured per pass (Customer: spgInactiveCustomerData/spdInactiveCustomerData,
/// Participant: spgInactiveParticipantData/spdInactiveParticipantData) since both passes share the
/// identical single-@Ids/@UpdatedCount shape - only the proc names and result category differ,
/// so a single configurable class replaces what would otherwise be two near-identical copies.
/// </summary>
public sealed class SqlRecordUpdateCleanupRepository(string getProcedureName, string applyProcedureName, string category)
    : IBatchCleanupRepository<RecordToUpdate>
{
    [ExcludeFromCodeCoverage] // Real Dapper/stored-procedure call - needs a live SQL Server to exercise.
    public async Task<IReadOnlyList<RecordToUpdate>> GetBatchAsync(
        IDbConnection connection, int batchSize, DateTime referenceDate, IReadOnlySet<Guid> excludedIds,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            getProcedureName,
            new
            {
                BatchSize = batchSize,
                ReferenceDate = referenceDate,
                ProcessedIds = GuidTableValuedParameter.Create(excludedIds)
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<RecordToUpdate>(command).ConfigureAwait(false);
        return rows.ToList();
    }

    [ExcludeFromCodeCoverage] // Real Dapper/stored-procedure call - needs a live SQL Server to exercise.
    public async Task<IReadOnlyDictionary<string, int>> ApplyBatchAsync(
        IDbConnection connection, IDbTransaction transaction, IReadOnlyList<RecordToUpdate> batch,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Ids", GuidTableValuedParameter.Create(ExtractIds(batch)));
        parameters.Add("UpdatedCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var command = new CommandDefinition(
            applyProcedureName, parameters, transaction, commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command).ConfigureAwait(false);

        return new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [category] = parameters.Get<int>("UpdatedCount")
        };
    }

    /// <summary>Pure extraction, kept separate from the Dapper call above so it can be unit tested directly.</summary>
    internal static IReadOnlyList<Guid> ExtractIds(IReadOnlyList<RecordToUpdate> batch) =>
        batch.Select(r => r.Id).ToList();
}
