using PTL.Lambda.Shared;

namespace PTL.Lambda.DeleteConsultant;

/// <summary>Ported from ProficiencyTestingDeleteConsultantService/Poco/ConsultantToDelete.cs, extended to implement ICleanupRecord.</summary>
public sealed class ConsultantToDelete : ICleanupRecord
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime InactiveDate { get; init; }
    public Guid SsoId { get; init; }
    public ConsultantType ConsultantType { get; init; }

    public DateTime InactiveSince => InactiveDate;
    public string Category => ConsultantType.ToString();
}
