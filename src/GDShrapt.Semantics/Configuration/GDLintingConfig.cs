using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Configuration for linting and formatting rules.
/// </summary>
public class GDLintingConfig
{
    /// <summary>
    /// Enable/disable all linting. Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Formatting check level. Default: Light
    /// </summary>
    public GDFormattingLevel FormattingLevel { get; set; } = GDFormattingLevel.Light;

    /// <summary>
    /// Indentation style preference.
    /// </summary>
    public GDIndentationStyle IndentationStyle { get; set; } = GDIndentationStyle.Tabs;

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
    /// Key is rule ID (e.g., "GDS001", "GDL201").
    /// </summary>
    public Dictionary<string, GDRuleConfig> Rules { get; set; } = new();
}

/// <summary>
/// Per-rule configuration.
/// </summary>
public class GDRuleConfig
{
    /// <summary>
    /// Enable/disable this specific rule.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Override severity for this rule.
    /// </summary>
    public GDDiagnosticSeverity? Severity { get; set; }

    /// <summary>
    /// Rule-specific options.
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();
}

/// <summary>
/// Formatting check strictness level.
/// </summary>
public enum GDFormattingLevel
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
public enum GDIndentationStyle
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
/// Diagnostic severity levels.
/// </summary>
public enum GDDiagnosticSeverity
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
