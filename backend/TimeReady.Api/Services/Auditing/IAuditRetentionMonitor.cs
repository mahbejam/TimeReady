namespace TimeReady.Api.Services.Auditing;

/// <summary>Everything an operator wants to know about the retention job.</summary>
/// <param name="RunCount">Completed runs since the application started.</param>
/// <param name="FailureCount">Runs that ended in an exception.</param>
/// <param name="LastRunAtUtc">When the last run finished, successfully or not.</param>
/// <param name="LastSuccessAtUtc">When the last successful run finished.</param>
/// <param name="LastArchived">Entries archived by the last successful run.</param>
/// <param name="LastPurged">Entries purged by the last successful run.</param>
/// <param name="LastDuration">Duration of the last successful run.</param>
/// <param name="LastError">Message of the last failure, or null.</param>
public record AuditRetentionStatus(
    int RunCount = 0,
    int FailureCount = 0,
    DateTimeOffset? LastRunAtUtc = null,
    DateTimeOffset? LastSuccessAtUtc = null,
    int LastArchived = 0,
    int LastPurged = 0,
    TimeSpan? LastDuration = null,
    string? LastError = null);

/// <summary>
/// Remembers how the retention job is doing. It is a singleton so the status
/// survives between runs, and is read by the status endpoint and the health check.
/// </summary>
public interface IAuditRetentionMonitor
{
    /// <summary>The current status.</summary>
    AuditRetentionStatus Current { get; }

    /// <summary>Records a completed run.</summary>
    void RecordSuccess(AuditRetentionResult result, DateTimeOffset completedAt);

    /// <summary>Records a run that threw.</summary>
    void RecordFailure(Exception exception, DateTimeOffset failedAt);
}
