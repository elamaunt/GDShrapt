using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the dead-code command.
/// </summary>
public static class DeadCodeCommandBuilder
{
    public static Command Build(
        Option<string> globalFormatOption,
        Option<bool> verboseOption,
        Option<bool> debugOption,
        Option<bool> quietOption)
    {
        var command = new Command("dead-code", "Find unused code (variables, functions, signals)");

        var pathArg = new Argument<string>("project-path", () => ".", "Path to the Godot project");

        var fileOption = new Option<string?>(
            ["--file", "-f"],
            "Analyze a specific file instead of the whole project");

        var includeVarsOption = new Option<bool>(
            ["--include-variables"],
            () => true,
            "Include unused variables");

        var noVarsOption = new Option<bool>(
            ["--no-variables"],
            "Exclude unused variables");

        var includeFuncsOption = new Option<bool>(
            ["--include-functions"],
            () => true,
            "Include unused functions");

        var noFuncsOption = new Option<bool>(
            ["--no-functions"],
            "Exclude unused functions");

        var includeSignalsOption = new Option<bool>(
            ["--include-signals"],
            () => true,
            "Include unused signals");

        var noSignalsOption = new Option<bool>(
            ["--no-signals"],
            "Exclude unused signals");

        var includeParamsOption = new Option<bool>(
            ["--include-parameters"],
            "Include unused parameters");

        var includePrivateOption = new Option<bool>(
            ["--include-private"],
            "Include private members (starting with _)");

        var includeUnreachableOption = new Option<bool>(
            ["--include-unreachable"],
            () => true,
            "Include unreachable code");

        var kindOption = new Option<string?>(
            ["--kind", "-k"],
            "Filter by kind: Variable, Function, Signal, Parameter, Unreachable");

        var failIfFoundOption = new Option<bool>(
            ["--fail-if-found"],
            "Exit with error code if any dead code is found (for CI)");

        command.AddArgument(pathArg);
        command.AddOption(fileOption);
        command.AddOption(includeVarsOption);
        command.AddOption(noVarsOption);
        command.AddOption(includeFuncsOption);
        command.AddOption(noFuncsOption);
        command.AddOption(includeSignalsOption);
        command.AddOption(noSignalsOption);
        command.AddOption(includeParamsOption);
        command.AddOption(includePrivateOption);
        command.AddOption(includeUnreachableOption);
        command.AddOption(kindOption);
        command.AddOption(failIfFoundOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var file = context.ParseResult.GetValueForOption(fileOption);
            var includeVars = context.ParseResult.GetValueForOption(includeVarsOption);
            var noVars = context.ParseResult.GetValueForOption(noVarsOption);
            var includeFuncs = context.ParseResult.GetValueForOption(includeFuncsOption);
            var noFuncs = context.ParseResult.GetValueForOption(noFuncsOption);
            var includeSignals = context.ParseResult.GetValueForOption(includeSignalsOption);
            var noSignals = context.ParseResult.GetValueForOption(noSignalsOption);
            var includeParams = context.ParseResult.GetValueForOption(includeParamsOption);
            var includePrivate = context.ParseResult.GetValueForOption(includePrivateOption);
            var includeUnreachable = context.ParseResult.GetValueForOption(includeUnreachableOption);
            var kind = context.ParseResult.GetValueForOption(kindOption);
            var failIfFound = context.ParseResult.GetValueForOption(failIfFoundOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var debug = context.ParseResult.GetValueForOption(debugOption);

            var logger = GDCliLogger.FromFlags(quiet, verbose, debug);
            var formatter = CommandHelpers.GetFormatter(format);

            var options = new GDDeadCodeCommandOptions
            {
                FilePath = file,
                IncludeVariables = includeVars && !noVars,
                IncludeFunctions = includeFuncs && !noFuncs,
                IncludeSignals = includeSignals && !noSignals,
                IncludeParameters = includeParams,
                IncludePrivate = includePrivate,
                IncludeUnreachable = includeUnreachable,
                Kind = kind,
                FailIfFound = failIfFound
            };

            var cmd = new GDDeadCodeCommand(projectPath, formatter, logger: logger, options: options);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
