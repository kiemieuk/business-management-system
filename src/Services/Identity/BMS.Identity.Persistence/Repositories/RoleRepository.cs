using BMS.BuildingBlocks.Persistence.Repositories;
using BMS.Identity.Domain.Roles;
using BMS.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace BMS.Identity.Persistence.Repositories;

internal sealed class RoleRepository(IdentityDbContext context) : EfRepository<Role, IdentityDbContext>(context), IRoleRepository
{
    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = Role.Normalize(name);
        return Set.FirstOrDefaultAsync(r => r.NormalizedName == normalized, cancellationToken);
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludeRoleId = null, CancellationToken cancellationToken = default)
    {
        var normalized = Role.Normalize(name);
        return Set.AnyAsync(r => r.NormalizedName == normalized && r.Id != excludeRoleId, cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var list = ids.ToList();
        return await Set.Where(r => list.Contains(r.Id)).ToListAsync(cancellationToken);
    }

    public Task<int> CountUsersAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        Context.Set<UserRole>().CountAsync(ur => ur.RoleId == roleId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> CountUsersPerRoleAsync(CancellationToken cancellationToken = default) =>
        await Context.Set<UserRole>()
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);
}
