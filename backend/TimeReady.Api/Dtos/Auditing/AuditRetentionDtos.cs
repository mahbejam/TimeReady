namespace TimeReady.Api.Dtos.Auditing;

/// <summary>The configured retention policy, as reported to an administrator.</summary>
/// <param name="Enabled">Whether the background job runs.</param>
/// <param name="RetentionDays">Age at which an entry is archived.</param>
/// <param name="PurgeEnabled">Whether archived entries are deleted permanently.</param>
/// <param name="ArchiveRetentionDays">Age at which an archived entry is purged.</param>
/// <param name="IntervalHours">Hours between two runs.</param>
public record AuditRetentionPolicyDto(
    bool Enabled,
    int RetentionDays,
    bool PurgeEnabled,
    int ArchiveRetentionDays,
    int IntervalHours);

/// <summary>How the retention job is doing.</summary>
/// <param name="RunCount">Completed runs since startup.</param>
/// <param name="FailureCount">Runs that failed.</param>
/// <param name="LastRunAtUtc">End of the last run.</param>
/// <param name="LastSuccessAtUtc">End of the last successful run.</param>
/// <param name="LastArchived">Entries archived by the last successful run.</param>
/// <param name="LastPurged">Entries purged by the last successful run.</param>
/// <param name="LastDurationMs">Duration of the last successful run in milliseconds.</param>
/// <param name="LastError">Message of the last failure.</param>
public record AuditRetentionStatusDto(
    int RunCount,
    int FailureCount,
    DateTimeOffset? LastRunAtUtc,
    DateTimeOffset? LastSuccessAtUtc,
    int LastArchived,
    int LastPurged,
    double? LastDurationMs,
    string? LastError);

/// <summary>Policy and status together, as returned by the status endpoint.</summary>
/// <param name="Policy">The configured policy.</param>
/// <param name="Status">The current status of the job.</param>
/// <param name="LiveEntryCount">Entries currently in the live table.</param>
/// <param name="ArchivedEntryCount">Entries currently in the archive.</param>
public record AuditRetentionOverviewDto(
    AuditRetentionPolicyDto Policy,
    AuditRetentionStatusDto Status,
    int LiveEntryCount,
    int ArchivedEntryCount);

/// <summary>Result of a manually triggered run.</summary>
/// <param name="Archived">Entries moved to the archive.</param>
/// <param name="Purged">Archived entries deleted permanently.</param>
/// <param name="ArchiveCutoffUtc">Entries older than this were archived.</param>
/// <param name="PurgeCutoffUtc">Archived entries older than this were purged.</param>
/// <param name="DurationMs">How long the run took, in milliseconds.</param>
/// <param name="Skipped">True when the policy is disabled and nothing was done.</param>
public record AuditRetentionRunDto(
    int Archived,
    int Purged,
    DateTimeOffset ArchiveCutoffUtc,
    DateTimeOffset? PurgeCutoffUtc,
    double DurationMs,
    bool Skipped);

/// <summary>An archived audit entry: the original entry plus when it was archived.</summary>
/// <param name="Entry">The recorded change.</param>
/// <param name="ArchivedAtUtc">When it was moved to the archive.</param>
public record ArchivedAuditEntryDto(AuditEntryDto Entry, DateTimeOffset ArchivedAtUtc);
