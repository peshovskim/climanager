namespace CliManager.Infrastructure.Google;

internal static class GoogleDriveQuery
{
    public const string NonFolderFiles =
        "trashed = false and mimeType != 'application/vnd.google-apps.folder'";

    public static string NameContains(string query) =>
        $"{NonFolderFiles} and name contains '{Escape(query)}'";

    public static string FolderInParent(string name, string parentId) =>
        $"mimeType = 'application/vnd.google-apps.folder' and name = '{Escape(name)}' and '{Escape(parentId)}' in parents and trashed = false";

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal);
}
