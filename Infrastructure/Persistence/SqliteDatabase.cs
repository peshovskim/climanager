using CliManager.Infrastructure.Paths;
using Microsoft.Data.Sqlite;

namespace CliManager.Infrastructure.Persistence;

internal static class SqliteDatabase
{
    public static string ResolveConnectionString(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;

        if (string.IsNullOrWhiteSpace(dataSource) ||
            dataSource == ":memory:" ||
            Path.IsPathRooted(dataSource))
        {
            return connectionString;
        }

        builder.DataSource = RepositoryPaths.Resolve(dataSource);

        return builder.ConnectionString;
    }

    public static void EnsureDataDirectory(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(
            ResolveConnectionString(connectionString));
        var dataSource = builder.DataSource;

        if (string.IsNullOrWhiteSpace(dataSource) || dataSource == ":memory:")
        {
            return;
        }

        var directory = Path.GetDirectoryName(dataSource);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
