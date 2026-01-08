using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Root configuration for GDShrapt tools.
/// Stored in .gdshrapt.json in project root for team sharing.
/// Used by CLI, LSP, and Plugin.
/// </summary>
public class GDProjectConfig
{
    /// <summary>
    /// Linting and formatting rules configuration.
    /// </summary>
    public GDLintingConfig Linting { get; set; } = new();

    /// <summary>
    /// Code formatter configuration using GDShrapt.Formatter.
    /// </summary>
    public GDFormatterConfig Formatter { get; set; } = new();

    /// <summary>
    /// Advanced linting configuration using GDShrapt.Linter.
    /// </summary>
    public GDAdvancedLintingConfig AdvancedLinting { get; set; } = new();

    /// <summary>
    /// CLI-specific settings.
    /// </summary>
    public GDCliConfig Cli { get; set; } = new();
}

/// <summary>
/// CLI-specific configuration options.
/// </summary>
public class GDCliConfig
{
    /// <summary>
    /// Fail on warnings (exit code 1). Default: false.
    /// </summary>
    public bool FailOnWarning { get; set; } = false;

    /// <summary>
    /// Fail on hints (exit code 1). Default: false.
    /// </summary>
    public bool FailOnHint { get; set; } = false;

    /// <summary>
    /// File/directory patterns to exclude from analysis.
    /// Uses glob patterns (e.g., "addons/**", ".godot/**").
    /// </summary>
    public List<string> Exclude { get; set; } = new()
    {
        ".godot/**",
        "addons/**"
    };
}
