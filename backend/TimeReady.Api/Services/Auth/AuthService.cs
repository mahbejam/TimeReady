using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeReady.Api.Data;
using TimeReady.Api.Dtos.Auth;
using TimeReady.Api.Models.Identity;

namespace TimeReady.Api.Services.Auth;

/// <inheritdoc cref="IAuthService" />
public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    AppDbContext context,
    ITokenService tokenService,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    /// <inheritdoc />
    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            logger.LogInformation("Login failed: no such account");
            return AuthResult.Fail(AuthFailure.InvalidCredentials);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return AuthResult.Fail(AuthFailure.LockedOut);
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);

            logger.LogInformation("Login failed for user {UserId}: wrong password", user.Id);

            return await userManager.IsLockedOutAsync(user)
                ? AuthResult.Fail(AuthFailure.LockedOut)
                : AuthResult.Fail(AuthFailure.InvalidCredentials);
        }

        await userManager.ResetAccessFailedCountAsync(user);
        await RemoveExpiredTokensAsync(user.Id, cancellationToken);

        logger.LogInformation("User {UserId} signed in", user.Id);

        return AuthResult.Success(await IssueTokensAsync(user, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = tokenService.Hash(refreshToken);
        var now = timeProvider.GetUtcNow();

        var stored = await context.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);

        if (stored is null)
        {
            return AuthResult.Fail(AuthFailure.InvalidRefreshToken);
        }

        // A token that was already rotated must never be usable again. Seeing one
        // means it leaked, so every remaining token of that user is dropped. A
        // token revoked by logout has no replacement and is simply refused.
        if (stored.RevokedAtUtc is not null)
        {
            if (stored.ReplacedByTokenHash is not null)
            {
                logger.LogWarning("Reused refresh token detected for user {UserId}", stored.UserId);
                await RevokeAllTokensAsync(stored.UserId, now, cancellationToken);
            }

            return AuthResult.Fail(AuthFailure.InvalidRefreshToken);
        }

        if (!stored.IsActive(now) || stored.User is null)
        {
            return AuthResult.Fail(AuthFailure.InvalidRefreshToken);
        }

        var response = await IssueTokensAsync(stored.User, cancellationToken, rotated: stored);

        return AuthResult.Success(response);
    }

    /// <inheritdoc />
    public async Task RevokeAsync(string refreshToken, string userId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var hash = tokenService.Hash(refreshToken);

        var stored = await context.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);

        // Unknown, already revoked, or belonging to someone else: treat as a no-op
        // so logout stays idempotent and does not leak whether a token exists.
        if (stored is null || stored.RevokedAtUtc is not null || stored.UserId != userId)
        {
            return;
        }

        stored.RevokedAtUtc = timeProvider.GetUtcNow();
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResponse> IssueTokensAsync(
        ApplicationUser user,
        CancellationToken cancellationToken,
        RefreshToken? rotated = null)
    {
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.CreateAccessToken(user, roles);
        var refreshToken = tokenService.CreateRefreshToken();

        if (rotated is not null)
        {
            rotated.RevokedAtUtc = timeProvider.GetUtcNow();
            rotated.ReplacedByTokenHash = refreshToken.Hash;
        }

        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshToken.Hash,
            CreatedAtUtc = timeProvider.GetUtcNow(),
            ExpiresAtUtc = refreshToken.ExpiresAtUtc
        });

        await context.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            refreshToken.Value,
            refreshToken.ExpiresAtUtc,
            new AuthUserDto(user.Id, user.Email ?? string.Empty, user.FullName, roles.ToList()));
    }

    private async Task RevokeAllTokensAsync(
        string userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var tokens = await context.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAtUtc = now;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task RemoveExpiredTokensAsync(string userId, CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow();

        await context.RefreshTokens
            .Where(token => token.UserId == userId && token.ExpiresAtUtc <= cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
