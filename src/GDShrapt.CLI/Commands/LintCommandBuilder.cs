using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;
using GDShrapt.Reader;

namespace GDShrapt.CLI.Commands;

/// <summary>
/// Builder for the lint command.
/// </summary>
public static class LintCommandBuilder
{
    public static Command Build(Option<string> globalFormatOption)
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
        command.AddOption(maxLineLengthOption);
        command.AddOption(maxFileLinesOption);
        command.AddOption(maxParametersOption);
        command.AddOption(maxFunctionLengthOption);
        command.AddOption(maxComplexityOption);

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

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";

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

            var cmd = new GDLintCommand(projectPath, formatter, optionsOverrides: overrides);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
