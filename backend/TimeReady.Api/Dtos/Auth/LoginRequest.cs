namespace TimeReady.Api.Dtos.Auth;

/// <summary>Credentials for the login endpoint.</summary>
/// <param name="Email">Email address of the account.</param>
/// <param name="Password">Account password.</param>
public record LoginRequest(string Email, string Password);
