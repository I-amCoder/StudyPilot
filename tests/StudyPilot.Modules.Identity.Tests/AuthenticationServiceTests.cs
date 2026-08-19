using Microsoft.Extensions.Options;
using StudyPilot.Modules.Identity.Authentication;
using StudyPilot.Modules.Identity.Domain;

namespace StudyPilot.Modules.Identity.Tests;

/// <summary>Credential verification: the success path and, more importantly, the failure paths.</summary>
public class AuthenticationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);

    private static JwtTokenIssuer Issuer() =>
        new(Options.Create(new JwtOptions
        {
            Issuer = "studypilot-tests",
            Audience = "studypilot-tests",
            SigningKey = "a-test-signing-key-of-more-than-32-characters",
            AccessTokenLifetimeMinutes = 60,
        }), new FixedClock(Now));

    private static User CreateUser(string email = "user@example.com", string hash = "hashed:correct") =>
        User.Create(EmailAddress.Create(email).Value, hash);

    [Fact]
    public async Task Valid_credentials_produce_a_token()
    {
        var repository = new FakeUserRepository();
        await repository.AddAsync(CreateUser());

        var service = new AuthenticationService(
            repository,
            new StubPasswordHasher(PasswordVerificationOutcome.Succeeded),
            Issuer());

        var result = await service.AuthenticateAsync("user@example.com", "correct");

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Value);
        Assert.Equal(Now.AddMinutes(60), result.Value.ExpiresAtUtc);
    }

    [Fact]
    public async Task An_unknown_account_and_a_wrong_password_fail_identically()
    {
        // Different messages would let an attacker enumerate which addresses have accounts.
        var withUser = new FakeUserRepository();
        await withUser.AddAsync(CreateUser());

        var unknown = await new AuthenticationService(
            new FakeUserRepository(),
            new StubPasswordHasher(PasswordVerificationOutcome.Failed),
            Issuer()).AuthenticateAsync("nobody@example.com", "whatever");

        var wrongPassword = await new AuthenticationService(
            withUser,
            new StubPasswordHasher(PasswordVerificationOutcome.Failed),
            Issuer()).AuthenticateAsync("user@example.com", "wrong");

        Assert.True(unknown.IsFailure);
        Assert.True(wrongPassword.IsFailure);
        Assert.Equal(unknown.Error!.Code, wrongPassword.Error!.Code);
        Assert.Equal(IdentityErrors.InvalidCredentials.Code, unknown.Error.Code);
    }

    [Fact]
    public async Task A_malformed_address_fails_as_bad_credentials_not_as_validation()
    {
        var result = await new AuthenticationService(
            new FakeUserRepository(),
            new StubPasswordHasher(PasswordVerificationOutcome.Failed),
            Issuer()).AuthenticateAsync("not-an-email", "whatever");

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidCredentials.Code, result.Error!.Code);
    }

    [Fact]
    public async Task An_unknown_account_still_verifies_a_hash_so_timing_does_not_leak()
    {
        var hasher = new StubPasswordHasher(PasswordVerificationOutcome.Failed);

        await new AuthenticationService(new FakeUserRepository(), hasher, Issuer())
            .AuthenticateAsync("nobody@example.com", "whatever");

        Assert.Equal(1, hasher.VerifyCallCount);
    }

    [Fact]
    public async Task A_deactivated_account_cannot_authenticate_even_with_the_right_password()
    {
        var repository = new FakeUserRepository();
        var user = CreateUser();
        user.Deactivate();
        await repository.AddAsync(user);

        var result = await new AuthenticationService(
            repository,
            new StubPasswordHasher(PasswordVerificationOutcome.Succeeded),
            Issuer()).AuthenticateAsync("user@example.com", "correct");

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidCredentials.Code, result.Error!.Code);
    }

    [Fact]
    public async Task An_outdated_hash_is_upgraded_without_invalidating_other_sessions()
    {
        // Rehashing is maintenance, not a credential change: rolling the security stamp here
        // would silently sign the user out everywhere else.
        var repository = new FakeUserRepository();
        var user = CreateUser();
        await repository.AddAsync(user);
        var stampBefore = user.SecurityStamp;

        var result = await new AuthenticationService(
            repository,
            new StubPasswordHasher(PasswordVerificationOutcome.SucceededButNeedsRehash),
            Issuer()).AuthenticateAsync("user@example.com", "correct");

        Assert.True(result.IsSuccess);
        Assert.Equal("hashed:correct", user.PasswordHash);
        Assert.Equal(stampBefore, user.SecurityStamp);
        Assert.Equal(1, repository.SaveCount);
    }
}
