namespace GDShrapt.Semantics;

/// <summary>
/// Code formatter configuration using GDShrapt.Formatter.
/// Maps to GDFormatterOptions from GDShrapt.Formatter library.
/// </summary>
public class GDFormatterConfig
{
    // Indentation
    /// <summary>
    /// Indentation style: Tabs or Spaces. Default: Tabs.
    /// </summary>
    public GDIndentationStyle IndentStyle { get; set; } = GDIndentationStyle.Tabs;

    /// <summary>
    /// Number of spaces per indentation level. Default: 4.
    /// </summary>
    public int IndentSize { get; set; } = 4;

    // Line endings
    /// <summary>
    /// Line ending style. Default: LF.
    /// </summary>
    public GDLineEndingStyle LineEnding { get; set; } = GDLineEndingStyle.LF;

    // Blank lines
    /// <summary>
    /// Number of blank lines between top-level functions. Default: 2.
    /// </summary>
    public int BlankLinesBetweenFunctions { get; set; } = 2;

    /// <summary>
    /// Number of blank lines after class declaration. Default: 1.
    /// </summary>
    public int BlankLinesAfterClassDeclaration { get; set; } = 1;

    /// <summary>
    /// Number of blank lines between different member types. Default: 1.
    /// </summary>
    public int BlankLinesBetweenMemberTypes { get; set; } = 1;

    // Spacing
    /// <summary>
    /// Add space around binary operators. Default: true.
    /// </summary>
    public bool SpaceAroundOperators { get; set; } = true;

    /// <summary>
    /// Add space after commas. Default: true.
    /// </summary>
    public bool SpaceAfterComma { get; set; } = true;

    /// <summary>
    /// Add space after colons in type hints. Default: true.
    /// </summary>
    public bool SpaceAfterColon { get; set; } = true;

    /// <summary>
    /// Add space before colons in type hints. Default: false.
    /// </summary>
    public bool SpaceBeforeColon { get; set; } = false;

    /// <summary>
    /// Add space inside parentheses. Default: false.
    /// </summary>
    public bool SpaceInsideParentheses { get; set; } = false;

    /// <summary>
    /// Add space inside brackets. Default: false.
    /// </summary>
    public bool SpaceInsideBrackets { get; set; } = false;

    /// <summary>
    /// Add space inside braces. Default: true.
    /// </summary>
    public bool SpaceInsideBraces { get; set; } = true;

    // Trailing whitespace
    /// <summary>
    /// Remove trailing whitespace from lines. Default: true.
    /// </summary>
    public bool RemoveTrailingWhitespace { get; set; } = true;

    /// <summary>
    /// Ensure file ends with a newline. Default: true.
    /// </summary>
    public bool EnsureTrailingNewline { get; set; } = true;

    // Line wrapping
    /// <summary>
    /// Maximum line length. 0 to disable. Default: 100.
    /// </summary>
    public int MaxLineLength { get; set; } = 100;

    /// <summary>
    /// Enable automatic line wrapping for long lines. Default: true.
    /// </summary>
    public bool WrapLongLines { get; set; } = true;

    /// <summary>
    /// Style of line wrapping. Default: AfterOpeningBracket.
    /// </summary>
    public GDLineWrapStyle LineWrapStyle { get; set; } = GDLineWrapStyle.AfterOpeningBracket;

    // Auto type hints
    /// <summary>
    /// Automatically add inferred type hints. Default: false.
    /// </summary>
    public bool AutoAddTypeHints { get; set; } = false;

    /// <summary>
    /// Add type hints to local variables. Default: true.
    /// </summary>
    public bool AutoAddTypeHintsToLocals { get; set; } = true;

    /// <summary>
    /// Add type hints to class variables. Default: true.
    /// </summary>
    public bool AutoAddTypeHintsToClassVariables { get; set; } = true;

    /// <summary>
    /// Add type hints to function parameters. Default: false.
    /// </summary>
    public bool AutoAddTypeHintsToParameters { get; set; } = false;

    // Code reordering
    /// <summary>
    /// Enable code member reordering. Default: false.
    /// </summary>
    public bool ReorderCode { get; set; } = false;
}

/// <summary>
/// Line ending style options.
/// </summary>
public enum GDLineEndingStyle
{
    /// <summary>
    /// Unix style line endings (\n).
    /// </summary>
    LF,

    /// <summary>
    /// Windows style line endings (\r\n).
    /// </summary>
    CRLF,

    /// <summary>
    /// Use the platform's default.
    /// </summary>
    Platform
}

/// <summary>
/// Style of line wrapping.
/// </summary>
public enum GDLineWrapStyle
{
    /// <summary>
    /// Wrap after opening bracket.
    /// </summary>
    AfterOpeningBracket,

    /// <summary>
    /// Wrap before elements.
    /// </summary>
    BeforeElements
}
