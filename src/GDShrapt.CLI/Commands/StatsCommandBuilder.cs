using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the stats command.
/// </summary>
public static class StatsCommandBuilder
{
    public static Command Build(
        Option<string> globalFormatOption,
        Option<bool> verboseOption,
        Option<bool> debugOption,
        Option<bool> quietOption,
        Option<string?> logLevelOption)
    {
        var command = new Command("stats", "Show a combined summary of project size, complexity, and health.\n\nExamples:\n  gdshrapt stats                           Project statistics\n  gdshrapt stats --format json             JSON output");

        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        var projectOption = new Option<string?>(
            new[] { "--project", "-p" },
            "Path to the Godot project (alternative to positional argument)");

        command.AddArgument(pathArg);
        command.AddOption(projectOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForOption(projectOption)
                ?? context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var debug = context.ParseResult.GetValueForOption(debugOption);

            var logLevel = context.ParseResult.GetValueForOption(logLevelOption);
            var logger = GDCliLogger.FromFlags(quiet, verbose, debug, logLevel);
            var formatter = CommandHelpers.GetFormatter(format);

            var cmd = new GDStatsCommand(projectPath, formatter, logger: logger);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
