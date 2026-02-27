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
        var command = new Command("deps", "Analyze code/scene/signal dependencies, find cycles and coupling hotspots.\n\nExamples:\n  gdshrapt deps                            Analyze all dependencies\n  gdshrapt deps --graph code               Show only code dependencies\n  gdshrapt deps --explain --top 5          Show top 5 with edge details\n  gdshrapt deps --group-by dir --depth 2   Module-level dependency graph\n  gdshrapt deps --fail-on-cycles           CI mode: fail if cycles found");

        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        var projectOption = new Option<string?>(
            new[] { "--project", "-p" },
            "Path to the Godot project (alternative to positional argument)");

        var fileOption = new Option<string?>(
            ["--file", "-f"],
            "Analyze a specific file instead of the whole project");

        var graphOption = new Option<string>(
            ["--graph"],
            () => "all",
            "Graph layer: code, scenes, signals, all");

        var topNOption = new Option<int>(
            ["--top", "-n"],
            () => 10,
            "Number of top entries per section");

        var explainOption = new Option<bool>(
            ["--explain"],
            "Show edge types and reasons for dependencies");

        var groupByOption = new Option<string?>(
            ["--group-by"],
            "Aggregate by: dir (directory-level module graph)");

        var depthOption = new Option<int>(
            ["--depth"],
            () => 2,
            "Directory depth for --group-by dir");

        var dirOption = new Option<string?>(
            ["--dir"],
            "Restrict analysis to a directory subtree");

        var excludeAddonsOption = new Option<bool>(
            ["--exclude-addons"],
            "Exclude addons/ directory from analysis");

        var excludeTestsOption = new Option<bool>(
            ["--exclude-tests"],
            "Exclude test directories from analysis");

        var failOnCyclesOption = new Option<bool>(
            ["--fail-on-cycles"],
            "Exit with error if circular dependencies are found");

        command.AddArgument(pathArg);
        command.AddOption(projectOption);
        command.AddOption(fileOption);
        command.AddOption(graphOption);
        command.AddOption(topNOption);
        command.AddOption(explainOption);
        command.AddOption(groupByOption);
        command.AddOption(depthOption);
        command.AddOption(dirOption);
        command.AddOption(excludeAddonsOption);
        command.AddOption(excludeTestsOption);
        command.AddOption(failOnCyclesOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForOption(projectOption)
                ?? context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var file = context.ParseResult.GetValueForOption(fileOption);
            var graph = context.ParseResult.GetValueForOption(graphOption) ?? "all";
            var topN = context.ParseResult.GetValueForOption(topNOption);
            var explain = context.ParseResult.GetValueForOption(explainOption);
            var groupBy = context.ParseResult.GetValueForOption(groupByOption);
            var depth = context.ParseResult.GetValueForOption(depthOption);
            var dir = context.ParseResult.GetValueForOption(dirOption);
            var excludeAddons = context.ParseResult.GetValueForOption(excludeAddonsOption);
            var excludeTests = context.ParseResult.GetValueForOption(excludeTestsOption);
            var failOnCycles = context.ParseResult.GetValueForOption(failOnCyclesOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var debug = context.ParseResult.GetValueForOption(debugOption);

            var logLevel = context.ParseResult.GetValueForOption(logLevelOption);
            var logger = GDCliLogger.FromFlags(quiet, verbose, debug, logLevel);
            var formatter = CommandHelpers.GetFormatter(format);

            var graphLayer = graph.ToLowerInvariant() switch
            {
                "code" => GDDepsGraphLayer.Code,
                "scenes" => GDDepsGraphLayer.Scenes,
                "signals" => GDDepsGraphLayer.Signals,
                _ => GDDepsGraphLayer.All
            };

            var options = new GDDepsOptions
            {
                FilePath = file,
                FailOnCycles = failOnCycles,
                GraphLayer = graphLayer,
                TopN = topN,
                Explain = explain,
                GroupByDir = groupBy,
                GroupDepth = depth,
                Dir = dir,
                ExcludeAddons = excludeAddons,
                ExcludeTests = excludeTests
            };

            var cmd = new GDDepsCommand(projectPath, formatter, logger: logger, options: options);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
