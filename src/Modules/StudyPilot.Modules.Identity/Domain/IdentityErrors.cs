using StudyPilot.SharedKernel.Results;

namespace StudyPilot.Modules.Identity.Domain;

/// <summary>Failure reasons for the Identity module. Codes are stable; messages are not.</summary>
public static class IdentityErrors
{
    public static readonly Error EmailRequired = new("identity.email.required", "An email address is required.");

    public static readonly Error EmailTooLong = new("identity.email.too_long", "The email address is too long.");

    public static readonly Error EmailInvalid = new("identity.email.invalid", "The email address is not valid.");

    public static readonly Error PasswordTooShort = new("identity.password.too_short", "The password is too short.");

    public static readonly Error EmailAlreadyRegistered =
        new("identity.email.already_registered", "That email address is already registered.");

    /// <summary>
    /// Deliberately identical for an unknown account, a wrong password and a deactivated account.
    /// Distinguishing them would let an attacker enumerate which addresses have accounts.
    /// </summary>
    public static readonly Error InvalidCredentials =
        new("identity.credentials.invalid", "The email address or password is incorrect.");
}
