using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the parse command.
/// </summary>
public static class ParseCommandBuilder
{
    public static Command Build(
        Option<string> globalFormatOption,
        Option<bool> verboseOption,
        Option<bool> debugOption,
        Option<bool> quietOption,
        Option<string?> logLevelOption)
    {
        var command = new Command("parse", "Parse a GDScript file and display its abstract syntax tree.\n\nExamples:\n  gdshrapt parse player.gd                 Show AST tree\n  gdshrapt parse player.gd -o json         Output as JSON\n  gdshrapt parse player.gd -o tokens       Show token stream");

        var fileArg = new Argument<string>("file", "Path to the GDScript file");
        var outputFormatOption = new Option<string>(
            new[] { "--output", "-o" },
            getDefaultValue: () => "tree",
            description: "Output format (tree, json, tokens)");
        var positionsOption = new Option<bool>(
            new[] { "--positions", "-p" },
            "Show position information in output");

        command.AddArgument(fileArg);
        command.AddOption(outputFormatOption);
        command.AddOption(positionsOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var filePath = context.ParseResult.GetValueForArgument(fileArg);
            var outputFormat = context.ParseResult.GetValueForOption(outputFormatOption)!;
            var showPositions = context.ParseResult.GetValueForOption(positionsOption);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";

            var formatter = CommandHelpers.GetFormatter(format);
            var parseOutputFormat = CommandHelpers.ParseOutputFormat(outputFormat);
            var cmd = new GDParseCommand(filePath, formatter, outputFormat: parseOutputFormat, showPositions: showPositions);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
