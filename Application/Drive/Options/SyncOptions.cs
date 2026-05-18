namespace CliManager.Application.Drive.Options;

public sealed class SyncOptions
{
    public const string SectionName = "Sync";

    public string DownloadsPath { get; set; } = "Downloads";

    public int MaxDegreeOfParallelism { get; set; } = 4;
}
