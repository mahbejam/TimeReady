using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TimeReady.Api.Dtos;
using Xunit;

namespace TimeReady.Tests.Integration;

[Collection("Integration")]
public class EmployeesEndpointsTests(TimeReadyApiFactory factory) : IAsyncLifetime
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
    public async Task GetAll_ReturnsTheSeededEmployees()
    {
        var employees = await _client.GetFromJsonAsync<List<EmployeeDto>>("/api/employees");

        Assert.NotNull(employees);
        Assert.True(employees.Count >= 5);
        Assert.Contains(employees, e => e.FullName == "Michael Hofer");
    }

    [Fact]
    public async Task GetAll_ReturnsUnauthorized_WithoutAToken()
    {
        using var anonymous = factory.CreateClient();

        var response = await anonymous.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_IsAllowed_ForAnOperator()
    {
        var employees = await _client.GetFromJsonAsync<List<EmployeeDto>>("/api/employees");

        Assert.NotNull(employees);

        var target = employees[0];
        using var operatorClient = await factory.CreateOperatorClientAsync();

        var response = await operatorClient.PutAsJsonAsync(
            $"/api/employees/{target.Id}",
            new EmployeeRequest(
                target.FullName,
                target.TimeBalanceHours,
                target.RemainingVacationDays,
                target.VacationStartDate,
                true,
                true));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_IsForbidden_ForAnOperator()
    {
        var created = await (await _client.PostAsJsonAsync(
                "/api/employees",
                new EmployeeRequest("Role Check Person", 0m, 5, null, true, true)))
            .Content.ReadFromJsonAsync<EmployeeDto>();

        Assert.NotNull(created);

        using var operatorClient = await factory.CreateOperatorClientAsync();

        var response = await operatorClient.DeleteAsync($"/api/employees/{created.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForUnknownId()
    {
        var response = await _client.GetAsync("/api/employees/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_StoresTheEmployeeAndReturnsItsLocation()
    {
        var request = new EmployeeRequest(
            "Integration Test Person",
            -3.5m,
            8,
            DateOnly.FromDateTime(DateTime.Today).AddDays(20),
            true,
            false);

        var response = await _client.PostAsJsonAsync("/api/employees", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<EmployeeDto>();

        Assert.NotNull(created);
        Assert.Equal("Integration Test Person", created.FullName);
        Assert.True(created.Id > 0);

        var reloaded = await _client.GetFromJsonAsync<EmployeeDto>($"/api/employees/{created.Id}");

        Assert.NotNull(reloaded);
        Assert.Equal(created.Id, reloaded.Id);
        Assert.Equal(-3.5m, reloaded.TimeBalanceHours);
    }

    [Fact]
    public async Task Create_ReturnsValidationProblem_WhenTheNameIsMissing()
    {
        var request = new EmployeeRequest(string.Empty, 0m, 5, null, false, false);

        var response = await _client.PostAsJsonAsync("/api/employees", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains(problem.Errors, error => error.Key.Contains("FullName", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Update_ReturnsNotFound_ForUnknownId()
    {
        var request = new EmployeeRequest("Ghost Employee", 0m, 5, null, true, true);

        var response = await _client.PutAsJsonAsync("/api/employees/9999", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTheEmployee()
    {
        var request = new EmployeeRequest("Temporary Person", 0m, 5, null, true, true);
        var created = await (await _client.PostAsJsonAsync("/api/employees", request))
            .Content.ReadFromJsonAsync<EmployeeDto>();

        Assert.NotNull(created);

        var deleteResponse = await _client.DeleteAsync($"/api/employees/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/employees/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
