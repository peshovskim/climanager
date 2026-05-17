namespace CliManager.Application.Drive.Options;

public sealed class SyncOptions
{
    public const string SectionName = "Sync";

    public string DownloadsPath { get; set; } = "MyDrive";

    public int MaxDegreeOfParallelism { get; set; } = 4;
}
