using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TimeReady.Api.Dtos.Auth;
using TimeReady.Api.Models.Identity;
using TimeReady.Api.Services.Auth;

namespace TimeReady.Api.Controllers;

/// <summary>
/// Sign in, token refresh and sign out.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(
    IAuthService authService,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    /// <summary>Signs in with an email address and password.</summary>
    /// <param name="request">The credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The tokens and the signed-in account.</response>
    /// <response code="400">The request did not pass validation.</response>
    /// <response code="401">The credentials are wrong or the account is locked.</response>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);

        return result.Succeeded ? Ok(result.Response) : Unauthorized(result.Failure);
    }

    /// <summary>Exchanges a refresh token for a new pair of tokens.</summary>
    /// <param name="request">The refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">A new token pair.</response>
    /// <response code="400">The request did not pass validation.</response>
    /// <response code="401">The refresh token is unknown, expired or already used.</response>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RefreshAsync(request.RefreshToken, cancellationToken);

        return result.Succeeded ? Ok(result.Response) : Unauthorized(result.Failure);
    }

    /// <summary>Revokes a refresh token. The access token stays valid until it expires.</summary>
    /// <param name="request">The refresh token to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">The token was revoked, or was already invalid.</response>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        if (userId is not null)
        {
            await authService.RevokeAsync(request.RefreshToken, userId, cancellationToken);
        }

        return NoContent();
    }

    /// <summary>Returns the account behind the current access token.</summary>
    /// <response code="200">The signed-in account.</response>
    /// <response code="401">No valid access token was sent.</response>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<AuthUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthUserDto>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var user = userId is null ? null : await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);

        return Ok(new AuthUserDto(user.Id, user.Email ?? string.Empty, user.FullName, roles.ToList()));
    }

    private ObjectResult Unauthorized(AuthFailure failure) => Problem(
        statusCode: StatusCodes.Status401Unauthorized,
        title: failure == AuthFailure.LockedOut ? "Account locked." : "Sign in failed.",
        detail: failure switch
        {
            AuthFailure.LockedOut => "Too many failed attempts. Try again in a few minutes.",
            AuthFailure.InvalidRefreshToken => "The session has expired. Sign in again.",
            _ => "The email address or password is not correct."
        });
}
