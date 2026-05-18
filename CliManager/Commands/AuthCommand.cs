using CliManager.Application.Drive.Commands.Authenticate;
using CliManager.Common;
using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CliManager.Commands;

public sealed class AuthCommand(IMediator mediator) : AsyncCommand
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AuthenticateCommand(), cancellationToken);

        if (result.IsFailure)
        {
            return ResultConsole.WriteFailure(result);
        }

        AnsiConsole.MarkupLine("[green]Google Drive authentication succeeded.[/]");

        AnsiConsole.MarkupLine("[dim]Tokens saved under .climanager/tokens — you should not need to sign in again.[/]");
        
        return 0;
    }
}
