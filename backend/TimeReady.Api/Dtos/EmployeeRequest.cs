namespace TimeReady.Api.Dtos;

/// <summary>
/// Payload for creating or updating an employee. Create and update accept the
/// same fields, so one contract keeps the API predictable.
/// </summary>
public record EmployeeRequest(
    string FullName,
    decimal TimeBalanceHours,
    int RemainingVacationDays,
    DateOnly? VacationStartDate,
    bool ManagerInformed,
    bool HandoverCompleted);
