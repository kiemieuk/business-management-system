using BMS.BuildingBlocks.Domain.Abstractions;

namespace BMS.Identity.Domain.Roles;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, Guid? excludeRoleId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task<int> CountUsersAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, int>> CountUsersPerRoleAsync(CancellationToken cancellationToken = default);
}
