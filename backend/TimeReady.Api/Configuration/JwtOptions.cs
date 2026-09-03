using System.ComponentModel.DataAnnotations;

namespace TimeReady.Api.Configuration;

/// <summary>
/// Settings for issuing and validating access tokens.
/// </summary>
public class JwtOptions
{
    /// <summary>Configuration section that holds these values.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Token issuer, written into and checked on every token.</summary>
    [Required]
    public string Issuer { get; set; } = "TimeReady";

    /// <summary>Intended audience of the token.</summary>
    [Required]
    public string Audience { get; set; } = "TimeReadyClient";

    /// <summary>
    /// Symmetric signing key. Never commit a real key: set it through the
    /// environment (<c>Jwt__SigningKey</c>) or user secrets. Startup fails when
    /// it is missing or too short.
    /// </summary>
    [Required(ErrorMessage = "A JWT signing key must be configured.")]
    [MinLength(32, ErrorMessage = "The JWT signing key must be at least 32 characters long.")]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Lifetime of an access token.</summary>
    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 30;

    /// <summary>Lifetime of a refresh token.</summary>
    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 7;
}
