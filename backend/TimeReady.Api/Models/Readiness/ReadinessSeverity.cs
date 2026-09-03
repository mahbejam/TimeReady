namespace TimeReady.Api.Models.Readiness;

/// <summary>
/// How urgent a finding is. Only <see cref="Critical"/> findings block readiness.
/// </summary>
public enum ReadinessSeverity
{
    Info,
    Warning,
    Critical
}
