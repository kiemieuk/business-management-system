namespace BMS.Identity.Persistence.Seeding;

/// <summary>
/// Bound from the <c>Seed</c> configuration section. The initial administrator is only created when the
/// database contains no users and a password has been configured (e.g. environment variable <c>Seed__AdminPassword</c>).
/// </summary>
public sealed class IdentitySeedOptions
{
    public const string SectionName = "Seed";

    public string AdminUsername { get; set; } = "admin";

    public string AdminEmail { get; set; } = "admin@bms.local";

    public string AdminFullName { get; set; } = "System Administrator";

    public string? AdminPassword { get; set; }
}
