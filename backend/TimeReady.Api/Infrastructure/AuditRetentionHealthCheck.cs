using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TimeReady.Api.Configuration;
using TimeReady.Api.Services.Auditing;

namespace TimeReady.Api.Infrastructure;

/// <summary>
/// Reports the retention job as degraded when its last run failed or when it has
/// not run for more than two intervals. A job that quietly stops running is the
/// failure mode worth catching here.
/// </summary>
public sealed class AuditRetentionHealthCheck(
    IAuditRetentionMonitor monitor,
    IOptions<AuditRetentionOptions> options,
    TimeProvider timeProvider) : IHealthCheck
{
    private readonly AuditRetentionOptions _options = options.Value;

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Audit retention is disabled by configuration."));
        }

        var status = monitor.Current;

        var data = new Dictionary<string, object>
        {
            ["runCount"] = status.RunCount,
            ["failureCount"] = status.FailureCount,
            ["lastArchived"] = status.LastArchived,
            ["lastPurged"] = status.LastPurged
        };

        if (status.LastError is not null)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "The last audit retention run failed.",
                data: data));
        }

        if (status.LastSuccessAtUtc is null)
        {
            return Task.FromResult(HealthCheckResult.Healthy(
                "Audit retention has not run yet.",
                data));
        }

        var overdueAfter = _options.Interval * 2;
        var sinceLastRun = timeProvider.GetUtcNow() - status.LastSuccessAtUtc.Value;

        return Task.FromResult(sinceLastRun > overdueAfter
            ? HealthCheckResult.Degraded(
                $"The last successful audit retention run was {sinceLastRun:g} ago.",
                data: data)
            : HealthCheckResult.Healthy(
                $"Last audit retention run {sinceLastRun:g} ago.",
                data));
    }
}
