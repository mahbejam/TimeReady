using TimeReady.Api.Models.Auditing;

namespace TimeReady.Api.Models;

/// <summary>
/// An employee tracked by HR in the run-up to a planned absence.
/// </summary>
public class Employee : IAuditable
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Flex-time balance in hours. A negative value means the employee still
    /// owes hours, a positive value means overtime has been built up.
    /// </summary>
    public decimal TimeBalanceHours { get; set; }

    public int RemainingVacationDays { get; set; }

    /// <summary>
    /// First day of the next planned vacation. Null when nothing is planned.
    /// </summary>
    public DateOnly? VacationStartDate { get; set; }

    public bool ManagerInformed { get; set; }

    public bool HandoverCompleted { get; set; }
}
