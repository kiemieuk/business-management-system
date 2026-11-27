using BMS.BuildingBlocks.Domain.Abstractions;
using BMS.BuildingBlocks.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BMS.BuildingBlocks.Persistence.Repositories;

/// <summary>
/// Generic Entity Framework Core repository providing the CRUD operations for an aggregate root.
/// Service specific repositories derive from it and add their own queries.
/// </summary>
public class EfRepository<TEntity, TContext>(TContext context) : IRepository<TEntity>
    where TEntity : Entity, IAggregateRoot
    where TContext : DbContext
{
    protected TContext Context { get; } = context;

    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    public virtual Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default) =>
        await Set.ToListAsync(cancellationToken);

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken);

    public virtual void Update(TEntity entity) => Set.Update(entity);

    public virtual void Remove(TEntity entity) => Set.Remove(entity);
}
