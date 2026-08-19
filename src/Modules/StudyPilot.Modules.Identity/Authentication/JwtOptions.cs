using System.ComponentModel.DataAnnotations;

namespace StudyPilot.Modules.Identity.Authentication;

/// <summary>Token settings bound from the "Identity:Jwt" configuration section.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Identity:Jwt";

    /// <summary>Minimum key length for HMAC-SHA256. Shorter keys weaken the signature.</summary>
    public const int MinimumSigningKeyLength = 32;

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Signing secret. Supplied per environment and never committed — there is deliberately no
    /// default, so a missing key stops startup instead of silently signing with a known value.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [MinLength(MinimumSigningKeyLength)]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenLifetimeMinutes { get; set; } = 60;

    /// <summary>Tolerance for clock differences when validating a token's lifetime.</summary>
    [Range(0, 300)]
    public int ClockSkewSeconds { get; set; } = 30;
}
