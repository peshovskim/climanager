using System.Diagnostics;
using CliManager.Application.Common.Abstractions;
using CliManager.Application.Drive.Interfaces;
using CliManager.Application.Drive.Options;
using CliManager.Application.Drive.Repositories;
using CliManager.Application.Drive.Responses;
using CliManager.Domain.Drive;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharedKernel;
using SharedKernel.Cqrs;

namespace CliManager.Application.Drive.Commands.SyncFiles;

public sealed record SyncFilesCommand : ICommand<Result<SyncResultResponse>>;

public sealed class SyncFilesCommandHandler(
    IGoogleAuthService googleAuthService,
    IDriveFileRepository driveFileRepository,
    IOptions<SyncOptions> syncOptions,
    IServiceScopeFactory scopeFactory)
    : IRequestHandler<SyncFilesCommand, Result<SyncResultResponse>>
{
    public async Task<Result<SyncResultResponse>> Handle(
        SyncFilesCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            await googleAuthService.EnsureAuthenticatedAsync(cancellationToken);

            IReadOnlyList<DriveFile> files =
                await driveFileRepository.ListAllAsync(cancellationToken);

            int totalFiles = files.Count;
            int successfulDownloads = 0;
            int skippedDownloads = 0;
            int failedDownloads = 0;

            var stopwatch = Stopwatch.StartNew();
            string downloadsRoot = syncOptions.Value.DownloadsPath;
            Directory.CreateDirectory(downloadsRoot);

            IReadOnlyDictionary<string, SyncEntry> manifestByDriveId =
                await LoadManifestAsync(cancellationToken);

            var driveFileIds = files
                .Select(file => file.Id)
                .ToHashSet(StringComparer.Ordinal);

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, syncOptions.Value.MaxDegreeOfParallelism),
                CancellationToken = cancellationToken,
            };

            await Parallel.ForEachAsync(files, parallelOptions, async (file, ct) =>
            {
                try
                {
                    string localPath = ResolveLocalPath(downloadsRoot, file.Name);
                    manifestByDriveId.TryGetValue(file.Id, out SyncEntry? existingEntry);

                    if (IsUnchanged(file, existingEntry, localPath))
                    {
                        Interlocked.Increment(ref skippedDownloads);
                        return;
                    }

                    RemoveStaleLocalFile(existingEntry, localPath);

                    await driveFileRepository.DownloadAsync(file, localPath, ct);
                    ApplyDriveModifiedTime(localPath, file);

                    using IServiceScope scope = scopeFactory.CreateScope();
                    var syncEntryRepository =
                        scope.ServiceProvider.GetRequiredService<ISyncEntryRepository>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    await syncEntryRepository.UpsertAsync(
                        new SyncEntry
                        {
                            DriveFileId = file.Id,
                            FileName = file.Name,
                            LocalPath = localPath,
                            DownloadedAt = DateTimeOffset.UtcNow,
                        },
                        ct);

                    await unitOfWork.SaveChangesAsync(ct);

                    Interlocked.Increment(ref successfulDownloads);
                }
                catch
                {
                    Interlocked.Increment(ref failedDownloads);
                }
            });

            int removedLocalFiles = await RemoveFilesDeletedOnDriveAsync(
                driveFileIds,
                cancellationToken);

            stopwatch.Stop();

            return Result<SyncResultResponse>.Success(
                new SyncResultResponse(
                    totalFiles,
                    successfulDownloads,
                    skippedDownloads,
                    failedDownloads,
                    removedLocalFiles,
                    stopwatch.Elapsed));
        }
        catch (FileNotFoundException ex)
        {
            return Result<SyncResultResponse>.Invalid(ResultCodes.Validation, ex.Message);
        }
        catch (Exception ex)
        {
            return Result<SyncResultResponse>.InternalError(ResultCodes.InternalError, ex.Message);
        }
    }

    private async Task<IReadOnlyDictionary<string, SyncEntry>> LoadManifestAsync(
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        var syncEntryRepository =
            scope.ServiceProvider.GetRequiredService<ISyncEntryRepository>();

        IReadOnlyList<SyncEntry> entries =
            await syncEntryRepository.GetAllAsync(cancellationToken);

        return entries.ToDictionary(entry => entry.DriveFileId, StringComparer.Ordinal);
    }

    private async Task<int> RemoveFilesDeletedOnDriveAsync(
        IReadOnlySet<string> driveFileIds,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        var syncEntryRepository =
            scope.ServiceProvider.GetRequiredService<ISyncEntryRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        IReadOnlyList<SyncEntry> entries =
            await syncEntryRepository.GetAllAsync(cancellationToken);

        int removed = 0;

        foreach (SyncEntry entry in entries)
        {
            if (driveFileIds.Contains(entry.DriveFileId))
            {
                continue;
            }

            if (File.Exists(entry.LocalPath))
            {
                File.Delete(entry.LocalPath);
            }

            await syncEntryRepository.DeleteByDriveFileIdAsync(
                entry.DriveFileId,
                cancellationToken);

            removed++;
        }

        if (removed > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return removed;
    }

    private static bool IsUnchanged(DriveFile file, SyncEntry? entry, string localPath)
    {
        if (!File.Exists(localPath))
        {
            return false;
        }

        if (entry is not null && !PathsEqual(entry.LocalPath, localPath))
        {
            return false;
        }

        if (file.ModifiedTime is null)
        {
            return false;
        }

        DateTime driveModifiedUtc = file.ModifiedTime.Value.UtcDateTime;
        DateTime localModifiedUtc = File.GetLastWriteTimeUtc(localPath);

        return localModifiedUtc >= driveModifiedUtc;
    }

    private static void ApplyDriveModifiedTime(string localPath, DriveFile file)
    {
        if (file.ModifiedTime is null)
        {
            return;
        }

        File.SetLastWriteTimeUtc(localPath, file.ModifiedTime.Value.UtcDateTime);
    }

    private static void RemoveStaleLocalFile(SyncEntry? entry, string localPath)
    {
        if (entry is null || string.IsNullOrWhiteSpace(entry.LocalPath))
        {
            return;
        }

        if (PathsEqual(entry.LocalPath, localPath))
        {
            return;
        }

        if (File.Exists(entry.LocalPath))
        {
            File.Delete(entry.LocalPath);
        }
    }

    private static string ResolveLocalPath(string downloadsRoot, string fileName) =>
        Path.Combine(downloadsRoot, SanitizeFileName(fileName));

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            StringComparison.OrdinalIgnoreCase);

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "unnamed";
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new char[fileName.Length];
        int length = 0;

        foreach (char character in fileName)
        {
            sanitized[length++] = invalidChars.Contains(character) ? '_' : character;
        }

        return new string(sanitized, 0, length);
    }
}
