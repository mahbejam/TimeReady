using System.ComponentModel.DataAnnotations;

namespace TimeReady.Api.Configuration;

/// <summary>Account that is created on first start so the API is usable.</summary>
public class SeedUserOptions
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Accounts seeded on first start. The operator account exists so the two roles
/// can be demonstrated; it is only created outside production.
/// </summary>
public class IdentitySeedOptions
{
    /// <summary>Configuration section that holds these values.</summary>
    public const string SectionName = "IdentitySeed";

    /// <summary>The administrator account. Always seeded.</summary>
    public SeedUserOptions Admin { get; set; } = new();

    /// <summary>A demo operator account. Seeded outside production only.</summary>
    public SeedUserOptions Operator { get; set; } = new();
}
