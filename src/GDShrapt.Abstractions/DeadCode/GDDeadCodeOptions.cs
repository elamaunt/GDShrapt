using System;
using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Options for dead code analysis.
/// </summary>
public class GDDeadCodeOptions
{
    /// <summary>
    /// Godot 4.x virtual methods that are called by the engine.
    /// </summary>
    public static readonly ISet<string> Godot4VirtualMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Core lifecycle
        "_ready", "_process", "_physics_process", "_input", "_unhandled_input",
        "_unhandled_key_input", "_enter_tree", "_exit_tree", "_notification",
        "_init", "_to_string",
        // Rendering
        "_draw",
        // GUI
        "_gui_input", "_get_minimum_size", "_has_point", "_get_tooltip",
        "_clips_input", "_get_drag_data", "_can_drop_data", "_drop_data",
        "_structured_text_parser", "_get_allowed_size_flags_horizontal",
        "_get_allowed_size_flags_vertical", "_make_custom_tooltip",
        // Properties
        "_get", "_set", "_get_property_list", "_validate_property",
        "_property_can_revert", "_property_get_revert",
        // Configuration
        "_get_configuration_warnings",
        // Physics callbacks
        "_integrate_forces", "_state_machine_type", "_tile_data_runtime_update",
        // 2D/3D collision callbacks
        "_body_entered", "_body_exited", "_area_entered", "_area_exited",
        "_input_event", "_mouse_enter", "_mouse_exit", "_screen_entered", "_screen_exited",
        // Navigation
        "_link_reached", "_navigation_finished", "_path_changed", "_target_reached",
        "_velocity_computed", "_waypoint_reached",
        // Godot 4.x new methods
        "_shortcut_input", "_unhandled_input", "_physics_interpolation_reset",
        "_get_configuration_string", "_get_custom_item_rect",
        "_get_drag_preview", "_get_expand_icon", "_get_allowed_size_flags",
        "_get_base_script", "_get_category", "_get_class_name",
        "_get_description", "_get_documentation", "_get_global_class_name",
        "_get_icon_path", "_get_language_name", "_get_method_info",
        "_get_minimum_size", "_get_param_info", "_get_recognized_extensions",
        "_get_return_info", "_get_settings_list", "_get_type_name",
        "_on_focus_entered", "_on_focus_exited", "_on_resized",
        "_on_visibility_changed", "_post_import", "_save_external_data",
        "_update", "_validate_child_order"
    };

    /// <summary>
    /// Legacy Godot 3.x virtual methods (subset of 4.x).
    /// </summary>
    public static readonly ISet<string> Godot3VirtualMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "_ready", "_process", "_physics_process", "_input", "_unhandled_input",
        "_unhandled_key_input", "_enter_tree", "_exit_tree", "_notification",
        "_draw", "_gui_input", "_get_minimum_size", "_init", "_to_string",
        "_get", "_set", "_get_property_list", "_get_configuration_warning",
        "_integrate_forces", "_body_entered", "_body_exited",
        "_area_entered", "_area_exited", "_input_event",
        "_mouse_enter", "_mouse_exit"
    };
    /// <summary>
    /// Maximum confidence level to include in results.
    /// Base version enforces Strict only.
    /// Pro version allows Potential and NameMatch.
    /// </summary>
    public GDReferenceConfidence MaxConfidence { get; set; } = GDReferenceConfidence.Strict;

    /// <summary>
    /// Include unused variables in analysis.
    /// </summary>
    public bool IncludeVariables { get; set; } = true;

    /// <summary>
    /// Include unused functions/methods in analysis.
    /// </summary>
    public bool IncludeFunctions { get; set; } = true;

    /// <summary>
    /// Include unused signals in analysis.
    /// </summary>
    public bool IncludeSignals { get; set; } = true;

    /// <summary>
    /// Include unused parameters in analysis.
    /// </summary>
    public bool IncludeParameters { get; set; } = false;

    /// <summary>
    /// Include unused constants in analysis.
    /// </summary>
    public bool IncludeConstants { get; set; } = true;

    /// <summary>
    /// Include unused enum values in analysis.
    /// </summary>
    public bool IncludeEnumValues { get; set; } = true;

    /// <summary>
    /// Include unused inner classes in analysis.
    /// </summary>
    public bool IncludeInnerClasses { get; set; } = true;

    /// <summary>
    /// Include private members (starting with _) in analysis.
    /// </summary>
    public bool IncludePrivate { get; set; } = true;

    /// <summary>
    /// Include unreachable code detection.
    /// </summary>
    public bool IncludeUnreachable { get; set; } = true;

    /// <summary>
    /// Skip Godot virtual methods (_ready, _process, etc.).
    /// When true, uses SkipMethods collection.
    /// </summary>
    public bool SkipGodotVirtuals { get; set; } = true;

    /// <summary>
    /// Set of method names to skip during dead code analysis.
    /// Default: Godot4VirtualMethods.
    /// Can be customized to add project-specific methods.
    /// </summary>
    public ISet<string> SkipMethods { get; set; } = Godot4VirtualMethods;

    /// <summary>
    /// Additional method names to skip (merged with SkipMethods).
    /// Use this to add project-specific callbacks without replacing the default set.
    /// </summary>
    public HashSet<string> AdditionalSkipMethods { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Exclude test files from analysis.
    /// </summary>
    public bool ExcludeTestFiles { get; set; }

    /// <summary>
    /// Path patterns that identify test files.
    /// </summary>
    public HashSet<string> TestPathPatterns { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "test_", "tests/", "test/", "_test.gd"
    };

    /// <summary>
    /// Collect evidence details for --explain mode.
    /// </summary>
    public bool CollectEvidence { get; set; }

    /// <summary>
    /// Collect items dropped by reflection for --show-dropped-by-reflection mode.
    /// </summary>
    public bool CollectDroppedByReflection { get; set; }

    /// <summary>
    /// When true, non-private members on classes with class_name are downgraded
    /// from Strict to Potential confidence (they may be used externally).
    /// </summary>
    public bool TreatClassNameAsPublicAPI { get; set; } = true;

    /// <summary>
    /// Method name prefixes for framework-invoked methods (e.g., "test_").
    /// Functions matching these prefixes are skipped from dead code analysis.
    /// Only applies when the declaring class extends one of FrameworkBaseClasses
    /// (or when FrameworkBaseClasses is empty — matches all classes).
    /// </summary>
    public HashSet<string> FrameworkMethodPrefixes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Base class names that activate framework method prefix recognition.
    /// If empty, prefix matching applies to all classes.
    /// </summary>
    public HashSet<string> FrameworkBaseClasses { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if a file should be skipped based on test path patterns.
    /// </summary>
    public bool ShouldSkipFile(string filePath)
    {
        if (!ExcludeTestFiles)
            return false;

        var normalized = filePath.Replace('\\', '/');
        foreach (var pattern in TestPathPatterns)
        {
            if (normalized.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if a method name should be skipped.
    /// </summary>
    public bool ShouldSkipMethod(string methodName)
    {
        if (!SkipGodotVirtuals)
            return false;

        if (SkipMethods.Contains(methodName))
            return true;

        if (AdditionalSkipMethods.Count > 0 && AdditionalSkipMethods.Contains(methodName))
            return true;

        return false;
    }

    /// <summary>
    /// Default options for Base (safe) analysis.
    /// </summary>
    public static GDDeadCodeOptions Default => new GDDeadCodeOptions();

    /// <summary>
    /// Options for comprehensive analysis (Pro).
    /// </summary>
    public static GDDeadCodeOptions Comprehensive => new GDDeadCodeOptions
    {
        MaxConfidence = GDReferenceConfidence.NameMatch,
        IncludeParameters = true
    };

    /// <summary>
    /// Creates a copy with MaxConfidence forced to Strict (for Base handler).
    /// </summary>
    public GDDeadCodeOptions WithStrictConfidenceOnly()
    {
        return new GDDeadCodeOptions
        {
            MaxConfidence = GDReferenceConfidence.Strict,
            IncludeVariables = IncludeVariables,
            IncludeFunctions = IncludeFunctions,
            IncludeSignals = IncludeSignals,
            IncludeParameters = IncludeParameters,
            IncludeConstants = IncludeConstants,
            IncludeEnumValues = IncludeEnumValues,
            IncludeInnerClasses = IncludeInnerClasses,
            IncludePrivate = IncludePrivate,
            IncludeUnreachable = IncludeUnreachable,
            SkipGodotVirtuals = SkipGodotVirtuals,
            SkipMethods = SkipMethods,
            AdditionalSkipMethods = AdditionalSkipMethods,
            ExcludeTestFiles = ExcludeTestFiles,
            TestPathPatterns = TestPathPatterns,
            CollectEvidence = CollectEvidence,
            CollectDroppedByReflection = CollectDroppedByReflection,
            FrameworkMethodPrefixes = FrameworkMethodPrefixes,
            FrameworkBaseClasses = FrameworkBaseClasses,
            TreatClassNameAsPublicAPI = TreatClassNameAsPublicAPI
        };
    }

    /// <summary>
    /// Creates options with Godot 3.x virtual methods preset.
    /// </summary>
    public static GDDeadCodeOptions ForGodot3() => new GDDeadCodeOptions
    {
        SkipMethods = Godot3VirtualMethods
    };

    /// <summary>
    /// Creates options with Godot 4.x virtual methods preset.
    /// </summary>
    public static GDDeadCodeOptions ForGodot4() => new GDDeadCodeOptions
    {
        SkipMethods = Godot4VirtualMethods
    };

    /// <summary>
    /// Creates options without skipping any methods.
    /// </summary>
    public static GDDeadCodeOptions WithNoSkipMethods() => new GDDeadCodeOptions
    {
        SkipGodotVirtuals = false
    };
}
