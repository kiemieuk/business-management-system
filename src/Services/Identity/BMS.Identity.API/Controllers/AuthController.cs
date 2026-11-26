using BMS.BuildingBlocks.Domain.Abstractions;
using BMS.Identity.Application.Contracts;
using BMS.Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BMS.Identity.API.Controllers;

[ApiController]
[Route("api/identity/auth")]
[Authorize]
public sealed class AuthController(IAuthService authService, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Signs in with username and password and returns a JWT access token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        if (response is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Sign-in failed.",
                detail: "Invalid username or password, or the account is inactive or locked.");
        }

        return response;
    }

    /// <summary>Returns the profile, roles and permissions of the signed-in user.</summary>
    [HttpGet("me")]
    public Task<UserProfileDto> Me(CancellationToken cancellationToken) =>
        authService.GetProfileAsync(RequireUserId(), cancellationToken);

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ChangePasswordAsync(RequireUserId(), request, cancellationToken);
        return NoContent();
    }

    private Guid RequireUserId() =>
        currentUser.UserId ?? throw new UnauthorizedAccessException("The access token does not contain a user id.");
}
