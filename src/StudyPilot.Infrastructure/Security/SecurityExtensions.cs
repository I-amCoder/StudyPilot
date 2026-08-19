using Microsoft.Extensions.DependencyInjection;
using StudyPilot.SharedKernel.Security;

namespace StudyPilot.Infrastructure.Security;

/// <summary>Registers request-scoped security services shared by every module.</summary>
public static class SecurityExtensions
{
    public static IServiceCollection AddCurrentUserAccessor(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        return services;
    }
}
