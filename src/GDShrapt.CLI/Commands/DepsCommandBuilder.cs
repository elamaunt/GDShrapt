using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the deps command.
/// </summary>
public static class DepsCommandBuilder
{
    public static Command Build(
        Option<string> globalFormatOption,
        Option<bool> verboseOption,
        Option<bool> debugOption,
        Option<bool> quietOption,
        Option<string?> logLevelOption)
    {
        var command = new Command("deps", "Visualize file dependencies (extends, preload) and detect circular imports.\n\nExamples:\n  gdshrapt deps                            Show dependency graph\n  gdshrapt deps --fail-on-cycles           Fail if cycles found\n  gdshrapt deps --show-coupled             Show most coupled files");

        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        var projectOption = new Option<string?>(
            new[] { "--project", "-p" },
            "Path to the Godot project (alternative to positional argument)");

        var fileOption = new Option<string?>(
            ["--file", "-f"],
            "Analyze a specific file instead of the whole project");

        var showCoupledOption = new Option<bool>(
            ["--show-coupled"],
            "Show files with most outgoing dependencies");

        var showDependentOption = new Option<bool>(
            ["--show-dependent"],
            "Show files with most incoming dependencies");

        var failOnCyclesOption = new Option<bool>(
            ["--fail-on-cycles"],
            "Exit with error if circular dependencies are found");

        command.AddArgument(pathArg);
        command.AddOption(projectOption);
        command.AddOption(fileOption);
        command.AddOption(showCoupledOption);
        command.AddOption(showDependentOption);
        command.AddOption(failOnCyclesOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForOption(projectOption)
                ?? context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var file = context.ParseResult.GetValueForOption(fileOption);
            var showCoupled = context.ParseResult.GetValueForOption(showCoupledOption);
            var showDependent = context.ParseResult.GetValueForOption(showDependentOption);
            var failOnCycles = context.ParseResult.GetValueForOption(failOnCyclesOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var debug = context.ParseResult.GetValueForOption(debugOption);

            var logLevel = context.ParseResult.GetValueForOption(logLevelOption);
            var logger = GDCliLogger.FromFlags(quiet, verbose, debug, logLevel);
            var formatter = CommandHelpers.GetFormatter(format);

            var options = new GDDepsOptions
            {
                FilePath = file,
                ShowCoupled = showCoupled,
                ShowDependent = showDependent,
                FailOnCycles = failOnCycles
            };

            var cmd = new GDDepsCommand(projectPath, formatter, logger: logger, options: options);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
