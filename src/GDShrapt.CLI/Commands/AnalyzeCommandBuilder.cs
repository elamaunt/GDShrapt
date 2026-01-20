using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the analyze command.
/// </summary>
public static class AnalyzeCommandBuilder
{
    public static Command Build(Option<string> globalFormatOption)
    {
        var command = new Command("analyze", "Analyze a GDScript project and output diagnostics");

        var pathArg = new Argument<string>("project-path", () => ".", "Path to the Godot project");
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

        command.AddArgument(pathArg);
        command.AddOption(failOnOption);
        command.AddOption(minSeverityOption);
        command.AddOption(maxIssuesOption);
        command.AddOption(groupByOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var failOn = context.ParseResult.GetValueForOption(failOnOption);
            var minSeverity = context.ParseResult.GetValueForOption(minSeverityOption);
            var maxIssues = context.ParseResult.GetValueForOption(maxIssuesOption);
            var groupBy = context.ParseResult.GetValueForOption(groupByOption);

            // Parse group-by
            GDGroupBy groupByMode = GDGroupBy.File;
            if (groupBy != null)
            {
                groupByMode = groupBy.ToLowerInvariant() switch
                {
                    "rule" => GDGroupBy.Rule,
                    "severity" => GDGroupBy.Severity,
                    _ => GDGroupBy.File
                };
            }

            // Build config with fail-on overrides
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

            // Parse min severity
            GDSeverity? minSev = null;
            if (minSeverity != null)
            {
                minSev = minSeverity.ToLowerInvariant() switch
                {
                    "error" => GDSeverity.Error,
                    "warning" => GDSeverity.Warning,
                    "info" or "information" => GDSeverity.Information,
                    "hint" => GDSeverity.Hint,
                    _ => null
                };
            }

            var formatter = CommandHelpers.GetFormatter(format);
            var cmd = new GDAnalyzeCommand(projectPath, formatter, config: config, minSeverity: minSev, maxIssues: maxIssues, groupBy: groupByMode);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
