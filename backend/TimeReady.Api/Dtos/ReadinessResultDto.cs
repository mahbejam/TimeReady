namespace TimeReady.Api.Dtos;

/// <summary>
/// Readiness outcome for one employee.
/// </summary>
public record ReadinessResultDto(
    int EmployeeId,
    string FullName,
    bool IsReady,
    string Status,
    IReadOnlyList<ReadinessWarningDto> Warnings);
