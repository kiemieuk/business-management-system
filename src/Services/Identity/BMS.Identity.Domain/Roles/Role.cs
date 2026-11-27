using BMS.BuildingBlocks.Domain.Entities;
using BMS.BuildingBlocks.Domain.Exceptions;

namespace BMS.Identity.Domain.Roles;

/// <summary>
/// A named set of permissions that can be assigned to users.
/// </summary>
public sealed class Role : AuditableEntity, IAggregateRoot
{
    public const int NameMaxLength = 64;
    public const int DescriptionMaxLength = 256;

    /// <summary>Name of the built-in role that always holds every permission.</summary>
    public const string AdministratorRoleName = "Administrator";

    private readonly List<RolePermission> _permissions = [];

    private Role()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>System roles are created by the platform and cannot be renamed or deleted.</summary>
    public bool IsSystem { get; private set; }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    public static Role Create(string name, string? description, bool isSystem = false)
    {
        var role = new Role { IsSystem = isSystem };
        role.SetName(name);
        role.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        return role;
    }

    public static string Normalize(string name) => name.Trim().ToUpperInvariant();

    public void Update(string name, string? description)
    {
        if (IsSystem && Normalize(name) != NormalizedName)
        {
            throw new DomainException($"System role '{Name}' cannot be renamed.");
        }

        SetName(name);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>Replaces the role's permissions with the given set of permission codes.</summary>
    public void SetPermissions(IEnumerable<string> permissionCodes)
    {
        var desired = permissionCodes.Select(c => c.Trim()).Where(c => c.Length > 0).ToHashSet(StringComparer.Ordinal);
        _permissions.RemoveAll(p => !desired.Contains(p.PermissionCode));
        foreach (var code in desired.Where(c => _permissions.All(p => p.PermissionCode != c)))
        {
            _permissions.Add(new RolePermission(Id, code));
        }
    }

    public void EnsureCanBeDeleted()
    {
        if (IsSystem)
        {
            throw new DomainException($"System role '{Name}' cannot be deleted.");
        }
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Role name is required.");
        }

        Name = name.Trim();
        NormalizedName = Normalize(name);
    }
}
