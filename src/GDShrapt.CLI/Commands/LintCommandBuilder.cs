using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the lint command.
/// </summary>
public static class LintCommandBuilder
{
    public static Command Build(
        Option<string> globalFormatOption,
        Option<bool> verboseOption,
        Option<bool> debugOption,
        Option<bool> quietOption)
    {
        var command = new Command("lint", "Lint GDScript files for style and best practices");

        // Path argument
        var pathArg = new Argument<string>("project-path", () => ".", "Path to the Godot project");
        command.AddArgument(pathArg);

        // Filtering options
        var rulesOption = new Option<string?>(
            new[] { "--rules", "-r" },
            "Only run specific rules (comma-separated, e.g., GDL001,GDL003)");
        var categoryOption = new Option<string?>(
            new[] { "--category" },
            "Only run rules in category (naming, style, best-practices)");
        command.AddOption(rulesOption);
        command.AddOption(categoryOption);

        // Naming convention options
        var classNameCaseOption = new Option<string?>(
            new[] { "--class-name-case" },
            "Naming case for classes (pascal, snake, camel, screaming, any)");
        var functionNameCaseOption = new Option<string?>(
            new[] { "--function-name-case" },
            "Naming case for functions (default: snake)");
        var variableNameCaseOption = new Option<string?>(
            new[] { "--variable-name-case" },
            "Naming case for variables (default: snake)");
        var constantNameCaseOption = new Option<string?>(
            new[] { "--constant-name-case" },
            "Naming case for constants (default: screaming)");
        var signalNameCaseOption = new Option<string?>(
            new[] { "--signal-name-case" },
            "Naming case for signals (default: snake)");
        var enumNameCaseOption = new Option<string?>(
            new[] { "--enum-name-case" },
            "Naming case for enums (default: pascal)");
        var enumValueCaseOption = new Option<string?>(
            new[] { "--enum-value-case" },
            "Naming case for enum values (default: screaming)");
        var innerClassNameCaseOption = new Option<string?>(
            new[] { "--inner-class-name-case" },
            "Naming case for inner classes (default: pascal)");
        var requireUnderscoreOption = new Option<bool?>(
            new[] { "--require-underscore-private" },
            "Require underscore prefix for private members");
        command.AddOption(classNameCaseOption);
        command.AddOption(functionNameCaseOption);
        command.AddOption(variableNameCaseOption);
        command.AddOption(constantNameCaseOption);
        command.AddOption(signalNameCaseOption);
        command.AddOption(enumNameCaseOption);
        command.AddOption(enumValueCaseOption);
        command.AddOption(innerClassNameCaseOption);
        command.AddOption(requireUnderscoreOption);

        // Limit options
        var maxLineLengthOption = new Option<int?>(
            new[] { "--max-line-length" },
            "Maximum line length (0 to disable)");
        var maxFileLinesOption = new Option<int?>(
            new[] { "--max-file-lines" },
            "Maximum lines per file (0 to disable)");
        var maxParametersOption = new Option<int?>(
            new[] { "--max-parameters" },
            "Maximum function parameters (0 to disable)");
        var maxFunctionLengthOption = new Option<int?>(
            new[] { "--max-function-length" },
            "Maximum function length in lines (0 to disable)");
        var maxComplexityOption = new Option<int?>(
            new[] { "--max-complexity" },
            "Maximum cyclomatic complexity (0 to disable)");

        // Complexity limits (new rules)
        var maxPublicMethodsOption = new Option<int?>(
            new[] { "--max-public-methods" },
            "Maximum public methods per class (0 to disable)");
        var maxReturnsOption = new Option<int?>(
            new[] { "--max-returns" },
            "Maximum return statements per function (0 to disable)");
        var maxNestingDepthOption = new Option<int?>(
            new[] { "--max-nesting-depth" },
            "Maximum nesting depth in a function (0 to disable)");
        var maxLocalVariablesOption = new Option<int?>(
            new[] { "--max-local-variables" },
            "Maximum local variables per function (0 to disable)");
        var maxClassVariablesOption = new Option<int?>(
            new[] { "--max-class-variables" },
            "Maximum class variables (0 to disable)");
        var maxBranchesOption = new Option<int?>(
            new[] { "--max-branches" },
            "Maximum branches in a function (0 to disable)");
        var maxBooleanExpressionsOption = new Option<int?>(
            new[] { "--max-boolean-expressions" },
            "Maximum boolean expressions in a condition (0 to disable)");
        var maxInnerClassesOption = new Option<int?>(
            new[] { "--max-inner-classes" },
            "Maximum inner classes per file (0 to disable)");

        command.AddOption(maxLineLengthOption);
        command.AddOption(maxFileLinesOption);
        command.AddOption(maxParametersOption);
        command.AddOption(maxFunctionLengthOption);
        command.AddOption(maxComplexityOption);
        command.AddOption(maxPublicMethodsOption);
        command.AddOption(maxReturnsOption);
        command.AddOption(maxNestingDepthOption);
        command.AddOption(maxLocalVariablesOption);
        command.AddOption(maxClassVariablesOption);
        command.AddOption(maxBranchesOption);
        command.AddOption(maxBooleanExpressionsOption);
        command.AddOption(maxInnerClassesOption);

        // Warning toggles
        var warnUnusedVariablesOption = new Option<bool?>(
            new[] { "--warn-unused-variables" },
            "Warn about unused variables");
        var warnUnusedParametersOption = new Option<bool?>(
            new[] { "--warn-unused-parameters" },
            "Warn about unused parameters");
        var warnUnusedSignalsOption = new Option<bool?>(
            new[] { "--warn-unused-signals" },
            "Warn about unused signals");
        var warnEmptyFunctionsOption = new Option<bool?>(
            new[] { "--warn-empty-functions" },
            "Warn about empty functions");
        var warnMagicNumbersOption = new Option<bool?>(
            new[] { "--warn-magic-numbers" },
            "Warn about magic numbers");
        var warnVariableShadowingOption = new Option<bool?>(
            new[] { "--warn-variable-shadowing" },
            "Warn about variable shadowing");
        var warnAwaitInLoopOption = new Option<bool?>(
            new[] { "--warn-await-in-loop" },
            "Warn about await in loops");
        var warnNoElifReturnOption = new Option<bool?>(
            new[] { "--warn-no-elif-return" },
            "Warn about unnecessary elif after return");
        var warnNoElseReturnOption = new Option<bool?>(
            new[] { "--warn-no-else-return" },
            "Warn about unnecessary else after return");
        var warnPrivateMethodCallOption = new Option<bool?>(
            new[] { "--warn-private-method-call" },
            "Warn about calling private methods on external objects");
        var warnDuplicatedLoadOption = new Option<bool?>(
            new[] { "--warn-duplicated-load" },
            "Warn about duplicated load/preload calls");
        var warnExpressionNotAssignedOption = new Option<bool?>(
            new[] { "--warn-expression-not-assigned" },
            "Warn when expression result is not assigned");
        var warnUselessAssignmentOption = new Option<bool?>(
            new[] { "--warn-useless-assignment" },
            "Warn when assigned value is never read before being overwritten");
        var warnInconsistentReturnOption = new Option<bool?>(
            new[] { "--warn-inconsistent-return" },
            "Warn when function has inconsistent return statements");
        var warnMissingReturnOption = new Option<bool?>(
            new[] { "--warn-missing-return" },
            "Warn when function with return type does not return in all code paths");
        var warnNoLonelyIfOption = new Option<bool?>(
            new[] { "--warn-no-lonely-if" },
            "Warn when if is the only statement in else block");
        var warnGodClassOption = new Option<bool?>(
            new[] { "--warn-god-class" },
            "Warn about god classes (classes with too many responsibilities)");
        var warnCommentedCodeOption = new Option<bool?>(
            new[] { "--warn-commented-code" },
            "Warn about commented-out code");
        var warnDebugPrintOption = new Option<bool?>(
            new[] { "--warn-debug-print" },
            "Warn about debug print statements");
        command.AddOption(warnUnusedVariablesOption);
        command.AddOption(warnUnusedParametersOption);
        command.AddOption(warnUnusedSignalsOption);
        command.AddOption(warnEmptyFunctionsOption);
        command.AddOption(warnMagicNumbersOption);
        command.AddOption(warnVariableShadowingOption);
        command.AddOption(warnAwaitInLoopOption);
        command.AddOption(warnNoElifReturnOption);
        command.AddOption(warnNoElseReturnOption);
        command.AddOption(warnPrivateMethodCallOption);
        command.AddOption(warnDuplicatedLoadOption);
        command.AddOption(warnExpressionNotAssignedOption);
        command.AddOption(warnUselessAssignmentOption);
        command.AddOption(warnInconsistentReturnOption);
        command.AddOption(warnMissingReturnOption);
        command.AddOption(warnNoLonelyIfOption);
        command.AddOption(warnGodClassOption);
        command.AddOption(warnCommentedCodeOption);
        command.AddOption(warnDebugPrintOption);

        // God class thresholds
        var godClassMaxVariablesOption = new Option<int?>(
            new[] { "--god-class-max-variables" },
            "Maximum variables for god class detection (default: 15)");
        var godClassMaxMethodsOption = new Option<int?>(
            new[] { "--god-class-max-methods" },
            "Maximum methods for god class detection (default: 20)");
        var godClassMaxLinesOption = new Option<int?>(
            new[] { "--god-class-max-lines" },
            "Maximum lines for god class detection (default: 500)");
        command.AddOption(godClassMaxVariablesOption);
        command.AddOption(godClassMaxMethodsOption);
        command.AddOption(godClassMaxLinesOption);

        // Strict typing options
        var strictTypingOption = new Option<string?>(
            new[] { "--strict-typing" },
            "Strict typing severity for all elements (error, warning, off)");
        var strictTypingClassVarsOption = new Option<string?>(
            new[] { "--strict-typing-class-vars" },
            "Strict typing for class variables (error, warning, off)");
        var strictTypingLocalVarsOption = new Option<string?>(
            new[] { "--strict-typing-local-vars" },
            "Strict typing for local variables (error, warning, off)");
        var strictTypingParamsOption = new Option<string?>(
            new[] { "--strict-typing-params" },
            "Strict typing for parameters (error, warning, off)");
        var strictTypingReturnOption = new Option<string?>(
            new[] { "--strict-typing-return" },
            "Strict typing for return types (error, warning, off)");
        command.AddOption(strictTypingOption);
        command.AddOption(strictTypingClassVarsOption);
        command.AddOption(strictTypingLocalVarsOption);
        command.AddOption(strictTypingParamsOption);
        command.AddOption(strictTypingReturnOption);

        // Suppression options
        var enableSuppressionOption = new Option<bool?>(
            new[] { "--enable-suppression" },
            "Enable inline comment suppression (gdlint:ignore)");
        command.AddOption(enableSuppressionOption);

        // Formatting/style checks
        var indentationStyleOption = new Option<string?>(
            new[] { "--indentation-style" },
            "Indentation style: tabs or spaces");
        var tabWidthOption = new Option<int?>(
            new[] { "--tab-width" },
            "Tab width in spaces (default: 4)");
        var checkTrailingWhitespaceOption = new Option<bool?>(
            new[] { "--check-trailing-whitespace" },
            "Check for trailing whitespace");
        var checkTrailingNewlineOption = new Option<bool?>(
            new[] { "--check-trailing-newline" },
            "Check for trailing newline at end of file");
        var checkSpaceAroundOperatorsOption = new Option<bool?>(
            new[] { "--check-space-around-operators" },
            "Check for space around operators");
        var checkSpaceAfterCommaOption = new Option<bool?>(
            new[] { "--check-space-after-comma" },
            "Check for space after commas");
        command.AddOption(indentationStyleOption);
        command.AddOption(tabWidthOption);
        command.AddOption(checkTrailingWhitespaceOption);
        command.AddOption(checkTrailingNewlineOption);
        command.AddOption(checkSpaceAroundOperatorsOption);
        command.AddOption(checkSpaceAfterCommaOption);

        // Blank lines config
        var emptyLinesBetweenFunctionsOption = new Option<int?>(
            new[] { "--empty-lines-between-functions" },
            "Required empty lines between functions (default: 2)");
        var maxConsecutiveEmptyLinesOption = new Option<int?>(
            new[] { "--max-consecutive-empty-lines" },
            "Maximum consecutive empty lines allowed");
        var requireBlankAfterClassOption = new Option<bool?>(
            new[] { "--require-blank-after-class" },
            "Require blank line after class declaration");
        var requireTwoBlankBetweenFunctionsOption = new Option<bool?>(
            new[] { "--require-two-blank-between-functions" },
            "Require two blank lines between functions");
        var requireBlankBetweenMemberTypesOption = new Option<bool?>(
            new[] { "--require-blank-between-member-types" },
            "Require blank line between different member types");
        command.AddOption(emptyLinesBetweenFunctionsOption);
        command.AddOption(maxConsecutiveEmptyLinesOption);
        command.AddOption(requireBlankAfterClassOption);
        command.AddOption(requireTwoBlankBetweenFunctionsOption);
        command.AddOption(requireBlankBetweenMemberTypesOption);

        // Best practices
        var suggestTypeHintsOption = new Option<bool?>(
            new[] { "--suggest-type-hints" },
            "Suggest adding type hints to variables");
        var requireTrailingCommaOption = new Option<bool?>(
            new[] { "--require-trailing-comma" },
            "Require trailing comma in multi-line collections");
        var enforceMemberOrderingOption = new Option<bool?>(
            new[] { "--enforce-member-ordering" },
            "Enforce member ordering in classes");
        command.AddOption(suggestTypeHintsOption);
        command.AddOption(requireTrailingCommaOption);
        command.AddOption(enforceMemberOrderingOption);

        // Magic numbers whitelist
        var allowedMagicNumbersOption = new Option<string?>(
            new[] { "--allowed-magic-numbers" },
            "Comma-separated list of allowed magic numbers (e.g., 0,1,2,-1)");
        command.AddOption(allowedMagicNumbersOption);

        // Member ordering options
        var abstractMethodPositionOption = new Option<string?>(
            new[] { "--abstract-method-position" },
            "Position of abstract methods: first, last, or none (no constraint)");
        var privateMethodPositionOption = new Option<string?>(
            new[] { "--private-method-position" },
            "Position of private methods: after_public, before_public, or none");
        var staticMethodPositionOption = new Option<string?>(
            new[] { "--static-method-position" },
            "Position of static methods: first, after_constants, or none");
        command.AddOption(abstractMethodPositionOption);
        command.AddOption(privateMethodPositionOption);
        command.AddOption(staticMethodPositionOption);

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
            var minSeverity = context.ParseResult.GetValueForOption(minSeverityOption);
            var maxIssues = context.ParseResult.GetValueForOption(maxIssuesOption);
            var groupBy = context.ParseResult.GetValueForOption(groupByOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var debug = context.ParseResult.GetValueForOption(debugOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);

            // Create logger from verbosity flags
            var logger = GDCliLogger.FromFlags(quiet, verbose, debug);

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

            // Parse min severity to linter severity
            GDLintSeverity? minSev = null;
            if (minSeverity != null)
            {
                minSev = minSeverity.ToLowerInvariant() switch
                {
                    "error" => GDLintSeverity.Error,
                    "warning" => GDLintSeverity.Warning,
                    "info" or "information" => GDLintSeverity.Info,
                    "hint" => GDLintSeverity.Hint,
                    _ => null
                };
            }

            var formatter = CommandHelpers.GetFormatter(format);

            // Build overrides from CLI flags
            var overrides = new GDLinterOptionsOverrides();

            // Filtering (these are handled differently - passed directly to command)
            overrides.Rules = context.ParseResult.GetValueForOption(rulesOption);
            overrides.Category = context.ParseResult.GetValueForOption(categoryOption);

            // Naming conventions
            var classNameCase = context.ParseResult.GetValueForOption(classNameCaseOption);
            if (classNameCase != null)
                overrides.ClassNameCase = OptionParsers.ParseNamingCase(classNameCase);

            var functionNameCase = context.ParseResult.GetValueForOption(functionNameCaseOption);
            if (functionNameCase != null)
                overrides.FunctionNameCase = OptionParsers.ParseNamingCase(functionNameCase);

            var variableNameCase = context.ParseResult.GetValueForOption(variableNameCaseOption);
            if (variableNameCase != null)
                overrides.VariableNameCase = OptionParsers.ParseNamingCase(variableNameCase);

            var constantNameCase = context.ParseResult.GetValueForOption(constantNameCaseOption);
            if (constantNameCase != null)
                overrides.ConstantNameCase = OptionParsers.ParseNamingCase(constantNameCase);

            var signalNameCase = context.ParseResult.GetValueForOption(signalNameCaseOption);
            if (signalNameCase != null)
                overrides.SignalNameCase = OptionParsers.ParseNamingCase(signalNameCase);

            var enumNameCase = context.ParseResult.GetValueForOption(enumNameCaseOption);
            if (enumNameCase != null)
                overrides.EnumNameCase = OptionParsers.ParseNamingCase(enumNameCase);

            var enumValueCase = context.ParseResult.GetValueForOption(enumValueCaseOption);
            if (enumValueCase != null)
                overrides.EnumValueCase = OptionParsers.ParseNamingCase(enumValueCase);

            var innerClassNameCase = context.ParseResult.GetValueForOption(innerClassNameCaseOption);
            if (innerClassNameCase != null)
                overrides.InnerClassNameCase = OptionParsers.ParseNamingCase(innerClassNameCase);

            overrides.RequireUnderscoreForPrivate = context.ParseResult.GetValueForOption(requireUnderscoreOption);

            // Limits
            overrides.MaxLineLength = context.ParseResult.GetValueForOption(maxLineLengthOption);
            overrides.MaxFileLines = context.ParseResult.GetValueForOption(maxFileLinesOption);
            overrides.MaxParameters = context.ParseResult.GetValueForOption(maxParametersOption);
            overrides.MaxFunctionLength = context.ParseResult.GetValueForOption(maxFunctionLengthOption);
            overrides.MaxCyclomaticComplexity = context.ParseResult.GetValueForOption(maxComplexityOption);

            // Complexity limits (new rules)
            overrides.MaxPublicMethods = context.ParseResult.GetValueForOption(maxPublicMethodsOption);
            overrides.MaxReturns = context.ParseResult.GetValueForOption(maxReturnsOption);
            overrides.MaxNestingDepth = context.ParseResult.GetValueForOption(maxNestingDepthOption);
            overrides.MaxLocalVariables = context.ParseResult.GetValueForOption(maxLocalVariablesOption);
            overrides.MaxClassVariables = context.ParseResult.GetValueForOption(maxClassVariablesOption);
            overrides.MaxBranches = context.ParseResult.GetValueForOption(maxBranchesOption);
            overrides.MaxBooleanExpressions = context.ParseResult.GetValueForOption(maxBooleanExpressionsOption);
            overrides.MaxInnerClasses = context.ParseResult.GetValueForOption(maxInnerClassesOption);

            // Warnings
            overrides.WarnUnusedVariables = context.ParseResult.GetValueForOption(warnUnusedVariablesOption);
            overrides.WarnUnusedParameters = context.ParseResult.GetValueForOption(warnUnusedParametersOption);
            overrides.WarnUnusedSignals = context.ParseResult.GetValueForOption(warnUnusedSignalsOption);
            overrides.WarnEmptyFunctions = context.ParseResult.GetValueForOption(warnEmptyFunctionsOption);
            overrides.WarnMagicNumbers = context.ParseResult.GetValueForOption(warnMagicNumbersOption);
            overrides.WarnVariableShadowing = context.ParseResult.GetValueForOption(warnVariableShadowingOption);
            overrides.WarnAwaitInLoop = context.ParseResult.GetValueForOption(warnAwaitInLoopOption);
            overrides.WarnNoElifReturn = context.ParseResult.GetValueForOption(warnNoElifReturnOption);
            overrides.WarnNoElseReturn = context.ParseResult.GetValueForOption(warnNoElseReturnOption);
            overrides.WarnPrivateMethodCall = context.ParseResult.GetValueForOption(warnPrivateMethodCallOption);
            overrides.WarnDuplicatedLoad = context.ParseResult.GetValueForOption(warnDuplicatedLoadOption);

            // New warnings (new rules)
            overrides.WarnExpressionNotAssigned = context.ParseResult.GetValueForOption(warnExpressionNotAssignedOption);
            overrides.WarnUselessAssignment = context.ParseResult.GetValueForOption(warnUselessAssignmentOption);
            overrides.WarnInconsistentReturn = context.ParseResult.GetValueForOption(warnInconsistentReturnOption);
            overrides.WarnMissingReturn = context.ParseResult.GetValueForOption(warnMissingReturnOption);
            overrides.WarnNoLonelyIf = context.ParseResult.GetValueForOption(warnNoLonelyIfOption);

            // God class, commented code, debug print
            overrides.WarnGodClass = context.ParseResult.GetValueForOption(warnGodClassOption);
            overrides.WarnCommentedCode = context.ParseResult.GetValueForOption(warnCommentedCodeOption);
            overrides.WarnDebugPrint = context.ParseResult.GetValueForOption(warnDebugPrintOption);
            overrides.GodClassMaxVariables = context.ParseResult.GetValueForOption(godClassMaxVariablesOption);
            overrides.GodClassMaxMethods = context.ParseResult.GetValueForOption(godClassMaxMethodsOption);
            overrides.GodClassMaxLines = context.ParseResult.GetValueForOption(godClassMaxLinesOption);

            // Strict typing
            var strictTyping = context.ParseResult.GetValueForOption(strictTypingOption);
            if (strictTyping != null)
            {
                var severity = OptionParsers.ParseSeverity(strictTyping);
                overrides.StrictTypingClassVariables = severity;
                overrides.StrictTypingLocalVariables = severity;
                overrides.StrictTypingParameters = severity;
                overrides.StrictTypingReturnTypes = severity;
            }

            var strictTypingClassVars = context.ParseResult.GetValueForOption(strictTypingClassVarsOption);
            if (strictTypingClassVars != null)
                overrides.StrictTypingClassVariables = OptionParsers.ParseSeverity(strictTypingClassVars);

            var strictTypingLocalVars = context.ParseResult.GetValueForOption(strictTypingLocalVarsOption);
            if (strictTypingLocalVars != null)
                overrides.StrictTypingLocalVariables = OptionParsers.ParseSeverity(strictTypingLocalVars);

            var strictTypingParams = context.ParseResult.GetValueForOption(strictTypingParamsOption);
            if (strictTypingParams != null)
                overrides.StrictTypingParameters = OptionParsers.ParseSeverity(strictTypingParams);

            var strictTypingReturn = context.ParseResult.GetValueForOption(strictTypingReturnOption);
            if (strictTypingReturn != null)
                overrides.StrictTypingReturnTypes = OptionParsers.ParseSeverity(strictTypingReturn);

            // Suppression
            overrides.EnableCommentSuppression = context.ParseResult.GetValueForOption(enableSuppressionOption);

            // Member ordering
            overrides.AbstractMethodPosition = context.ParseResult.GetValueForOption(abstractMethodPositionOption);
            overrides.PrivateMethodPosition = context.ParseResult.GetValueForOption(privateMethodPositionOption);
            overrides.StaticMethodPosition = context.ParseResult.GetValueForOption(staticMethodPositionOption);

            // Formatting/style checks
            var indentationStyle = context.ParseResult.GetValueForOption(indentationStyleOption);
            if (indentationStyle != null)
                overrides.IndentationStyle = OptionParsers.ParseIndentationStyle(indentationStyle);
            overrides.TabWidth = context.ParseResult.GetValueForOption(tabWidthOption);
            overrides.CheckTrailingWhitespace = context.ParseResult.GetValueForOption(checkTrailingWhitespaceOption);
            overrides.CheckTrailingNewline = context.ParseResult.GetValueForOption(checkTrailingNewlineOption);
            overrides.CheckSpaceAroundOperators = context.ParseResult.GetValueForOption(checkSpaceAroundOperatorsOption);
            overrides.CheckSpaceAfterComma = context.ParseResult.GetValueForOption(checkSpaceAfterCommaOption);

            // Blank lines config
            overrides.EmptyLinesBetweenFunctions = context.ParseResult.GetValueForOption(emptyLinesBetweenFunctionsOption);
            overrides.MaxConsecutiveEmptyLines = context.ParseResult.GetValueForOption(maxConsecutiveEmptyLinesOption);
            overrides.RequireBlankLineAfterClassDecl = context.ParseResult.GetValueForOption(requireBlankAfterClassOption);
            overrides.RequireTwoBlankLinesBetweenFunctions = context.ParseResult.GetValueForOption(requireTwoBlankBetweenFunctionsOption);
            overrides.RequireBlankLineBetweenMemberTypes = context.ParseResult.GetValueForOption(requireBlankBetweenMemberTypesOption);

            // Best practices
            overrides.SuggestTypeHints = context.ParseResult.GetValueForOption(suggestTypeHintsOption);
            overrides.RequireTrailingComma = context.ParseResult.GetValueForOption(requireTrailingCommaOption);
            overrides.EnforceMemberOrdering = context.ParseResult.GetValueForOption(enforceMemberOrderingOption);

            // Magic numbers whitelist
            overrides.AllowedMagicNumbers = context.ParseResult.GetValueForOption(allowedMagicNumbersOption);

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

            var cmd = new GDLintCommand(projectPath, formatter, config: config, minSeverity: minSev, maxIssues: maxIssues, groupBy: groupByMode, optionsOverrides: overrides, logger: logger);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
