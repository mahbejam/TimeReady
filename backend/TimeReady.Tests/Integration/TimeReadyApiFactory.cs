using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using TimeReady.Api.Data;
using TimeReady.Api.Dtos.Auth;

namespace TimeReady.Tests.Integration;

/// <summary>
/// Boots the real API against a throwaway PostgreSQL database. The application
/// applies its migrations and seeds demo data and accounts on startup, so the
/// tests run against the same provider and the same code path as production.
/// <para>
/// A server has to be reachable. The development compose stack publishes one on
/// localhost:5432 (<c>docker compose up -d db</c>); CI starts one as a service.
/// Override the server with the <c>TIMEREADY_TEST_DB</c> environment variable.
/// </para>
/// </summary>
public class TimeReadyApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Email of the seeded administrator.</summary>
    public const string AdminEmail = "admin@timeready.test";

    /// <summary>Password of the seeded administrator.</summary>
    public const string AdminPassword = "Admin#Test2026";

    /// <summary>Email of the seeded operator.</summary>
    public const string OperatorEmail = "operator@timeready.test";

    /// <summary>Password of the seeded operator.</summary>
    public const string OperatorPassword = "Operator#Test2026";

    private const string DefaultServer = "Host=localhost;Port=5432;Username=timeready;Password=timeready";

    /// <summary>Each factory gets its own database so test classes cannot collide.</summary>
    private readonly string _databaseName = $"timeready_tests_{Guid.NewGuid():N}";

    private string ConnectionString
    {
        get
        {
            var server = Environment.GetEnvironmentVariable("TIMEREADY_TEST_DB") ?? DefaultServer;

            return $"{server.TrimEnd(';')};Database={_databaseName};Include Error Detail=true";
        }
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Each integration test class gets its own host. Reset Serilog's static
        // logger so a previous fixture does not leave it in a frozen state.
        Log.CloseAndFlush();

        // Production skips appsettings.Development.json, whose signing key would
        // otherwise disagree with the integration-test key injected below.
        builder.UseEnvironment("Production");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TimeReadyDb"] = ConnectionString,
                ["Cors:AllowedOrigins:0"] = "http://localhost:4200",
                ["Jwt:SigningKey"] = "integration-test-signing-key-not-a-secret-0123456789",
                ["Jwt:AccessTokenMinutes"] = "30",
                ["Jwt:RefreshTokenDays"] = "7",
                ["IdentitySeed:Admin:Email"] = AdminEmail,
                ["IdentitySeed:Admin:FullName"] = "Test Administrator",
                ["IdentitySeed:Admin:Password"] = AdminPassword,
                ["IdentitySeed:Operator:Email"] = OperatorEmail,
                ["IdentitySeed:Operator:FullName"] = "Test Operator",
                ["IdentitySeed:Operator:Password"] = OperatorPassword,
                // The retention policy stays enabled so the endpoints behave as
                // configured, but the background job is pushed far enough out
                // that it never competes with a test run.
                ["AuditRetention:Enabled"] = "true",
                ["AuditRetention:InitialDelaySeconds"] = "3600",
                ["AuditRetention:RetentionDays"] = "90",
                ["AuditRetention:PurgeEnabled"] = "false"
            });
        });
    }

    /// <summary>Signs in and returns the issued tokens.</summary>
    public async Task<AuthResponse> LoginAsync(string email, string password)
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();

        var tokens = await response.Content.ReadFromJsonAsync<AuthResponse>();

        return tokens ?? throw new InvalidOperationException("The login response was empty.");
    }

    /// <summary>A client that sends the administrator's bearer token.</summary>
    public Task<HttpClient> CreateAdminClientAsync() => CreateAuthenticatedClientAsync(AdminEmail, AdminPassword);

    /// <summary>A client that sends the operator's bearer token.</summary>
    public Task<HttpClient> CreateOperatorClientAsync() =>
        CreateAuthenticatedClientAsync(OperatorEmail, OperatorPassword);

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var tokens = await LoginAsync(email, password);
        var client = CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        return client;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DropDatabase();
        }

        base.Dispose(disposing);
    }

    private void DropDatabase()
    {
        try
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Pooled connections would keep the database busy and block the drop.
            NpgsqlConnection.ClearAllPools();
            context.Database.EnsureDeleted();
        }
        catch (Exception exception)
        {
            // A leftover test database is untidy but must not turn a green run red.
            // Write to the console so CI logs still show the cleanup failure.
            Console.Error.WriteLine(
                $"Failed to drop integration-test database '{_databaseName}': {exception.Message}");
        }
    }
}
