using System.Collections.Generic;
using GDShrapt.Semantics;

namespace GDShrapt.Plugin;

/// <summary>
/// Plugin-specific configuration (UI, notifications, cache, todo tags).
/// These settings are specific to the Godot editor plugin and are not shared
/// with CLI or LSP.
/// </summary>
internal class PluginConfig
{
    /// <summary>
    /// UI panels and docks enable/disable settings.
    /// </summary>
    public UIConfig UI { get; set; } = new();

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
    public GDDiagnosticSeverity MinSeverity { get; set; } = GDDiagnosticSeverity.Warning;
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
