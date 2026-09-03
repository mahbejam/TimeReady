using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TimeReady.Api.Configuration;
using TimeReady.Api.Data;
using TimeReady.Api.Models.Auditing;
using TimeReady.Api.Services.Auditing;
using Xunit;

namespace TimeReady.Tests.Unit;

/// <summary>
/// Retention is a database job, so these tests run against an in-memory SQLite
/// database with a frozen clock.
/// </summary>
public class AuditRetentionServiceTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 7, 20);
    private static readonly DateTimeOffset Now = new(Today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;

    public AuditRetentionServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Run_ArchivesEntriesOlderThanTheRetentionPeriod()
    {
        await SeedAsync(ageInDays: [200, 120, 91, 89, 1]);

        var result = await CreateService().RunAsync(CancellationToken.None);

        Assert.Equal(3, result.Archived);
        Assert.Equal(0, result.Purged);
        Assert.Equal(2, await _context.AuditEntries.CountAsync());
        Assert.Equal(3, await _context.AuditArchiveEntries.CountAsync());
    }

    [Fact]
    public async Task Run_KeepsTheOriginalIdAndValuesInTheArchive()
    {
        await SeedAsync(ageInDays: [200]);

        var original = await _context.AuditEntries.SingleAsync();

        await CreateService().RunAsync(CancellationToken.None);

        var archived = await _context.AuditArchiveEntries.SingleAsync();

        Assert.Equal(original.Id, archived.OriginalId);
        Assert.Equal(original.EntityName, archived.EntityName);
        Assert.Equal(original.EntityId, archived.EntityId);
        Assert.Equal(original.Action, archived.Action);
        Assert.Equal(original.UserName, archived.UserName);
        Assert.Equal(original.NewValues, archived.NewValues);
        Assert.Equal(original.TimestampUtc, archived.TimestampUtc);
        Assert.Equal(Now, archived.ArchivedAtUtc);
    }

    [Fact]
    public async Task Run_ArchivesInBatchesUntilNothingIsLeft()
    {
        await SeedAsync(ageInDays: [200, 199, 198, 197, 196]);

        var result = await CreateService(new AuditRetentionOptions { BatchSize = 2 })
            .RunAsync(CancellationToken.None);

        Assert.Equal(5, result.Archived);
        Assert.Empty(await _context.AuditEntries.ToListAsync());
    }

    [Fact]
    public async Task Run_DoesNotPurgeWhilePurgingIsDisabled()
    {
        await SeedAsync(ageInDays: [3000]);

        var result = await CreateService().RunAsync(CancellationToken.None);

        Assert.Equal(1, result.Archived);
        Assert.Equal(0, result.Purged);
        Assert.Null(result.PurgeCutoffUtc);
        Assert.Single(await _context.AuditArchiveEntries.ToListAsync());
    }

    [Fact]
    public async Task Run_PurgesArchivedEntriesPastTheArchiveRetention_WhenEnabled()
    {
        await SeedAsync(ageInDays: [3000, 100]);

        var options = new AuditRetentionOptions
        {
            RetentionDays = 90,
            PurgeEnabled = true,
            ArchiveRetentionDays = 730
        };

        var result = await CreateService(options).RunAsync(CancellationToken.None);

        Assert.Equal(2, result.Archived);
        Assert.Equal(1, result.Purged);

        var remaining = Assert.Single(await _context.AuditArchiveEntries.ToListAsync());

        Assert.Equal(Now.AddDays(-100), remaining.TimestampUtc);
    }

    [Fact]
    public async Task Run_DoesNothing_WhenTheEntriesAreYoungerThanTheRetention()
    {
        await SeedAsync(ageInDays: [10, 20, 30]);

        var result = await CreateService().RunAsync(CancellationToken.None);

        Assert.Equal(0, result.Archived);
        Assert.Equal(3, await _context.AuditEntries.CountAsync());
        Assert.Empty(await _context.AuditArchiveEntries.ToListAsync());
    }

    [Fact]
    public async Task Run_IsSkipped_WhenTheJobIsDisabled()
    {
        await SeedAsync(ageInDays: [500]);

        var result = await CreateService(new AuditRetentionOptions { Enabled = false })
            .RunAsync(CancellationToken.None);

        Assert.True(result.Skipped);
        Assert.Equal(0, result.Archived);
        Assert.Single(await _context.AuditEntries.ToListAsync());
    }

    [Fact]
    public async Task Run_ReportsTheCutoffItUsed()
    {
        var result = await CreateService(new AuditRetentionOptions { RetentionDays = 30 })
            .RunAsync(CancellationToken.None);

        Assert.Equal(Now.AddDays(-30), result.ArchiveCutoffUtc);
    }

    private AuditRetentionService CreateService(AuditRetentionOptions? options = null) =>
        new(
            _context,
            Options.Create(options ?? new AuditRetentionOptions()),
            new FixedTimeProvider(Today),
            new AuditRetentionRunGate(),
            NullLogger<AuditRetentionService>.Instance);

    private async Task SeedAsync(int[] ageInDays)
    {
        foreach (var age in ageInDays)
        {
            _context.AuditEntries.Add(new AuditEntry
            {
                EntityName = "Employee",
                EntityId = age.ToString(),
                Action = AuditAction.Updated,
                UserName = "anna@timeready.test",
                UserId = "user-1",
                TimestampUtc = Now.AddDays(-age),
                NewValues = """{"FullName":"Anna Gruber"}"""
            });
        }

        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
