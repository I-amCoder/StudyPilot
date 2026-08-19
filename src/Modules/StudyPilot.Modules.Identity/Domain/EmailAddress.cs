using System.Text.RegularExpressions;
using StudyPilot.SharedKernel.Results;

namespace StudyPilot.Modules.Identity.Domain;

/// <summary>
/// A validated email address. Carries both the address as entered and a normalised form; lookups
/// and uniqueness use the normalised value so "User@Example.com" and "user@example.com" cannot
/// become two accounts.
/// </summary>
public sealed partial record EmailAddress
{
    public const int MaxLength = 256;

    private EmailAddress(string value, string normalized)
    {
        Value = value;
        Normalized = normalized;
    }

    public string Value { get; }

    public string Normalized { get; }

    public static Result<EmailAddress> Create(string? input)
    {
        var trimmed = input?.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return Result.Failure<EmailAddress>(IdentityErrors.EmailRequired);
        }

        if (trimmed.Length > MaxLength)
        {
            return Result.Failure<EmailAddress>(IdentityErrors.EmailTooLong);
        }

        if (!EmailPattern().IsMatch(trimmed))
        {
            return Result.Failure<EmailAddress>(IdentityErrors.EmailInvalid);
        }

        return Result.Success(new EmailAddress(trimmed, trimmed.ToUpperInvariant()));
    }

    public override string ToString() => Value;

    // Deliberately permissive: the authoritative check that an address exists is sending mail to
    // it, and over-strict patterns reject valid addresses.
    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();
}
