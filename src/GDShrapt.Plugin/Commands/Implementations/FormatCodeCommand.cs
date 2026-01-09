using GDShrapt.Plugin.Config;
using GDShrapt.Reader;
using GDShrapt.Semantics;
using System;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

internal class FormatCodeCommand : Command
{
    public FormatCodeCommand(GDShraptPlugin plugin)
        : base(plugin)
    {
    }

    public override async Task Execute(IScriptEditor controller)
    {
        Logger.Info("Format code requested");

        if (!controller.IsValid)
        {
            Logger.Info("Format code cancelled: Editor is not valid");
            return;
        }

        var content = controller.Text;
        if (string.IsNullOrEmpty(content))
        {
            Logger.Info("Format code cancelled: No content to format");
            return;
        }

        try
        {
            // Create formatter with options from configuration
            var options = CreateFormatterOptions();
            var formatter = new GDFormatter(options);

            // Format the code
            var formatted = formatter.FormatCode(content);

            if (formatted == content)
            {
                Logger.Info("Code is already properly formatted");
                return;
            }

            // Replace content in editor
            controller.Text = formatted;

            Logger.Info("Code formatted successfully");
        }
        catch (Exception ex)
        {
            Logger.Error($"Format failed: {ex.Message}");
            Logger.Error(ex);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates GDFormatterOptions from plugin configuration.
    /// </summary>
    private GDFormatterOptions CreateFormatterOptions()
    {
        var config = Plugin.ConfigManager?.Config?.Formatter;
        if (config == null)
        {
            return GDFormatterOptions.Default;
        }

        return new GDFormatterOptions
        {
            // Indentation
            IndentStyle = config.IndentStyle == GDIndentationStyle.Tabs
                ? IndentStyle.Tabs
                : IndentStyle.Spaces,
            IndentSize = config.IndentSize,

            // Line endings
            LineEnding = MapLineEnding(config.LineEnding),

            // Blank lines
            BlankLinesBetweenFunctions = config.BlankLinesBetweenFunctions,
            BlankLinesAfterClassDeclaration = config.BlankLinesAfterClassDeclaration,
            BlankLinesBetweenMemberTypes = config.BlankLinesBetweenMemberTypes,

            // Spacing
            SpaceAroundOperators = config.SpaceAroundOperators,
            SpaceAfterComma = config.SpaceAfterComma,
            SpaceAfterColon = config.SpaceAfterColon,
            SpaceBeforeColon = config.SpaceBeforeColon,
            SpaceInsideParentheses = config.SpaceInsideParentheses,
            SpaceInsideBrackets = config.SpaceInsideBrackets,
            SpaceInsideBraces = config.SpaceInsideBraces,

            // Trailing whitespace
            RemoveTrailingWhitespace = config.RemoveTrailingWhitespace,
            EnsureTrailingNewline = config.EnsureTrailingNewline,

            // Line wrapping
            MaxLineLength = config.MaxLineLength,
            WrapLongLines = config.WrapLongLines,
            LineWrapStyle = MapLineWrapStyle(config.LineWrapStyle),

            // Auto type hints
            AutoAddTypeHints = config.AutoAddTypeHints,
            AutoAddTypeHintsToLocals = config.AutoAddTypeHintsToLocals,
            AutoAddTypeHintsToClassVariables = config.AutoAddTypeHintsToClassVariables,
            AutoAddTypeHintsToParameters = config.AutoAddTypeHintsToParameters,

            // Code reordering
            ReorderCode = config.ReorderCode
        };
    }

    private static Reader.LineEndingStyle MapLineEnding(GDLineEndingStyle style)
    {
        return style switch
        {
            GDLineEndingStyle.LF => Reader.LineEndingStyle.LF,
            GDLineEndingStyle.CRLF => Reader.LineEndingStyle.CRLF,
            GDLineEndingStyle.Platform => Reader.LineEndingStyle.Platform,
            _ => Reader.LineEndingStyle.LF
        };
    }

    private static Reader.LineWrapStyle MapLineWrapStyle(GDLineWrapStyle style)
    {
        return style switch
        {
            GDLineWrapStyle.AfterOpeningBracket => Reader.LineWrapStyle.AfterOpeningBracket,
            GDLineWrapStyle.BeforeElements => Reader.LineWrapStyle.BeforeElements,
            _ => Reader.LineWrapStyle.AfterOpeningBracket
        };
    }
}
