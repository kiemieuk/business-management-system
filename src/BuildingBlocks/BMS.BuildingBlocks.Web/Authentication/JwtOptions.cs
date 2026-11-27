using System.ComponentModel.DataAnnotations;

namespace BMS.BuildingBlocks.Web.Authentication;

/// <summary>
/// JWT settings shared by the token issuer (Identity service) and every resource service that validates tokens.
/// Bound from the <c>Jwt</c> configuration section.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Minimum signing key length in bytes for HMAC-SHA256.</summary>
    public const int MinimumSigningKeyLength = 32;

    [Required]
    public string Issuer { get; set; } = "bms-identity";

    [Required]
    public string Audience { get; set; } = "bms";

    /// <summary>
    /// Symmetric key used to sign/validate tokens. Must be provided through configuration
    /// (environment variable <c>Jwt__SigningKey</c>, user secrets, a vault...) and must never be committed for production.
    /// </summary>
    [Required]
    [MinLength(MinimumSigningKeyLength)]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 24 * 60)]
    public int AccessTokenLifetimeMinutes { get; set; } = 60;
}
