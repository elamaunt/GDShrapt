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
        Option<bool> quietOption,
        Option<string?> logLevelOption)
    {
        var command = new Command("lint", "Check GDScript files for style violations, naming conventions, and best practices.\n\nUse --preset to apply predefined rule sets, or configure individual\noptions below. Individual options override preset values.\nMost options can also be set in .gdshrapt.json (see 'gdshrapt init').\n\nExamples:\n  gdshrapt lint                            Lint current project\n  gdshrapt lint --preset strict            Use strict rules\n  gdshrapt lint --rules GDL001,GDL003      Run specific rules\n  gdshrapt lint --category naming          Only naming rules");

        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        var projectOption = new Option<string?>(
            new[] { "--project", "-p" },
            "Path to the Godot project (alternative to positional argument)");
        command.AddArgument(pathArg);
        command.AddOption(projectOption);

        var rulesOption = new Option<string?>(
            new[] { "--rules", "-r" },
            "Only run specific rules (comma-separated, e.g., GDL001,GDL003)");
        var categoryOption = new Option<string?>(
            new[] { "--category" },
            "Only run rules in category (naming, style, best-practices)");
        var presetOption = new Option<string?>(
            new[] { "--preset" },
            "Apply a built-in lint preset (strict, relaxed, recommended, gdquest). Individual flags override preset values.");
        command.AddOption(rulesOption);
        command.AddOption(categoryOption);
        command.AddOption(presetOption);

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
            var minSeverity = context.ParseResult.GetValueForOption(minSeverityOption);
            var maxIssues = context.ParseResult.GetValueForOption(maxIssuesOption);
            var groupBy = context.ParseResult.GetValueForOption(groupByOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var debug = context.ParseResult.GetValueForOption(debugOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);

            var logLevel = context.ParseResult.GetValueForOption(logLevelOption);
            var logger = GDCliLogger.FromFlags(quiet, verbose, debug, logLevel);

            var groupByMode = OptionParsers.ParseGroupBy(groupBy);
            var minSev = OptionParsers.ParseSeverity(minSeverity);

            var formatter = CommandHelpers.GetFormatter(format);

            // Load preset as baseline (if specified), then override with individual CLI flags
            var preset = context.ParseResult.GetValueForOption(presetOption);
            var overrides = GDLintPresets.GetPreset(preset) ?? new GDLinterOptionsOverrides();

            if (preset != null && GDLintPresets.GetPreset(preset) == null)
            {
                logger.Warning($"Unknown preset '{preset}'. Available presets: {string.Join(", ", GDLintPresets.AvailablePresets)}");
            }

            // Filtering (these are handled differently - passed directly to command)
            // Rules/Category always override preset (they filter which rules run, not rule config)
            var rulesValue = context.ParseResult.GetValueForOption(rulesOption);
            if (rulesValue != null)
                overrides.Rules = rulesValue;
            var categoryValue = context.ParseResult.GetValueForOption(categoryOption);
            if (categoryValue != null)
                overrides.Category = categoryValue;

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

            var requireUnderscore = context.ParseResult.GetValueForOption(requireUnderscoreOption);
            if (requireUnderscore.HasValue)
                overrides.RequireUnderscoreForPrivate = requireUnderscore;

            var maxLineLength = context.ParseResult.GetValueForOption(maxLineLengthOption);
            if (maxLineLength.HasValue)
                overrides.MaxLineLength = maxLineLength;
            var maxFileLines = context.ParseResult.GetValueForOption(maxFileLinesOption);
            if (maxFileLines.HasValue)
                overrides.MaxFileLines = maxFileLines;
            var maxParameters = context.ParseResult.GetValueForOption(maxParametersOption);
            if (maxParameters.HasValue)
                overrides.MaxParameters = maxParameters;
            var maxFunctionLength = context.ParseResult.GetValueForOption(maxFunctionLengthOption);
            if (maxFunctionLength.HasValue)
                overrides.MaxFunctionLength = maxFunctionLength;
            var maxComplexity = context.ParseResult.GetValueForOption(maxComplexityOption);
            if (maxComplexity.HasValue)
                overrides.MaxCyclomaticComplexity = maxComplexity;

            var maxPublicMethods = context.ParseResult.GetValueForOption(maxPublicMethodsOption);
            if (maxPublicMethods.HasValue)
                overrides.MaxPublicMethods = maxPublicMethods;
            var maxReturns = context.ParseResult.GetValueForOption(maxReturnsOption);
            if (maxReturns.HasValue)
                overrides.MaxReturns = maxReturns;
            var maxNestingDepth = context.ParseResult.GetValueForOption(maxNestingDepthOption);
            if (maxNestingDepth.HasValue)
                overrides.MaxNestingDepth = maxNestingDepth;
            var maxLocalVariables = context.ParseResult.GetValueForOption(maxLocalVariablesOption);
            if (maxLocalVariables.HasValue)
                overrides.MaxLocalVariables = maxLocalVariables;
            var maxClassVariables = context.ParseResult.GetValueForOption(maxClassVariablesOption);
            if (maxClassVariables.HasValue)
                overrides.MaxClassVariables = maxClassVariables;
            var maxBranches = context.ParseResult.GetValueForOption(maxBranchesOption);
            if (maxBranches.HasValue)
                overrides.MaxBranches = maxBranches;
            var maxBooleanExpressions = context.ParseResult.GetValueForOption(maxBooleanExpressionsOption);
            if (maxBooleanExpressions.HasValue)
                overrides.MaxBooleanExpressions = maxBooleanExpressions;
            var maxInnerClasses = context.ParseResult.GetValueForOption(maxInnerClassesOption);
            if (maxInnerClasses.HasValue)
                overrides.MaxInnerClasses = maxInnerClasses;

            var warnUnusedVariables = context.ParseResult.GetValueForOption(warnUnusedVariablesOption);
            if (warnUnusedVariables.HasValue)
                overrides.WarnUnusedVariables = warnUnusedVariables;
            var warnUnusedParameters = context.ParseResult.GetValueForOption(warnUnusedParametersOption);
            if (warnUnusedParameters.HasValue)
                overrides.WarnUnusedParameters = warnUnusedParameters;
            var warnUnusedSignals = context.ParseResult.GetValueForOption(warnUnusedSignalsOption);
            if (warnUnusedSignals.HasValue)
                overrides.WarnUnusedSignals = warnUnusedSignals;
            var warnEmptyFunctions = context.ParseResult.GetValueForOption(warnEmptyFunctionsOption);
            if (warnEmptyFunctions.HasValue)
                overrides.WarnEmptyFunctions = warnEmptyFunctions;
            var warnMagicNumbers = context.ParseResult.GetValueForOption(warnMagicNumbersOption);
            if (warnMagicNumbers.HasValue)
                overrides.WarnMagicNumbers = warnMagicNumbers;
            var warnVariableShadowing = context.ParseResult.GetValueForOption(warnVariableShadowingOption);
            if (warnVariableShadowing.HasValue)
                overrides.WarnVariableShadowing = warnVariableShadowing;
            var warnAwaitInLoop = context.ParseResult.GetValueForOption(warnAwaitInLoopOption);
            if (warnAwaitInLoop.HasValue)
                overrides.WarnAwaitInLoop = warnAwaitInLoop;
            var warnNoElifReturn = context.ParseResult.GetValueForOption(warnNoElifReturnOption);
            if (warnNoElifReturn.HasValue)
                overrides.WarnNoElifReturn = warnNoElifReturn;
            var warnNoElseReturn = context.ParseResult.GetValueForOption(warnNoElseReturnOption);
            if (warnNoElseReturn.HasValue)
                overrides.WarnNoElseReturn = warnNoElseReturn;
            var warnPrivateMethodCall = context.ParseResult.GetValueForOption(warnPrivateMethodCallOption);
            if (warnPrivateMethodCall.HasValue)
                overrides.WarnPrivateMethodCall = warnPrivateMethodCall;
            var warnDuplicatedLoad = context.ParseResult.GetValueForOption(warnDuplicatedLoadOption);
            if (warnDuplicatedLoad.HasValue)
                overrides.WarnDuplicatedLoad = warnDuplicatedLoad;

            var warnExpressionNotAssigned = context.ParseResult.GetValueForOption(warnExpressionNotAssignedOption);
            if (warnExpressionNotAssigned.HasValue)
                overrides.WarnExpressionNotAssigned = warnExpressionNotAssigned;
            var warnUselessAssignment = context.ParseResult.GetValueForOption(warnUselessAssignmentOption);
            if (warnUselessAssignment.HasValue)
                overrides.WarnUselessAssignment = warnUselessAssignment;
            var warnInconsistentReturn = context.ParseResult.GetValueForOption(warnInconsistentReturnOption);
            if (warnInconsistentReturn.HasValue)
                overrides.WarnInconsistentReturn = warnInconsistentReturn;
            var warnMissingReturn = context.ParseResult.GetValueForOption(warnMissingReturnOption);
            if (warnMissingReturn.HasValue)
                overrides.WarnMissingReturn = warnMissingReturn;
            var warnNoLonelyIf = context.ParseResult.GetValueForOption(warnNoLonelyIfOption);
            if (warnNoLonelyIf.HasValue)
                overrides.WarnNoLonelyIf = warnNoLonelyIf;

            var warnGodClass = context.ParseResult.GetValueForOption(warnGodClassOption);
            if (warnGodClass.HasValue)
                overrides.WarnGodClass = warnGodClass;
            var warnCommentedCode = context.ParseResult.GetValueForOption(warnCommentedCodeOption);
            if (warnCommentedCode.HasValue)
                overrides.WarnCommentedCode = warnCommentedCode;
            var warnDebugPrint = context.ParseResult.GetValueForOption(warnDebugPrintOption);
            if (warnDebugPrint.HasValue)
                overrides.WarnDebugPrint = warnDebugPrint;
            var godClassMaxVariables = context.ParseResult.GetValueForOption(godClassMaxVariablesOption);
            if (godClassMaxVariables.HasValue)
                overrides.GodClassMaxVariables = godClassMaxVariables;
            var godClassMaxMethods = context.ParseResult.GetValueForOption(godClassMaxMethodsOption);
            if (godClassMaxMethods.HasValue)
                overrides.GodClassMaxMethods = godClassMaxMethods;
            var godClassMaxLines = context.ParseResult.GetValueForOption(godClassMaxLinesOption);
            if (godClassMaxLines.HasValue)
                overrides.GodClassMaxLines = godClassMaxLines;

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

            var enableSuppression = context.ParseResult.GetValueForOption(enableSuppressionOption);
            if (enableSuppression.HasValue)
                overrides.EnableCommentSuppression = enableSuppression;

            var abstractMethodPosition = context.ParseResult.GetValueForOption(abstractMethodPositionOption);
            if (abstractMethodPosition != null)
                overrides.AbstractMethodPosition = abstractMethodPosition;
            var privateMethodPosition = context.ParseResult.GetValueForOption(privateMethodPositionOption);
            if (privateMethodPosition != null)
                overrides.PrivateMethodPosition = privateMethodPosition;
            var staticMethodPosition = context.ParseResult.GetValueForOption(staticMethodPositionOption);
            if (staticMethodPosition != null)
                overrides.StaticMethodPosition = staticMethodPosition;

            var indentationStyle = context.ParseResult.GetValueForOption(indentationStyleOption);
            if (indentationStyle != null)
                overrides.IndentationStyle = OptionParsers.ParseIndentationStyle(indentationStyle);
            var tabWidth = context.ParseResult.GetValueForOption(tabWidthOption);
            if (tabWidth.HasValue)
                overrides.TabWidth = tabWidth;
            var checkTrailingWhitespace = context.ParseResult.GetValueForOption(checkTrailingWhitespaceOption);
            if (checkTrailingWhitespace.HasValue)
                overrides.CheckTrailingWhitespace = checkTrailingWhitespace;
            var checkTrailingNewline = context.ParseResult.GetValueForOption(checkTrailingNewlineOption);
            if (checkTrailingNewline.HasValue)
                overrides.CheckTrailingNewline = checkTrailingNewline;
            var checkSpaceAroundOperators = context.ParseResult.GetValueForOption(checkSpaceAroundOperatorsOption);
            if (checkSpaceAroundOperators.HasValue)
                overrides.CheckSpaceAroundOperators = checkSpaceAroundOperators;
            var checkSpaceAfterComma = context.ParseResult.GetValueForOption(checkSpaceAfterCommaOption);
            if (checkSpaceAfterComma.HasValue)
                overrides.CheckSpaceAfterComma = checkSpaceAfterComma;

            var emptyLinesBetweenFunctions = context.ParseResult.GetValueForOption(emptyLinesBetweenFunctionsOption);
            if (emptyLinesBetweenFunctions.HasValue)
                overrides.EmptyLinesBetweenFunctions = emptyLinesBetweenFunctions;
            var maxConsecutiveEmptyLines = context.ParseResult.GetValueForOption(maxConsecutiveEmptyLinesOption);
            if (maxConsecutiveEmptyLines.HasValue)
                overrides.MaxConsecutiveEmptyLines = maxConsecutiveEmptyLines;
            var requireBlankAfterClass = context.ParseResult.GetValueForOption(requireBlankAfterClassOption);
            if (requireBlankAfterClass.HasValue)
                overrides.RequireBlankLineAfterClassDecl = requireBlankAfterClass;
            var requireTwoBlankBetweenFunctions = context.ParseResult.GetValueForOption(requireTwoBlankBetweenFunctionsOption);
            if (requireTwoBlankBetweenFunctions.HasValue)
                overrides.RequireTwoBlankLinesBetweenFunctions = requireTwoBlankBetweenFunctions;
            var requireBlankBetweenMemberTypes = context.ParseResult.GetValueForOption(requireBlankBetweenMemberTypesOption);
            if (requireBlankBetweenMemberTypes.HasValue)
                overrides.RequireBlankLineBetweenMemberTypes = requireBlankBetweenMemberTypes;

            var suggestTypeHints = context.ParseResult.GetValueForOption(suggestTypeHintsOption);
            if (suggestTypeHints.HasValue)
                overrides.SuggestTypeHints = suggestTypeHints;
            var requireTrailingComma = context.ParseResult.GetValueForOption(requireTrailingCommaOption);
            if (requireTrailingComma.HasValue)
                overrides.RequireTrailingComma = requireTrailingComma;
            var enforceMemberOrdering = context.ParseResult.GetValueForOption(enforceMemberOrderingOption);
            if (enforceMemberOrdering.HasValue)
                overrides.EnforceMemberOrdering = enforceMemberOrdering;

            var allowedMagicNumbers = context.ParseResult.GetValueForOption(allowedMagicNumbersOption);
            if (allowedMagicNumbers != null)
                overrides.AllowedMagicNumbers = allowedMagicNumbers;

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

            var exclude = context.ParseResult.GetValueForOption(excludeOption);
            var cmd = new GDLintCommand(projectPath, formatter, config: config, minSeverity: minSev, maxIssues: maxIssues, groupBy: groupByMode, optionsOverrides: overrides, logger: logger, cliExcludePatterns: exclude?.ToList());
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
