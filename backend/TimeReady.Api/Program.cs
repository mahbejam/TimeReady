using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using TimeReady.Api.Extensions;
using TimeReady.Api.Infrastructure;
using TimeReady.Api.Validation;

// A bootstrap logger captures failures that happen before configuration is read.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    const string frontendCorsPolicy = "frontend";

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services
        .AddApplicationOptions(builder.Configuration)
        .AddPersistence(builder.Configuration)
        .AddApplicationIdentity()
        .AddJwtAuthentication(builder.Configuration)
        .AddAuthorizationPolicies()
        .AddDomainServices()
        .AddAuditRetention()
        .AddRequestValidation()
        .AddProblemDetailsResponses()
        .AddApiVersioningSupport()
        .AddApiDocumentation()
        .AddFrontendCors(builder.Configuration, frontendCorsPolicy)
        .AddRequestRateLimiting(builder.Configuration)
        .AddApplicationHealthChecks();

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "TimeReady API v1");
            options.DocumentTitle = "TimeReady API";
        });
    }
    else
    {
        app.UseHsts();
    }

    app.UseRateLimiter();
    app.UseCors(frontendCorsPolicy);

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Health probes stay anonymous (orchestrators have no JWT). Exception
    // messages are omitted from the JSON body — see HealthCheckResponseWriter.
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = HealthCheckResponseWriter.WriteAsync
    }).AllowAnonymous();

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live"),
        ResponseWriter = HealthCheckResponseWriter.WriteAsync
    }).AllowAnonymous();

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = HealthCheckResponseWriter.WriteAsync
    }).AllowAnonymous();

    app.MigrateAndSeedDatabase();
    await app.SeedIdentityAsync();

    Log.Information("TimeReady API started in {Environment}", app.Environment.EnvironmentName);
    await app.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "TimeReady API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Exposed so the integration tests can start the real application through
/// WebApplicationFactory.
/// </summary>
public partial class Program;
