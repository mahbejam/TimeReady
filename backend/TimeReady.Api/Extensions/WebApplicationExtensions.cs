using Microsoft.EntityFrameworkCore;
using TimeReady.Api.Data;
using TimeReady.Api.Data.Seeding;

namespace TimeReady.Api.Extensions;

/// <summary>
/// Startup steps that need a built application rather than a service collection.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Applies pending migrations and loads demo data when the table is empty.
    /// Convenient for a single-instance demo; a production deployment would run
    /// migrations as its own step before the application starts.
    /// </summary>
    public static WebApplication MigrateAndSeedDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
        SeedData.Initialize(context);

        return app;
    }
}
