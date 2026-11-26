using BMS.Identity.Domain.Roles;

namespace BMS.Identity.Domain.Users;

/// <summary>
/// Many-to-many link between users and roles.
/// </summary>
public sealed class UserRole
{
    private UserRole()
    {
    }

    internal UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public Role Role { get; private set; } = null!;
}
