using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using StudyPilot.Modules.Identity.Domain;
using StudyPilot.SharedKernel.Security;

namespace StudyPilot.Modules.Identity.Authentication;

/// <summary>
/// Rejects tokens issued before the account's credentials last changed. Without this the security
/// stamp is decorative: a password change would not actually revoke tokens already handed out.
/// </summary>
public static class SecurityStampValidation
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;

        var subject = principal?.FindFirst("sub")?.Value;
        var stamp = principal?.FindFirst(StudyPilotClaimTypes.SecurityStamp)?.Value;

        if (!Guid.TryParse(subject, out var userId) || string.IsNullOrEmpty(stamp))
        {
            context.Fail("Token is missing the subject or security stamp.");
            return;
        }

        var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        var user = await users.FindByIdAsync(userId, context.HttpContext.RequestAborted);

        if (user is null || !user.IsActive)
        {
            context.Fail("The account no longer exists or is deactivated.");
            return;
        }

        if (!string.Equals(user.SecurityStamp, stamp, StringComparison.Ordinal))
        {
            context.Fail("The token was issued before the credentials changed.");
        }
    }
}
