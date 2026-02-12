using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the metrics command.
/// </summary>
public static class MetricsCommandBuilder
{
    public static Command Build(
        Option<string> globalFormatOption,
        Option<bool> verboseOption,
        Option<bool> debugOption,
        Option<bool> quietOption,
        Option<string?> logLevelOption)
    {
        var command = new Command("metrics", "Calculate cyclomatic complexity, maintainability index, and other metrics.\n\nExamples:\n  gdshrapt metrics                         All project metrics\n  gdshrapt metrics --sort-by complexity     Sort by complexity\n  gdshrapt metrics --top 10 --show-methods Top 10 with methods");

        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        var projectOption = new Option<string?>(
            new[] { "--project", "-p" },
            "Path to the Godot project (alternative to positional argument)");

        var fileOption = new Option<string?>(
            ["--file", "-f"],
            "Analyze a specific file instead of the whole project");

        var sortByOption = new Option<string?>(
            ["--sort-by", "-s"],
            "Sort files by: complexity, lines, methods, maintainability");

        var topOption = new Option<int>(
            ["--top", "-t"],
            () => 0,
            "Show only top N files (0 = all)");

        var showMethodsOption = new Option<bool>(
            ["--show-methods", "-m"],
            "Show method-level metrics");

        var showFilesOption = new Option<bool>(
            ["--show-files"],
            () => true,
            "Show file-level details");

        command.AddArgument(pathArg);
        command.AddOption(projectOption);
        command.AddOption(fileOption);
        command.AddOption(sortByOption);
        command.AddOption(topOption);
        command.AddOption(showMethodsOption);
        command.AddOption(showFilesOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForOption(projectOption)
                ?? context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var file = context.ParseResult.GetValueForOption(fileOption);
            var sortBy = context.ParseResult.GetValueForOption(sortByOption);
            var top = context.ParseResult.GetValueForOption(topOption);
            var showMethods = context.ParseResult.GetValueForOption(showMethodsOption);
            var showFiles = context.ParseResult.GetValueForOption(showFilesOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var debug = context.ParseResult.GetValueForOption(debugOption);

            var logLevel = context.ParseResult.GetValueForOption(logLevelOption);
            var logger = GDCliLogger.FromFlags(quiet, verbose, debug, logLevel);
            var formatter = CommandHelpers.GetFormatter(format);

            var options = new GDMetricsOptions
            {
                FilePath = file,
                SortBy = sortBy,
                Top = top,
                ShowMethods = showMethods,
                ShowFiles = showFiles
            };

            var cmd = new GDMetricsCommand(projectPath, formatter, logger: logger, options: options);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
