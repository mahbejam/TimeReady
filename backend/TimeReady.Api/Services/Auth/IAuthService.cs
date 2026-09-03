using TimeReady.Api.Dtos.Auth;

namespace TimeReady.Api.Services.Auth;

/// <summary>Why an authentication attempt did not succeed.</summary>
public enum AuthFailure
{
    /// <summary>No failure.</summary>
    None,

    /// <summary>Unknown account or wrong password.</summary>
    InvalidCredentials,

    /// <summary>Too many failed attempts.</summary>
    LockedOut,

    /// <summary>The refresh token is unknown, expired or already used.</summary>
    InvalidRefreshToken
}

/// <summary>Outcome of a login or refresh attempt.</summary>
/// <param name="Response">The issued tokens, when the attempt succeeded.</param>
/// <param name="Failure">The reason, when it did not.</param>
public record AuthResult(AuthResponse? Response, AuthFailure Failure)
{
    /// <summary>True when tokens were issued.</summary>
    public bool Succeeded => Failure == AuthFailure.None && Response is not null;

    /// <summary>Creates a successful result.</summary>
    public static AuthResult Success(AuthResponse response) => new(response, AuthFailure.None);

    /// <summary>Creates a failed result.</summary>
    public static AuthResult Fail(AuthFailure failure) => new(null, failure);
}

/// <summary>Sign in, token rotation and sign out.</summary>
public interface IAuthService
{
    /// <summary>Validates credentials and issues a token pair.</summary>
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <summary>Exchanges a valid refresh token for a new token pair.</summary>
    Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>Revokes a refresh token so it can no longer be exchanged.</summary>
    /// <param name="refreshToken">The token presented by the client.</param>
    /// <param name="userId">The signed-in user; the token must belong to them.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeAsync(string refreshToken, string userId, CancellationToken cancellationToken);
}
