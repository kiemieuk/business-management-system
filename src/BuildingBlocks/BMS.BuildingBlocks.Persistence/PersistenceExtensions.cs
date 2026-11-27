using BMS.BuildingBlocks.Domain.Abstractions;
using BMS.BuildingBlocks.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BMS.BuildingBlocks.Persistence;

public static class PersistenceExtensions
{
    /// <summary>
    /// Registers a service <see cref="DbContext"/> using the provider configured in the <c>Database</c> section,
    /// plus the auditing interceptor and the <see cref="IUnitOfWork"/>.
    /// </summary>
    public static IServiceCollection AddServiceDbContext<TContext>(this IServiceCollection services, IConfiguration configuration)
        where TContext : BaseDbContext
    {
        var options = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException(
                $"Configuration value '{DatabaseOptions.SectionName}:{nameof(DatabaseOptions.ConnectionString)}' is required.");
        }

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<TContext>((sp, builder) =>
        {
            var migrationsAssembly = typeof(TContext).Assembly.GetName().Name;
            switch (options.Provider)
            {
                case DatabaseProvider.Sqlite:
                    builder.UseSqlite(options.ConnectionString, b => b.MigrationsAssembly(migrationsAssembly));
                    break;
                default:
                    builder.UseSqlServer(options.ConnectionString, b =>
                    {
                        b.MigrationsAssembly(migrationsAssembly);
                        b.EnableRetryOnFailure();
                    });
                    break;
            }

            builder.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TContext>());
        return services;
    }

    /// <summary>
    /// Brings the database schema up to date. SQL Server uses EF Core migrations; SQLite (development/tests)
    /// creates the schema directly from the model.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this DbContext context, CancellationToken cancellationToken = default)
    {
        if (context.Database.IsSqlServer())
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }
    }
}
