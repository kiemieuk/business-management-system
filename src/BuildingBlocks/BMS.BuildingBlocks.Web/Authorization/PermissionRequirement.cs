using Microsoft.AspNetCore.Authorization;

namespace BMS.BuildingBlocks.Web.Authorization;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
