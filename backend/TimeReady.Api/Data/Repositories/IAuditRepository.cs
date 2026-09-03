using TimeReady.Api.Dtos;
using TimeReady.Api.Dtos.Auditing;
using TimeReady.Api.Models.Auditing;

namespace TimeReady.Api.Data.Repositories;

/// <summary>Read access to the audit trail. Entries are never modified.</summary>
public interface IAuditRepository
{
    /// <summary>Returns one page of entries matching the filter, newest first.</summary>
    Task<PagedResult<AuditEntry>> SearchAsync(
        AuditQueryParameters parameters,
        CancellationToken cancellationToken);

    /// <summary>Returns a single entry, or null when the id is unknown.</summary>
    Task<AuditEntry?> FindAsync(long id, CancellationToken cancellationToken);

    /// <summary>Searches the archive with the same filters, newest first.</summary>
    Task<PagedResult<AuditArchiveEntry>> SearchArchiveAsync(
        AuditQueryParameters parameters,
        CancellationToken cancellationToken);

    /// <summary>Number of entries in the live table.</summary>
    Task<int> CountLiveAsync(CancellationToken cancellationToken);

    /// <summary>Number of entries in the archive.</summary>
    Task<int> CountArchivedAsync(CancellationToken cancellationToken);
}
