namespace CliManager.Application.Drive.Responses;

public sealed record SyncResultResponse(
    int TotalFiles,
    int SuccessfulDownloads,
    int FailedDownloads,
    TimeSpan Elapsed);
