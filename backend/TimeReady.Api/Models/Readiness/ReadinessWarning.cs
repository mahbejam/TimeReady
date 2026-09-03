namespace TimeReady.Api.Models.Readiness;

/// <summary>
/// A single finding produced by the rule engine.
/// </summary>
/// <param name="Code">Stable identifier, useful for filtering and translations.</param>
/// <param name="Severity">Info, Warning or Critical.</param>
/// <param name="Message">What the rule found.</param>
/// <param name="Recommendation">What HR should do about it.</param>
public record ReadinessWarning(
    string Code,
    ReadinessSeverity Severity,
    string Message,
    string Recommendation);
