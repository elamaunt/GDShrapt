using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the extract-style command.
/// </summary>
public static class ExtractStyleCommandBuilder
{
    public static Command Build(
        Option<string> globalFormatOption,
        Option<bool> verboseOption,
        Option<bool> debugOption,
        Option<bool> quietOption)
    {
        var command = new Command("extract-style", "Extract formatting style from sample GDScript code");

        var fileArg = new Argument<string>("file", "Path to the sample GDScript file");
        var outputFormatOption = new Option<string>(
            new[] { "--output", "-o" },
            getDefaultValue: () => "toml",
            description: "Output format (toml, json, text)");

        command.AddArgument(fileArg);
        command.AddOption(outputFormatOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var filePath = context.ParseResult.GetValueForArgument(fileArg);
            var outputFormat = context.ParseResult.GetValueForOption(outputFormatOption)!;
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";

            var formatter = CommandHelpers.GetFormatter(format);
            var styleOutputFormat = CommandHelpers.ParseStyleOutputFormat(outputFormat);
            var cmd = new GDExtractStyleCommand(filePath, formatter, outputFormat: styleOutputFormat);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
