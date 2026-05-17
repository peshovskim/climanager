using System.Diagnostics;
using CliManager.Application.Drive.Interfaces;
using CliManager.Application.Drive.Options;
using CliManager.Application.Drive.Repositories;
using CliManager.Application.Drive.Responses;
using CliManager.Application.Common.Abstractions;
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
            int failedDownloads = 0;

            var stopwatch = Stopwatch.StartNew();
            string downloadsRoot = syncOptions.Value.DownloadsPath;
            Directory.CreateDirectory(downloadsRoot);

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, syncOptions.Value.MaxDegreeOfParallelism),
                CancellationToken = cancellationToken,
            };

            await Parallel.ForEachAsync(files, parallelOptions, async (file, ct) =>
            {
                try
                {
                    string localPath = ResolveLocalPath(downloadsRoot, file);

                    await driveFileRepository.DownloadAsync(file, localPath, ct);

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

            stopwatch.Stop();

            return Result<SyncResultResponse>.Success(
                new SyncResultResponse(
                    totalFiles,
                    successfulDownloads,
                    failedDownloads,
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

    private static string ResolveLocalPath(string downloadsRoot, DriveFile file)
    {
        string safeName = SanitizeFileName(file.Name);
        string localPath = Path.Combine(downloadsRoot, safeName);

        if (!File.Exists(localPath))
        {
            return localPath;
        }

        string extension = Path.GetExtension(safeName);
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(safeName);

        return Path.Combine(downloadsRoot, $"{nameWithoutExtension}_{file.Id}{extension}");
    }

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
