using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Testing;
using StudyPilot.Infrastructure.Modules;
using StudyPilot.Modules.Academic;
using StudyPilot.Modules.Identity;

namespace StudyPilot.Architecture.Tests;

/// <summary>
/// Verifies the API host actually composes every module it ships with. A module that exists but
/// is never registered is the silent failure this guards against.
/// </summary>
public class HostCompositionTests(StudyPilotApiFactory factory)
    : IClassFixture<StudyPilotApiFactory>
{
    private static readonly Assembly[] ModuleAssemblies =
        [typeof(IdentityModule).Assembly, typeof(AcademicModule).Assembly];

    private static string[] DeclaredModuleNames() => ModuleAssemblies
        .SelectMany(a => a.GetTypes())
        .Where(t => typeof(IModule).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
        .Select(t => ((IModule)Activator.CreateInstance(t)!).Name)
        .OrderBy(n => n)
        .ToArray();

    [Fact]
    public async Task Every_declared_module_is_registered_in_the_host()
    {
        var response = await factory.CreateClient().GetAsync("/modules");
        response.EnsureSuccessStatusCode();

        var registered = (await response.Content.ReadFromJsonAsync<string[]>() ?? [])
            .OrderBy(n => n)
            .ToArray();

        Assert.Equal(DeclaredModuleNames(), registered);
    }

    [Theory]
    [InlineData("/api/identity/ping")]
    [InlineData("/api/academic/ping")]
    public async Task Module_endpoints_are_mapped(string route)
    {
        var response = await factory.CreateClient().GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Liveness_endpoint_reports_healthy_without_a_database()
    {
        // Liveness must not depend on the database; readiness is covered by the infrastructure
        // integration tests, which run against a real PostgreSQL container.
        var response = await factory.CreateClient().GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public void Registering_two_modules_with_the_same_name_is_rejected()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddModules(configuration, new IdentityModule(), new IdentityModule()));
    }
}
