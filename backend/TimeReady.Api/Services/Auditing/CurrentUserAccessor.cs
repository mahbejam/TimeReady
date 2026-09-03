using System.Diagnostics;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace TimeReady.Api.Services.Auditing;

/// <inheritdoc cref="ICurrentUserAccessor" />
public sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    /// <summary>Used when a change happens outside a request, such as startup seeding.</summary>
    public const string SystemUserName = "system";

    /// <inheritdoc />
    public string? UserId =>
        User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <inheritdoc />
    public string UserName =>
        User?.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? User?.Identity?.Name
        ?? SystemUserName;

    /// <inheritdoc />
    public string? TraceId =>
        Activity.Current?.Id ?? httpContextAccessor.HttpContext?.TraceIdentifier;

    private ClaimsPrincipal? User
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;

            return user?.Identity?.IsAuthenticated == true ? user : null;
        }
    }
}
