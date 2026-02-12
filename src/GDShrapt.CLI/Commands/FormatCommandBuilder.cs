using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;
using GDShrapt.Reader;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the format command.
/// </summary>
public static class FormatCommandBuilder
{
    public static Command Build(
        Option<string> globalFormatOption,
        Option<bool> verboseOption,
        Option<bool> debugOption,
        Option<bool> quietOption,
        Option<string?> logLevelOption)
    {
        var command = new Command("format", "Auto-format GDScript files (indentation, spacing, blank lines).\n\nExamples:\n  gdshrapt format                          Format current directory\n  gdshrapt format player.gd                Format a single file\n  gdshrapt format --dry-run                Preview changes\n  gdshrapt format --check                  Check formatting (for CI)");

        var pathArg = new Argument<string>("path", "Path to file or directory") { Arity = ArgumentArity.ZeroOrOne };
        var projectOption = new Option<string?>(
            new[] { "--project", "-p" },
            "Path to the Godot project (alternative to positional argument)");
        command.AddArgument(pathArg);
        command.AddOption(projectOption);

        // Mode options
        var dryRunOption = new Option<bool>(
            new[] { "--dry-run", "-n" },
            "Show what would be formatted without making changes");
        var checkOption = new Option<bool>(
            new[] { "--check", "-c" },
            "Check if files are formatted (exit 1 if not)");
        command.AddOption(dryRunOption);
        command.AddOption(checkOption);

        // Indentation options
        var indentStyleOption = new Option<string?>(
            new[] { "--indent-style" },
            "Indentation style (tabs, spaces)");
        var indentSizeOption = new Option<int?>(
            new[] { "--indent-size" },
            "Spaces per indent level (default: 4)");
        command.AddOption(indentStyleOption);
        command.AddOption(indentSizeOption);

        // Line ending options
        var lineEndingOption = new Option<string?>(
            new[] { "--line-ending" },
            "Line ending style (lf, crlf, platform)");
        command.AddOption(lineEndingOption);

        // Line length and wrapping options
        var maxLineLengthOption = new Option<int?>(
            new[] { "--max-line-length" },
            "Maximum line length (0 to disable wrapping)");
        var wrapLongLinesOption = new Option<bool?>(
            new[] { "--wrap-long-lines" },
            "Enable automatic line wrapping");
        var lineWrapStyleOption = new Option<string?>(
            new[] { "--line-wrap-style" },
            "Line wrap style (afteropen, before)");
        var continuationIndentOption = new Option<int?>(
            new[] { "--continuation-indent" },
            "Additional indent for wrapped lines");
        var useBackslashOption = new Option<bool?>(
            new[] { "--use-backslash" },
            "Use backslash continuation for method chains");
        var addTrailingCommaWrappedOption = new Option<bool?>(
            new[] { "--add-trailing-comma-wrapped" },
            "Add trailing comma when wrapping collections");
        var arrayWrapStyleOption = new Option<string?>(
            new[] { "--array-wrap-style" },
            "Array-specific wrap style (afteropen, before, hanging, compact, aligned)");
        var parameterWrapStyleOption = new Option<string?>(
            new[] { "--parameter-wrap-style" },
            "Parameter-specific wrap style (afteropen, before, hanging, compact, aligned)");
        var dictionaryWrapStyleOption = new Option<string?>(
            new[] { "--dictionary-wrap-style" },
            "Dictionary-specific wrap style (afteropen, before, hanging, compact, aligned)");
        var minChainLengthToWrapOption = new Option<int?>(
            new[] { "--min-chain-length-to-wrap" },
            "Minimum method chain length to trigger wrapping (default: 2)");
        command.AddOption(maxLineLengthOption);
        command.AddOption(wrapLongLinesOption);
        command.AddOption(lineWrapStyleOption);
        command.AddOption(continuationIndentOption);
        command.AddOption(useBackslashOption);
        command.AddOption(addTrailingCommaWrappedOption);
        command.AddOption(arrayWrapStyleOption);
        command.AddOption(parameterWrapStyleOption);
        command.AddOption(dictionaryWrapStyleOption);
        command.AddOption(minChainLengthToWrapOption);

        // Spacing options
        var spaceAroundOperatorsOption = new Option<bool?>(
            new[] { "--space-around-operators" },
            "Add spaces around operators");
        var spaceAfterCommaOption = new Option<bool?>(
            new[] { "--space-after-comma" },
            "Add space after commas");
        var spaceAfterColonOption = new Option<bool?>(
            new[] { "--space-after-colon" },
            "Add space after colons");
        var spaceBeforeColonOption = new Option<bool?>(
            new[] { "--space-before-colon" },
            "Add space before colons");
        var spaceInsideParensOption = new Option<bool?>(
            new[] { "--space-inside-parens" },
            "Add spaces inside parentheses");
        var spaceInsideBracketsOption = new Option<bool?>(
            new[] { "--space-inside-brackets" },
            "Add spaces inside brackets");
        var spaceInsideBracesOption = new Option<bool?>(
            new[] { "--space-inside-braces" },
            "Add spaces inside braces");
        command.AddOption(spaceAroundOperatorsOption);
        command.AddOption(spaceAfterCommaOption);
        command.AddOption(spaceAfterColonOption);
        command.AddOption(spaceBeforeColonOption);
        command.AddOption(spaceInsideParensOption);
        command.AddOption(spaceInsideBracketsOption);
        command.AddOption(spaceInsideBracesOption);

        // Blank lines options
        var blankLinesBetweenFunctionsOption = new Option<int?>(
            new[] { "--blank-lines-between-functions" },
            "Blank lines between functions (default: 2)");
        var blankLinesAfterClassDeclOption = new Option<int?>(
            new[] { "--blank-lines-after-class" },
            "Blank lines after class declaration (default: 1)");
        var blankLinesBetweenMemberTypesOption = new Option<int?>(
            new[] { "--blank-lines-between-members" },
            "Blank lines between different member types (default: 1)");
        command.AddOption(blankLinesBetweenFunctionsOption);
        command.AddOption(blankLinesAfterClassDeclOption);
        command.AddOption(blankLinesBetweenMemberTypesOption);

        // Cleanup options
        var removeTrailingWhitespaceOption = new Option<bool?>(
            new[] { "--remove-trailing-whitespace" },
            "Remove trailing whitespace");
        var ensureTrailingNewlineOption = new Option<bool?>(
            new[] { "--ensure-trailing-newline" },
            "Ensure file ends with newline");
        var removeMultipleNewlinesOption = new Option<bool?>(
            new[] { "--remove-multiple-newlines" },
            "Remove multiple trailing newlines");
        command.AddOption(removeTrailingWhitespaceOption);
        command.AddOption(ensureTrailingNewlineOption);
        command.AddOption(removeMultipleNewlinesOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var path = context.ParseResult.GetValueForOption(projectOption)
                ?? context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var check = context.ParseResult.GetValueForOption(checkOption);

            var formatter = CommandHelpers.GetFormatter(format);

            var overrides = new GDFormatterOptionsOverrides();

            var indentStyle = context.ParseResult.GetValueForOption(indentStyleOption);
            if (indentStyle != null)
                overrides.IndentStyle = OptionParsers.ParseIndentStyle(indentStyle);

            overrides.IndentSize = context.ParseResult.GetValueForOption(indentSizeOption);

            var lineEnding = context.ParseResult.GetValueForOption(lineEndingOption);
            if (lineEnding != null)
                overrides.LineEnding = OptionParsers.ParseLineEnding(lineEnding);

            overrides.MaxLineLength = context.ParseResult.GetValueForOption(maxLineLengthOption);
            overrides.WrapLongLines = context.ParseResult.GetValueForOption(wrapLongLinesOption);

            var lineWrapStyle = context.ParseResult.GetValueForOption(lineWrapStyleOption);
            if (lineWrapStyle != null)
                overrides.LineWrapStyle = OptionParsers.ParseLineWrapStyle(lineWrapStyle);

            overrides.ContinuationIndentSize = context.ParseResult.GetValueForOption(continuationIndentOption);
            overrides.UseBackslashContinuation = context.ParseResult.GetValueForOption(useBackslashOption);
            overrides.AddTrailingCommaWhenWrapped = context.ParseResult.GetValueForOption(addTrailingCommaWrappedOption);

            var arrayWrapStyle = context.ParseResult.GetValueForOption(arrayWrapStyleOption);
            if (arrayWrapStyle != null)
                overrides.ArrayWrapStyle = OptionParsers.ParseLineWrapStyleExtended(arrayWrapStyle);

            var parameterWrapStyle = context.ParseResult.GetValueForOption(parameterWrapStyleOption);
            if (parameterWrapStyle != null)
                overrides.ParameterWrapStyle = OptionParsers.ParseLineWrapStyleExtended(parameterWrapStyle);

            var dictionaryWrapStyle = context.ParseResult.GetValueForOption(dictionaryWrapStyleOption);
            if (dictionaryWrapStyle != null)
                overrides.DictionaryWrapStyle = OptionParsers.ParseLineWrapStyleExtended(dictionaryWrapStyle);

            overrides.MinMethodChainLengthToWrap = context.ParseResult.GetValueForOption(minChainLengthToWrapOption);

            overrides.SpaceAroundOperators = context.ParseResult.GetValueForOption(spaceAroundOperatorsOption);
            overrides.SpaceAfterComma = context.ParseResult.GetValueForOption(spaceAfterCommaOption);
            overrides.SpaceAfterColon = context.ParseResult.GetValueForOption(spaceAfterColonOption);
            overrides.SpaceBeforeColon = context.ParseResult.GetValueForOption(spaceBeforeColonOption);
            overrides.SpaceInsideParentheses = context.ParseResult.GetValueForOption(spaceInsideParensOption);
            overrides.SpaceInsideBrackets = context.ParseResult.GetValueForOption(spaceInsideBracketsOption);
            overrides.SpaceInsideBraces = context.ParseResult.GetValueForOption(spaceInsideBracesOption);

            overrides.BlankLinesBetweenFunctions = context.ParseResult.GetValueForOption(blankLinesBetweenFunctionsOption);
            overrides.BlankLinesAfterClassDeclaration = context.ParseResult.GetValueForOption(blankLinesAfterClassDeclOption);
            overrides.BlankLinesBetweenMemberTypes = context.ParseResult.GetValueForOption(blankLinesBetweenMemberTypesOption);

            overrides.RemoveTrailingWhitespace = context.ParseResult.GetValueForOption(removeTrailingWhitespaceOption);
            overrides.EnsureTrailingNewline = context.ParseResult.GetValueForOption(ensureTrailingNewlineOption);
            overrides.RemoveMultipleTrailingNewlines = context.ParseResult.GetValueForOption(removeMultipleNewlinesOption);

            var cmd = new GDFormatCommand(path, formatter, dryRun: dryRun, checkOnly: check, optionsOverrides: overrides);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
