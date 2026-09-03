using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TimeReady.Api.Models;
using TimeReady.Api.Models.Auditing;
using TimeReady.Api.Models.Identity;

namespace TimeReady.Api.Data;

/// <summary>
/// Single database context for the application: HR data, the Identity tables and
/// the audit trail live in the same PostgreSQL database.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    /// <summary>Employees tracked by HR.</summary>
    public DbSet<Employee> Employees => Set<Employee>();

    /// <summary>Issued refresh tokens, stored as hashes.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>Recorded changes to auditable entities.</summary>
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <summary>Audit entries that have been moved out of the live table.</summary>
    public DbSet<AuditArchiveEntry> AuditArchiveEntries => Set<AuditArchiveEntry>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // SQLite (unit tests only) cannot compare DateTimeOffset columns. Store
        // them as binary so retention and audit queries stay server-side.
        // ProviderName check avoids referencing the SQLite package from the API.
        if (string.Equals(
                Database.ProviderName,
                "Microsoft.EntityFrameworkCore.Sqlite",
                StringComparison.Ordinal))
        {
            ApplySqliteDateTimeOffsetConverters(modelBuilder);
        }
    }

    private static void ApplySqliteDateTimeOffsetConverters(ModelBuilder modelBuilder)
    {
        var converter = new DateTimeOffsetToBinaryConverter();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.ClrType.GetProperties()
                         .Where(property =>
                             property.PropertyType == typeof(DateTimeOffset)
                             || property.PropertyType == typeof(DateTimeOffset?)))
            {
                modelBuilder
                    .Entity(entityType.ClrType)
                    .Property(property.Name)
                    .HasConversion(converter);
            }
        }
    }
}
