using CliManager.Application.Common;
using CliManager.Application.Common.Abstractions;
using CliManager.Application.Drive.Interfaces;
using CliManager.Application.Drive.Repositories;
using CliManager.Application.Drive.Responses;
using CliManager.Domain.Drive;
using MediatR;
using SharedKernel;
using SharedKernel.Cqrs;

namespace CliManager.Application.Drive.Commands.UploadFile;

public sealed record UploadFileCommand(string LocalPath, string DrivePath)
    : ICommand<Result<UploadFileResponse>>;

public sealed class UploadFileCommandHandler(
    IGoogleAuthService googleAuthService,
    IDriveFileRepository driveFileRepository,
    ISyncEntryRepository syncEntryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UploadFileCommand, Result<UploadFileResponse>>
{
    public async Task<Result<UploadFileResponse>> Handle(
        UploadFileCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.LocalPath))
        {
            return Result<UploadFileResponse>.Invalid(
                ResultCodes.Validation,
                "Local file path is required.");
        }

        string localPath = Path.GetFullPath(command.LocalPath);

        if (Directory.Exists(localPath))
        {
            return Result<UploadFileResponse>.Invalid(
                ResultCodes.Validation,
                "Local path must be a file, not a directory.");
        }

        if (!File.Exists(localPath))
        {
            return Result<UploadFileResponse>.NotFound(
                ResultCodes.NotFound,
                $"Local file not found: {localPath}");
        }

        try
        {
            await googleAuthService.EnsureAuthenticatedAsync(cancellationToken);

            string driveFolderPath = command.DrivePath?.Trim() ?? string.Empty;
            string parentFolderId =
                await driveFileRepository.EnsureFolderPathAsync(driveFolderPath, cancellationToken);

            DriveFile uploaded = await driveFileRepository.UploadAsync(
                localPath,
                parentFolderId,
                cancellationToken);

            await syncEntryRepository.UpsertAsync(
                new SyncEntry
                {
                    DriveFileId = uploaded.Id,
                    FileName = uploaded.Name,
                    LocalPath = localPath,
                    DownloadedAt = DateTimeOffset.UtcNow,
                },
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<UploadFileResponse>.Success(
                new UploadFileResponse(
                    uploaded.Id,
                    uploaded.Name,
                    string.IsNullOrEmpty(driveFolderPath) ? "/" : driveFolderPath));
        }
        catch (Exception ex)
        {
            return DriveCommandResults.FromException<UploadFileResponse>(ex);
        }
    }
}
