namespace TimeReady.Api.Infrastructure;

/// <summary>
/// Adds the response headers a JSON API should always send. Swagger UI needs to
/// load its own scripts and styles, so the content security policy is applied
/// everywhere except the documentation routes.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private const string SwaggerPathPrefix = "/swagger";

    /// <summary>Writes the headers and passes the request on.</summary>
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";

        if (!context.Request.Path.StartsWithSegments(SwaggerPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        }

        return next(context);
    }
}
