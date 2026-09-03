using System.Net;
using System.Net.Http.Json;
using TimeReady.Api.Dtos;
using TimeReady.Api.Dtos.Auditing;
using Xunit;

namespace TimeReady.Tests.Integration;

public class AuditEndpointsTests(TimeReadyApiFactory factory) : IClassFixture<TimeReadyApiFactory>, IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync() => _client = await factory.CreateAdminClientAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Search_ReturnsTheSeedingEntries_WithPagingMetadata()
    {
        var page = await _client.GetFromJsonAsync<PagedResult<AuditEntryDto>>("/api/audit?pageSize=5");

        Assert.NotNull(page);
        Assert.Equal(1, page.Page);
        Assert.Equal(5, page.PageSize);
        Assert.True(page.TotalCount >= 5);
        Assert.True(page.Items.Count <= 5);
        Assert.All(page.Items, entry => Assert.Equal("Employee", entry.EntityName));
    }

    [Fact]
    public async Task Search_ReturnsUnauthorized_WithoutAToken()
    {
        using var anonymous = factory.CreateClient();

        var response = await anonymous.GetAsync("/api/audit");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_IsForbidden_ForAnOperator()
    {
        using var operatorClient = await factory.CreateOperatorClientAsync();

        var response = await operatorClient.GetAsync("/api/audit");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Search_RejectsAnImpossiblePageSize()
    {
        var response = await _client.GetAsync("/api/audit?pageSize=5000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_RejectsAnInvertedDateRange()
    {
        var response = await _client.GetAsync("/api/audit?from=2026-07-01T00:00:00Z&to=2026-06-01T00:00:00Z");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatingAnEmployee_IsRecordedWithTheSignedInUser()
    {
        var created = await (await _client.PostAsJsonAsync(
                "/api/employees",
                new EmployeeRequest("Audited Person", 1m, 10, null, true, true)))
            .Content.ReadFromJsonAsync<EmployeeDto>();

        Assert.NotNull(created);

        var history = await _client.GetFromJsonAsync<PagedResult<AuditEntryDto>>(
            $"/api/audit/employees/{created.Id}");

        Assert.NotNull(history);

        var entry = Assert.Single(history.Items);

        Assert.Equal("Created", entry.Action);
        Assert.Equal(TimeReadyApiFactory.AdminEmail, entry.UserName);
        Assert.NotNull(entry.NewValues);
        Assert.Equal("Audited Person", entry.NewValues["FullName"]);
    }

    [Fact]
    public async Task UpdatingAnEmployee_RecordsOnlyTheChangedColumns()
    {
        var created = await (await _client.PostAsJsonAsync(
                "/api/employees",
                new EmployeeRequest("History Person", 0m, 10, null, false, false)))
            .Content.ReadFromJsonAsync<EmployeeDto>();

        Assert.NotNull(created);

        var update = await _client.PutAsJsonAsync(
            $"/api/employees/{created.Id}",
            new EmployeeRequest("History Person", 0m, 10, null, true, false));

        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var history = await _client.GetFromJsonAsync<PagedResult<AuditEntryDto>>(
            $"/api/audit/employees/{created.Id}");

        Assert.NotNull(history);

        var entry = history.Items.First();

        Assert.Equal("Updated", entry.Action);
        Assert.NotNull(entry.ChangedColumns);
        Assert.Equal("ManagerInformed", Assert.Single(entry.ChangedColumns));
    }

    [Fact]
    public async Task Search_CanFilterByAction()
    {
        var page = await _client.GetFromJsonAsync<PagedResult<AuditEntryDto>>("/api/audit?action=Created");

        Assert.NotNull(page);
        Assert.All(page.Items, entry => Assert.Equal("Created", entry.Action));
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForAnUnknownEntry()
    {
        var response = await _client.GetAsync("/api/audit/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
