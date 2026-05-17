namespace CliManager.Application.Drive.Responses;

public sealed record UploadFileResponse(
    string DriveFileId,
    string FileName,
    string DriveFolderPath);
