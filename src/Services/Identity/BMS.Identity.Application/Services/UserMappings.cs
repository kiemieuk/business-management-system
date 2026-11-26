using BMS.Identity.Application.Contracts;
using BMS.Identity.Domain.Users;

namespace BMS.Identity.Application.Services;

internal static class UserMappings
{
    public static UserDto ToDto(User user) => new(
        user.Id,
        user.Username,
        user.Email,
        user.FullName,
        user.IsActive,
        [.. user.UserRoles.Select(ur => new RoleSummaryDto(ur.RoleId, ur.Role.Name)).OrderBy(r => r.Name)],
        user.CreatedAt,
        user.LastLoginAt);

    public static UserProfileDto ToProfile(User user) => new(
        user.Id,
        user.Username,
        user.Email,
        user.FullName,
        [.. user.UserRoles.Select(ur => ur.Role.Name).Order()],
        [.. user.UserRoles.SelectMany(ur => ur.Role.Permissions).Select(p => p.PermissionCode).Distinct().Order()]);
}
