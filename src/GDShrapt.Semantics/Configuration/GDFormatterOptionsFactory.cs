using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Factory for creating GDFormatterOptions from configuration.
/// Centralizes the mapping logic used by CLI, LSP, and Plugin.
/// </summary>
public static class GDFormatterOptionsFactory
{
    /// <summary>
    /// Creates GDFormatterOptions from project configuration.
    /// </summary>
    /// <param name="config">Project configuration.</param>
    /// <returns>Configured GDFormatterOptions instance.</returns>
    public static GDFormatterOptions FromConfig(GDProjectConfig config)
    {
        return FromConfig(config.Formatter);
    }

    /// <summary>
    /// Creates GDFormatterOptions from formatter configuration.
    /// </summary>
    /// <param name="formatter">Formatter configuration.</param>
    /// <returns>Configured GDFormatterOptions instance.</returns>
    public static GDFormatterOptions FromConfig(GDFormatterConfig formatter)
    {
        return new GDFormatterOptions
        {
            // Indentation
            IndentStyle = MapIndentStyle(formatter.IndentStyle),
            IndentSize = formatter.IndentSize,

            // Line endings
            LineEnding = MapLineEnding(formatter.LineEnding),

            // Blank lines
            BlankLinesBetweenFunctions = formatter.BlankLinesBetweenFunctions,
            BlankLinesAfterClassDeclaration = formatter.BlankLinesAfterClassDeclaration,
            BlankLinesBetweenMemberTypes = formatter.BlankLinesBetweenMemberTypes,

            // Spacing
            SpaceAroundOperators = formatter.SpaceAroundOperators,
            SpaceAfterComma = formatter.SpaceAfterComma,
            SpaceAfterColon = formatter.SpaceAfterColon,
            SpaceBeforeColon = formatter.SpaceBeforeColon,
            SpaceInsideParentheses = formatter.SpaceInsideParentheses,
            SpaceInsideBrackets = formatter.SpaceInsideBrackets,
            SpaceInsideBraces = formatter.SpaceInsideBraces,

            // Trailing whitespace
            RemoveTrailingWhitespace = formatter.RemoveTrailingWhitespace,
            EnsureTrailingNewline = formatter.EnsureTrailingNewline,

            // Line wrapping
            MaxLineLength = formatter.MaxLineLength,
            WrapLongLines = formatter.WrapLongLines,
            LineWrapStyle = MapLineWrapStyle(formatter.LineWrapStyle)
        };
    }

    /// <summary>
    /// Creates default GDFormatterOptions when no configuration is available.
    /// </summary>
    public static GDFormatterOptions CreateDefault()
    {
        return GDFormatterOptions.Default;
    }

    /// <summary>
    /// Maps GDIndentationStyle from configuration to IndentStyle used by formatter.
    /// </summary>
    public static IndentStyle MapIndentStyle(GDIndentationStyle style)
    {
        return style switch
        {
            GDIndentationStyle.Tabs => IndentStyle.Tabs,
            GDIndentationStyle.Spaces => IndentStyle.Spaces,
            _ => IndentStyle.Tabs
        };
    }

    /// <summary>
    /// Maps GDLineEndingStyle from configuration to LineEndingStyle used by formatter.
    /// </summary>
    public static LineEndingStyle MapLineEnding(GDLineEndingStyle style)
    {
        return style switch
        {
            GDLineEndingStyle.LF => LineEndingStyle.LF,
            GDLineEndingStyle.CRLF => LineEndingStyle.CRLF,
            GDLineEndingStyle.Platform => LineEndingStyle.Platform,
            _ => LineEndingStyle.LF
        };
    }

    /// <summary>
    /// Maps GDLineWrapStyle from configuration to LineWrapStyle used by formatter.
    /// </summary>
    public static LineWrapStyle MapLineWrapStyle(GDLineWrapStyle style)
    {
        return style switch
        {
            GDLineWrapStyle.AfterOpeningBracket => LineWrapStyle.AfterOpeningBracket,
            GDLineWrapStyle.BeforeElements => LineWrapStyle.BeforeElements,
            _ => LineWrapStyle.AfterOpeningBracket
        };
    }
}
