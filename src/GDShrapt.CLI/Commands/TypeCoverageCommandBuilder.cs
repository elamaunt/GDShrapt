using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the type-coverage command.
/// </summary>
public static class TypeCoverageCommandBuilder
{
    public static Command Build(
        Option<string> globalFormatOption,
        Option<bool> verboseOption,
        Option<bool> debugOption,
        Option<bool> quietOption)
    {
        var command = new Command("type-coverage", "Analyze type annotation coverage");

        var pathArg = new Argument<string>("project-path", () => ".", "Path to the Godot project");

        var fileOption = new Option<string?>(
            ["--file", "-f"],
            "Analyze a specific file instead of the whole project");

        command.AddArgument(pathArg);
        command.AddOption(fileOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var file = context.ParseResult.GetValueForOption(fileOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var debug = context.ParseResult.GetValueForOption(debugOption);

            var logger = GDCliLogger.FromFlags(quiet, verbose, debug);
            var formatter = CommandHelpers.GetFormatter(format);

            var options = new GDTypeCoverageOptions
            {
                FilePath = file
            };

            var cmd = new GDTypeCoverageCommand(projectPath, formatter, logger: logger, options: options);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
