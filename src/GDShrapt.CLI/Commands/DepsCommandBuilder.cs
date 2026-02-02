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
        Option<bool> quietOption)
    {
        var command = new Command("deps", "Show file dependencies and detect cycles");

        var pathArg = new Argument<string>("project-path", () => ".", "Path to the Godot project");

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
        command.AddOption(fileOption);
        command.AddOption(showCoupledOption);
        command.AddOption(showDependentOption);
        command.AddOption(failOnCyclesOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var file = context.ParseResult.GetValueForOption(fileOption);
            var showCoupled = context.ParseResult.GetValueForOption(showCoupledOption);
            var showDependent = context.ParseResult.GetValueForOption(showDependentOption);
            var failOnCycles = context.ParseResult.GetValueForOption(failOnCyclesOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var debug = context.ParseResult.GetValueForOption(debugOption);

            var logger = GDCliLogger.FromFlags(quiet, verbose, debug);
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
