using CliManager.Infrastructure.Paths;

namespace CliManager.Infrastructure.Auth;

internal static class GoogleAuthPathResolver
{
    public static string ResolveSecretPath(string contentRoot, string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        string fileName = Path.GetFileName(configuredPath);
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "client_secret.json";
        }

        var candidates = new List<string>
        {
            Path.GetFullPath(Path.Combine(contentRoot, configuredPath)),
        };

        string? directory = contentRoot;
        for (int depth = 0; depth < 8 && directory is not null; depth++)
        {
            candidates.Add(Path.Combine(directory, fileName));
            directory = Directory.GetParent(directory)?.FullName;
        }

        foreach (string candidate in candidates.Distinct(StringComparer.Ordinal))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            $"OAuth client secret file not found. Set GoogleAuth:ClientSecretPath or place '{fileName}' next to {RepositoryPaths.SolutionFileName}. " +
            $"Searched: {string.Join(Environment.NewLine, candidates.Distinct(StringComparer.Ordinal))}",
            candidates[0]);
    }
}
