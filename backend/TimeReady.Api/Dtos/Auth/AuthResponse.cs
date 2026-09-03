namespace TimeReady.Api.Dtos.Auth;

/// <summary>The signed-in user, as returned by login, refresh and /me.</summary>
public record AuthUserDto(string Id, string Email, string FullName, IReadOnlyList<string> Roles);

/// <summary>A fresh pair of tokens plus the account they belong to.</summary>
/// <param name="AccessToken">Bearer token for the API.</param>
/// <param name="ExpiresAtUtc">When the access token stops being accepted.</param>
/// <param name="RefreshToken">Token used to obtain the next access token.</param>
/// <param name="RefreshTokenExpiresAtUtc">When the refresh token expires.</param>
/// <param name="User">The account that signed in.</param>
public record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    AuthUserDto User);
