using System.Collections.Generic;
using GDShrapt.Plugin.TodoTags;

namespace GDShrapt.Plugin.Config;

/// <summary>
/// Root configuration for GDShrapt plugin settings.
/// Stored in .gdshrapt.json in project root for team sharing.
/// </summary>
internal class ProjectConfig
{
    /// <summary>
    /// UI panels and docks enable/disable settings.
    /// </summary>
    public UIConfig UI { get; set; } = new();

    /// <summary>
    /// Linting and formatting rules configuration.
    /// </summary>
    public LintingConfig Linting { get; set; } = new();

    /// <summary>
    /// Code formatter configuration using GDShrapt.Formatter.
    /// </summary>
    public FormatterConfig Formatter { get; set; } = new();

    /// <summary>
    /// Advanced linting configuration using GDShrapt.Linter.
    /// </summary>
    public AdvancedLintingConfig AdvancedLinting { get; set; } = new();

    /// <summary>
    /// Notification panel behavior configuration.
    /// </summary>
    public NotificationConfig Notifications { get; set; } = new();

    /// <summary>
    /// Cache settings configuration.
    /// </summary>
    public CacheConfig Cache { get; set; } = new();

    /// <summary>
    /// TODO/FIXME tags panel configuration.
    /// </summary>
    public TodoTagsConfig TodoTags { get; set; } = new();
}

/// <summary>
/// UI panels and docks configuration.
/// </summary>
internal class UIConfig
{
    /// <summary>
    /// Enable AST Viewer dock in bottom panel. Default: true
    /// </summary>
    public bool AstViewerEnabled { get; set; } = true;

    /// <summary>
    /// Enable CodeLens (inline reference counts above functions). Default: true
    /// </summary>
    public bool CodeLensEnabled { get; set; } = true;

    /// <summary>
    /// Enable reference counter badges on identifiers. Default: true
    /// </summary>
    public bool ReferencesCounterEnabled { get; set; } = true;

    /// <summary>
    /// Enable Problems dock in bottom panel. Default: true
    /// </summary>
    public bool ProblemsDockEnabled { get; set; } = true;

    /// <summary>
    /// Enable TODO Tags dock in bottom panel. Default: true
    /// </summary>
    public bool TodoTagsDockEnabled { get; set; } = true;

    /// <summary>
    /// Enable API Documentation dock in bottom panel. Default: true
    /// </summary>
    public bool ApiDocumentationDockEnabled { get; set; } = true;

    /// <summary>
    /// Enable Find References dock in bottom panel. Default: true
    /// </summary>
    public bool FindReferencesDockEnabled { get; set; } = true;
}

/// <summary>
/// Configuration for TODO/FIXME tags panel.
/// </summary>
internal class TodoTagsConfig
{
    /// <summary>
    /// Enable/disable TODO tags scanning. Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Scan on project startup. Default: true
    /// </summary>
    public bool ScanOnStartup { get; set; } = true;

    /// <summary>
    /// Auto-refresh when files change. Default: true
    /// </summary>
    public bool AutoRefresh { get; set; } = true;

    /// <summary>
    /// Case-sensitive tag matching. Default: false
    /// </summary>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// Default grouping mode. Default: ByFile
    /// </summary>
    public TodoGroupingMode DefaultGrouping { get; set; } = TodoGroupingMode.ByFile;

    /// <summary>
    /// Tag definitions with colors and enabled states.
    /// </summary>
    public List<TodoTagDefinition> Tags { get; set; } = new()
    {
        new TodoTagDefinition("TODO", "#4FC3F7", TodoPriority.Normal),
        new TodoTagDefinition("FIXME", "#FF8A65", TodoPriority.High),
        new TodoTagDefinition("HACK", "#FFD54F", TodoPriority.Normal),
        new TodoTagDefinition("NOTE", "#81C784", TodoPriority.Low),
        new TodoTagDefinition("BUG", "#EF5350", TodoPriority.High),
        new TodoTagDefinition("XXX", "#CE93D8", TodoPriority.Low)
    };

    /// <summary>
    /// Directories to exclude from scanning (relative to project root).
    /// </summary>
    public List<string> ExcludedDirectories { get; set; } = new()
    {
        ".godot",
        "addons"
    };
}

/// <summary>
/// Grouping mode for TODO items display.
/// </summary>
internal enum TodoGroupingMode
{
    /// <summary>
    /// Group by file.
    /// </summary>
    ByFile,

    /// <summary>
    /// Group by tag type.
    /// </summary>
    ByTag
}

/// <summary>
/// Configuration for linting and formatting rules.
/// </summary>
internal class LintingConfig
{
    /// <summary>
    /// Enable/disable all linting. Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Formatting check level. Default: Light
    /// </summary>
    public FormattingLevel FormattingLevel { get; set; } = FormattingLevel.Light;

    /// <summary>
    /// Indentation style preference.
    /// </summary>
    public IndentationStyle IndentationStyle { get; set; } = IndentationStyle.Tabs;

    /// <summary>
    /// Tab width for indentation (spaces). Default: 4
    /// </summary>
    public int TabWidth { get; set; } = 4;

    /// <summary>
    /// Maximum line length before warning. Default: 120, 0 = disabled
    /// </summary>
    public int MaxLineLength { get; set; } = 120;

    /// <summary>
    /// Per-rule configuration overrides.
    /// Key is rule ID (e.g., "GDS001").
    /// </summary>
    public Dictionary<string, RuleConfig> Rules { get; set; } = new();
}

/// <summary>
/// Formatting check strictness level.
/// </summary>
internal enum FormattingLevel
{
    /// <summary>
    /// No formatting checks.
    /// </summary>
    Off = 0,

    /// <summary>
    /// Basic checks: indentation, trailing whitespace, trailing newline.
    /// </summary>
    Light = 1,

    /// <summary>
    /// All formatting checks including spacing around operators, commas, etc.
    /// </summary>
    Full = 2
}

/// <summary>
/// Indentation style preference.
/// </summary>
internal enum IndentationStyle
{
    /// <summary>
    /// Use tabs for indentation (GDScript default).
    /// </summary>
    Tabs,

    /// <summary>
    /// Use spaces for indentation.
    /// </summary>
    Spaces
}

/// <summary>
/// Per-rule configuration.
/// </summary>
internal class RuleConfig
{
    /// <summary>
    /// Enable/disable this specific rule.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Override severity for this rule.
    /// </summary>
    public DiagnosticSeverity? Severity { get; set; }

    /// <summary>
    /// Rule-specific options.
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();
}

/// <summary>
/// Diagnostic severity levels.
/// </summary>
internal enum DiagnosticSeverity
{
    /// <summary>
    /// Subtle hint, lowest priority.
    /// </summary>
    Hint = 0,

    /// <summary>
    /// Informational message.
    /// </summary>
    Info = 1,

    /// <summary>
    /// Warning that should be addressed.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error that must be fixed.
    /// </summary>
    Error = 3
}

/// <summary>
/// Configuration for notification panel behavior.
/// </summary>
internal class NotificationConfig
{
    /// <summary>
    /// Enable/disable notification panel. Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Show expanded panel on first file open with problems. Default: true
    /// </summary>
    public bool ShowExpandedOnFirstOpen { get; set; } = true;

    /// <summary>
    /// Auto-hide panel after N seconds (0 = don't auto-hide). Default: 0
    /// </summary>
    public int AutoHideSeconds { get; set; } = 0;

    /// <summary>
    /// Show project-wide summary on startup. Default: true
    /// </summary>
    public bool ShowProjectSummaryOnStartup { get; set; } = true;

    /// <summary>
    /// Minimum severity to show in notifications. Default: Warning
    /// </summary>
    public DiagnosticSeverity MinSeverity { get; set; } = DiagnosticSeverity.Warning;
}

/// <summary>
/// Configuration for caching behavior.
/// </summary>
internal class CacheConfig
{
    /// <summary>
    /// Enable/disable caching. Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cache directory name relative to project root. Default: ".gdshrapt"
    /// </summary>
    public string CacheDirectory { get; set; } = ".gdshrapt";

    /// <summary>
    /// Maximum cache age in days before cleanup. Default: 30
    /// </summary>
    public int MaxCacheAgeDays { get; set; } = 30;
}

/// <summary>
/// Code formatter configuration using GDShrapt.Formatter.
/// Maps to GDFormatterOptions from GDShrapt.Formatter library.
/// </summary>
internal class FormatterConfig
{
    // Indentation
    /// <summary>
    /// Indentation style: Tabs or Spaces. Default: Tabs.
    /// </summary>
    public IndentationStyle IndentStyle { get; set; } = IndentationStyle.Tabs;

    /// <summary>
    /// Number of spaces per indentation level. Default: 4.
    /// </summary>
    public int IndentSize { get; set; } = 4;

    // Line endings
    /// <summary>
    /// Line ending style. Default: LF.
    /// </summary>
    public LineEndingStyle LineEnding { get; set; } = LineEndingStyle.LF;

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
    public LineWrapStyle LineWrapStyle { get; set; } = LineWrapStyle.AfterOpeningBracket;

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
internal enum LineEndingStyle
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
internal enum LineWrapStyle
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

/// <summary>
/// Advanced linting configuration using GDShrapt.Linter.
/// Maps to GDLinterOptions from GDShrapt.Linter library.
/// </summary>
internal class AdvancedLintingConfig
{
    // Naming conventions
    /// <summary>
    /// Expected case for class names. Default: PascalCase.
    /// </summary>
    public NamingCase ClassNameCase { get; set; } = NamingCase.PascalCase;

    /// <summary>
    /// Expected case for function names. Default: SnakeCase.
    /// </summary>
    public NamingCase FunctionNameCase { get; set; } = NamingCase.SnakeCase;

    /// <summary>
    /// Expected case for variable names. Default: SnakeCase.
    /// </summary>
    public NamingCase VariableNameCase { get; set; } = NamingCase.SnakeCase;

    /// <summary>
    /// Expected case for constant names. Default: ScreamingSnakeCase.
    /// </summary>
    public NamingCase ConstantNameCase { get; set; } = NamingCase.ScreamingSnakeCase;

    /// <summary>
    /// Expected case for signal names. Default: SnakeCase.
    /// </summary>
    public NamingCase SignalNameCase { get; set; } = NamingCase.SnakeCase;

    /// <summary>
    /// Expected case for enum names. Default: PascalCase.
    /// </summary>
    public NamingCase EnumNameCase { get; set; } = NamingCase.PascalCase;

    /// <summary>
    /// Expected case for enum values. Default: ScreamingSnakeCase.
    /// </summary>
    public NamingCase EnumValueCase { get; set; } = NamingCase.ScreamingSnakeCase;

    /// <summary>
    /// Whether private members should be prefixed with underscore. Default: true.
    /// </summary>
    public bool RequireUnderscoreForPrivate { get; set; } = true;

    // Best practices
    /// <summary>
    /// Warn about unused variables. Default: true.
    /// </summary>
    public bool WarnUnusedVariables { get; set; } = true;

    /// <summary>
    /// Warn about unused parameters. Default: true.
    /// </summary>
    public bool WarnUnusedParameters { get; set; } = true;

    /// <summary>
    /// Warn about unused signals. Default: false.
    /// </summary>
    public bool WarnUnusedSignals { get; set; } = false;

    /// <summary>
    /// Warn about empty functions. Default: true.
    /// </summary>
    public bool WarnEmptyFunctions { get; set; } = true;

    /// <summary>
    /// Warn about magic numbers. Default: false.
    /// </summary>
    public bool WarnMagicNumbers { get; set; } = false;

    /// <summary>
    /// Warn about variable shadowing. Default: true.
    /// </summary>
    public bool WarnVariableShadowing { get; set; } = true;

    /// <summary>
    /// Warn about await in loops. Default: true.
    /// </summary>
    public bool WarnAwaitInLoop { get; set; } = true;

    // Limits
    /// <summary>
    /// Maximum parameters in a function. 0 to disable. Default: 5.
    /// </summary>
    public int MaxParameters { get; set; } = 5;

    /// <summary>
    /// Maximum statements in a function. 0 to disable. Default: 50.
    /// </summary>
    public int MaxFunctionLength { get; set; } = 50;

    /// <summary>
    /// Maximum cyclomatic complexity. 0 to disable. Default: 10.
    /// </summary>
    public int MaxCyclomaticComplexity { get; set; } = 10;

    // Strict typing
    /// <summary>
    /// Severity for missing type hints on class variables. Null to disable.
    /// </summary>
    public StrictTypingSeverity? StrictTypingClassVariables { get; set; } = null;

    /// <summary>
    /// Severity for missing type hints on local variables. Null to disable.
    /// </summary>
    public StrictTypingSeverity? StrictTypingLocalVariables { get; set; } = null;

    /// <summary>
    /// Severity for missing type hints on parameters. Null to disable.
    /// </summary>
    public StrictTypingSeverity? StrictTypingParameters { get; set; } = null;

    /// <summary>
    /// Severity for missing return type hints. Null to disable.
    /// </summary>
    public StrictTypingSeverity? StrictTypingReturnTypes { get; set; } = null;

    // Comment suppression
    /// <summary>
    /// Process inline suppression comments (gdlint:ignore, gdlint:disable). Default: true.
    /// </summary>
    public bool EnableCommentSuppression { get; set; } = true;
}

/// <summary>
/// Naming case conventions.
/// </summary>
internal enum NamingCase
{
    /// <summary>
    /// snake_case
    /// </summary>
    SnakeCase,

    /// <summary>
    /// PascalCase
    /// </summary>
    PascalCase,

    /// <summary>
    /// camelCase
    /// </summary>
    CamelCase,

    /// <summary>
    /// SCREAMING_SNAKE_CASE
    /// </summary>
    ScreamingSnakeCase,

    /// <summary>
    /// Any case is allowed.
    /// </summary>
    Any
}

/// <summary>
/// Severity level for strict typing rules.
/// </summary>
internal enum StrictTypingSeverity
{
    /// <summary>
    /// Report as warning.
    /// </summary>
    Warning,

    /// <summary>
    /// Report as error.
    /// </summary>
    Error
}
