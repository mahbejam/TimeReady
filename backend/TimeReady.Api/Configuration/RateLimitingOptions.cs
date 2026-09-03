using System.ComponentModel.DataAnnotations;

namespace TimeReady.Api.Configuration;

/// <summary>
/// Fixed-window rate limit applied per client IP address.
/// </summary>
public class RateLimitingOptions
{
    /// <summary>Configuration section that holds these values.</summary>
    public const string SectionName = "RateLimiting";

    /// <summary>Requests allowed inside one window.</summary>
    [Range(1, 100_000)]
    public int PermitLimit { get; set; } = 120;

    /// <summary>Length of the window in seconds.</summary>
    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;
}
