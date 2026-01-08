using GDShrapt.Plugin.Config;
using GDShrapt.Reader;
using System;
using Reader = GDShrapt.Reader;

namespace GDShrapt.Plugin.Formatting;

/// <summary>
/// Service for formatting GDScript code using configuration options.
/// </summary>
internal class FormattingService
{
    private readonly ConfigManager _configManager;

    public FormattingService(ConfigManager configManager)
    {
        _configManager = configManager;
    }

    /// <summary>
    /// Formats the given GDScript code according to project configuration.
    /// </summary>
    /// <param name="code">The code to format.</param>
    /// <returns>The formatted code.</returns>
    public string Format(string code)
    {
        if (string.IsNullOrEmpty(code))
            return code;

        try
        {
            var options = CreateFormatterOptions();
            var formatter = new GDFormatter(options);
            return formatter.FormatCode(code);
        }
        catch (Exception ex)
        {
            Logger.Error($"Formatting failed: {ex.Message}");
            return code;
        }
    }

    /// <summary>
    /// Formats a GDScript method declaration.
    /// </summary>
    /// <param name="method">The method to format.</param>
    /// <returns>Formatted method as string.</returns>
    public string FormatMethod(GDMethodDeclaration method)
    {
        if (method == null)
            return string.Empty;

        try
        {
            method.UpdateIntendation();
            var code = method.ToString();
            return Format(code);
        }
        catch (Exception ex)
        {
            Logger.Error($"Method formatting failed: {ex.Message}");
            return method.ToString();
        }
    }

    /// <summary>
    /// Formats a GDScript node to string using configured formatting.
    /// </summary>
    /// <param name="node">The AST node to format.</param>
    /// <returns>Formatted code.</returns>
    public string FormatNode(GDNode node)
    {
        if (node == null)
            return string.Empty;

        try
        {
            if (node is GDIntendedNode indented)
                indented.UpdateIntendation();

            var code = node.ToString();
            return Format(code);
        }
        catch (Exception ex)
        {
            Logger.Error($"Node formatting failed: {ex.Message}");
            return node.ToString();
        }
    }

    /// <summary>
    /// Creates formatter options from current configuration.
    /// </summary>
    public GDFormatterOptions CreateFormatterOptions()
    {
        var config = _configManager?.Config?.Formatter;
        if (config == null)
            return GDFormatterOptions.Default;

        return new GDFormatterOptions
        {
            // Indentation
            IndentStyle = config.IndentStyle == IndentationStyle.Tabs
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

    private static Reader.LineEndingStyle MapLineEnding(Config.LineEndingStyle style)
    {
        return style switch
        {
            Config.LineEndingStyle.LF => Reader.LineEndingStyle.LF,
            Config.LineEndingStyle.CRLF => Reader.LineEndingStyle.CRLF,
            Config.LineEndingStyle.Platform => Reader.LineEndingStyle.Platform,
            _ => Reader.LineEndingStyle.LF
        };
    }

    private static Reader.LineWrapStyle MapLineWrapStyle(Config.LineWrapStyle style)
    {
        return style switch
        {
            Config.LineWrapStyle.AfterOpeningBracket => Reader.LineWrapStyle.AfterOpeningBracket,
            Config.LineWrapStyle.BeforeElements => Reader.LineWrapStyle.BeforeElements,
            _ => Reader.LineWrapStyle.AfterOpeningBracket
        };
    }
}
