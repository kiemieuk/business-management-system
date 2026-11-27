using BMS.BuildingBlocks.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BMS.BuildingBlocks.Persistence;

/// <summary>
/// Base EF Core context for every microservice. Each service owns its own database (database-per-service),
/// and applies the entity configurations found in its own assembly.
/// </summary>
public abstract class BaseDbContext(DbContextOptions options) : DbContext(options), IUnitOfWork
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}
