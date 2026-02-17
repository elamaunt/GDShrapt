using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the find-refs command.
/// </summary>
public static class FindRefsCommandBuilder
{
    public static Command Build(
        Option<string> globalFormatOption,
        Option<bool> verboseOption,
        Option<bool> debugOption,
        Option<bool> quietOption,
        Option<string?> logLevelOption)
    {
        var command = new Command("find-refs", "Find all references to a symbol across the project.\nSearch by name or by position in a file (--line/--column).\n\nExamples:\n  gdshrapt find-refs health                              Find by symbol name\n  gdshrapt find-refs take_damage -p ./my-project         Search in specific project\n  gdshrapt find-refs health --file player.gd             Search in one file only\n  gdshrapt find-refs --file player.gd --line 15          Find symbol at line 15\n  gdshrapt find-refs --file player.gd --line 15 --column 8  With column\n  gdshrapt find-refs health --format json                 Output as JSON");

        var symbolArg = new Argument<string?>("symbol", "Symbol name to find references for (optional if --line is used)");
        symbolArg.Arity = ArgumentArity.ZeroOrOne;
        var projectOption = new Option<string>(
            new[] { "--project", "-p" },
            getDefaultValue: () => ".",
            description: "Path to the Godot project");
        var fileOption = new Option<string?>(
            new[] { "--file" },
            "Path to a specific GDScript file (required with --line)");
        var lineOption = new Option<int?>(
            new[] { "--line" },
            "Line number (1-based) to identify symbol at position. Requires --file");
        var columnOption = new Option<int?>(
            new[] { "--column" },
            "Column number (1-based, default: 1). Used with --line");
        var explainOption = new Option<bool>(
            "--explain",
            "Show per-reference evidence chains");

        command.AddArgument(symbolArg);
        command.AddOption(projectOption);
        command.AddOption(fileOption);
        command.AddOption(lineOption);
        command.AddOption(columnOption);
        command.AddOption(explainOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var symbol = context.ParseResult.GetValueForArgument(symbolArg);
            var projectPath = context.ParseResult.GetValueForOption(projectOption)!;
            var filePath = context.ParseResult.GetValueForOption(fileOption);
            var line = context.ParseResult.GetValueForOption(lineOption);
            var column = context.ParseResult.GetValueForOption(columnOption);
            var explain = context.ParseResult.GetValueForOption(explainOption);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";

            var formatter = CommandHelpers.GetFormatter(format);
            var cmd = new GDFindRefsCommand(symbol, projectPath, filePath, formatter, line: line, column: column, explain: explain);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
