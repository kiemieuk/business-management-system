using BMS.BuildingBlocks.Contracts.Authorization;
using BMS.BuildingBlocks.Web.Authorization;
using BMS.Identity.Application.Contracts;
using BMS.Identity.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BMS.Identity.API.Controllers;

[ApiController]
[Route("api/identity/users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Identity.Users.View)]
    public Task<IReadOnlyList<UserDto>> List(CancellationToken cancellationToken) => userService.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Identity.Users.View)]
    public Task<UserDto> Get(Guid id, CancellationToken cancellationToken) => userService.GetAsync(id, cancellationToken);

    [HttpPost]
    [HasPermission(Permissions.Identity.Users.Create)]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Identity.Users.Update)]
    public Task<UserDto> Update(Guid id, UpdateUserRequest request, CancellationToken cancellationToken) =>
        userService.UpdateAsync(id, request, cancellationToken);

    [HttpPut("{id:guid}/roles")]
    [HasPermission(Permissions.Identity.Users.Update)]
    public Task<UserDto> AssignRoles(Guid id, AssignRolesRequest request, CancellationToken cancellationToken) =>
        userService.AssignRolesAsync(id, request, cancellationToken);

    [HttpPost("{id:guid}/reset-password")]
    [HasPermission(Permissions.Identity.Users.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await userService.ResetPasswordAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Identity.Users.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await userService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
