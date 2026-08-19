using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StudyPilot.Infrastructure.Modules;
using StudyPilot.Infrastructure.Persistence;
using StudyPilot.Modules.Identity.Authentication;
using StudyPilot.Modules.Identity.Domain;
using StudyPilot.Modules.Identity.Endpoints;
using StudyPilot.Modules.Identity.Persistence;

namespace StudyPilot.Modules.Identity;

/// <summary>
/// Owns users, authentication, authorization, profiles and preferences (Epic SP-1).
/// SP-144 provides the authentication foundation; profile and preferences arrive with SP-145.
/// </summary>
public sealed class IdentityModule : IModule
{
    public string Name => "Identity";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<IdentityDbContext>(IdentityDbContext.SchemaName);

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            // A missing or short signing key must stop startup, not silently weaken every token.
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITokenIssuer, JwtTokenIssuer>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserAccountService, UserAccountService>();

        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddAuthorization();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/identity/ping", () => Results.Ok(new { module = "Identity" }))
            .AllowAnonymous()
            .WithTags("Identity");

        endpoints.MapAuthenticationEndpoints();

        return endpoints;
    }
}
