using CliManager.Application.Common.Abstractions;
using CliManager.Application.Drive.Interfaces;
using CliManager.Application.Drive.Repositories;
using CliManager.Infrastructure.Auth;
using CliManager.Infrastructure.Google;
using CliManager.Infrastructure.Local;
using CliManager.Infrastructure.Options;
using CliManager.Infrastructure.Persistence;
using CliManager.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CliManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=climanager.db";

        services.AddOptions<DatabaseOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                options.ConnectionString =
                    config.GetConnectionString("DefaultConnection") ?? connectionString;
            });

        services.AddDbContext<ManifestDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IDriveFileRepository, GoogleDriveFileRepository>();
        services.AddScoped<ISyncManifestRepository, SyncManifestRepository>();
        services.AddScoped<ILocalSyncState, LocalSyncState>();
        services.AddEfUnitOfWork<ManifestDbContext>();

        return services;
    }

    public static IServiceCollection AddEfUnitOfWork<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IUnitOfWork, EfUnitOfWork<TDbContext>>();
        return services;
    }
}
