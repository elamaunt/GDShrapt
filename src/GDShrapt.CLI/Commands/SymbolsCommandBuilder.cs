using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the symbols command.
/// </summary>
public static class SymbolsCommandBuilder
{
    public static Command Build(
        Option<string> globalFormatOption,
        Option<bool> verboseOption,
        Option<bool> debugOption,
        Option<bool> quietOption,
        Option<string?> logLevelOption)
    {
        var command = new Command("symbols", "List all classes, functions, variables, and signals in a file.\n\nExamples:\n  gdshrapt symbols player.gd               List symbols\n  gdshrapt symbols player.gd --format json JSON output");

        var fileArg = new Argument<string>("file", "Path to the GDScript file");
        command.AddArgument(fileArg);

        command.SetHandler(async (InvocationContext context) =>
        {
            var filePath = context.ParseResult.GetValueForArgument(fileArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";

            var formatter = CommandHelpers.GetFormatter(format);
            var cmd = new GDSymbolsCommand(filePath, formatter);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
