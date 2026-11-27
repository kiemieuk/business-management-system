namespace BMS.Identity.Domain.Roles;

/// <summary>
/// A permission (identified by its code from the permission catalogue) granted to a role.
/// </summary>
public sealed class RolePermission
{
    private RolePermission()
    {
    }

    internal RolePermission(Guid roleId, string permissionCode)
    {
        RoleId = roleId;
        PermissionCode = permissionCode;
    }

    public Guid RoleId { get; private set; }

    public string PermissionCode { get; private set; } = string.Empty;
}
