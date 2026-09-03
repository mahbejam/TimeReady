namespace TimeReady.Api.Services.Auditing;

/// <summary>What one retention run did.</summary>
/// <param name="Archived">Entries moved to the archive.</param>
/// <param name="Purged">Archived entries deleted permanently.</param>
/// <param name="ArchiveCutoffUtc">Entries older than this were archived.</param>
/// <param name="PurgeCutoffUtc">Archived entries older than this were purged, when purging is on.</param>
/// <param name="Duration">How long the run took.</param>
/// <param name="Skipped">True when the policy is disabled and nothing was done.</param>
public record AuditRetentionResult(
    int Archived,
    int Purged,
    DateTimeOffset ArchiveCutoffUtc,
    DateTimeOffset? PurgeCutoffUtc,
    TimeSpan Duration,
    bool Skipped = false);

/// <summary>
/// Moves audit entries past their retention period into the archive, and – only
/// when explicitly enabled – deletes archived entries that are past the archive
/// retention period.
/// </summary>
public interface IAuditRetentionService
{
    /// <summary>Runs one archive and purge pass.</summary>
    /// <param name="cancellationToken">Cancellation token; a cancelled run keeps what it already committed.</param>
    Task<AuditRetentionResult> RunAsync(CancellationToken cancellationToken);
}
