namespace BMS.BuildingBlocks.Domain.Abstractions;

/// <summary>
/// Information about the user executing the current request.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    string? UserName { get; }

    bool IsAuthenticated { get; }
}
