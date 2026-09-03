using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TimeReady.Api.Configuration;
using TimeReady.Api.Data;
using TimeReady.Api.Models.Auditing;

namespace TimeReady.Api.Services.Auditing;

/// <summary>
/// <inheritdoc cref="IAuditRetentionService" />
/// <para>
/// This is a data maintenance job, so it works against the context directly
/// rather than through the read repositories: it moves rows in batches and each
/// batch is committed on its own. A run that is cancelled half way therefore
/// leaves a consistent database and simply continues next time.
/// </para>
/// </summary>
public sealed class AuditRetentionService(
    AppDbContext context,
    IOptions<AuditRetentionOptions> options,
    TimeProvider timeProvider,
    AuditRetentionRunGate runGate,
    ILogger<AuditRetentionService> logger) : IAuditRetentionService
{
    private readonly AuditRetentionOptions _options = options.Value;

    /// <inheritdoc />
    public Task<AuditRetentionResult> RunAsync(CancellationToken cancellationToken) =>
        runGate.RunExclusiveAsync(RunCoreAsync, cancellationToken);

    private async Task<AuditRetentionResult> RunCoreAsync(CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetUtcNow();
        var stopwatch = Stopwatch.StartNew();

        var archiveCutoff = startedAt.AddDays(-_options.RetentionDays);
        var purgeCutoff = _options.PurgeEnabled
            ? startedAt.AddDays(-_options.ArchiveRetentionDays)
            : (DateTimeOffset?)null;

        if (!_options.Enabled)
        {
            logger.LogDebug("Audit retention is disabled; nothing to do");

            return new AuditRetentionResult(0, 0, archiveCutoff, purgeCutoff, stopwatch.Elapsed, Skipped: true);
        }

        var archived = await ArchiveAsync(archiveCutoff, startedAt, cancellationToken);
        var purged = purgeCutoff is null ? 0 : await PurgeAsync(purgeCutoff.Value, cancellationToken);

        stopwatch.Stop();

        if (archived > 0 || purged > 0)
        {
            logger.LogInformation(
                "Audit retention archived {Archived} and purged {Purged} entries in {ElapsedMs} ms "
                + "(archive cutoff {ArchiveCutoff:u})",
                archived,
                purged,
                stopwatch.ElapsedMilliseconds,
                archiveCutoff);
        }
        else
        {
            logger.LogDebug("Audit retention found nothing older than {ArchiveCutoff:u}", archiveCutoff);
        }

        return new AuditRetentionResult(archived, purged, archiveCutoff, purgeCutoff, stopwatch.Elapsed);
    }

    private async Task<int> ArchiveAsync(
        DateTimeOffset cutoff,
        DateTimeOffset archivedAt,
        CancellationToken cancellationToken)
    {
        var archived = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var batch = await context.AuditEntries
                .Where(entry => entry.TimestampUtc < cutoff)
                .OrderBy(entry => entry.Id)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0)
            {
                break;
            }

            context.AuditArchiveEntries.AddRange(batch.Select(entry => ToArchiveEntry(entry, archivedAt)));
            context.AuditEntries.RemoveRange(batch);

            await context.SaveChangesAsync(cancellationToken);

            // Detach so a long catch-up run does not keep every archived row in memory.
            context.ChangeTracker.Clear();

            archived += batch.Count;

            logger.LogDebug("Archived a batch of {Count} audit entries", batch.Count);
        }

        return archived;
    }

    private async Task<int> PurgeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        var purged = await context.AuditArchiveEntries
            .Where(entry => entry.TimestampUtc < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (purged > 0)
        {
            // Permanent deletion is worth a warning, not an information line:
            // it is the only place in the system where history disappears.
            logger.LogWarning(
                "Purged {Purged} archived audit entries older than {PurgeCutoff:u}",
                purged,
                cutoff);
        }

        return purged;
    }

    private static AuditArchiveEntry ToArchiveEntry(AuditEntry entry, DateTimeOffset archivedAt) => new()
    {
        OriginalId = entry.Id,
        EntityName = entry.EntityName,
        EntityId = entry.EntityId,
        Action = entry.Action,
        UserId = entry.UserId,
        UserName = entry.UserName,
        TimestampUtc = entry.TimestampUtc,
        ArchivedAtUtc = archivedAt,
        ChangedColumns = entry.ChangedColumns,
        OldValues = entry.OldValues,
        NewValues = entry.NewValues,
        TraceId = entry.TraceId
    };
}
