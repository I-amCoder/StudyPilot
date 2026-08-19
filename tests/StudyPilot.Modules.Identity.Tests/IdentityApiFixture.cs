using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudyPilot.Modules.Identity.Persistence;
using Testcontainers.PostgreSql;

namespace StudyPilot.Modules.Identity.Tests;

/// <summary>
/// Runs the real API against a real PostgreSQL container, with migrations applied. End-to-end
/// authentication is the only way to prove the bearer pipeline, the fallback authorization policy
/// and security-stamp revocation actually hold together.
/// </summary>
public sealed class IdentityApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SigningKey = "an-integration-test-signing-key-well-over-32-chars";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("studypilot_identity_tests")
        .WithUsername("studypilot")
        .WithPassword("studypilot")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = _postgres.GetConnectionString(),
                ["Database:MigrateOnStartup"] = "false",
                ["Identity:Jwt:Issuer"] = "studypilot-tests",
                ["Identity:Jwt:Audience"] = "studypilot-tests",
                ["Identity:Jwt:SigningKey"] = SigningKey,
                ["Identity:Jwt:AccessTokenLifetimeMinutes"] = "60",
            }));

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Apply the real migration rather than EnsureCreated, so the schema under test is the one
        // that will actually be deployed.
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class IdentityApiCollection : ICollectionFixture<IdentityApiFixture>
{
    public const string Name = "identity-api";
}
