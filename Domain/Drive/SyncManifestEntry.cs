namespace CliManager.Domain.Drive;

public sealed class SyncManifestEntry
{
    public int Id { get; set; }

    public required string DriveFileId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string LocalPath { get; set; } = string.Empty;

    public DateTimeOffset DownloadedAt { get; set; }
}
