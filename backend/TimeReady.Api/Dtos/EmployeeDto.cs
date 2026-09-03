namespace TimeReady.Api.Dtos;

/// <summary>
/// Employee data as returned by the API.
/// </summary>
public record EmployeeDto(
    int Id,
    string FullName,
    decimal TimeBalanceHours,
    int RemainingVacationDays,
    DateOnly? VacationStartDate,
    bool ManagerInformed,
    bool HandoverCompleted);
