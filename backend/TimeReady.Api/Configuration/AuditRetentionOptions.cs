using System.ComponentModel.DataAnnotations;

namespace TimeReady.Api.Configuration;

/// <summary>
/// How long audit entries stay in the live table, how long the archive keeps
/// them afterwards, and how often the background job runs.
/// </summary>
public class AuditRetentionOptions : IValidatableObject
{
    /// <summary>Configuration section that holds these values.</summary>
    public const string SectionName = "AuditRetention";

    /// <summary>Turns the background job off without removing the configuration.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Entries older than this are moved to the archive.</summary>
    [Range(1, 3650)]
    public int RetentionDays { get; set; } = 90;

    /// <summary>
    /// Permanent deletion of archived entries. Off by default: archiving is
    /// reversible, purging is not.
    /// </summary>
    public bool PurgeEnabled { get; set; }

    /// <summary>
    /// Total age at which an archived entry is deleted, counted from when the
    /// change happened. Must not be shorter than <see cref="RetentionDays"/>.
    /// </summary>
    [Range(1, 7300)]
    public int ArchiveRetentionDays { get; set; } = 730;

    /// <summary>Hours between two runs of the background job.</summary>
    [Range(1, 168)]
    public int IntervalHours { get; set; } = 24;

    /// <summary>Delay before the first run, so startup is not competing with it.</summary>
    [Range(0, 3600)]
    public int InitialDelaySeconds { get; set; } = 30;

    /// <summary>Entries moved per database round trip.</summary>
    [Range(10, 10_000)]
    public int BatchSize { get; set; } = 500;

    /// <summary>Interval between runs.</summary>
    public TimeSpan Interval => TimeSpan.FromHours(IntervalHours);

    /// <summary>Delay before the first run.</summary>
    public TimeSpan InitialDelay => TimeSpan.FromSeconds(InitialDelaySeconds);

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PurgeEnabled && ArchiveRetentionDays < RetentionDays)
        {
            yield return new ValidationResult(
                "ArchiveRetentionDays must not be shorter than RetentionDays, otherwise entries would be "
                + "purged before they are archived.",
                [nameof(ArchiveRetentionDays)]);
        }
    }
}
