using Microsoft.Extensions.Options;
using TimeReady.Api.Configuration;
using TimeReady.Api.Models;
using TimeReady.Api.Models.Readiness;

namespace TimeReady.Api.Services;

/// <summary>
/// Rule-based decision engine. Every rule is an explicit, readable check – there
/// is no model and no external AI service involved. The interface keeps the rest
/// of the application independent of the implementation, so a future version
/// could add an LLM-generated summary on top of these findings.
/// </summary>
public class ReadinessService(IOptions<ReadinessOptions> options, TimeProvider timeProvider) : IReadinessService
{
    private readonly ReadinessOptions _options = options.Value;

    public ReadinessResult Evaluate(Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var warnings = new List<ReadinessWarning>();

        AddTimeBalanceWarnings(employee, warnings);
        AddVacationWarnings(employee, today, warnings);
        AddPreparationWarnings(employee, warnings);

        // An employee is ready once a vacation is planned and nothing critical
        // is open. Warnings and info findings stay visible but do not block.
        var isReady = employee.VacationStartDate.HasValue
                      && warnings.TrueForAll(w => w.Severity != ReadinessSeverity.Critical);

        return new ReadinessResult(employee.Id, employee.FullName, isReady, warnings);
    }

    private void AddTimeBalanceWarnings(Employee employee, List<ReadinessWarning> warnings)
    {
        if (employee.TimeBalanceHours <= _options.CriticalNegativeBalanceHours)
        {
            warnings.Add(new ReadinessWarning(
                ReadinessCodes.NegativeTimeBalance,
                ReadinessSeverity.Critical,
                $"Time balance is {employee.TimeBalanceHours:0.##} hours.",
                "Agree on a plan to compensate the missing hours before the vacation starts."));
        }
        else if (employee.TimeBalanceHours <= _options.WarningNegativeBalanceHours)
        {
            warnings.Add(new ReadinessWarning(
                ReadinessCodes.NegativeTimeBalance,
                ReadinessSeverity.Warning,
                $"Time balance is {employee.TimeBalanceHours:0.##} hours.",
                "Keep an eye on the balance – it should not grow further before the absence."));
        }
    }

    private void AddVacationWarnings(Employee employee, DateOnly today, List<ReadinessWarning> warnings)
    {
        if (employee.VacationStartDate is null)
        {
            warnings.Add(new ReadinessWarning(
                ReadinessCodes.NoVacationPlanned,
                ReadinessSeverity.Info,
                "No vacation is planned.",
                "Add a vacation start date to run the full readiness check."));
            return;
        }

        var daysUntilStart = employee.VacationStartDate.Value.DayNumber - today.DayNumber;

        if (daysUntilStart >= 0 && daysUntilStart <= _options.ImminentVacationDays)
        {
            warnings.Add(new ReadinessWarning(
                ReadinessCodes.VacationStartsSoon,
                ReadinessSeverity.Warning,
                daysUntilStart == 0
                    ? "Vacation starts today."
                    : $"Vacation starts in {daysUntilStart} day(s).",
                "Complete the remaining preparation steps now."));
        }

        if (employee.RemainingVacationDays < _options.LowVacationDaysThreshold)
        {
            warnings.Add(new ReadinessWarning(
                ReadinessCodes.LowVacationDays,
                ReadinessSeverity.Info,
                $"Only {employee.RemainingVacationDays} vacation day(s) remain.",
                "Check whether the planned absence is covered by the remaining entitlement."));
        }
    }

    private static void AddPreparationWarnings(Employee employee, List<ReadinessWarning> warnings)
    {
        if (!employee.ManagerInformed)
        {
            warnings.Add(new ReadinessWarning(
                ReadinessCodes.ManagerNotInformed,
                ReadinessSeverity.Critical,
                "The line manager has not been informed.",
                "Send the vacation confirmation to the line manager."));
        }

        if (!employee.HandoverCompleted)
        {
            warnings.Add(new ReadinessWarning(
                ReadinessCodes.HandoverIncomplete,
                ReadinessSeverity.Critical,
                "The handover is not completed.",
                "Document the open tasks and assign a stand-in."));
        }
    }
}
