using CliManager.Application.Common.Abstractions;
using CliManager.Application.Drive.Interfaces;
using CliManager.Application.Drive.Repositories;
using CliManager.Infrastructure.Auth;
using CliManager.Infrastructure.Options;
using CliManager.Infrastructure.Persistence;
using CliManager.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
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
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing or empty.");
        }

        services.AddOptions<DatabaseOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                options.ConnectionString =
                    config.GetConnectionString("DefaultConnection") ?? string.Empty;
            });

        var sqliteConnectionString = SqliteDatabase.ResolveConnectionString(connectionString);
        SqliteDatabase.EnsureDataDirectory(sqliteConnectionString);

        services.AddDbContext<CliManagerDbContext>(options =>
            options.UseSqlite(sqliteConnectionString));

        services.AddScoped<ISyncEntryRepository, SyncEntryRepository>();
        services.AddEfUnitOfWork<CliManagerDbContext>();

        services.AddOptions<GoogleAuthOptions>()
            .Bind(configuration.GetSection(GoogleAuthOptions.SectionName));

        services.AddSingleton<IGoogleAuthService, GoogleAuthService>();

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
