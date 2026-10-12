namespace PTL.Lambda.Shared;

/// <summary>Minimal shape <see cref="BatchCleanupEngine{TRecord}"/> needs from a paged cleanup record.</summary>
public interface ICleanupRecord
{
    Guid Id { get; }
    DateTime InactiveSince { get; }
    string Category { get; }
}
