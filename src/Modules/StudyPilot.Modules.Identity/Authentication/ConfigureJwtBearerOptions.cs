using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace StudyPilot.Modules.Identity.Authentication;

/// <summary>
/// Builds the bearer validation parameters from <see cref="JwtOptions"/>. Written as a deferred
/// configurator rather than an inline lambda so options validation runs first — otherwise a
/// missing signing key surfaces as an obscure crypto error instead of a clear startup failure.
/// </summary>
public sealed class ConfigureJwtBearerOptions(IOptions<JwtOptions> jwtOptions)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name is not null && name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }

        Configure(options);
    }

    public void Configure(JwtBearerOptions options)
    {
        var jwt = jwtOptions.Value;

        // Keep claim names exactly as issued; the default mapping rewrites "sub" and makes the
        // token's contents differ from what the issuer put in it.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds),
            NameClaimType = "sub",
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = SecurityStampValidation.ValidateAsync,
        };
    }
}
