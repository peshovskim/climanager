using CliManager.Application.Drive.Commands.SyncFiles;
using CliManager.Common;
using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CliManager.Commands;

public sealed class SyncCommand(IMediator mediator) : AsyncCommand
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SyncFilesCommand(), cancellationToken);

        if (result.IsFailure)
        {
            return ResultConsole.WriteFailure(result);
        }

        var stats = result.Value!;

        AnsiConsole.MarkupLine("[bold]Sync complete[/]");
        AnsiConsole.MarkupLine($"Total files: [cyan]{stats.TotalFiles}[/]");
        AnsiConsole.MarkupLine($"Downloaded: [green]{stats.SuccessfulDownloads}[/]");
        AnsiConsole.MarkupLine($"Skipped (unchanged): [yellow]{stats.SkippedDownloads}[/]");
        AnsiConsole.MarkupLine($"Removed (deleted on Drive): [dim]{stats.RemovedLocalFiles}[/]");
        AnsiConsole.MarkupLine($"Failed: [red]{stats.FailedDownloads}[/]");
        AnsiConsole.MarkupLine($"Elapsed: [dim]{stats.Elapsed:hh\\:mm\\:ss\\.fff}[/]");

        return stats.FailedDownloads > 0 ? 1 : 0;
    }
}
