using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the validate command.
/// </summary>
public static class ValidateCommandBuilder
{
    public static Command Build(Option<string> globalFormatOption)
    {
        var command = new Command("validate", "Validate GDScript syntax and semantics");

        // Path argument
        var pathArg = new Argument<string>("project-path", () => ".", "Path to the Godot project");
        command.AddArgument(pathArg);

        // Check selection
        var checksOption = new Option<string?>(
            new[] { "--checks" },
            "Checks to run: syntax, scope, types, calls, controlflow, indentation, memberaccess, abstract, signals, resourcepaths, or 'all'");
        command.AddOption(checksOption);

        // Severity control
        var strictOption = new Option<bool>(
            new[] { "--strict" },
            "Treat all issues as errors");
        command.AddOption(strictOption);

        // Individual check toggles - basic checks
        var checkSyntaxOption = new Option<bool?>(
            new[] { "--check-syntax" },
            "Enable/disable syntax checking");
        var checkScopeOption = new Option<bool?>(
            new[] { "--check-scope" },
            "Enable/disable scope checking");
        var checkTypesOption = new Option<bool?>(
            new[] { "--check-types" },
            "Enable/disable type checking");
        var checkCallsOption = new Option<bool?>(
            new[] { "--check-calls" },
            "Enable/disable call checking");
        var checkControlFlowOption = new Option<bool?>(
            new[] { "--check-control-flow" },
            "Enable/disable control flow checking");
        var checkIndentationOption = new Option<bool?>(
            new[] { "--check-indentation" },
            "Enable/disable indentation checking");

        // Individual check toggles - advanced checks
        var checkMemberAccessOption = new Option<bool?>(
            new[] { "--check-member-access" },
            "Enable/disable member access checking on typed/untyped expressions (GD7xxx)");
        var checkAbstractOption = new Option<bool?>(
            new[] { "--check-abstract" },
            "Enable/disable @abstract annotation checking (GD8xxx)");
        var checkSignalsOption = new Option<bool?>(
            new[] { "--check-signals" },
            "Enable/disable signal operation validation");
        var checkResourcePathsOption = new Option<bool?>(
            new[] { "--check-resource-paths" },
            "Enable/disable resource path validation in load/preload calls");

        // Add basic options
        command.AddOption(checkSyntaxOption);
        command.AddOption(checkScopeOption);
        command.AddOption(checkTypesOption);
        command.AddOption(checkCallsOption);
        command.AddOption(checkControlFlowOption);
        command.AddOption(checkIndentationOption);

        // Add advanced options
        command.AddOption(checkMemberAccessOption);
        command.AddOption(checkAbstractOption);
        command.AddOption(checkSignalsOption);
        command.AddOption(checkResourcePathsOption);

        // Fail threshold
        var failOnOption = new Option<string?>(
            new[] { "--fail-on" },
            "Fail threshold: error (default), warning, or hint");
        command.AddOption(failOnOption);

        // Severity filtering
        var minSeverityOption = new Option<string?>(
            new[] { "--min-severity" },
            "Minimum severity to report: error, warning, info, or hint");
        var maxIssuesOption = new Option<int?>(
            new[] { "--max-issues" },
            "Maximum number of issues to report (0 = unlimited)");
        var groupByOption = new Option<string?>(
            new[] { "--group-by" },
            "Group output by: file (default), rule, or severity");
        command.AddOption(minSeverityOption);
        command.AddOption(maxIssuesOption);
        command.AddOption(groupByOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var checks = context.ParseResult.GetValueForOption(checksOption);
            var strict = context.ParseResult.GetValueForOption(strictOption);
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
            var validationChecks = OptionParsers.ParseValidationChecks(checks);

            // Apply individual check overrides
            var checkOverrides = new GDValidationCheckOverrides
            {
                CheckSyntax = context.ParseResult.GetValueForOption(checkSyntaxOption),
                CheckScope = context.ParseResult.GetValueForOption(checkScopeOption),
                CheckTypes = context.ParseResult.GetValueForOption(checkTypesOption),
                CheckCalls = context.ParseResult.GetValueForOption(checkCallsOption),
                CheckControlFlow = context.ParseResult.GetValueForOption(checkControlFlowOption),
                CheckIndentation = context.ParseResult.GetValueForOption(checkIndentationOption),
                CheckMemberAccess = context.ParseResult.GetValueForOption(checkMemberAccessOption),
                CheckAbstract = context.ParseResult.GetValueForOption(checkAbstractOption),
                CheckSignals = context.ParseResult.GetValueForOption(checkSignalsOption),
                CheckResourcePaths = context.ParseResult.GetValueForOption(checkResourcePathsOption)
            };

            validationChecks = checkOverrides.ApplyTo(validationChecks);

            // Build config with fail-on overrides
            var failOn = context.ParseResult.GetValueForOption(failOnOption);
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

            var cmd = new GDValidateCommand(projectPath, formatter, config: config, checks: validationChecks, strict: strict, minSeverity: minSev, maxIssues: maxIssues, groupBy: groupByMode);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
