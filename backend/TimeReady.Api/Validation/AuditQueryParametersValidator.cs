using FluentValidation;
using TimeReady.Api.Dtos.Auditing;

namespace TimeReady.Api.Validation;

/// <summary>Keeps paging and date filters inside sensible bounds.</summary>
public class AuditQueryParametersValidator : AbstractValidator<AuditQueryParameters>
{
    public AuditQueryParametersValidator()
    {
        RuleFor(parameters => parameters.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page numbering starts at 1.");

        RuleFor(parameters => parameters.PageSize)
            .InclusiveBetween(1, AuditQueryParameters.MaxPageSize)
            .WithMessage($"Page size must be between 1 and {AuditQueryParameters.MaxPageSize}.");

        RuleFor(parameters => parameters.EntityName)
            .MaximumLength(100);

        RuleFor(parameters => parameters.EntityId)
            .MaximumLength(64);

        RuleFor(parameters => parameters.User)
            .MaximumLength(256);

        RuleFor(parameters => parameters.To)
            .GreaterThanOrEqualTo(parameters => parameters.From!.Value)
            .When(parameters => parameters.From is not null && parameters.To is not null)
            .WithMessage("The end of the range must not be before its start.");
    }
}
