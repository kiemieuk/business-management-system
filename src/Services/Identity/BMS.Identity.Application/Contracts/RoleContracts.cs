using System.ComponentModel.DataAnnotations;
using BMS.Identity.Domain.Roles;

namespace BMS.Identity.Application.Contracts;

public sealed record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    IReadOnlyList<string> Permissions,
    int UserCount);

public sealed record CreateRoleRequest(
    [Required, StringLength(Role.NameMaxLength, MinimumLength = 2)] string Name,
    [StringLength(Role.DescriptionMaxLength)] string? Description,
    IReadOnlyList<string>? Permissions);

public sealed record UpdateRoleRequest(
    [Required, StringLength(Role.NameMaxLength, MinimumLength = 2)] string Name,
    [StringLength(Role.DescriptionMaxLength)] string? Description);

public sealed record SetRolePermissionsRequest([Required] IReadOnlyList<string> Permissions);

public sealed record PermissionDto(string Code, string Name, string Group, string? Description);
