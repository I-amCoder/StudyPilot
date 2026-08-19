using Microsoft.AspNetCore.Identity;
using StudyPilot.Modules.Identity.Domain;

namespace StudyPilot.Modules.Identity.Authentication;

/// <summary>
/// Wraps the ASP.NET Core password hasher (PBKDF2, per-password salt, iteration count carried in
/// the hash). Rolling our own here would be a needless cryptographic risk.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    // Hashing a throwaway password gives a realistic hash to compare against when an account does
    // not exist, so a failed login costs the same time either way.
    private static readonly Lazy<string> DummyHash =
        new(() => new PasswordHasher<User>().HashPassword(null!, "not-a-real-password"));

    public static string DummyHashForTimingParity => DummyHash.Value;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return _inner.HashPassword(null!, password);
    }

    public PasswordVerificationOutcome Verify(string hash, string providedPassword)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(providedPassword))
        {
            return PasswordVerificationOutcome.Failed;
        }

        return _inner.VerifyHashedPassword(null!, hash, providedPassword) switch
        {
            PasswordVerificationResult.Success => PasswordVerificationOutcome.Succeeded,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerificationOutcome.SucceededButNeedsRehash,
            _ => PasswordVerificationOutcome.Failed,
        };
    }
}
