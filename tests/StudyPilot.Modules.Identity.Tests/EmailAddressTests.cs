using StudyPilot.Modules.Identity.Domain;

namespace StudyPilot.Modules.Identity.Tests;

public class EmailAddressTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("first.last@sub.example.co.uk")]
    [InlineData("user+tag@example.com")]
    public void Accepts_valid_addresses(string input)
    {
        var result = EmailAddress.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(input, result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    [InlineData("no@domain")]
    [InlineData("two@@example.com")]
    [InlineData("spaces in@example.com")]
    public void Rejects_invalid_addresses(string? input)
    {
        Assert.True(EmailAddress.Create(input).IsFailure);
    }

    [Fact]
    public void Rejects_addresses_beyond_the_maximum_length()
    {
        var tooLong = new string('a', EmailAddress.MaxLength) + "@example.com";

        var result = EmailAddress.Create(tooLong);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.EmailTooLong.Code, result.Error!.Code);
    }

    [Fact]
    public void Normalises_case_so_one_address_cannot_become_two_accounts()
    {
        var lower = EmailAddress.Create("user@example.com").Value;
        var mixed = EmailAddress.Create("User@Example.COM").Value;

        Assert.Equal(lower.Normalized, mixed.Normalized);
        Assert.NotEqual(lower.Value, mixed.Value);   // the address as entered is preserved
    }

    [Fact]
    public void Trims_surrounding_whitespace()
    {
        var result = EmailAddress.Create("  user@example.com  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("user@example.com", result.Value.Value);
    }
}
