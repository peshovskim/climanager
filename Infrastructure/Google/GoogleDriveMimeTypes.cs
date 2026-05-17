namespace CliManager.Infrastructure.Google;

internal static class GoogleDriveMimeTypes
{
    public const string Folder = "application/vnd.google-apps.folder";

    private static readonly IReadOnlyDictionary<string, string> ExportMimeTypes =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["application/vnd.google-apps.document"] =
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ["application/vnd.google-apps.spreadsheet"] =
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ["application/vnd.google-apps.presentation"] =
                "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ["application/vnd.google-apps.drawing"] = "image/png",
        };

    public static bool IsFolder(string? mimeType) =>
        string.Equals(mimeType, Folder, StringComparison.Ordinal);

    public static bool IsGoogleWorkspaceFile(string? mimeType) =>
        mimeType?.StartsWith("application/vnd.google-apps.", StringComparison.Ordinal) == true
        && !IsFolder(mimeType);

    public static string GetExportMimeType(string mimeType) =>
        ExportMimeTypes.TryGetValue(mimeType, out string? exportMime)
            ? exportMime
            : "application/pdf";

    public static string GetContentType(string localPath) =>
        Path.GetExtension(localPath).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".zip" => "application/zip",
            ".docx" =>
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" =>
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".pptx" =>
                "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream",
        };
}
