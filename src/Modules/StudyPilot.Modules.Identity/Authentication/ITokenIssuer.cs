using StudyPilot.Modules.Identity.Domain;

namespace StudyPilot.Modules.Identity.Authentication;

/// <summary>Issues access tokens for authenticated accounts.</summary>
public interface ITokenIssuer
{
    AccessToken Issue(User user);
}

/// <summary>A signed access token and the moment it stops being valid.</summary>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);
