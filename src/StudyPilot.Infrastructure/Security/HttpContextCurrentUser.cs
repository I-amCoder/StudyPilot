using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using StudyPilot.SharedKernel.Security;

namespace StudyPilot.Infrastructure.Security;

/// <summary>Reads the current account from the request's authenticated principal.</summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var subject = accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? accessor.HttpContext?.User?.FindFirstValue("sub");

            return Guid.TryParse(subject, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => UserId is not null;

    public Guid RequireUserId() =>
        UserId ?? throw new InvalidOperationException(
            "No authenticated user on the current request. This code path requires authorization.");
}
