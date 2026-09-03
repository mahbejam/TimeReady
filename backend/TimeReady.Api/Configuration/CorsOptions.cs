using System.ComponentModel.DataAnnotations;

namespace TimeReady.Api.Configuration;

/// <summary>
/// Origins that are allowed to call the API from a browser.
/// </summary>
public class CorsOptions
{
    /// <summary>Configuration section that holds these values.</summary>
    public const string SectionName = "Cors";

    /// <summary>Exact origins, for example <c>http://localhost:4200</c>.</summary>
    [MinLength(1, ErrorMessage = "At least one allowed origin must be configured.")]
    public string[] AllowedOrigins { get; set; } = [];
}
