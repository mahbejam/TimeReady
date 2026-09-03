using FluentValidation;
using TimeReady.Api.Dtos.Auth;

namespace TimeReady.Api.Validation;

/// <summary>Checks the shape of a login request before it reaches Identity.</summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Enter your email address.")
            .EmailAddress().WithMessage("Enter a valid email address.");

        RuleFor(request => request.Password)
            .NotEmpty().WithMessage("Enter your password.");
    }
}

/// <summary>Checks that a refresh token was sent at all.</summary>
public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty().WithMessage("A refresh token is required.");
    }
}
