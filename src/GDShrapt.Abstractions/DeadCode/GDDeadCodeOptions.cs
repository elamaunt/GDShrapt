using System;
using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Options for dead code analysis.
/// </summary>
public class GDDeadCodeOptions
{
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
    /// When true, virtual methods are detected dynamically from TypesMap.
    /// </summary>
    public bool SkipGodotVirtuals { get; set; } = true;

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
    /// Glob patterns to exclude files from analysis (e.g., "addons/**", ".godot/**").
    /// Merged from config and CLI --exclude option.
    /// </summary>
    public List<string> ExcludePatterns { get; set; } = new();

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
    /// When true, members annotated with @public_api, @dynamic_use, or custom
    /// suppression annotations are excluded from dead code results.
    /// </summary>
    public bool RespectSuppressionAnnotations { get; set; } = true;

    /// <summary>
    /// Additional annotation names that suppress dead code detection.
    /// Specified via --suppress-annotation CLI option.
    /// </summary>
    public HashSet<string> CustomSuppressionAnnotations { get; set; } = new(StringComparer.OrdinalIgnoreCase);

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
        var normalized = filePath.Replace('\\', '/');

        if (ExcludePatterns.Count > 0)
        {
            foreach (var pattern in ExcludePatterns)
            {
                if (GDGlobMatcher.Matches(normalized, pattern.Replace('\\', '/')))
                    return true;
            }
        }

        if (ExcludeTestFiles)
        {
            foreach (var pattern in TestPathPatterns)
            {
                if (normalized.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
        }

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
            ExcludeTestFiles = ExcludeTestFiles,
            TestPathPatterns = TestPathPatterns,
            ExcludePatterns = ExcludePatterns,
            CollectEvidence = CollectEvidence,
            CollectDroppedByReflection = CollectDroppedByReflection,
            FrameworkMethodPrefixes = FrameworkMethodPrefixes,
            FrameworkBaseClasses = FrameworkBaseClasses,
            TreatClassNameAsPublicAPI = TreatClassNameAsPublicAPI,
            RespectSuppressionAnnotations = RespectSuppressionAnnotations,
            CustomSuppressionAnnotations = CustomSuppressionAnnotations
        };
    }

    /// <summary>
    /// Creates options without skipping any virtual methods.
    /// </summary>
    public static GDDeadCodeOptions WithNoSkipMethods() => new GDDeadCodeOptions
    {
        SkipGodotVirtuals = false
    };
}
