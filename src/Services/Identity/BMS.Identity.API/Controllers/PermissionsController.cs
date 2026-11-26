using BMS.BuildingBlocks.Contracts.Authorization;
using BMS.BuildingBlocks.Web.Authorization;
using BMS.Identity.Application.Contracts;
using BMS.Identity.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BMS.Identity.API.Controllers;

[ApiController]
[Route("api/identity/permissions")]
public sealed class PermissionsController(IRoleService roleService) : ControllerBase
{
    /// <summary>Lists every permission that can be granted to a role.</summary>
    [HttpGet]
    [HasPermission(Permissions.Identity.Roles.View)]
    public IReadOnlyList<PermissionDto> List() => roleService.ListPermissions();
}
