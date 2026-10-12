using PTL.Lambda.Shared;

namespace PTL.Lambda.RemoveCustomerData;

/// <summary>
/// Ported from ProficiencyTestingRemoveCustomerDataService/Poco/RecordToUpdate.cs, extended to
/// implement ICleanupRecord. Shared by both the Customer and Participant passes - each pass's
/// stored procedures only ever return one RecordType, so Category is always consistent within a run.
/// </summary>
public sealed class RecordToUpdate : ICleanupRecord
{
    public Guid Id { get; init; }
    public DateTime InactiveDate { get; init; }
    public RecordType Type { get; init; }
    public string Identifier { get; init; } = string.Empty;

    public DateTime InactiveSince => InactiveDate;
    public string Category => Type.ToString();
}
