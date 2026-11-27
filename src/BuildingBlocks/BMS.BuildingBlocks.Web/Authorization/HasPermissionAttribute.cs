using Microsoft.AspNetCore.Authorization;

namespace BMS.BuildingBlocks.Web.Authorization;

/// <summary>
/// Requires the authenticated user to hold the given permission (granted through one of their roles).
/// Usage: <c>[HasPermission(Permissions.Identity.Users.View)]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute(string permission) : AuthorizeAttribute, IAuthorizationRequirementData
{
    public string Permission { get; } = permission;

    public IEnumerable<IAuthorizationRequirement> GetRequirements() => [new PermissionRequirement(Permission)];
}
