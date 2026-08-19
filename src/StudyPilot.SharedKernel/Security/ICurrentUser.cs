namespace StudyPilot.SharedKernel.Security;

/// <summary>
/// The account behind the current request. Modules depend on this rather than on HTTP types, so
/// user-scoped data access stays testable and cannot silently fall back to "no user".
/// </summary>
public interface ICurrentUser
{
    /// <summary>The authenticated account id, or null when the request is anonymous.</summary>
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    /// <summary>
    /// The authenticated account id, or throws when there is none. Use where a caller has already
    /// been through authorization and a missing user means a bug rather than an anonymous request.
    /// </summary>
    Guid RequireUserId();
}
