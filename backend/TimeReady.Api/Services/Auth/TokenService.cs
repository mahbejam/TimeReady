using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TimeReady.Api.Configuration;
using TimeReady.Api.Models.Identity;

namespace TimeReady.Api.Services.Auth;

/// <inheritdoc cref="ITokenService" />
public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    private const int RefreshTokenBytes = 32;

    private readonly JwtOptions _options = options.Value;

    /// <inheritdoc />
    public AccessToken CreateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var issuedAt = timeProvider.GetUtcNow();
        var expiresAt = issuedAt.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.FullName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(SigningKey(), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    /// <inheritdoc />
    public IssuedRefreshToken CreateRefreshToken()
    {
        var value = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
        var expiresAt = timeProvider.GetUtcNow().AddDays(_options.RefreshTokenDays);

        return new IssuedRefreshToken(value, Hash(value), expiresAt);
    }

    /// <inheritdoc />
    public string Hash(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    private SymmetricSecurityKey SigningKey() =>
        new(Encoding.UTF8.GetBytes(_options.SigningKey));
}
