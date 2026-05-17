namespace CliManager.Domain.Drive;

public sealed class DriveFile
{
    public required string Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? MimeType { get; init; }

    public string? ParentFolderId { get; init; }

    public DateTimeOffset? ModifiedTime { get; init; }
}
