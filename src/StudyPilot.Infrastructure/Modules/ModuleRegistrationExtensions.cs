using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StudyPilot.Infrastructure.Modules;

/// <summary>Composition helpers used by the API host to wire modules in and out.</summary>
public static class ModuleRegistrationExtensions
{
    /// <summary>Registers each module's services and exposes the set as a <see cref="ModuleRegistry"/>.</summary>
    public static IServiceCollection AddModules(
        this IServiceCollection services,
        IConfiguration configuration,
        params IModule[] modules)
    {
        ArgumentNullException.ThrowIfNull(modules);

        var duplicates = modules
            .GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                $"Duplicate module names registered: {string.Join(", ", duplicates)}.");
        }

        foreach (var module in modules)
        {
            module.RegisterServices(services, configuration);
        }

        services.AddSingleton(new ModuleRegistry(modules));
        return services;
    }

    /// <summary>Maps every registered module's endpoints.</summary>
    public static IEndpointRouteBuilder MapModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var registry = endpoints.ServiceProvider.GetRequiredService<ModuleRegistry>();

        foreach (var module in registry.Modules)
        {
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }
}
