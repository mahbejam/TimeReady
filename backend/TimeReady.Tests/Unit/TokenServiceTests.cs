using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using TimeReady.Api.Authorization;
using TimeReady.Api.Configuration;
using TimeReady.Api.Models.Identity;
using TimeReady.Api.Services.Auth;
using Xunit;

namespace TimeReady.Tests.Unit;

public class TokenServiceTests
{
    private static readonly DateOnly Today = new(2026, 7, 20);
    private const string SigningKey = "unit-test-signing-key-that-is-long-enough-0123";

    [Fact]
    public void CreateAccessToken_CarriesTheAccountAndItsRoles()
    {
        var token = ReadToken(CreateService().CreateAccessToken(User(), [Roles.Admin, Roles.Operator]).Value);

        Assert.Equal("user-1", token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("anna@timeready.test", token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Anna Gruber", token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Name).Value);

        var roles = token.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

        Assert.Contains(Roles.Admin, roles);
        Assert.Contains(Roles.Operator, roles);
    }

    [Fact]
    public void CreateAccessToken_ExpiresAfterTheConfiguredLifetime()
    {
        var options = Options(accessTokenMinutes: 45);

        var token = CreateService(options).CreateAccessToken(User(), []);

        Assert.Equal(
            Today.ToDateTime(TimeOnly.MinValue).AddMinutes(45),
            token.ExpiresAtUtc.UtcDateTime);
    }

    [Fact]
    public void CreateAccessToken_UsesTheConfiguredIssuerAndAudience()
    {
        var token = ReadToken(CreateService().CreateAccessToken(User(), []).Value);

        Assert.Equal("TimeReady", token.Issuer);
        Assert.Contains("TimeReadyClient", token.Audiences);
    }

    [Fact]
    public void CreateRefreshToken_ProducesADifferentSecretEveryTime()
    {
        var service = CreateService();

        var first = service.CreateRefreshToken();
        var second = service.CreateRefreshToken();

        Assert.NotEqual(first.Value, second.Value);
        Assert.NotEqual(first.Hash, second.Hash);
    }

    [Fact]
    public void CreateRefreshToken_StoresAHashInsteadOfTheSecret()
    {
        var service = CreateService();

        var token = service.CreateRefreshToken();

        Assert.NotEqual(token.Value, token.Hash);
        Assert.Equal(token.Hash, service.Hash(token.Value));
        Assert.Equal(64, token.Hash.Length);
    }

    [Fact]
    public void CreateRefreshToken_ExpiresAfterTheConfiguredNumberOfDays()
    {
        var token = CreateService(Options(refreshTokenDays: 14)).CreateRefreshToken();

        Assert.Equal(Today.ToDateTime(TimeOnly.MinValue).AddDays(14), token.ExpiresAtUtc.UtcDateTime);
    }

    private static JwtOptions Options(
        string signingKey = SigningKey,
        int accessTokenMinutes = 30,
        int refreshTokenDays = 7) => new()
    {
        Issuer = "TimeReady",
        Audience = "TimeReadyClient",
        SigningKey = signingKey,
        AccessTokenMinutes = accessTokenMinutes,
        RefreshTokenDays = refreshTokenDays
    };

    private static TokenService CreateService(JwtOptions? options = null) =>
        new(Microsoft.Extensions.Options.Options.Create(options ?? Options()), new FixedTimeProvider(Today));

    private static ApplicationUser User() => new()
    {
        Id = "user-1",
        Email = "anna@timeready.test",
        UserName = "anna@timeready.test",
        FullName = "Anna Gruber"
    };

    private static JwtSecurityToken ReadToken(string value) => new JwtSecurityTokenHandler().ReadJwtToken(value);
}
