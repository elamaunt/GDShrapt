using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the analyze command.
/// </summary>
public static class AnalyzeCommandBuilder
{
    public static Command Build(
        Option<string> globalFormatOption,
        Option<bool> verboseOption,
        Option<bool> debugOption,
        Option<bool> quietOption,
        Option<string?> logLevelOption)
    {
        var command = new Command("analyze", "Analyze a GDScript project and report all diagnostics (validation + linting).\n\nExamples:\n  gdshrapt analyze                         Analyze current directory\n  gdshrapt analyze ./my-project            Analyze specific project\n  gdshrapt analyze --format json           Output as JSON\n  gdshrapt analyze --fail-on warning       Fail on warnings (for CI)");

        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        var projectOption = new Option<string?>(
            new[] { "--project", "-p" },
            "Path to the Godot project (alternative to positional argument)");
        var failOnOption = new Option<string?>(
            new[] { "--fail-on" },
            "Fail threshold: error (default), warning, or hint");
        var minSeverityOption = new Option<string?>(
            new[] { "--min-severity" },
            "Minimum severity to report: error, warning, info, or hint");
        var maxIssuesOption = new Option<int?>(
            new[] { "--max-issues" },
            "Maximum number of issues to report (0 = unlimited)");
        var groupByOption = new Option<string?>(
            new[] { "--group-by" },
            "Group output by: file (default), rule, or severity");

        var excludeOption = new Option<string[]>(
            ["--exclude"],
            "Glob patterns to exclude files (repeatable, e.g. addons/** .godot/**)")
        {
            AllowMultipleArgumentsPerToken = true
        };

        command.AddArgument(pathArg);
        command.AddOption(projectOption);
        command.AddOption(failOnOption);
        command.AddOption(minSeverityOption);
        command.AddOption(maxIssuesOption);
        command.AddOption(groupByOption);
        command.AddOption(excludeOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForOption(projectOption)
                ?? context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var failOn = context.ParseResult.GetValueForOption(failOnOption);
            var minSeverity = context.ParseResult.GetValueForOption(minSeverityOption);
            var maxIssues = context.ParseResult.GetValueForOption(maxIssuesOption);
            var groupBy = context.ParseResult.GetValueForOption(groupByOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var debug = context.ParseResult.GetValueForOption(debugOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);

            var logLevel = context.ParseResult.GetValueForOption(logLevelOption);
            var logger = GDCliLogger.FromFlags(quiet, verbose, debug, logLevel);

            var groupByMode = OptionParsers.ParseGroupBy(groupBy);

            GDProjectConfig? config = null;
            if (failOn != null)
            {
                config = new GDProjectConfig();
                switch (failOn.ToLowerInvariant())
                {
                    case "warning":
                        config.Cli.FailOnWarning = true;
                        break;
                    case "hint":
                        config.Cli.FailOnWarning = true;
                        config.Cli.FailOnHint = true;
                        break;
                }
            }

            var exclude = context.ParseResult.GetValueForOption(excludeOption);
            var minSev = OptionParsers.ParseGDSeverity(minSeverity);

            var formatter = CommandHelpers.GetFormatter(format);
            var cmd = new GDAnalyzeCommand(projectPath, formatter, config: config, minSeverity: minSev, maxIssues: maxIssues, groupBy: groupByMode, logger: logger, cliExcludePatterns: exclude?.ToList());
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
