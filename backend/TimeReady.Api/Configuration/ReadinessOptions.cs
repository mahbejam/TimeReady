using System.ComponentModel.DataAnnotations;

namespace TimeReady.Api.Configuration;

/// <summary>
/// Thresholds used by the rule engine. Kept in configuration so HR policies can
/// change without a code change.
/// </summary>
public class ReadinessOptions
{
    /// <summary>Configuration section that holds these values.</summary>
    public const string SectionName = "Readiness";

    /// <summary>Balance at or below this value blocks readiness.</summary>
    [Range(-500, 0)]
    public decimal CriticalNegativeBalanceHours { get; set; } = -20m;

    /// <summary>Balance at or below this value produces a warning.</summary>
    [Range(-500, 0)]
    public decimal WarningNegativeBalanceHours { get; set; } = -8m;

    /// <summary>A vacation starting within this many days counts as imminent.</summary>
    [Range(0, 365)]
    public int ImminentVacationDays { get; set; } = 7;

    /// <summary>Fewer remaining days than this is worth pointing out.</summary>
    [Range(0, 60)]
    public int LowVacationDaysThreshold { get; set; } = 3;
}
