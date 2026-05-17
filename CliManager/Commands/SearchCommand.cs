using CliManager.Application.Drive.Queries.SearchFiles;
using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CliManager.Commands;

public sealed class SearchSettings : CommandSettings
{
    [CommandArgument(0, "<QUERY>")]
    public string Query { get; set; } = string.Empty;
}

public sealed class SearchCommand(IMediator mediator) : AsyncCommand<SearchSettings>
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        SearchSettings settings,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SearchFilesQuery(settings.Query), cancellationToken);

        if (result.IsFailure)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(result.Error!.Message)}[/]");
            return 1;
        }

        IReadOnlyList<Application.Drive.Responses.SearchFileReadModel> items = result.Value!;

        if (items.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]No results found for[/] [cyan]{Markup.Escape(settings.Query)}[/]");
            return 0;
        }

        foreach (var item in items)
        {
            if (item.IsFolder)
            {
                AnsiConsole.MarkupLine(
                    $"[blue]{Markup.Escape(item.Name)}[/] [dim](folder)[/]");
                continue;
            }

            if (item.IsDownloaded)
            {
                AnsiConsole.MarkupLine(Markup.Escape(item.Name));
                continue;
            }

            AnsiConsole.MarkupLine(
                $"{Markup.Escape(item.Name)} [yellow][Not Downloaded][/]");
        }

        return 0;
    }
}
