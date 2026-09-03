using TimeReady.Api.Models.Identity;

namespace TimeReady.Api.Services.Auth;

/// <summary>An issued access token and the moment it expires.</summary>
/// <param name="Value">The encoded JWT.</param>
/// <param name="ExpiresAtUtc">Expiry of the token.</param>
public record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);

/// <summary>An issued refresh token: the secret goes to the client, the hash to the database.</summary>
/// <param name="Value">The secret handed to the client.</param>
/// <param name="Hash">SHA-256 hash stored in the database.</param>
/// <param name="ExpiresAtUtc">Expiry of the token.</param>
public record IssuedRefreshToken(string Value, string Hash, DateTimeOffset ExpiresAtUtc);

/// <summary>Creates the tokens used by the authentication endpoints.</summary>
public interface ITokenService
{
    /// <summary>Creates a signed access token carrying the user's roles.</summary>
    AccessToken CreateAccessToken(ApplicationUser user, IEnumerable<string> roles);

    /// <summary>Creates a cryptographically random refresh token.</summary>
    IssuedRefreshToken CreateRefreshToken();

    /// <summary>Hashes a refresh token so it can be compared with a stored value.</summary>
    string Hash(string refreshToken);
}
