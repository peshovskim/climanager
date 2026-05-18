using CliManager.Application.Common.Abstractions;
using CliManager.Application.Drive.Interfaces;
using CliManager.Application.Drive.Options;
using CliManager.Application.Drive.Repositories;
using CliManager.Infrastructure.Auth;
using CliManager.Infrastructure.Google;
using CliManager.Infrastructure.Options;
using CliManager.Infrastructure.Paths;
using CliManager.Infrastructure.Persistence;
using CliManager.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CliManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing or empty.");
        }

        var sqliteConnectionString = SqliteDatabase.ResolveConnectionString(connectionString);
        SqliteDatabase.EnsureDataDirectory(sqliteConnectionString);

        services.AddDbContext<CliManagerDbContext>(options =>
            options.UseSqlite(sqliteConnectionString));

        services.AddScoped<ISyncEntryRepository, SyncEntryRepository>();
        services.AddEfUnitOfWork<CliManagerDbContext>();

        services.AddOptions<SyncOptions>()
            .Bind(configuration.GetSection(SyncOptions.SectionName))
            .PostConfigure(options =>
            {
                options.DownloadsPath = RepositoryPaths.Resolve(options.DownloadsPath);
                Directory.CreateDirectory(options.DownloadsPath);
            });

        services.AddOptions<GoogleAuthOptions>()
            .Bind(configuration.GetSection(GoogleAuthOptions.SectionName))
            .PostConfigure(options =>
            {
                options.ClientSecretPath = GoogleAuthPathResolver.ResolveSecretPath(
                    RepositoryPaths.Root,
                    options.ClientSecretPath);
                options.TokenStorePath = RepositoryPaths.Resolve(options.TokenStorePath);
                Directory.CreateDirectory(options.TokenStorePath);
            });

        services.AddSingleton<GoogleAuthService>();
        services.AddSingleton<IGoogleAuthService>(static sp =>
            sp.GetRequiredService<GoogleAuthService>());
        services.AddSingleton<IGoogleCredentialSource>(static sp =>
            sp.GetRequiredService<GoogleAuthService>());

        services.AddSingleton<IGoogleDriveClientFactory, GoogleDriveClientFactory>();
        services.AddScoped<IDriveFileRepository, GoogleDriveFileRepository>();

        return services;
    }

    public static async Task EnsureDatabaseCreatedAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CliManagerDbContext>();
        await db.Database.EnsureCreatedAsync(cancellationToken);
    }

    public static IServiceCollection AddEfUnitOfWork<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IUnitOfWork, EfUnitOfWork<TDbContext>>();
        return services;
    }
}
