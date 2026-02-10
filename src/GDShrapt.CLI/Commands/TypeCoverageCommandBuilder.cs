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
        Option<bool> quietOption,
        Option<string?> logLevelOption)
    {
        var command = new Command("type-coverage", "Report how much of your code has explicit type annotations.\n\nExamples:\n  gdshrapt type-coverage                   Project-wide coverage\n  gdshrapt type-coverage --file player.gd  Single file coverage");

        var pathArg = new Argument<string>("project-path", () => ".", "Path to the Godot project");
        var projectOption = new Option<string?>(
            new[] { "--project", "-p" },
            "Path to the Godot project (alternative to positional argument)");

        var fileOption = new Option<string?>(
            ["--file", "-f"],
            "Analyze a specific file instead of the whole project");

        command.AddArgument(pathArg);
        command.AddOption(projectOption);
        command.AddOption(fileOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForOption(projectOption)
                ?? context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var file = context.ParseResult.GetValueForOption(fileOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var debug = context.ParseResult.GetValueForOption(debugOption);

            var logLevel = context.ParseResult.GetValueForOption(logLevelOption);
            var logger = GDCliLogger.FromFlags(quiet, verbose, debug, logLevel);
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
