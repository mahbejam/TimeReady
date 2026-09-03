namespace TimeReady.Api.Models.Auditing;

/// <summary>What happened to an entity.</summary>
public enum AuditAction
{
    /// <summary>The record was created.</summary>
    Created,

    /// <summary>One or more properties changed.</summary>
    Updated,

    /// <summary>The record was deleted.</summary>
    Deleted
}
