using BMS.BuildingBlocks.Persistence;
using BMS.Identity.Domain.Roles;
using BMS.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace BMS.Identity.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : BaseDbContext(options)
{
    public const string Schema = "identity";

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        base.OnModelCreating(modelBuilder);
    }
}
