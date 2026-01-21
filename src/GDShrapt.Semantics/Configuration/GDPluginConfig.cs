using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Plugin-specific configuration (UI, notifications, cache, todo tags).
/// These settings are specific to the Godot editor plugin and are
/// ignored by CLI and LSP.
/// </summary>
public class GDPluginConfig
{
    /// <summary>
    /// UI panels and docks enable/disable settings.
    /// </summary>
    public GDUIConfig UI { get; set; } = new();

    /// <summary>
    /// Notification panel behavior configuration.
    /// </summary>
    public GDNotificationConfig Notifications { get; set; } = new();

    /// <summary>
    /// Cache settings configuration.
    /// </summary>
    public GDCacheConfig Cache { get; set; } = new();

    /// <summary>
    /// TODO/FIXME tags panel configuration.
    /// </summary>
    public GDTodoTagsConfig TodoTags { get; set; } = new();
}

/// <summary>
/// UI panels and docks configuration.
/// </summary>
public class GDUIConfig
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
    /// Enable Find References dock in bottom panel. Default: true
    /// </summary>
    public bool FindReferencesDockEnabled { get; set; } = true;

    /// <summary>
    /// Enable error line background highlighting (VS Code style). Default: true
    /// </summary>
    public bool ErrorLineBackgroundEnabled { get; set; } = true;
}

/// <summary>
/// Configuration for notification panel behavior.
/// </summary>
public class GDNotificationConfig
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
    public GDUnifiedDiagnosticSeverity MinSeverity { get; set; } = GDUnifiedDiagnosticSeverity.Warning;
}

/// <summary>
/// Configuration for caching behavior.
/// </summary>
public class GDCacheConfig
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
/// Configuration for TODO/FIXME tags panel.
/// </summary>
public class GDTodoTagsConfig
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
    public GDTodoGroupingMode DefaultGrouping { get; set; } = GDTodoGroupingMode.ByFile;

    /// <summary>
    /// Tag definitions with colors and enabled states.
    /// </summary>
    public List<GDTodoTagDefinition> Tags { get; set; } = new()
    {
        new GDTodoTagDefinition("TODO", "#4FC3F7", GDTodoPriority.Normal),
        new GDTodoTagDefinition("FIXME", "#FF8A65", GDTodoPriority.High),
        new GDTodoTagDefinition("HACK", "#FFD54F", GDTodoPriority.Normal),
        new GDTodoTagDefinition("NOTE", "#81C784", GDTodoPriority.Low),
        new GDTodoTagDefinition("BUG", "#EF5350", GDTodoPriority.High),
        new GDTodoTagDefinition("XXX", "#CE93D8", GDTodoPriority.Low)
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
public enum GDTodoGroupingMode
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
/// Defines a TODO tag type with its display properties.
/// </summary>
public class GDTodoTagDefinition
{
    /// <summary>
    /// The tag name (e.g., "TODO").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this tag is enabled for scanning.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Color for display in the dock (hex format like "#FF0000").
    /// </summary>
    public string Color { get; set; } = "#FFFFFF";

    /// <summary>
    /// Default priority for this tag type.
    /// </summary>
    public GDTodoPriority DefaultPriority { get; set; } = GDTodoPriority.Normal;

    public GDTodoTagDefinition() { }

    public GDTodoTagDefinition(string name, string color, GDTodoPriority priority = GDTodoPriority.Normal)
    {
        Name = name;
        Color = color;
        DefaultPriority = priority;
        Enabled = true;
    }
}

/// <summary>
/// Priority level for TODO items.
/// </summary>
public enum GDTodoPriority
{
    Low,
    Normal,
    High
}

/// <summary>
/// Diagnostic category for rule classification.
/// </summary>
public enum GDDiagnosticCategory
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
