namespace BMS.BuildingBlocks.Web.Authentication;

/// <summary>
/// Claim types issued in BMS access tokens.
/// </summary>
public static class BmsClaimTypes
{
    public const string UserId = "sub";
    public const string UserName = "name";
    public const string Email = "email";
    public const string Role = "role";
    public const string Permission = BMS.BuildingBlocks.Contracts.Authorization.Permissions.ClaimType;
}
