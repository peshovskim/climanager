using CliManager.Application.Common;
using CliManager.Application.Drive.Interfaces;
using CliManager.Application.Drive.Repositories;
using CliManager.Application.Drive.Responses;
using CliManager.Domain.Drive;
using MediatR;
using SharedKernel;
using SharedKernel.Cqrs;

namespace CliManager.Application.Drive.Queries.SearchFiles;

public sealed record SearchFilesQuery(string Query) : IQuery<Result<IReadOnlyList<SearchFileReadModel>>>;

public sealed class SearchFilesQueryHandler(
    IGoogleAuthService googleAuthService,
    IDriveFileRepository driveFileRepository,
    ISyncEntryRepository syncEntryRepository)
    : IRequestHandler<SearchFilesQuery, Result<IReadOnlyList<SearchFileReadModel>>>
{
    public async Task<Result<IReadOnlyList<SearchFileReadModel>>> Handle(
        SearchFilesQuery query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Query))
        {
            return Result<IReadOnlyList<SearchFileReadModel>>.Invalid(
                ResultCodes.Validation,
                "Search query is required.");
        }

        try
        {
            await googleAuthService.EnsureAuthenticatedAsync(cancellationToken);

            IReadOnlyList<DriveFile> driveItems =
                await driveFileRepository.SearchByNameAsync(query.Query, cancellationToken);

            var results = new List<SearchFileReadModel>(driveItems.Count);

            foreach (DriveFile item in driveItems)
            {
                bool isFolder = DriveMimeTypes.IsFolder(item.MimeType);
                bool isDownloaded = false;

                if (!isFolder)
                {
                    isDownloaded = await IsDownloadedAsync(item.Id, cancellationToken);
                }

                results.Add(
                    new SearchFileReadModel(
                        item.Id,
                        item.Name,
                        isFolder,
                        isDownloaded));
            }

            return Result<IReadOnlyList<SearchFileReadModel>>.Success(results);
        }
        catch (Exception ex)
        {
            return DriveCommandResults.FromException<IReadOnlyList<SearchFileReadModel>>(ex);
        }
    }

    private async Task<bool> IsDownloadedAsync(
        string driveFileId,
        CancellationToken cancellationToken)
    {
        SyncEntry? entry =
            await syncEntryRepository.GetByDriveFileIdAsync(driveFileId, cancellationToken);

        return entry is not null && File.Exists(entry.LocalPath);
    }
}
