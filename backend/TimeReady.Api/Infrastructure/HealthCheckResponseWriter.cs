using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TimeReady.Api.Infrastructure;

/// <summary>
/// Writes health check results as JSON so a probe or a person can see which
/// check failed, not only that something did. Exception messages stay out of
/// the response — they can leak connection strings or internal paths.
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    /// <summary>Serialises the report to the response body.</summary>
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            durationMs = Math.Round(report.TotalDuration.TotalMilliseconds),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, SerializerOptions));
    }
}
