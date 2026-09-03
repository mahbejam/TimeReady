namespace TimeReady.Api.Dtos;

/// <summary>
/// A single finding of the rule engine.
/// </summary>
public record ReadinessWarningDto(
    string Code,
    string Severity,
    string Message,
    string Recommendation);
