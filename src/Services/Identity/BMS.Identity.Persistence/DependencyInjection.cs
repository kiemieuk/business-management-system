using BMS.BuildingBlocks.Persistence;
using BMS.Identity.Domain.Roles;
using BMS.Identity.Domain.Users;
using BMS.Identity.Persistence.Repositories;
using BMS.Identity.Persistence.Seeding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BMS.Identity.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceDbContext<IdentityDbContext>(configuration);
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.Configure<IdentitySeedOptions>(configuration.GetSection(IdentitySeedOptions.SectionName));
        services.AddScoped<IdentityDataSeeder>();
        return services;
    }
}
