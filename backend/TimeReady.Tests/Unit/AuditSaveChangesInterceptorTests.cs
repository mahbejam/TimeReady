using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TimeReady.Api.Data;
using TimeReady.Api.Data.Auditing;
using TimeReady.Api.Models;
using TimeReady.Api.Models.Auditing;
using TimeReady.Api.Services.Auditing;
using Xunit;

namespace TimeReady.Tests.Unit;

/// <summary>
/// The interceptor is only meaningful together with a change tracker, so these
/// tests run against a real (in-memory) SQLite database.
/// </summary>
public class AuditSaveChangesInterceptorTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 7, 20);

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;

    public AuditSaveChangesInterceptorTests()
        : this(new StubCurrentUserAccessor())
    {
    }

    private AuditSaveChangesInterceptorTests(ICurrentUserAccessor currentUser)
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new AuditSaveChangesInterceptor(currentUser, new FixedTimeProvider(Today)))
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Insert_IsRecordedAsCreated_WithTheNewValues()
    {
        var employee = await AddEmployeeAsync();

        var entry = Assert.Single(await _context.AuditEntries.ToListAsync());

        Assert.Equal(nameof(Employee), entry.EntityName);
        Assert.Equal(employee.Id.ToString(), entry.EntityId);
        Assert.Equal(AuditAction.Created, entry.Action);
        Assert.Null(entry.OldValues);
        Assert.Equal("Anna Gruber", Values(entry.NewValues)["FullName"]);
    }

    [Fact]
    public async Task Update_RecordsOnlyTheColumnsThatChanged()
    {
        var employee = await AddEmployeeAsync();
        await ClearAuditAsync();

        employee.HandoverCompleted = true;
        employee.TimeBalanceHours = -22.5m;
        await _context.SaveChangesAsync();

        var entry = Assert.Single(await _context.AuditEntries.ToListAsync());
        var changed = JsonSerializer.Deserialize<List<string>>(entry.ChangedColumns!)!;

        Assert.Equal(AuditAction.Updated, entry.Action);
        Assert.Equal(2, changed.Count);
        Assert.Contains("HandoverCompleted", changed);
        Assert.Contains("TimeBalanceHours", changed);
        Assert.Equal("False", Values(entry.OldValues)["HandoverCompleted"]);
        Assert.Equal("True", Values(entry.NewValues)["HandoverCompleted"]);
        Assert.DoesNotContain("FullName", Values(entry.NewValues).Keys);
    }

    [Fact]
    public async Task Update_FormatsNumbersWithTheInvariantCulture()
    {
        var employee = await AddEmployeeAsync();
        await ClearAuditAsync();

        employee.TimeBalanceHours = -22.5m;
        await _context.SaveChangesAsync();

        var entry = Assert.Single(await _context.AuditEntries.ToListAsync());

        Assert.Equal("-22.5", Values(entry.NewValues)["TimeBalanceHours"]);
    }

    [Fact]
    public async Task Delete_IsRecordedWithTheValuesThatWereLost()
    {
        var employee = await AddEmployeeAsync();
        await ClearAuditAsync();

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();

        var entry = Assert.Single(await _context.AuditEntries.ToListAsync());

        Assert.Equal(AuditAction.Deleted, entry.Action);
        Assert.Equal(employee.Id.ToString(), entry.EntityId);
        Assert.Null(entry.NewValues);
        Assert.Equal("Anna Gruber", Values(entry.OldValues)["FullName"]);
    }

    [Fact]
    public async Task SaveWithoutChanges_RecordsNothing()
    {
        await AddEmployeeAsync();
        await ClearAuditAsync();

        await _context.SaveChangesAsync();

        Assert.Empty(await _context.AuditEntries.ToListAsync());
    }

    [Fact]
    public async Task SettingAPropertyToItsCurrentValue_RecordsNothing()
    {
        var employee = await AddEmployeeAsync();
        await ClearAuditAsync();

        employee.FullName = "Anna Gruber";
        await _context.SaveChangesAsync();

        Assert.Empty(await _context.AuditEntries.ToListAsync());
    }

    [Fact]
    public async Task Entry_RecordsWhoMadeTheChangeAndWhen()
    {
        await AddEmployeeAsync();

        var entry = Assert.Single(await _context.AuditEntries.ToListAsync());

        Assert.Equal("user-1", entry.UserId);
        Assert.Equal("anna@timeready.test", entry.UserName);
        Assert.Equal("trace-1", entry.TraceId);
        Assert.Equal(Today.ToDateTime(TimeOnly.MinValue), entry.TimestampUtc.UtcDateTime);
    }

    [Fact]
    public async Task AuditEntries_AreNotAuditedThemselves()
    {
        await AddEmployeeAsync();

        // One entry for the employee, and nothing for the audit row that was
        // written for it – otherwise the table would grow forever.
        Assert.Single(await _context.AuditEntries.ToListAsync());
    }

    private async Task<Employee> AddEmployeeAsync()
    {
        var employee = new Employee
        {
            FullName = "Anna Gruber",
            TimeBalanceHours = 12.5m,
            RemainingVacationDays = 18,
            VacationStartDate = Today.AddDays(10),
            ManagerInformed = true,
            HandoverCompleted = false
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return employee;
    }

    private async Task ClearAuditAsync()
    {
        _context.AuditEntries.RemoveRange(await _context.AuditEntries.ToListAsync());
        await _context.SaveChangesAsync();
    }

    private static Dictionary<string, string?> Values(string? json) =>
        JsonSerializer.Deserialize<Dictionary<string, string?>>(json!)!;

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
