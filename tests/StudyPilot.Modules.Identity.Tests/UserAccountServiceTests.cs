using StudyPilot.Modules.Identity.Authentication;
using StudyPilot.Modules.Identity.Domain;

namespace StudyPilot.Modules.Identity.Tests;

public class UserAccountServiceTests
{
    private const string ValidPassword = "a-sufficiently-long-password";

    private static UserAccountService Service(FakeUserRepository repository) =>
        new(repository, new StubPasswordHasher(PasswordVerificationOutcome.Succeeded));

    [Fact]
    public async Task Creates_an_account_for_valid_input()
    {
        var repository = new FakeUserRepository();

        var result = await Service(repository).CreateAsync("user@example.com", ValidPassword);

        Assert.True(result.IsSuccess);
        Assert.Equal("user@example.com", result.Value.Email);
        Assert.True(result.Value.IsActive);
        Assert.Single(repository.Users);
    }

    [Fact]
    public async Task Never_stores_the_password_in_plaintext()
    {
        // Uses the real hasher: the stub echoes its input, so it could not detect this.
        var repository = new FakeUserRepository();
        var service = new UserAccountService(repository, new PasswordHasher());

        var result = await service.CreateAsync("user@example.com", ValidPassword);

        Assert.DoesNotContain(ValidPassword, result.Value.PasswordHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rejects_a_password_below_the_minimum_length()
    {
        var tooShort = new string('x', UserAccountService.MinimumPasswordLength - 1);

        var result = await Service(new FakeUserRepository()).CreateAsync("user@example.com", tooShort);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.PasswordTooShort.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Rejects_an_invalid_address()
    {
        var result = await Service(new FakeUserRepository()).CreateAsync("nope", ValidPassword);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.EmailInvalid.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Rejects_an_address_that_is_already_registered_regardless_of_case()
    {
        var repository = new FakeUserRepository();
        await Service(repository).CreateAsync("user@example.com", ValidPassword);

        var result = await Service(repository).CreateAsync("USER@EXAMPLE.COM", ValidPassword);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.EmailAlreadyRegistered.Code, result.Error!.Code);
    }

    [Fact]
    public async Task A_concurrent_registration_losing_the_unique_index_reports_a_conflict()
    {
        // Both requests pass the existence check; the database rejects the loser. That must read
        // as a normal conflict, not as an unhandled server error.
        var repository = new FakeUserRepository { ThrowDuplicateOnSave = true };

        var result = await Service(repository).CreateAsync("user@example.com", ValidPassword);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.EmailAlreadyRegistered.Code, result.Error!.Code);
    }
}
