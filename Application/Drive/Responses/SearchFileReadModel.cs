namespace CliManager.Application.Drive.Responses;

public sealed record SearchFileReadModel(
    string Id,
    string Name,
    bool IsFolder,
    bool IsDownloaded);
