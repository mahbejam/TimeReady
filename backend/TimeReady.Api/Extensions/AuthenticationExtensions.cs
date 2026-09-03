using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TimeReady.Api.Authorization;
using TimeReady.Api.Configuration;
using TimeReady.Api.Data;
using TimeReady.Api.Data.Seeding;
using TimeReady.Api.Models.Identity;
using TimeReady.Api.Services.Auth;

namespace TimeReady.Api.Extensions;

/// <summary>
/// Registration of ASP.NET Core Identity, JWT bearer authentication and the
/// authorization policies the controllers refer to.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>Adds the Identity stores, password rules and lockout settings.</summary>
    public static IServiceCollection AddApplicationIdentity(this IServiceCollection services)
    {
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IdentitySeeder>();

        return services;
    }

    /// <summary>Configures bearer authentication against the issued access tokens.</summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Resolve JWT settings when bearer options are built so validation uses
        // the same IOptions<JwtOptions> binding as TokenService (including test
        // overrides from WebApplicationFactory).
        services.AddSingleton<
            IConfigureNamedOptions<JwtBearerOptions>,
            ConfigureJwtBearerOptions>();

        return services;
    }

    private sealed class ConfigureJwtBearerOptions(IOptions<JwtOptions> jwtOptions)
        : IConfigureNamedOptions<JwtBearerOptions>
    {
        public void Configure(string? name, JwtBearerOptions options)
        {
            if (!string.Equals(name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal))
            {
                return;
            }

            var jwt = jwtOptions.Value;

            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = JwtRegisteredClaimNames.Name,
                RoleClaimType = ClaimTypes.Role
            };
        }

        public void Configure(JwtBearerOptions options) =>
            Configure(JwtBearerDefaults.AuthenticationScheme, options);
    }

    /// <summary>Registers the policies used by the <c>[Authorize]</c> attributes.</summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        // Authenticated-by-default: a new endpoint without [AllowAnonymous] or an
        // explicit policy cannot accidentally ship as public.
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy(Policies.ReadEmployees, policy =>
                policy.RequireRole(Roles.Admin, Roles.Operator))
            .AddPolicy(Policies.UpdateEmployees, policy =>
                policy.RequireRole(Roles.Admin, Roles.Operator))
            .AddPolicy(Policies.ManageEmployees, policy =>
                policy.RequireRole(Roles.Admin))
            .AddPolicy(Policies.ReadAuditTrail, policy =>
                policy.RequireRole(Roles.Admin));

        return services;
    }

    /// <summary>Creates the roles and seed accounts after the database is migrated.</summary>
    public static async Task<WebApplication> SeedIdentityAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedAsync();

        return app;
    }
}
