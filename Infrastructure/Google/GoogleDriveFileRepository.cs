using CliManager.Application.Drive.Repositories;
using CliManager.Domain.Drive;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Upload;
using DriveApiFile = Google.Apis.Drive.v3.Data.File;

namespace CliManager.Infrastructure.Google;

public sealed class GoogleDriveFileRepository(IGoogleDriveClientFactory driveClientFactory)
    : IDriveFileRepository
{
    private const string ListFields =
        "nextPageToken, files(id, name, mimeType, parents, modifiedTime)";

    private const string FileFields = "id, name, mimeType, parents, modifiedTime";

    public async Task<IReadOnlyList<DriveFile>> ListAllAsync(
        CancellationToken cancellationToken = default)
    {
        var service = await driveClientFactory.CreateDriveServiceAsync(cancellationToken);
        var results = new List<DriveFile>();
        string? pageToken = null;

        do
        {
            var request = service.Files.List();
            request.Q = GoogleDriveQuery.NonFolderFiles;
            request.Fields = ListFields;
            request.PageSize = 100;
            request.PageToken = pageToken;
            request.SupportsAllDrives = true;
            request.IncludeItemsFromAllDrives = true;

            FileList response = await request.ExecuteAsync(cancellationToken);

            foreach (DriveApiFile file in response.Files)
            {
                results.Add(Map(file));
            }

            pageToken = response.NextPageToken;
        }
        while (!string.IsNullOrEmpty(pageToken));

        return results;
    }

    public async Task<IReadOnlyList<DriveFile>> SearchByNameAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var service = await driveClientFactory.CreateDriveServiceAsync(cancellationToken);
        var results = new List<DriveFile>();
        string? pageToken = null;

        do
        {
            var request = service.Files.List();
            request.Q = GoogleDriveQuery.NameContains(query.Trim());
            request.Fields = ListFields;
            request.PageSize = 100;
            request.PageToken = pageToken;
            request.SupportsAllDrives = true;
            request.IncludeItemsFromAllDrives = true;

            FileList response = await request.ExecuteAsync(cancellationToken);

            foreach (DriveApiFile file in response.Files)
            {
                results.Add(Map(file));
            }

            pageToken = response.NextPageToken;
        }
        while (!string.IsNullOrEmpty(pageToken));

        return results;
    }

    public async Task DownloadAsync(
        DriveFile file,
        string localPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localPath);

        var service = await driveClientFactory.CreateDriveServiceAsync(cancellationToken);

        string? directory = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using FileStream stream = System.IO.File.Create(localPath);

        if (GoogleDriveMimeTypes.IsGoogleWorkspaceFile(file.MimeType))
        {
            string exportMime = GoogleDriveMimeTypes.GetExportMimeType(file.MimeType!);
            FilesResource.ExportRequest exportRequest = service.Files.Export(file.Id, exportMime);
            await exportRequest.DownloadAsync(stream, cancellationToken);
            return;
        }

        FilesResource.GetRequest downloadRequest = service.Files.Get(file.Id);
        downloadRequest.SupportsAllDrives = true;
        await downloadRequest.DownloadAsync(stream, cancellationToken);
    }

    public async Task<DriveFile> UploadAsync(
        string localPath,
        string parentFolderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(localPath))
        {
            throw new ArgumentException("Local file path is required.", nameof(localPath));
        }

        if (!System.IO.File.Exists(localPath))
        {
            throw new FileNotFoundException("Local file not found.", localPath);
        }

        if (string.IsNullOrWhiteSpace(parentFolderId))
        {
            throw new ArgumentException("Parent folder id is required.", nameof(parentFolderId));
        }

        var service = await driveClientFactory.CreateDriveServiceAsync(cancellationToken);

        var metadata = new DriveApiFile
        {
            Name = Path.GetFileName(localPath),
            Parents = [parentFolderId],
        };

        await using FileStream stream = System.IO.File.OpenRead(localPath);
        string contentType = GoogleDriveMimeTypes.GetContentType(localPath);

        FilesResource.CreateMediaUpload uploadRequest = service.Files.Create(
            metadata,
            stream,
            contentType);

        uploadRequest.Fields = FileFields;
        uploadRequest.SupportsAllDrives = true;

        IUploadProgress progress = await uploadRequest.UploadAsync(cancellationToken);

        if (progress.Exception is not null)
        {
            throw progress.Exception;
        }

        if (progress.Status is not UploadStatus.Completed || uploadRequest.ResponseBody is null)
        {
            throw new InvalidOperationException(
                $"Upload failed with status '{progress.Status}'.");
        }

        return Map(uploadRequest.ResponseBody);
    }

    public async Task<string> EnsureFolderPathAsync(
        string drivePath,
        CancellationToken cancellationToken = default)
    {
        string[] segments = drivePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0)
        {
            return "root";
        }

        var service = await driveClientFactory.CreateDriveServiceAsync(cancellationToken);
        string parentId = "root";

        foreach (string segment in segments)
        {
            parentId = await FindOrCreateFolderAsync(
                service,
                segment,
                parentId,
                cancellationToken);
        }

        return parentId;
    }

    private static async Task<string> FindOrCreateFolderAsync(
        DriveService service,
        string name,
        string parentId,
        CancellationToken cancellationToken)
    {
        var listRequest = service.Files.List();
        listRequest.Q = GoogleDriveQuery.FolderInParent(name, parentId);
        listRequest.Fields = "files(id)";
        listRequest.PageSize = 1;
        listRequest.SupportsAllDrives = true;
        listRequest.IncludeItemsFromAllDrives = true;

        FileList existing = await listRequest.ExecuteAsync(cancellationToken);

        if (existing.Files.Count > 0 && !string.IsNullOrEmpty(existing.Files[0].Id))
        {
            return existing.Files[0].Id;
        }

        var folder = new DriveApiFile
        {
            Name = name,
            MimeType = GoogleDriveMimeTypes.Folder,
            Parents = [parentId],
        };

        FilesResource.CreateRequest createRequest = service.Files.Create(folder);
        createRequest.Fields = "id";
        createRequest.SupportsAllDrives = true;

        DriveApiFile created = await createRequest.ExecuteAsync(cancellationToken);

        return created.Id
            ?? throw new InvalidOperationException($"Failed to create folder '{name}'.");
    }

    private static DriveFile Map(DriveApiFile file) =>
        new()
        {
            Id = file.Id ?? throw new InvalidOperationException("Drive file id is missing."),
            Name = file.Name ?? string.Empty,
            MimeType = file.MimeType,
            ParentFolderId = file.Parents?.FirstOrDefault(),
            ModifiedTime = file.ModifiedTimeDateTimeOffset,
        };
}
