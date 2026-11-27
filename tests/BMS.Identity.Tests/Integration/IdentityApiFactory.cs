using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BMS.Identity.Tests.Integration;

/// <summary>
/// Hosts the Identity API in memory against a throw-away SQLite database.
/// </summary>
public sealed class IdentityApiFactory : WebApplicationFactory<Program>
{
    public const string AdminPassword = "Admin@12345";
    public const int MaxFailedAccessAttempts = 3;

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"bms-identity-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("Database:ConnectionString", $"Data Source={_databasePath};Pooling=False");
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-0123456789-abcdefghij");
        builder.UseSetting("Seed:AdminPassword", AdminPassword);
        builder.UseSetting("AccountSecurity:MaxFailedAccessAttempts", MaxFailedAccessAttempts.ToString());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
