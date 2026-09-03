using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TimeReady.Api.Validation;

/// <summary>
/// Runs the registered FluentValidation validator for every action argument and
/// answers with a standard ValidationProblemDetails response when it fails.
/// This keeps the controllers free of repeated validation code.
/// </summary>
public class ValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            if (serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument));

            if (result.IsValid)
            {
                continue;
            }

            context.Result = new BadRequestObjectResult(
                new ValidationProblemDetails(result.ToDictionary())
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred."
                });

            return;
        }

        await next();
    }
}
