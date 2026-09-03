using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TimeReady.Api.Authorization;
using TimeReady.Api.Configuration;
using TimeReady.Api.Models.Identity;

namespace TimeReady.Api.Data.Seeding;

/// <summary>
/// Creates the roles and the first accounts, so a fresh database can be signed
/// in to. The operator account is only created outside production; the admin
/// password must be supplied through configuration.
/// </summary>
public sealed class IdentitySeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<IdentitySeedOptions> options,
    IHostEnvironment environment,
    ILogger<IdentitySeeder> logger)
{
    private readonly IdentitySeedOptions _options = options.Value;

    /// <summary>Runs the seeding. Existing roles and accounts are left untouched.</summary>
    public async Task SeedAsync()
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role {Role}", role);
            }
        }

        await EnsureUserAsync(_options.Admin, Roles.Admin);

        if (!environment.IsProduction())
        {
            await EnsureUserAsync(_options.Operator, Roles.Operator);
        }
    }

    private async Task EnsureUserAsync(SeedUserOptions seed, string role)
    {
        if (string.IsNullOrWhiteSpace(seed.Email) || string.IsNullOrWhiteSpace(seed.Password))
        {
            logger.LogWarning("No seed account configured for role {Role}; skipping", role);
            return;
        }

        if (await userManager.FindByEmailAsync(seed.Email) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = seed.Email,
            Email = seed.Email,
            EmailConfirmed = true,
            FullName = seed.FullName
        };

        var result = await userManager.CreateAsync(user, seed.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            logger.LogError("Could not create the {Role} account: {Errors}", role, errors);
            return;
        }

        await userManager.AddToRoleAsync(user, role);

        logger.LogWarning(
            "Created the {Role} account {Email} from configuration. Change this password before deploying.",
            role,
            seed.Email);
    }
}
