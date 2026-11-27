namespace BMS.Identity.Application;

/// <summary>
/// Brute-force protection settings, bound from the <c>AccountSecurity</c> configuration section.
/// </summary>
public sealed class AccountSecurityOptions
{
    public const string SectionName = "AccountSecurity";

    /// <summary>Number of consecutive failed sign-ins before the account is locked. 0 disables lockout.</summary>
    public int MaxFailedAccessAttempts { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;
}
