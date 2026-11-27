using BMS.BuildingBlocks.Domain.Abstractions;
using BMS.BuildingBlocks.Domain.Exceptions;
using BMS.Identity.Domain.Roles;
using BMS.Identity.Domain.Users;

namespace BMS.Identity.Application.Security;

/// <summary>
/// Prevents privilege escalation by delegated administrators: a caller who is not an Administrator may only
/// grant, or manage holders of, permissions that the caller already holds. The caller's roles and permissions
/// are read from the database (not from the possibly stale token claims).
/// </summary>
public sealed class PrivilegeGuard(IUserRepository users, ICurrentUser currentUser)
{
    private Caller? _caller;

    public async Task EnsureCanManageUserAsync(User target, CancellationToken cancellationToken = default)
    {
        var caller = await GetCallerAsync(cancellationToken);
        if (caller.IsAdministrator)
        {
            return;
        }

        if (IsAdministrator(target))
        {
            throw new ForbiddenException("Only administrators can manage administrator accounts.");
        }

        if (!PermissionsOf(target).IsSubsetOf(caller.Permissions))
        {
            throw new ForbiddenException("You cannot manage a user who has permissions that you do not have.");
        }
    }

    public async Task EnsureCanGrantRolesAsync(IEnumerable<Role> roles, CancellationToken cancellationToken = default)
    {
        var caller = await GetCallerAsync(cancellationToken);
        if (caller.IsAdministrator)
        {
            return;
        }

        var list = roles.ToList();
        if (list.Any(r => r.Name == Role.AdministratorRoleName))
        {
            throw new ForbiddenException("Only administrators can grant the Administrator role.");
        }

        if (!list.SelectMany(r => r.Permissions).Select(p => p.PermissionCode).ToHashSet().IsSubsetOf(caller.Permissions))
        {
            throw new ForbiddenException("You cannot grant a role that has permissions that you do not have.");
        }
    }

    public async Task EnsureCanManageRoleAsync(Role role, CancellationToken cancellationToken = default) =>
        await EnsureCanGrantPermissionsAsync(role.Permissions.Select(p => p.PermissionCode), cancellationToken);

    public async Task EnsureCanGrantPermissionsAsync(IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        var caller = await GetCallerAsync(cancellationToken);
        if (!caller.IsAdministrator && !permissionCodes.ToHashSet().IsSubsetOf(caller.Permissions))
        {
            throw new ForbiddenException("You cannot grant or change permissions that you do not have.");
        }
    }

    private static bool IsAdministrator(User user) =>
        user.UserRoles.Any(ur => ur.Role.Name == Role.AdministratorRoleName);

    private static HashSet<string> PermissionsOf(User user) =>
        [.. user.UserRoles.SelectMany(ur => ur.Role.Permissions).Select(p => p.PermissionCode)];

    private async Task<Caller> GetCallerAsync(CancellationToken cancellationToken)
    {
        if (_caller is not null)
        {
            return _caller;
        }

        var actor = currentUser.UserId is { } id ? await users.GetByIdAsync(id, cancellationToken) : null;
        _caller = actor is { IsActive: true }
            ? new Caller(IsAdministrator(actor), PermissionsOf(actor))
            : new Caller(false, []);
        return _caller;
    }

    private sealed record Caller(bool IsAdministrator, HashSet<string> Permissions);
}
