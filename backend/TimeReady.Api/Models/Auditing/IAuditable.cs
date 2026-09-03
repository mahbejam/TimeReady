namespace TimeReady.Api.Models.Auditing;

/// <summary>
/// Marks an entity whose changes are written to the audit trail. Identity tables
/// and the audit trail itself deliberately do not carry this marker.
/// </summary>
public interface IAuditable;
