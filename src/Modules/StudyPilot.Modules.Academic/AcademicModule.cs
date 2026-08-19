using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudyPilot.Infrastructure.Modules;
using StudyPilot.Infrastructure.Persistence;
using StudyPilot.Modules.Academic.Persistence;

namespace StudyPilot.Modules.Academic;

/// <summary>
/// Owns universities, academic periods, courses and course context (Epic SP-2).
/// Present from the start so the module boundary rules are enforced against more than one
/// module — a single-module solution cannot demonstrate isolation.
/// </summary>
public sealed class AcademicModule : IModule
{
    public string Name => "Academic";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddModuleDbContext<AcademicDbContext>(AcademicDbContext.SchemaName);

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/academic/ping", () => Results.Ok(new { module = "Academic" }))
            .AllowAnonymous()
            .WithTags("Academic");

        return endpoints;
    }
}
