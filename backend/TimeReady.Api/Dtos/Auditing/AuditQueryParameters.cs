using TimeReady.Api.Models.Auditing;

namespace TimeReady.Api.Dtos.Auditing;

/// <summary>Filter, sort and paging options for the audit trail.</summary>
public class AuditQueryParameters
{
    /// <summary>Default number of entries per page.</summary>
    public const int DefaultPageSize = 25;

    /// <summary>Largest page a client may ask for.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Entity type, for example <c>Employee</c>.</summary>
    public string? EntityName { get; set; }

    /// <summary>Primary key of a single record.</summary>
    public string? EntityId { get; set; }

    /// <summary>Only create, update or delete entries.</summary>
    public AuditAction? Action { get; set; }

    /// <summary>Matches part of the user name or the user id.</summary>
    public string? User { get; set; }

    /// <summary>Only entries at or after this moment.</summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>Only entries at or before this moment.</summary>
    public DateTimeOffset? To { get; set; }

    /// <summary>One-based page number.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Entries per page, at most <see cref="MaxPageSize"/>.</summary>
    public int PageSize { get; set; } = DefaultPageSize;
}
