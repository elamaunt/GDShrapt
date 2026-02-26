using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the validate command.
/// </summary>
public static class ValidateCommandBuilder
{
    public static Command Build(
        Option<string> globalFormatOption,
        Option<bool> verboseOption,
        Option<bool> debugOption,
        Option<bool> quietOption,
        Option<string?> logLevelOption)
    {
        var command = new Command("validate", "Validate GDScript for syntax errors, type mismatches, and semantic issues.\n\nExamples:\n  gdshrapt validate                        Validate current project\n  gdshrapt validate --check-types          Only type checking\n  gdshrapt validate --strict               Treat all issues as errors");

        // Path argument
        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        var projectOption = new Option<string?>(
            new[] { "--project", "-p" },
            "Path to the Godot project (alternative to positional argument)");
        command.AddArgument(pathArg);
        command.AddOption(projectOption);

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

        // Nullable/typing options
        var nullableStrictnessOption = new Option<string?>(
            new[] { "--nullable-strictness" },
            "Nullable access check strictness: error, strict, normal, relaxed, off");
        var warnDictionaryIndexerOption = new Option<bool?>(
            new[] { "--warn-dictionary-indexer" },
            "Warn on Dictionary indexer access (values may be null)");
        var warnUntypedParametersOption = new Option<bool?>(
            new[] { "--warn-untyped-parameters" },
            "Warn on untyped function parameters");
        command.AddOption(nullableStrictnessOption);
        command.AddOption(warnDictionaryIndexerOption);
        command.AddOption(warnUntypedParametersOption);

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
        var excludeOption = new Option<string[]>(
            ["--exclude"],
            "Glob patterns to exclude files (repeatable, e.g. addons/** .godot/**)")
        {
            AllowMultipleArgumentsPerToken = true
        };

        command.AddOption(minSeverityOption);
        command.AddOption(maxIssuesOption);
        command.AddOption(groupByOption);
        command.AddOption(excludeOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForOption(projectOption)
                ?? context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var checks = context.ParseResult.GetValueForOption(checksOption);
            var strict = context.ParseResult.GetValueForOption(strictOption);
            var minSeverity = context.ParseResult.GetValueForOption(minSeverityOption);
            var maxIssues = context.ParseResult.GetValueForOption(maxIssuesOption);
            var groupBy = context.ParseResult.GetValueForOption(groupByOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var debug = context.ParseResult.GetValueForOption(debugOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);

            var logLevel = context.ParseResult.GetValueForOption(logLevelOption);
            var logger = GDCliLogger.FromFlags(quiet, verbose, debug, logLevel);

            var groupByMode = OptionParsers.ParseGroupBy(groupBy);
            var minSev = OptionParsers.ParseGDSeverity(minSeverity);

            var formatter = CommandHelpers.GetFormatter(format);
            var validationChecks = OptionParsers.ParseValidationChecks(checks);

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

            var validationConfigOverrides = new GDValidationConfigOverrides
            {
                NullableStrictness = context.ParseResult.GetValueForOption(nullableStrictnessOption),
                WarnOnDictionaryIndexer = context.ParseResult.GetValueForOption(warnDictionaryIndexerOption),
                WarnOnUntypedParameters = context.ParseResult.GetValueForOption(warnUntypedParametersOption)
            };

            var failOn = context.ParseResult.GetValueForOption(failOnOption);
            GDProjectConfig? config = null;
            if (failOn != null || validationConfigOverrides.NullableStrictness != null ||
                validationConfigOverrides.WarnOnDictionaryIndexer.HasValue ||
                validationConfigOverrides.WarnOnUntypedParameters.HasValue)
            {
                config = new GDProjectConfig();
                validationConfigOverrides.ApplyTo(config.Validation);

                if (failOn != null)
                {
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
            }

            var exclude = context.ParseResult.GetValueForOption(excludeOption);
            var cmd = new GDValidateCommand(projectPath, formatter, config: config, checks: validationChecks, strict: strict, minSeverity: minSev, maxIssues: maxIssues, groupBy: groupByMode, logger: logger, cliExcludePatterns: exclude?.ToList());
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
