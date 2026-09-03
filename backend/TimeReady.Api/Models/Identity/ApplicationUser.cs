using Microsoft.AspNetCore.Identity;

namespace TimeReady.Api.Models.Identity;

/// <summary>
/// An account that can sign in to TimeReady. Employees and users are separate:
/// an HR user is not necessarily one of the employees being tracked.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Display name shown in the UI.</summary>
    public string FullName { get; set; } = string.Empty;
}
