using CliManager.Domain.Drive;

namespace CliManager.Application.Drive.Repositories;

public interface ISyncManifestRepository
{
    Task<SyncManifestEntry?> GetByDriveFileIdAsync(string driveFileId, CancellationToken cancellationToken = default);

    Task UpsertAsync(SyncManifestEntry entry, CancellationToken cancellationToken = default);
}
