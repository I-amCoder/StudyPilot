using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using StudyPilot.Modules.Identity.Authentication;
using StudyPilot.Modules.Identity.Domain;
using StudyPilot.SharedKernel.Security;

namespace StudyPilot.Modules.Identity.Tests;

public class UserTests
{
    private static User CreateUser() =>
        User.Create(EmailAddress.Create("user@example.com").Value, "initial-hash");

    [Fact]
    public void A_new_account_is_active_and_has_a_security_stamp()
    {
        var user = CreateUser();

        Assert.True(user.IsActive);
        Assert.NotEmpty(user.SecurityStamp);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Fact]
    public void Changing_the_password_rolls_the_security_stamp_so_existing_tokens_stop_working()
    {
        var user = CreateUser();
        var before = user.SecurityStamp;

        user.ChangePassword("new-hash");

        Assert.Equal("new-hash", user.PasswordHash);
        Assert.NotEqual(before, user.SecurityStamp);
    }

    [Fact]
    public void Upgrading_the_hash_keeps_the_security_stamp()
    {
        var user = CreateUser();
        var before = user.SecurityStamp;

        user.UpgradePasswordHash("rehashed");

        Assert.Equal("rehashed", user.PasswordHash);
        Assert.Equal(before, user.SecurityStamp);
    }

    [Fact]
    public void Two_accounts_never_share_a_security_stamp()
    {
        Assert.NotEqual(CreateUser().SecurityStamp, CreateUser().SecurityStamp);
    }

    [Fact]
    public void An_empty_password_hash_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            User.Create(EmailAddress.Create("user@example.com").Value, "  "));
    }
}

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void A_correct_password_verifies()
    {
        var hash = _hasher.Hash("correct horse battery staple");

        Assert.Equal(
            PasswordVerificationOutcome.Succeeded,
            _hasher.Verify(hash, "correct horse battery staple"));
    }

    [Fact]
    public void A_wrong_password_does_not_verify()
    {
        var hash = _hasher.Hash("correct horse battery staple");

        Assert.Equal(PasswordVerificationOutcome.Failed, _hasher.Verify(hash, "wrong"));
    }

    [Fact]
    public void The_same_password_hashes_differently_each_time()
    {
        // A per-password salt means identical passwords must not produce identical hashes.
        Assert.NotEqual(_hasher.Hash("same password"), _hasher.Hash("same password"));
    }

    [Fact]
    public void The_hash_does_not_contain_the_password()
    {
        Assert.DoesNotContain("battery", _hasher.Hash("correct horse battery staple"), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Hashing_an_empty_password_is_rejected(string password)
    {
        Assert.Throws<ArgumentException>(() => _hasher.Hash(password));
    }

    [Fact]
    public void Verifying_against_an_empty_hash_fails_rather_than_throwing()
    {
        Assert.Equal(PasswordVerificationOutcome.Failed, _hasher.Verify(string.Empty, "anything"));
    }
}

public class JwtTokenIssuerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);

    private static JwtOptions Options() => new()
    {
        Issuer = "studypilot-tests",
        Audience = "studypilot-tests",
        SigningKey = "a-test-signing-key-of-more-than-32-characters",
        AccessTokenLifetimeMinutes = 30,
    };

    [Fact]
    public void The_token_carries_the_subject_email_and_security_stamp()
    {
        var user = User.Create(EmailAddress.Create("user@example.com").Value, "hash");
        var issuer = new JwtTokenIssuer(Microsoft.Extensions.Options.Options.Create(Options()), new FixedClock(Now));

        var token = issuer.Issue(user);
        var parsed = new JsonWebTokenHandler().ReadJsonWebToken(token.Value);

        Assert.Equal(user.Id.ToString(), parsed.GetClaim("sub").Value);
        Assert.Equal(user.Email, parsed.GetClaim("email").Value);
        Assert.Equal(user.SecurityStamp, parsed.GetClaim(StudyPilotClaimTypes.SecurityStamp).Value);
        Assert.Equal(Now.AddMinutes(30), token.ExpiresAtUtc);
    }

    [Fact]
    public void Each_token_gets_a_distinct_identifier()
    {
        var user = User.Create(EmailAddress.Create("user@example.com").Value, "hash");
        var issuer = new JwtTokenIssuer(Microsoft.Extensions.Options.Options.Create(Options()), new FixedClock(Now));

        var handler = new JsonWebTokenHandler();
        var first = handler.ReadJsonWebToken(issuer.Issue(user).Value).GetClaim("jti").Value;
        var second = handler.ReadJsonWebToken(issuer.Issue(user).Value).GetClaim("jti").Value;

        Assert.NotEqual(first, second);
    }
}
