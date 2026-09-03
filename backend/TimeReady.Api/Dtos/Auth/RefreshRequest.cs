namespace TimeReady.Api.Dtos.Auth;

/// <summary>Exchanges a refresh token for a new pair of tokens.</summary>
/// <param name="RefreshToken">The refresh token received at login.</param>
public record RefreshRequest(string RefreshToken);
