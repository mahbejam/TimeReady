using System.Diagnostics;
using System.Reflection;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using TimeReady.Api.Configuration;
using TimeReady.Api.Data;
using TimeReady.Api.Data.Auditing;
using TimeReady.Api.Data.Repositories;
using TimeReady.Api.Infrastructure;
using TimeReady.Api.Services;
using TimeReady.Api.Services.Auditing;
using TimeReady.Api.Validation;

namespace TimeReady.Api.Extensions;

/// <summary>
/// Groups the service registrations by concern so <c>Program.cs</c> stays a
/// readable list of what the application is made of.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string HealthTagLive = "live";
    private const string HealthTagReady = "ready";
    private const string HealthTagBackground = "background";

    /// <summary>Binds and validates every configuration section on startup.</summary>
    public static IServiceCollection AddApplicationOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ReadinessOptions>()
            .Bind(configuration.GetSection(ReadinessOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => options.CriticalNegativeBalanceHours <= options.WarningNegativeBalanceHours,
                "Readiness:CriticalNegativeBalanceHours must be less than or equal to WarningNegativeBalanceHours.")
            .ValidateOnStart();

        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AuditRetentionOptions>()
            .Bind(configuration.GetSection(AuditRetentionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.Configure<IdentitySeedOptions>(
            configuration.GetSection(IdentitySeedOptions.SectionName));

        return services;
    }

    /// <summary>Registers the database context and the repositories on top of it.</summary>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) => options
            .UseNpgsql(
                configuration.GetConnectionString("TimeReadyDb"),
                npgsql => npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null))
            .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();

        return services;
    }

    /// <summary>Registers the rule engine and the clock it depends on.</summary>
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IReadinessService, ReadinessService>();

        return services;
    }

    /// <summary>Registers the validators and the filter that applies them.</summary>
    public static IServiceCollection AddRequestValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<EmployeeRequestValidator>(ServiceLifetime.Singleton);

        return services;
    }

    /// <summary>
    /// Adds API versioning. The version is read from a header, a query string or
    /// the media type rather than from the URL, so existing clients keep working
    /// against <c>/api/employees</c> while a future v2 can be introduced.
    /// </summary>
    public static IServiceCollection AddApiVersioningSupport(this IServiceCollection services)
    {
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new HeaderApiVersionReader("X-Api-Version"),
                    new QueryStringApiVersionReader("api-version"),
                    new MediaTypeApiVersionReader("v"));
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = false;
            });

        return services;
    }

    /// <summary>Configures Swagger, including the XML comments written in the code.</summary>
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "TimeReady API",
                Version = "v1",
                Description =
                    "Leave and time-balance assistant for HR teams. Readiness is decided by a "
                    + "rule-based engine – explicit thresholds and flags, no prediction.",
                Contact = new OpenApiContact { Name = "TimeReady", Url = new Uri("https://github.com/") },
                License = new OpenApiLicense { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
            });

            options.SupportNonNullableReferenceTypes();

            var scheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the access token returned by /api/auth/login.",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            };

            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, scheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = [] });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    /// <summary>Adds ProblemDetails responses enriched with a trace identifier.</summary>
    public static IServiceCollection AddProblemDetailsResponses(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
            context.ProblemDetails.Extensions["traceId"] =
                Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        });

        return services;
    }

    /// <summary>Allows the configured frontend origins to call the API.</summary>
    public static IServiceCollection AddFrontendCors(
        this IServiceCollection services,
        IConfiguration configuration,
        string policyName)
    {
        var origins = configuration
            .GetSection(CorsOptions.SectionName)
            .Get<CorsOptions>()?.AllowedOrigins ?? [];

        services.AddCors(options => options.AddPolicy(policyName, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        return services;
    }

    /// <summary>
    /// Adds a fixed-window rate limit per client address. It protects the API
    /// from a runaway script; it is not a substitute for an API gateway.
    /// </summary>
    public static IServiceCollection AddRequestRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var limits = configuration
            .GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.PermitLimit,
                        Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                        QueueLimit = 0
                    }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = limits.WindowSeconds.ToString();
                context.HttpContext.Response.ContentType = "application/problem+json";

                await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests.",
                    Detail = $"Try again in {limits.WindowSeconds} seconds.",
                    Instance = context.HttpContext.Request.Path
                }, cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    /// Registers the retention service, the status monitor and the background
    /// job that applies the policy.
    /// </summary>
    public static IServiceCollection AddAuditRetention(this IServiceCollection services)
    {
        services.AddSingleton<AuditRetentionRunGate>();
        services.AddScoped<IAuditRetentionService, AuditRetentionService>();
        services.AddSingleton<IAuditRetentionMonitor, AuditRetentionMonitor>();
        services.AddHostedService<AuditRetentionBackgroundService>();

        return services;
    }

    /// <summary>Adds a liveness check and a readiness check that touches the database.</summary>
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("The API is running."), tags: [HealthTagLive])
            .AddDbContextCheck<AppDbContext>("database", tags: [HealthTagReady])
            .AddCheck<AuditRetentionHealthCheck>("audit-retention", tags: [HealthTagBackground]);

        return services;
    }
}
