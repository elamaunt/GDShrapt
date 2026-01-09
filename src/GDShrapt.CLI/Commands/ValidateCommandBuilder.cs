using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Commands;

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
            "Checks to run: syntax, scope, types, calls, controlflow, indentation, or 'all'");
        command.AddOption(checksOption);

        // Severity control
        var strictOption = new Option<bool>(
            new[] { "--strict" },
            "Treat all issues as errors");
        command.AddOption(strictOption);

        // Individual check toggles
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
        command.AddOption(checkSyntaxOption);
        command.AddOption(checkScopeOption);
        command.AddOption(checkTypesOption);
        command.AddOption(checkCallsOption);
        command.AddOption(checkControlFlowOption);
        command.AddOption(checkIndentationOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var checks = context.ParseResult.GetValueForOption(checksOption);
            var strict = context.ParseResult.GetValueForOption(strictOption);

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
                CheckIndentation = context.ParseResult.GetValueForOption(checkIndentationOption)
            };

            validationChecks = checkOverrides.ApplyTo(validationChecks);

            var cmd = new GDValidateCommand(projectPath, formatter, checks: validationChecks, strict: strict);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
