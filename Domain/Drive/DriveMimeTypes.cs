namespace CliManager.Domain.Drive;

public static class DriveMimeTypes
{
    public const string Folder = "application/vnd.google-apps.folder";

    public static bool IsFolder(string? mimeType) =>
        string.Equals(mimeType, Folder, StringComparison.Ordinal);
}
