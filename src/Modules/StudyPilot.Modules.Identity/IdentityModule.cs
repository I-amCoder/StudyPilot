using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudyPilot.Infrastructure.Modules;

namespace StudyPilot.Modules.Identity;

/// <summary>
/// Owns users, authentication, authorization, profiles and preferences (Epic SP-1).
/// Behaviour arrives with SP-144 (auth foundation) and SP-145 (profile model); this type
/// establishes the boundary and the composition seam.
/// </summary>
public sealed class IdentityModule : IModule
{
    public string Name => "Identity";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration) =>
        services;

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/identity/ping", () => Results.Ok(new { module = "Identity" }))
            .WithTags("Identity");

        return endpoints;
    }
}
