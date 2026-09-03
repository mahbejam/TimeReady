using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TimeReady.Api.Authorization;
using TimeReady.Api.Dtos.Auth;
using Xunit;

namespace TimeReady.Tests.Integration;

[Collection("Integration")]
public class AuthEndpointsTests(TimeReadyApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_ReturnsTokensAndRoles_ForTheSeededAdministrator()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(TimeReadyApiFactory.AdminEmail, TimeReadyApiFactory.AdminPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tokens = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(tokens);
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        Assert.Contains(Roles.Admin, tokens.User.Roles);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_ForAWrongPassword()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(TimeReadyApiFactory.AdminEmail, "definitely-not-the-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsValidationProblem_ForAMalformedEmail()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("not-an-email", "x"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsTheSignedInAccount()
    {
        using var client = await factory.CreateOperatorClientAsync();

        var user = await client.GetFromJsonAsync<AuthUserDto>("/api/auth/me");

        Assert.NotNull(user);
        Assert.Equal(TimeReadyApiFactory.OperatorEmail, user.Email);
        Assert.Contains(Roles.Operator, user.Roles);
    }

    [Fact]
    public async Task Me_ReturnsUnauthorized_WithoutAToken()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_IssuesNewTokens_AndInvalidatesTheOldRefreshToken()
    {
        var tokens = await factory.LoginAsync(TimeReadyApiFactory.AdminEmail, TimeReadyApiFactory.AdminPassword);

        var refreshResponse = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest(tokens.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var rotated = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(rotated);
        Assert.NotEqual(tokens.RefreshToken, rotated.RefreshToken);

        // The old token was rotated away and must not work a second time.
        var reuseResponse = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest(tokens.RefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_ReturnsUnauthorized_ForAnUnknownToken()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest("this-token-was-never-issued"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        var tokens = await factory.LoginAsync(TimeReadyApiFactory.AdminEmail, TimeReadyApiFactory.AdminPassword);

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var logoutResponse = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new RefreshRequest(tokens.RefreshToken));

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var refreshResponse = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest(tokens.RefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }
}
