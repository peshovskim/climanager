using CliManager.Domain.Drive;

namespace CliManager.Application.Drive.Repositories;

public interface ISyncEntryRepository
{
    Task<SyncEntry?> GetByDriveFileIdAsync(string driveFileId, CancellationToken cancellationToken = default);

    Task UpsertAsync(SyncEntry entry, CancellationToken cancellationToken = default);
}
