using Microsoft.Extensions.Options;
using TimeReady.Api.Configuration;
using TimeReady.Api.Services.Auditing;

namespace TimeReady.Api.Infrastructure;

/// <summary>
/// Runs the retention policy on a timer. The loop is deliberately defensive: a
/// failing run is logged and recorded, and the service keeps going, because a
/// housekeeping job must never take the application down with it.
/// </summary>
public sealed class AuditRetentionBackgroundService(
    IServiceScopeFactory scopeFactory,
    IAuditRetentionMonitor monitor,
    IOptions<AuditRetentionOptions> options,
    TimeProvider timeProvider,
    ILogger<AuditRetentionBackgroundService> logger) : BackgroundService
{
    private readonly AuditRetentionOptions _options = options.Value;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Audit retention job is disabled by configuration");
            return;
        }

        logger.LogInformation(
            "Audit retention job starts in {Delay} and then runs every {Interval}; "
            + "archiving after {RetentionDays} days, purging {PurgeState}",
            _options.InitialDelay,
            _options.Interval,
            _options.RetentionDays,
            _options.PurgeEnabled ? $"after {_options.ArchiveRetentionDays} days" : "disabled");

        try
        {
            await Task.Delay(_options.InitialDelay, timeProvider, stoppingToken);

            using var timer = new PeriodicTimer(_options.Interval, timeProvider);

            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Audit retention job stopped");
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();

            var service = scope.ServiceProvider.GetRequiredService<IAuditRetentionService>();
            var result = await service.RunAsync(cancellationToken);

            monitor.RecordSuccess(result, timeProvider.GetUtcNow());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            monitor.RecordFailure(exception, timeProvider.GetUtcNow());

            logger.LogError(exception, "Audit retention run failed; the job will try again on the next tick");
        }
    }
}
