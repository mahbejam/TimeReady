using System.Net;
using System.Net.Http.Json;
using TimeReady.Api.Dtos;
using TimeReady.Api.Dtos.Auditing;
using Xunit;

namespace TimeReady.Tests.Integration;

public class AuditRetentionEndpointsTests(TimeReadyApiFactory factory)
    : IClassFixture<TimeReadyApiFactory>, IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync() => _client = await factory.CreateAdminClientAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Overview_ReportsTheConfiguredPolicyAndTheTableSizes()
    {
        var overview = await _client.GetFromJsonAsync<AuditRetentionOverviewDto>("/api/audit/retention");

        Assert.NotNull(overview);
        Assert.True(overview.Policy.Enabled);
        Assert.Equal(90, overview.Policy.RetentionDays);
        Assert.False(overview.Policy.PurgeEnabled);
        Assert.True(overview.LiveEntryCount >= 5);
        Assert.Equal(0, overview.ArchivedEntryCount);
    }

    [Fact]
    public async Task Overview_ReturnsUnauthorized_WithoutAToken()
    {
        using var anonymous = factory.CreateClient();

        var response = await anonymous.GetAsync("/api/audit/retention");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Overview_IsForbidden_ForAnOperator()
    {
        using var operatorClient = await factory.CreateOperatorClientAsync();

        var response = await operatorClient.GetAsync("/api/audit/retention");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Run_ReportsWhatItDid_AndUpdatesTheStatus()
    {
        var response = await _client.PostAsync("/api/audit/retention/run", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AuditRetentionRunDto>();

        Assert.NotNull(result);
        Assert.False(result.Skipped);
        Assert.Null(result.PurgeCutoffUtc);

        // Nothing is 90 days old in a database that was created a moment ago.
        Assert.Equal(0, result.Archived);
        Assert.Equal(0, result.Purged);

        var overview = await _client.GetFromJsonAsync<AuditRetentionOverviewDto>("/api/audit/retention");

        Assert.NotNull(overview);
        Assert.True(overview.Status.RunCount >= 1);
        Assert.Null(overview.Status.LastError);
        Assert.NotNull(overview.Status.LastSuccessAtUtc);
    }

    [Fact]
    public async Task Run_IsForbidden_ForAnOperator()
    {
        using var operatorClient = await factory.CreateOperatorClientAsync();

        var response = await operatorClient.PostAsync("/api/audit/retention/run", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Archive_ReturnsAnEmptyPage_WhileNothingHasBeenArchived()
    {
        var page = await _client.GetFromJsonAsync<PagedResult<ArchivedAuditEntryDto>>(
            "/api/audit/archive?page=1&pageSize=10");

        Assert.NotNull(page);
        Assert.Empty(page.Items);
        Assert.Equal(1, page.Page);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(0, page.TotalCount);
        Assert.False(page.HasNextPage);
    }

    [Fact]
    public async Task Archive_RejectsAnImpossiblePageSize()
    {
        var response = await _client.GetAsync("/api/audit/archive?pageSize=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Archive_IsForbidden_ForAnOperator()
    {
        using var operatorClient = await factory.CreateOperatorClientAsync();

        var response = await operatorClient.GetAsync("/api/audit/archive");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Health_IncludesTheRetentionCheck()
    {
        using var anonymous = factory.CreateClient();

        var response = await anonymous.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("audit-retention", body);
    }
}
