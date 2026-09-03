using FluentValidation;
using TimeReady.Api.Dtos;

namespace TimeReady.Api.Validation;

public class EmployeeRequestValidator : AbstractValidator<EmployeeRequest>
{
    private const int MaxRemainingVacationDays = 60;

    public EmployeeRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(120).WithMessage("Full name must not exceed 120 characters.");

        RuleFor(x => x.TimeBalanceHours)
            .InclusiveBetween(-200m, 400m)
            .WithMessage("Time balance must be between -200 and 400 hours.");

        RuleFor(x => x.RemainingVacationDays)
            .InclusiveBetween(0, MaxRemainingVacationDays)
            .WithMessage($"Remaining vacation days must be between 0 and {MaxRemainingVacationDays}.");

        RuleFor(x => x.VacationStartDate)
            .Must(date => BeWithinAPlausibleRange(date, timeProvider))
            .When(x => x.VacationStartDate.HasValue)
            .WithMessage("Vacation start date must be within the last year or the next two years.");
    }

    private static bool BeWithinAPlausibleRange(DateOnly? date, TimeProvider timeProvider)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        return date >= today.AddYears(-1) && date <= today.AddYears(2);
    }
}
