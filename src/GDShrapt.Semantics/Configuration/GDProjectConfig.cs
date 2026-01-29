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

    /// <summary>
    /// Semantic validation settings.
    /// </summary>
    public GDValidationConfig Validation { get; set; } = new();

    /// <summary>
    /// Plugin-specific settings (UI, notifications, cache, etc.).
    /// Only used by the Godot editor plugin, ignored by CLI/LSP.
    /// </summary>
    public GDPluginConfig? Plugin { get; set; }
}

/// <summary>
/// Semantic validation configuration options.
/// </summary>
public class GDValidationConfig
{
    /// <summary>
    /// Nullable access check strictness.
    /// Values: "error", "strict", "normal", "relaxed", "off"
    /// Default: "strict"
    /// </summary>
    public string NullableStrictness { get; set; } = "strict";

    /// <summary>
    /// Warn on Dictionary indexer access (dict["key"]).
    /// Dictionary values may be null.
    /// Default: true
    /// </summary>
    public bool WarnOnDictionaryIndexer { get; set; } = true;

    /// <summary>
    /// Warn on untyped function parameters.
    /// Callers could technically pass null to untyped parameters.
    /// Default: true
    /// </summary>
    public bool WarnOnUntypedParameters { get; set; } = true;
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
