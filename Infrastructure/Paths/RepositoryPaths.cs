namespace CliManager.Infrastructure.Paths;

public static class RepositoryPaths
{
    public const string SolutionFileName = "CliManager.sln";

    private static string? _root;

    public static string Root => _root ??= FindRoot();

    public static string Resolve(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            return Path.GetFullPath(relativePath);
        }

        return Path.GetFullPath(Path.Combine(Root, relativePath));
    }

    public static string FindRoot()
    {
        string[] startDirectories =
        [
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory(),
        ];

        foreach (string startDirectory in startDirectories.Distinct(StringComparer.Ordinal))
        {
            string? directory = Directory.Exists(startDirectory)
                ? startDirectory
                : Path.GetDirectoryName(startDirectory);

            while (!string.IsNullOrEmpty(directory))
            {
                if (File.Exists(Path.Combine(directory, SolutionFileName)))
                {
                    return directory;
                }

                directory = Directory.GetParent(directory)?.FullName;
            }
        }

        throw new InvalidOperationException(
            $"Could not find the repository root (missing '{SolutionFileName}'). " +
            "Run the CLI from the cloned solution directory.");
    }
}
