namespace TimeReady.Api.Models.Identity;

/// <summary>
/// A long-lived token that can be exchanged for a new access token. Only the
/// SHA-256 hash is stored, so a leaked database does not hand out valid tokens.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }

    /// <summary>Owner of the token.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>SHA-256 hash of the token that was handed to the client.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>Set when the token was used or explicitly revoked.</summary>
    public DateTimeOffset? RevokedAtUtc { get; set; }

    /// <summary>Hash of the token that replaced this one during rotation.</summary>
    public string? ReplacedByTokenHash { get; set; }

    /// <summary>True while the token can still be exchanged.</summary>
    public bool IsActive(DateTimeOffset now) => RevokedAtUtc is null && ExpiresAtUtc > now;

    public ApplicationUser? User { get; set; }
}
