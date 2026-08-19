using StudyPilot.Modules.Identity.Domain;
using StudyPilot.SharedKernel.Results;

namespace StudyPilot.Modules.Identity.Authentication;

/// <summary>
/// Creates accounts. SP-144 provides the mechanism; the registration endpoint and its wider
/// business rules (verification mail, terms acceptance) belong to story SP-13.
/// </summary>
public interface IUserAccountService
{
    Task<Result<User>> CreateAsync(string? email, string? password, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class UserAccountService(IUserRepository users, IPasswordHasher passwordHasher)
    : IUserAccountService
{
    /// <summary>Floor only. The full password policy is SP-13's to define.</summary>
    public const int MinimumPasswordLength = 12;

    public async Task<Result<User>> CreateAsync(
        string? email,
        string? password,
        CancellationToken cancellationToken = default)
    {
        var parsedEmail = EmailAddress.Create(email);

        if (parsedEmail.IsFailure)
        {
            return Result.Failure<User>(parsedEmail.Error!);
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumPasswordLength)
        {
            return Result.Failure<User>(IdentityErrors.PasswordTooShort);
        }

        if (await users.EmailExistsAsync(parsedEmail.Value, cancellationToken))
        {
            return Result.Failure<User>(IdentityErrors.EmailAlreadyRegistered);
        }

        var user = User.Create(parsedEmail.Value, passwordHasher.Hash(password));
        await users.AddAsync(user, cancellationToken);

        try
        {
            await users.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateEmailException)
        {
            // Two concurrent registrations pass the existence check, then the unique index rejects
            // the loser. Reported as a normal conflict rather than surfacing as a 500.
            return Result.Failure<User>(IdentityErrors.EmailAlreadyRegistered);
        }

        return Result.Success(user);
    }
}

/// <summary>Raised when the unique email constraint rejects an insert.</summary>
public sealed class DuplicateEmailException(Exception inner)
    : Exception("An account with that email address already exists.", inner);
