using BMS.BuildingBlocks.Domain.Abstractions;

namespace BMS.Identity.Domain.Users;

public interface IUserRepository : IRepository<User>
{
    /// <summary>Loads a user (including roles and their permissions) by username, case-insensitively.</summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    /// <summary>Counts active users holding the given role, optionally excluding one user.</summary>
    Task<int> CountActiveUsersInRoleAsync(string roleName, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
}
