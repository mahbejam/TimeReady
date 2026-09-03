namespace TimeReady.Api.Services.Auditing;

/// <inheritdoc cref="IAuditRetentionMonitor" />
public sealed class AuditRetentionMonitor : IAuditRetentionMonitor
{
    private readonly Lock _gate = new();
    private AuditRetentionStatus _current = new();

    /// <inheritdoc />
    public AuditRetentionStatus Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    /// <inheritdoc />
    public void RecordSuccess(AuditRetentionResult result, DateTimeOffset completedAt)
    {
        lock (_gate)
        {
            _current = _current with
            {
                RunCount = _current.RunCount + 1,
                LastRunAtUtc = completedAt,
                LastSuccessAtUtc = completedAt,
                LastArchived = result.Archived,
                LastPurged = result.Purged,
                LastDuration = result.Duration,
                LastError = null
            };
        }
    }

    /// <inheritdoc />
    public void RecordFailure(Exception exception, DateTimeOffset failedAt)
    {
        lock (_gate)
        {
            _current = _current with
            {
                RunCount = _current.RunCount + 1,
                FailureCount = _current.FailureCount + 1,
                LastRunAtUtc = failedAt,
                LastError = exception.Message
            };
        }
    }
}
