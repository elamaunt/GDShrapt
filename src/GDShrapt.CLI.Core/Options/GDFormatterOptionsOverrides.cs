using GDShrapt.Reader;

namespace GDShrapt.CLI.Core;

/// <summary>
/// CLI overrides for formatter options. Null values mean "use config file value".
/// </summary>
public class GDFormatterOptionsOverrides
{
    // Indentation
    public IndentStyle? IndentStyle { get; set; }
    public int? IndentSize { get; set; }

    // Line endings
    public LineEndingStyle? LineEnding { get; set; }

    // Line length and wrapping
    public int? MaxLineLength { get; set; }
    public bool? WrapLongLines { get; set; }
    public LineWrapStyle? LineWrapStyle { get; set; }
    public int? ContinuationIndentSize { get; set; }
    public bool? UseBackslashContinuation { get; set; }

    // Spacing
    public bool? SpaceAroundOperators { get; set; }
    public bool? SpaceAfterComma { get; set; }
    public bool? SpaceAfterColon { get; set; }
    public bool? SpaceBeforeColon { get; set; }
    public bool? SpaceInsideParentheses { get; set; }
    public bool? SpaceInsideBrackets { get; set; }
    public bool? SpaceInsideBraces { get; set; }

    // Blank lines
    public int? BlankLinesBetweenFunctions { get; set; }
    public int? BlankLinesAfterClassDeclaration { get; set; }
    public int? BlankLinesBetweenMemberTypes { get; set; }

    // Cleanup
    public bool? RemoveTrailingWhitespace { get; set; }
    public bool? EnsureTrailingNewline { get; set; }
    public bool? RemoveMultipleTrailingNewlines { get; set; }

    // Advanced
    public bool? AutoAddTypeHints { get; set; }
    public bool? AutoAddTypeHintsToLocals { get; set; }
    public bool? AutoAddTypeHintsToClassVariables { get; set; }
    public bool? AutoAddTypeHintsToParameters { get; set; }
    public string? UnknownTypeFallback { get; set; }
    public bool? ReorderCode { get; set; }

    /// <summary>
    /// Applies overrides to the given formatter options.
    /// </summary>
    public void ApplyTo(GDFormatterOptions options)
    {
        // Indentation
        if (IndentStyle.HasValue)
            options.IndentStyle = IndentStyle.Value;
        if (IndentSize.HasValue)
            options.IndentSize = IndentSize.Value;

        // Line endings
        if (LineEnding.HasValue)
            options.LineEnding = LineEnding.Value;

        // Line length and wrapping
        if (MaxLineLength.HasValue)
            options.MaxLineLength = MaxLineLength.Value;
        if (WrapLongLines.HasValue)
            options.WrapLongLines = WrapLongLines.Value;
        if (LineWrapStyle.HasValue)
            options.LineWrapStyle = LineWrapStyle.Value;
        if (ContinuationIndentSize.HasValue)
            options.ContinuationIndentSize = ContinuationIndentSize.Value;
        if (UseBackslashContinuation.HasValue)
            options.UseBackslashContinuation = UseBackslashContinuation.Value;

        // Spacing
        if (SpaceAroundOperators.HasValue)
            options.SpaceAroundOperators = SpaceAroundOperators.Value;
        if (SpaceAfterComma.HasValue)
            options.SpaceAfterComma = SpaceAfterComma.Value;
        if (SpaceAfterColon.HasValue)
            options.SpaceAfterColon = SpaceAfterColon.Value;
        if (SpaceBeforeColon.HasValue)
            options.SpaceBeforeColon = SpaceBeforeColon.Value;
        if (SpaceInsideParentheses.HasValue)
            options.SpaceInsideParentheses = SpaceInsideParentheses.Value;
        if (SpaceInsideBrackets.HasValue)
            options.SpaceInsideBrackets = SpaceInsideBrackets.Value;
        if (SpaceInsideBraces.HasValue)
            options.SpaceInsideBraces = SpaceInsideBraces.Value;

        // Blank lines
        if (BlankLinesBetweenFunctions.HasValue)
            options.BlankLinesBetweenFunctions = BlankLinesBetweenFunctions.Value;
        if (BlankLinesAfterClassDeclaration.HasValue)
            options.BlankLinesAfterClassDeclaration = BlankLinesAfterClassDeclaration.Value;
        if (BlankLinesBetweenMemberTypes.HasValue)
            options.BlankLinesBetweenMemberTypes = BlankLinesBetweenMemberTypes.Value;

        // Cleanup
        if (RemoveTrailingWhitespace.HasValue)
            options.RemoveTrailingWhitespace = RemoveTrailingWhitespace.Value;
        if (EnsureTrailingNewline.HasValue)
            options.EnsureTrailingNewline = EnsureTrailingNewline.Value;
        if (RemoveMultipleTrailingNewlines.HasValue)
            options.RemoveMultipleTrailingNewlines = RemoveMultipleTrailingNewlines.Value;

        // Advanced
        if (AutoAddTypeHints.HasValue)
            options.AutoAddTypeHints = AutoAddTypeHints.Value;
        if (AutoAddTypeHintsToLocals.HasValue)
            options.AutoAddTypeHintsToLocals = AutoAddTypeHintsToLocals.Value;
        if (AutoAddTypeHintsToClassVariables.HasValue)
            options.AutoAddTypeHintsToClassVariables = AutoAddTypeHintsToClassVariables.Value;
        if (AutoAddTypeHintsToParameters.HasValue)
            options.AutoAddTypeHintsToParameters = AutoAddTypeHintsToParameters.Value;
        if (UnknownTypeFallback != null)
            options.UnknownTypeFallback = UnknownTypeFallback;
        if (ReorderCode.HasValue)
            options.ReorderCode = ReorderCode.Value;
    }
}
