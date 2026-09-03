namespace TimeReady.Api.Models.Readiness;

/// <summary>
/// Outcome of the readiness check for one employee.
/// </summary>
public record ReadinessResult(
    int EmployeeId,
    string FullName,
    bool IsReady,
    IReadOnlyList<ReadinessWarning> Warnings);
