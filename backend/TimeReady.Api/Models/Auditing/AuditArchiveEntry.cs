namespace TimeReady.Api.Models.Auditing;

/// <summary>
/// An audit entry that has been moved out of the live table. The shape is
/// repeated on purpose instead of shared through inheritance: the archive is a
/// separate table with its own lifecycle, and a change to the live schema should
/// not silently rewrite historical rows.
/// </summary>
public class AuditArchiveEntry : IAuditSearchRow
{
    public long Id { get; set; }

    /// <summary>Id the entry had in the live table.</summary>
    public long OriginalId { get; set; }

    public string EntityName { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public AuditAction Action { get; set; }

    public string? UserId { get; set; }

    public string UserName { get; set; } = "system";

    /// <summary>When the change happened.</summary>
    public DateTimeOffset TimestampUtc { get; set; }

    /// <summary>When the entry was moved to the archive.</summary>
    public DateTimeOffset ArchivedAtUtc { get; set; }

    public string? ChangedColumns { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? TraceId { get; set; }
}
