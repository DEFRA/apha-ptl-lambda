using PTL.Lambda.Shared;

namespace PTL.Lambda.DeleteAttachments;

/// <summary>Ported from ProficiencyTestingDeleteAttachmentsService/Poco/AttachmentToDelete.cs, extended to implement ICleanupRecord.</summary>
public sealed class AttachmentToDelete : ICleanupRecord
{
    public Guid Id { get; init; }
    public string Filename { get; init; } = string.Empty;
    public DateTime ResultsDate { get; init; }
    public AttachmentType AttachmentType { get; init; }
    public string ContentType { get; init; } = string.Empty;

    public DateTime InactiveSince => ResultsDate;
    public string Category => AttachmentType.ToString();
}
