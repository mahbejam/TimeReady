using TimeReady.Api.Models;

namespace TimeReady.Api.Data.Seeding;

/// <summary>
/// Demo data for the portfolio build. Seeding happens at startup instead of
/// through <c>HasData</c> because the vacation dates are relative to today –
/// otherwise the demo would go stale a few weeks after the migration was created.
/// </summary>
public static class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        if (context.Employees.Any())
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);

        context.Employees.AddRange(
            new Employee
            {
                FullName = "Anna Gruber",
                TimeBalanceHours = 12.5m,
                RemainingVacationDays = 18,
                VacationStartDate = today.AddDays(3),
                ManagerInformed = true,
                HandoverCompleted = false
            },
            new Employee
            {
                FullName = "Michael Hofer",
                TimeBalanceHours = -22.0m,
                RemainingVacationDays = 6,
                VacationStartDate = today.AddDays(12),
                ManagerInformed = false,
                HandoverCompleted = false
            },
            new Employee
            {
                FullName = "Sarah Lang",
                TimeBalanceHours = 3.25m,
                RemainingVacationDays = 24,
                VacationStartDate = null,
                ManagerInformed = false,
                HandoverCompleted = false
            },
            new Employee
            {
                FullName = "Thomas Egger",
                TimeBalanceHours = -4.5m,
                RemainingVacationDays = 11,
                VacationStartDate = today.AddDays(45),
                ManagerInformed = true,
                HandoverCompleted = true
            },
            new Employee
            {
                FullName = "Lena Moser",
                TimeBalanceHours = 38.0m,
                RemainingVacationDays = 2,
                VacationStartDate = today.AddDays(6),
                ManagerInformed = true,
                HandoverCompleted = true
            });

        context.SaveChanges();
    }
}
