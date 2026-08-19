using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StudyPilot.Modules.Identity.Authentication;
using StudyPilot.SharedKernel.Security;

namespace StudyPilot.Modules.Identity.Endpoints;

/// <summary>Sign-in and current-account endpoints.</summary>
public static class AuthenticationEndpoints
{
    public sealed record LoginRequest(string? Email, string? Password);

    public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAtUtc);

    public sealed record CurrentUserResponse(Guid UserId, string Email);

    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/identity").WithTags("Identity");

        group.MapPost("/login", async (
                LoginRequest request,
                IAuthenticationService authentication,
                CancellationToken cancellationToken) =>
            {
                var result = await authentication.AuthenticateAsync(
                    request.Email, request.Password, cancellationToken);

                // One response shape for every failure: an unknown account, a wrong password and a
                // deactivated account must be indistinguishable to the caller.
                return result.IsSuccess
                    ? Results.Ok(new LoginResponse(result.Value.Value, result.Value.ExpiresAtUtc))
                    : Results.Problem(
                        title: "Authentication failed",
                        detail: result.Error!.Message,
                        statusCode: StatusCodes.Status401Unauthorized);
            })
            .AllowAnonymous()
            .WithName("Login");

        group.MapGet("/me", (ICurrentUser currentUser, HttpContext context) =>
            {
                var email = context.User.FindFirst("email")?.Value ?? string.Empty;

                return Results.Ok(new CurrentUserResponse(currentUser.RequireUserId(), email));
            })
            .RequireAuthorization()
            .WithName("CurrentUser");

        return endpoints;
    }
}
