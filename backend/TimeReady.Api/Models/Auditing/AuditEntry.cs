namespace TimeReady.Api.Models.Auditing;

/// <summary>
/// One recorded change. Values are stored as JSON objects of column name to
/// formatted value, which keeps the table readable and independent of the shape
/// of the audited entity.
/// </summary>
public class AuditEntry : IAuditSearchRow
{
    public long Id { get; set; }

    /// <summary>CLR name of the changed entity, for example <c>Employee</c>.</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Primary key of the changed record, as text.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Create, update or delete.</summary>
    public AuditAction Action { get; set; }

    /// <summary>Identity user id, or null when the change came from the system.</summary>
    public string? UserId { get; set; }

    /// <summary>Email of the user, or <c>system</c> for startup seeding.</summary>
    public string UserName { get; set; } = "system";

    public DateTimeOffset TimestampUtc { get; set; }

    /// <summary>JSON array of the columns that changed. Null for create and delete.</summary>
    public string? ChangedColumns { get; set; }

    /// <summary>JSON object with the values before the change. Null for create.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON object with the values after the change. Null for delete.</summary>
    public string? NewValues { get; set; }

    /// <summary>Trace identifier of the request, so an entry can be found in the logs.</summary>
    public string? TraceId { get; set; }
}
