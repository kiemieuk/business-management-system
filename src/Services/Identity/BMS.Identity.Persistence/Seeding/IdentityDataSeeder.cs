using BMS.BuildingBlocks.Contracts.Authorization;
using BMS.BuildingBlocks.Persistence;
using BMS.Identity.Application.Abstractions;
using BMS.Identity.Application.Security;
using BMS.Identity.Domain.Roles;
using BMS.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BMS.Identity.Persistence.Seeding;

/// <summary>
/// Applies migrations and makes sure the built-in data exists:
/// the <c>Administrator</c> system role (always synchronised with the full permission catalogue),
/// the <c>User</c> system role, and an initial administrator account.
/// </summary>
public sealed class IdentityDataSeeder(
    IdentityDbContext context,
    IPasswordHasher passwordHasher,
    IOptions<IdentitySeedOptions> options,
    ILogger<IdentityDataSeeder> logger)
{
    public const string DefaultUserRoleName = "User";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await context.InitializeDatabaseAsync(cancellationToken);

        var administrator = await EnsureRoleAsync(Role.AdministratorRoleName, "Full access to every module.", cancellationToken);
        administrator.SetPermissions(Permissions.All.Select(p => p.Code));

        await EnsureRoleAsync(DefaultUserRoleName, "Default role for regular users.", cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await EnsureAdministratorUserAsync(administrator, cancellationToken);
    }

    private async Task<Role> EnsureRoleAsync(string name, string description, CancellationToken cancellationToken)
    {
        var normalized = Role.Normalize(name);
        var role = await context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == normalized, cancellationToken);
        if (role is null)
        {
            role = Role.Create(name, description, isSystem: true);
            context.Roles.Add(role);
            logger.LogInformation("Created system role {Role}", name);
        }

        return role;
    }

    private async Task EnsureAdministratorUserAsync(Role administrator, CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var seed = options.Value;
        if (string.IsNullOrWhiteSpace(seed.AdminPassword))
        {
            logger.LogWarning(
                "No users exist and no '{Section}:AdminPassword' is configured; the initial administrator was not created.",
                IdentitySeedOptions.SectionName);
            return;
        }

        PasswordPolicy.Validate(seed.AdminPassword, $"{IdentitySeedOptions.SectionName}:AdminPassword");

        var admin = User.Create(seed.AdminUsername, seed.AdminEmail, seed.AdminFullName, passwordHasher.Hash(seed.AdminPassword));
        admin.SetRoles([administrator]);
        context.Users.Add(admin);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created initial administrator account {Username}", seed.AdminUsername);
    }
}
