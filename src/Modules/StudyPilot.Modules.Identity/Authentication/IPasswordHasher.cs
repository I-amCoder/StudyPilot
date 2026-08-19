namespace StudyPilot.Modules.Identity.Authentication;

/// <summary>Hashes and verifies account passwords.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerificationOutcome Verify(string hash, string providedPassword);
}

/// <summary>Result of checking a password against a stored hash.</summary>
public enum PasswordVerificationOutcome
{
    Failed = 0,
    Succeeded = 1,

    /// <summary>Correct password, but stored with outdated parameters and worth rehashing.</summary>
    SucceededButNeedsRehash = 2,
}
