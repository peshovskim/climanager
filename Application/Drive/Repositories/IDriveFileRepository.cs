using CliManager.Domain.Drive;

namespace CliManager.Application.Drive.Repositories;

public interface IDriveFileRepository
{
    Task<IReadOnlyList<DriveFile>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DriveFile>> SearchByNameAsync(string query, CancellationToken cancellationToken = default);

    Task DownloadAsync(DriveFile file, string localPath, CancellationToken cancellationToken = default);

    Task<DriveFile> UploadAsync(string localPath, string parentFolderId, CancellationToken cancellationToken = default);

    Task<string> EnsureFolderPathAsync(string drivePath, CancellationToken cancellationToken = default);
}
