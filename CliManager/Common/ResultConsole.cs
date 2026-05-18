using SharedKernel;
using Spectre.Console;

namespace CliManager.Common;

public static class ResultConsole
{
    public static int WriteFailure(Result result)
    {
        if (result.IsSuccess || result.Error is null)
        {
            return 0;
        }

        string color = result.Error.Type switch
        {
            ResultType.Invalid => "yellow",
            ResultType.NotFound => "yellow",
            ResultType.Unauthorized => "red",
            ResultType.Forbidden => "red",
            ResultType.Conflicted => "orange1",
            _ => "red",
        };

        AnsiConsole.MarkupLine($"[{color}]{Markup.Escape(result.Error.Message)}[/]");
        return 1;
    }
}
