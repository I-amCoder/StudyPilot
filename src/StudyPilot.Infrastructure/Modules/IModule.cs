using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StudyPilot.Infrastructure.Modules;

/// <summary>
/// Contract every StudyPilot module implements. A module owns its domain, its services and its
/// endpoints; the API host is the only place that knows which modules exist (ADR-001).
/// </summary>
public interface IModule
{
    /// <summary>Stable module name, used in diagnostics and the module manifest endpoint.</summary>
    string Name { get; }

    /// <summary>Registers the module's own services. Must not reach into another module.</summary>
    IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>Maps the module's HTTP endpoints.</summary>
    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}
