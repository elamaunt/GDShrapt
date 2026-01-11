using GDShrapt.Semantics;

namespace GDShrapt.Plugin;

/// <summary>
/// Combined project configuration for GDShrapt plugin.
/// Composes shared configuration from Semantics (Linting, Formatter, AdvancedLinting)
/// with plugin-specific settings (UI, Notifications, Cache, TodoTags).
/// Stored in .gdshrapt.json in project root for team sharing.
/// </summary>
internal class ProjectConfig
{
    /// <summary>
    /// Shared configuration from GDShrapt.Semantics.
    /// Contains Linting, Formatter, AdvancedLinting, and CLI settings.
    /// </summary>
    public GDProjectConfig Core { get; set; } = new();

    /// <summary>
    /// Plugin-specific configuration.
    /// Contains UI, Notifications, Cache, and TodoTags settings.
    /// </summary>
    public PluginConfig Plugin { get; set; } = new();

    // Convenience accessors for backward compatibility

    /// <summary>
    /// Gets the linting configuration from Core.
    /// </summary>
    public GDLintingConfig Linting => Core.Linting;

    /// <summary>
    /// Gets the formatter configuration from Core.
    /// </summary>
    public GDFormatterConfig Formatter => Core.Formatter;

    /// <summary>
    /// Gets the advanced linting configuration from Core.
    /// </summary>
    public GDAdvancedLintingConfig AdvancedLinting => Core.AdvancedLinting;

    /// <summary>
    /// Gets the UI configuration from Plugin.
    /// </summary>
    public UIConfig UI => Plugin.UI;

    /// <summary>
    /// Gets the notifications configuration from Plugin.
    /// </summary>
    public NotificationConfig Notifications => Plugin.Notifications;

    /// <summary>
    /// Gets the cache configuration from Plugin.
    /// </summary>
    public CacheConfig Cache => Plugin.Cache;

    /// <summary>
    /// Gets the TodoTags configuration from Plugin.
    /// </summary>
    public TodoTagsConfig TodoTags => Plugin.TodoTags;
}

/// <summary>
/// Diagnostic category for rule classification.
/// </summary>
internal enum DiagnosticCategory
{
    /// <summary>
    /// Syntax errors from parser.
    /// </summary>
    Syntax,

    /// <summary>
    /// Formatting issues (whitespace, indentation).
    /// </summary>
    Formatting,

    /// <summary>
    /// Style issues (naming conventions).
    /// </summary>
    Style,

    /// <summary>
    /// Best practice recommendations.
    /// </summary>
    BestPractice,

    /// <summary>
    /// Performance-related suggestions.
    /// </summary>
    Performance,

    /// <summary>
    /// Potential correctness issues.
    /// </summary>
    Correctness
}
