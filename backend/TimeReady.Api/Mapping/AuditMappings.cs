using System.Text.Json;
using TimeReady.Api.Configuration;
using TimeReady.Api.Dtos.Auditing;
using TimeReady.Api.Models.Auditing;
using TimeReady.Api.Services.Auditing;

namespace TimeReady.Api.Mapping;

/// <summary>Turns stored audit rows into the API contract.</summary>
public static class AuditMappings
{
    /// <summary>Maps one entry, parsing the stored JSON back into objects.</summary>
    public static AuditEntryDto ToDto(this AuditEntry entry) => new(
        entry.Id,
        entry.EntityName,
        entry.EntityId,
        entry.Action.ToString(),
        entry.UserId,
        entry.UserName,
        entry.TimestampUtc,
        Deserialize<List<string>>(entry.ChangedColumns),
        Deserialize<Dictionary<string, string?>>(entry.OldValues),
        Deserialize<Dictionary<string, string?>>(entry.NewValues),
        entry.TraceId);

    /// <summary>Maps an archived entry, keeping the id it had in the live table.</summary>
    public static ArchivedAuditEntryDto ToDto(this AuditArchiveEntry entry) => new(
        new AuditEntryDto(
            entry.OriginalId,
            entry.EntityName,
            entry.EntityId,
            entry.Action.ToString(),
            entry.UserId,
            entry.UserName,
            entry.TimestampUtc,
            Deserialize<List<string>>(entry.ChangedColumns),
            Deserialize<Dictionary<string, string?>>(entry.OldValues),
            Deserialize<Dictionary<string, string?>>(entry.NewValues),
            entry.TraceId),
        entry.ArchivedAtUtc);

    /// <summary>Maps the configured policy for the status endpoint.</summary>
    public static AuditRetentionPolicyDto ToDto(this AuditRetentionOptions options) => new(
        options.Enabled,
        options.RetentionDays,
        options.PurgeEnabled,
        options.ArchiveRetentionDays,
        options.IntervalHours);

    /// <summary>Maps the job status for the status endpoint.</summary>
    public static AuditRetentionStatusDto ToDto(this AuditRetentionStatus status) => new(
        status.RunCount,
        status.FailureCount,
        status.LastRunAtUtc,
        status.LastSuccessAtUtc,
        status.LastArchived,
        status.LastPurged,
        status.LastDuration?.TotalMilliseconds,
        status.LastError);

    /// <summary>Maps the outcome of a manually triggered run.</summary>
    public static AuditRetentionRunDto ToDto(this AuditRetentionResult result) => new(
        result.Archived,
        result.Purged,
        result.ArchiveCutoffUtc,
        result.PurgeCutoffUtc,
        result.Duration.TotalMilliseconds,
        result.Skipped);

    private static T? Deserialize<T>(string? json) where T : class =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<T>(json);
}
