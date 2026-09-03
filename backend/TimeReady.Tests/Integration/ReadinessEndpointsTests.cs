using System.Net;
using System.Net.Http.Json;
using TimeReady.Api.Dtos;
using TimeReady.Api.Services;
using Xunit;

namespace TimeReady.Tests.Integration;

[Collection("Integration")]
public class ReadinessEndpointsTests(TimeReadyApiFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    /// <summary>Every request needs a bearer token now, so sign in first.</summary>
    public async Task InitializeAsync() => _client = await factory.CreateAdminClientAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetAll_ReturnsAResultForEveryEmployee()
    {
        var employees = await _client.GetFromJsonAsync<List<EmployeeDto>>("/api/employees");
        var results = await _client.GetFromJsonAsync<List<ReadinessResultDto>>("/api/readiness");

        Assert.NotNull(employees);
        Assert.NotNull(results);
        Assert.Equal(employees.Count, results.Count);
        Assert.All(results, result => Assert.Equal(result.IsReady ? "Ready" : "Not Ready", result.Status));
    }

    [Fact]
    public async Task GetByEmployeeId_ReturnsNotFound_ForUnknownId()
    {
        var response = await _client.GetAsync("/api/readiness/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Evaluate_ReportsBlockingFindings_ForUnsavedData()
    {
        var request = new EmployeeRequest(
            "Preview Person",
            -35m,
            2,
            DateOnly.FromDateTime(DateTime.Today).AddDays(2),
            false,
            false);

        var response = await _client.PostAsJsonAsync("/api/readiness/evaluate", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ReadinessResultDto>();

        Assert.NotNull(result);
        Assert.False(result.IsReady);
        Assert.Equal("Not Ready", result.Status);
        Assert.Contains(result.Warnings, w => w.Code == ReadinessCodes.ManagerNotInformed);
        Assert.Contains(result.Warnings, w => w.Code == ReadinessCodes.HandoverIncomplete);
        Assert.Contains(result.Warnings, w => w.Code == ReadinessCodes.NegativeTimeBalance);
    }

    [Fact]
    public async Task Evaluate_ReturnsReady_WhenEverythingIsPrepared()
    {
        var request = new EmployeeRequest(
            "Prepared Person",
            2m,
            15,
            DateOnly.FromDateTime(DateTime.Today).AddDays(40),
            true,
            true);

        var response = await _client.PostAsJsonAsync("/api/readiness/evaluate", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ReadinessResultDto>();

        Assert.NotNull(result);
        Assert.True(result.IsReady);
        Assert.Empty(result.Warnings);
    }
}
