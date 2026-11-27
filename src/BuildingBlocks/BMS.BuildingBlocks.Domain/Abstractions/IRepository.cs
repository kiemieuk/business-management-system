using BMS.BuildingBlocks.Domain.Entities;

namespace BMS.BuildingBlocks.Domain.Abstractions;

/// <summary>
/// Generic repository contract for aggregate roots. Implemented by the persistence layer (Entity Framework Core).
/// </summary>
public interface IRepository<TEntity> where TEntity : Entity, IAggregateRoot
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}
