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
    public static Command Build(Option<string> globalFormatOption)
    {
        var command = new Command("format", "Format GDScript files");

        // Path argument
        var pathArg = new Argument<string>("path", () => ".", "Path to file or directory");
        command.AddArgument(pathArg);

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
        command.AddOption(maxLineLengthOption);
        command.AddOption(wrapLongLinesOption);
        command.AddOption(lineWrapStyleOption);
        command.AddOption(continuationIndentOption);
        command.AddOption(useBackslashOption);

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
            var path = context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var check = context.ParseResult.GetValueForOption(checkOption);

            var formatter = CommandHelpers.GetFormatter(format);

            // Build overrides from CLI flags
            var overrides = new GDFormatterOptionsOverrides();

            // Indentation
            var indentStyle = context.ParseResult.GetValueForOption(indentStyleOption);
            if (indentStyle != null)
                overrides.IndentStyle = OptionParsers.ParseIndentStyle(indentStyle);

            overrides.IndentSize = context.ParseResult.GetValueForOption(indentSizeOption);

            // Line ending
            var lineEnding = context.ParseResult.GetValueForOption(lineEndingOption);
            if (lineEnding != null)
                overrides.LineEnding = OptionParsers.ParseLineEnding(lineEnding);

            // Line length and wrapping
            overrides.MaxLineLength = context.ParseResult.GetValueForOption(maxLineLengthOption);
            overrides.WrapLongLines = context.ParseResult.GetValueForOption(wrapLongLinesOption);

            var lineWrapStyle = context.ParseResult.GetValueForOption(lineWrapStyleOption);
            if (lineWrapStyle != null)
                overrides.LineWrapStyle = OptionParsers.ParseLineWrapStyle(lineWrapStyle);

            overrides.ContinuationIndentSize = context.ParseResult.GetValueForOption(continuationIndentOption);
            overrides.UseBackslashContinuation = context.ParseResult.GetValueForOption(useBackslashOption);

            // Spacing
            overrides.SpaceAroundOperators = context.ParseResult.GetValueForOption(spaceAroundOperatorsOption);
            overrides.SpaceAfterComma = context.ParseResult.GetValueForOption(spaceAfterCommaOption);
            overrides.SpaceAfterColon = context.ParseResult.GetValueForOption(spaceAfterColonOption);
            overrides.SpaceBeforeColon = context.ParseResult.GetValueForOption(spaceBeforeColonOption);
            overrides.SpaceInsideParentheses = context.ParseResult.GetValueForOption(spaceInsideParensOption);
            overrides.SpaceInsideBrackets = context.ParseResult.GetValueForOption(spaceInsideBracketsOption);
            overrides.SpaceInsideBraces = context.ParseResult.GetValueForOption(spaceInsideBracesOption);

            // Blank lines
            overrides.BlankLinesBetweenFunctions = context.ParseResult.GetValueForOption(blankLinesBetweenFunctionsOption);
            overrides.BlankLinesAfterClassDeclaration = context.ParseResult.GetValueForOption(blankLinesAfterClassDeclOption);
            overrides.BlankLinesBetweenMemberTypes = context.ParseResult.GetValueForOption(blankLinesBetweenMemberTypesOption);

            // Cleanup
            overrides.RemoveTrailingWhitespace = context.ParseResult.GetValueForOption(removeTrailingWhitespaceOption);
            overrides.EnsureTrailingNewline = context.ParseResult.GetValueForOption(ensureTrailingNewlineOption);
            overrides.RemoveMultipleTrailingNewlines = context.ParseResult.GetValueForOption(removeMultipleNewlinesOption);

            var cmd = new GDFormatCommand(path, formatter, dryRun: dryRun, checkOnly: check, optionsOverrides: overrides);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
