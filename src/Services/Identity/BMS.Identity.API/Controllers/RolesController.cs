using BMS.BuildingBlocks.Contracts.Authorization;
using BMS.BuildingBlocks.Web.Authorization;
using BMS.Identity.Application.Contracts;
using BMS.Identity.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BMS.Identity.API.Controllers;

[ApiController]
[Route("api/identity/roles")]
public sealed class RolesController(IRoleService roleService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Identity.Roles.View)]
    public Task<IReadOnlyList<RoleDto>> List(CancellationToken cancellationToken) => roleService.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Identity.Roles.View)]
    public Task<RoleDto> Get(Guid id, CancellationToken cancellationToken) => roleService.GetAsync(id, cancellationToken);

    [HttpPost]
    [HasPermission(Permissions.Identity.Roles.Create)]
    [ProducesResponseType<RoleDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RoleDto>> Create(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await roleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = role.Id }, role);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Identity.Roles.Update)]
    public Task<RoleDto> Update(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken) =>
        roleService.UpdateAsync(id, request, cancellationToken);

    [HttpPut("{id:guid}/permissions")]
    [HasPermission(Permissions.Identity.Roles.Update)]
    public Task<RoleDto> SetPermissions(Guid id, SetRolePermissionsRequest request, CancellationToken cancellationToken) =>
        roleService.SetPermissionsAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Identity.Roles.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await roleService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
