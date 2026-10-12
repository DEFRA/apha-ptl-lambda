namespace PTL.Lambda.Shared;

/// <summary>
/// Mirrors ProficiencyTestingServiceObjects.ServiceTypeEnum's exact integer values.
/// tblDeletionLog.fldService (via spInsertDeletionLog) is an INT column, not a free-text name - these
/// values must stay numerically identical to the legacy enum for audit-log continuity.
/// </summary>
public enum LegacyServiceType
{
    DeleteConsultantService = 1,
    RemoveCustomerDataService = 2,
    DeleteAttachmentsService = 3
}
