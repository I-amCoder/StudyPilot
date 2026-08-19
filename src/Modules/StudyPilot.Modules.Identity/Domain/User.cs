using StudyPilot.SharedKernel.Domain;

namespace StudyPilot.Modules.Identity.Domain;

/// <summary>
/// A StudyPilot account. Owns only what authentication needs — profile and preferences belong to
/// SP-145. The password hash never leaves this aggregate in plaintext form, and every credential
/// change rolls the security stamp so tokens issued earlier can be rejected.
/// </summary>
public sealed class User : Entity<Guid>, IAggregateRoot, IAuditable
{
    private User()
    {
        // EF materialisation.
        Email = string.Empty;
        NormalizedEmail = string.Empty;
        PasswordHash = string.Empty;
        SecurityStamp = string.Empty;
    }

    private User(Guid id, EmailAddress email, string passwordHash)
        : base(id)
    {
        Email = email.Value;
        NormalizedEmail = email.Normalized;
        PasswordHash = passwordHash;
        SecurityStamp = NewSecurityStamp();
        IsActive = true;
    }

    public string Email { get; private set; }

    /// <summary>Upper-cased address used for lookup and the uniqueness constraint.</summary>
    public string NormalizedEmail { get; private set; }

    public string PasswordHash { get; private set; }

    /// <summary>Changes whenever credentials change, so previously issued tokens can be rejected.</summary>
    public string SecurityStamp { get; private set; }

    /// <summary>A deactivated account cannot authenticate, but its data is retained.</summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    /// <summary>
    /// Creates an account. The caller hashes the password; this aggregate never sees plaintext.
    /// </summary>
    public static User Create(EmailAddress email, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        // Version 7 GUIDs are time-ordered, which keeps the primary-key index from fragmenting.
        return new User(Guid.CreateVersion7(), email, passwordHash);
    }

    public void ChangePassword(string newPasswordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);

        PasswordHash = newPasswordHash;
        SecurityStamp = NewSecurityStamp();
    }

    /// <summary>
    /// Replaces the stored hash after a successful login where the hash used outdated parameters.
    /// The security stamp deliberately does not change: the credentials are the same, so other
    /// sessions must not be invalidated by a maintenance rehash.
    /// </summary>
    public void UpgradePasswordHash(string rehashed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rehashed);

        PasswordHash = rehashed;
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;

    private static string NewSecurityStamp() => Guid.NewGuid().ToString("N");
}
