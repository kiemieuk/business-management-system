using BMS.BuildingBlocks.Persistence.Repositories;
using BMS.Identity.Domain.Roles;
using BMS.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace BMS.Identity.Persistence.Repositories;

internal sealed class UserRepository(IdentityDbContext context) : EfRepository<User, IdentityDbContext>(context), IUserRepository
{
    private IQueryable<User> UsersWithRoles => Set.Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

    public override Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        UsersWithRoles.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public override async Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default) =>
        await UsersWithRoles.AsSplitQuery().ToListAsync(cancellationToken);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = User.Normalize(username);
        return UsersWithRoles.FirstOrDefaultAsync(u => u.NormalizedUsername == normalized, cancellationToken);
    }

    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = User.Normalize(username);
        return Set.AnyAsync(u => u.NormalizedUsername == normalized, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLower();
        return Set.AnyAsync(u => u.Email.ToLower() == normalized && u.Id != excludeUserId, cancellationToken);
    }

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) => Set.AnyAsync(cancellationToken);

    public Task<int> CountActiveUsersInRoleAsync(string roleName, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var normalized = Role.Normalize(roleName);
        return Set.CountAsync(
            u => u.IsActive && u.Id != excludeUserId && u.UserRoles.Any(ur => ur.Role.NormalizedName == normalized),
            cancellationToken);
    }
}
