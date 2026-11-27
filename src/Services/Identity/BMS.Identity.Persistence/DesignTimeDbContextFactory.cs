using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BMS.Identity.Persistence;

/// <summary>
/// Used only by the <c>dotnet ef</c> tooling so that migrations are always generated for SQL Server.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer("Server=localhost;Database=bms_identity;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new IdentityDbContext(options);
    }
}
