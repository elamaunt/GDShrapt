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
        Option<bool> quietOption)
    {
        var command = new Command("stats", "Show combined project statistics summary");

        var pathArg = new Argument<string>("project-path", () => ".", "Path to the Godot project");

        command.AddArgument(pathArg);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var debug = context.ParseResult.GetValueForOption(debugOption);

            var logger = GDCliLogger.FromFlags(quiet, verbose, debug);
            var formatter = CommandHelpers.GetFormatter(format);

            var cmd = new GDStatsCommand(projectPath, formatter, logger: logger);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
