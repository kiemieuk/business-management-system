using BMS.BuildingBlocks.Domain.Abstractions;
using Microsoft.AspNetCore.Http;

namespace BMS.BuildingBlocks.Web.Authentication;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirst(BmsClaimTypes.UserId)?.Value, out var id) ? id : null;

    public string? UserName => httpContextAccessor.HttpContext?.User.FindFirst(BmsClaimTypes.UserName)?.Value;

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
