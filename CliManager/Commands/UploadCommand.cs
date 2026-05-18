using CliManager.Application.Drive.Commands.UploadFile;
using CliManager.Common;
using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CliManager.Commands;

public sealed class UploadSettings : CommandSettings
{
    [CommandArgument(0, "<LOCAL_PATH>")]
    public string LocalPath { get; set; } = string.Empty;

    [CommandArgument(1, "<DRIVE_PATH>")]
    public string DrivePath { get; set; } = string.Empty;
}

public sealed class UploadCommand(IMediator mediator) : AsyncCommand<UploadSettings>
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        UploadSettings settings,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UploadFileCommand(settings.LocalPath, settings.DrivePath),
            cancellationToken);

        if (result.IsFailure)
        {
            return ResultConsole.WriteFailure(result);
        }

        var upload = result.Value!;

        AnsiConsole.MarkupLine("[green]Upload complete[/]");
        AnsiConsole.MarkupLine($"File: [cyan]{Markup.Escape(upload.FileName)}[/]");
        AnsiConsole.MarkupLine($"Drive folder: [dim]{Markup.Escape(upload.DriveFolderPath)}[/]");
        AnsiConsole.MarkupLine($"Drive file id: [dim]{Markup.Escape(upload.DriveFileId)}[/]");

        return 0;
    }
}
