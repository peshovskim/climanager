using CliManager.Application.Drive.Repositories;
using CliManager.Domain.Drive;
using Microsoft.EntityFrameworkCore;

namespace CliManager.Infrastructure.Persistence.Repositories;

public sealed class SyncEntryRepository(CliManagerDbContext dbContext) : ISyncEntryRepository
{
    public async Task<SyncEntry?> GetByDriveFileIdAsync(
        string driveFileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SyncEntries
            .FirstOrDefaultAsync(entry => entry.DriveFileId == driveFileId, cancellationToken);
    }

    public async Task UpsertAsync(SyncEntry entry, CancellationToken cancellationToken = default)
    {
        SyncEntry? existing = await GetByDriveFileIdAsync(entry.DriveFileId, cancellationToken);

        if (existing is null)
        {
            await dbContext.SyncEntries.AddAsync(entry, cancellationToken);
            return;
        }

        existing.FileName = entry.FileName;
        existing.LocalPath = entry.LocalPath;
        existing.DownloadedAt = entry.DownloadedAt;
    }
}
