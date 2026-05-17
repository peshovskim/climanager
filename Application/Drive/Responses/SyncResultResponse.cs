namespace CliManager.Application.Drive.Responses;

public sealed record SyncResultResponse(
    int TotalFiles,
    int SuccessfulDownloads,
    int SkippedDownloads,
    int FailedDownloads,
    int RemovedLocalFiles,
    TimeSpan Elapsed);
