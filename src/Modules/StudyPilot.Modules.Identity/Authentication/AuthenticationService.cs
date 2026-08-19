using StudyPilot.Modules.Identity.Domain;
using StudyPilot.SharedKernel.Results;

namespace StudyPilot.Modules.Identity.Authentication;

/// <summary>Verifies credentials and issues access tokens.</summary>
public interface IAuthenticationService
{
    Task<Result<AccessToken>> AuthenticateAsync(
        string? email,
        string? password,
        CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class AuthenticationService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenIssuer tokenIssuer) : IAuthenticationService
{
    public async Task<Result<AccessToken>> AuthenticateAsync(
        string? email,
        string? password,
        CancellationToken cancellationToken = default)
    {
        var parsedEmail = EmailAddress.Create(email);

        if (parsedEmail.IsFailure)
        {
            // A malformed address is reported as bad credentials, not as a validation error: the
            // difference would tell an attacker which addresses are worth trying.
            return Result.Failure<AccessToken>(IdentityErrors.InvalidCredentials);
        }

        var user = await users.FindByEmailAsync(parsedEmail.Value, cancellationToken);

        if (user is null)
        {
            // Verify against a throwaway hash anyway, so an unknown account takes the same time as
            // a wrong password and cannot be distinguished by timing.
            passwordHasher.Verify(PasswordHasher.DummyHashForTimingParity, password ?? string.Empty);
            return Result.Failure<AccessToken>(IdentityErrors.InvalidCredentials);
        }

        var outcome = passwordHasher.Verify(user.PasswordHash, password ?? string.Empty);

        if (outcome == PasswordVerificationOutcome.Failed || !user.IsActive)
        {
            return Result.Failure<AccessToken>(IdentityErrors.InvalidCredentials);
        }

        if (outcome == PasswordVerificationOutcome.SucceededButNeedsRehash)
        {
            user.UpgradePasswordHash(passwordHasher.Hash(password!));
            await users.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(tokenIssuer.Issue(user));
    }
}
