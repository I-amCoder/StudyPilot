using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace StudyPilot.Architecture.Tests;

/// <summary>
/// Boots the API for composition tests. Supplies a syntactically valid connection string so
/// options validation passes; these tests never open a connection. Behaviour against a real
/// database lives in the infrastructure integration tests.
/// </summary>
public sealed class StudyPilotApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] =
                    "Host=localhost;Port=5432;Database=studypilot_unused;Username=unused;Password=unused",

                // Development configuration enables startup migration for convenience. These
                // tests assert composition only and must not reach a database.
                ["Database:MigrateOnStartup"] = "false",

                // Supplied explicitly rather than inherited from appsettings.Development.json, so
                // composition tests do not silently depend on a developer's local settings.
                ["Identity:Jwt:Issuer"] = "studypilot-architecture-tests",
                ["Identity:Jwt:Audience"] = "studypilot-architecture-tests",
                ["Identity:Jwt:SigningKey"] = "an-architecture-test-signing-key-over-32-chars",
            }));
}
