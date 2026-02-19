namespace GDShrapt.Semantics;

/// <summary>
/// Options for type annotation operations.
/// </summary>
public class GDTypeAnnotationOptions
{
    /// <summary>
    /// Whether to add type annotations to local variables.
    /// </summary>
    public bool IncludeLocals { get; set; } = true;

    /// <summary>
    /// Whether to add type annotations to class-level variables.
    /// </summary>
    public bool IncludeClassVariables { get; set; } = true;

    /// <summary>
    /// Whether to add type annotations to function parameters.
    /// </summary>
    public bool IncludeParameters { get; set; } = false;

    /// <summary>
    /// Whether to add return type annotations to functions.
    /// </summary>
    public bool IncludeReturnTypes { get; set; } = false;

    /// <summary>
    /// Whether to annotate void functions with -> void return type.
    /// Only effective when IncludeReturnTypes is true.
    /// </summary>
    public bool AnnotateVoidReturns { get; set; } = false;

    /// <summary>
    /// Minimum confidence level for type annotations.
    /// Annotations with lower confidence will be skipped.
    /// </summary>
    public GDTypeConfidence MinimumConfidence { get; set; } = GDTypeConfidence.Certain;

    /// <summary>
    /// Fallback type for variables whose type cannot be inferred.
    /// Set to null to skip such variables.
    /// </summary>
    public string? UnknownTypeFallback { get; set; }

    /// <summary>
    /// Whether to update existing type annotations when inference suggests a narrower type.
    /// When true, variables with explicit annotations that are wider than inferred will be re-annotated.
    /// </summary>
    public bool UpdateExistingAnnotations { get; set; } = false;

    /// <summary>
    /// Creates options with default values.
    /// </summary>
    public static GDTypeAnnotationOptions Default => new();

    /// <summary>
    /// Creates options that only include class variables with certain confidence.
    /// </summary>
    public static GDTypeAnnotationOptions ClassVariablesOnly => new()
    {
        IncludeLocals = false,
        IncludeClassVariables = true,
        IncludeParameters = false,
        MinimumConfidence = GDTypeConfidence.Certain
    };

    /// <summary>
    /// Creates options that include all variable types with high confidence.
    /// </summary>
    public static GDTypeAnnotationOptions All => new()
    {
        IncludeLocals = true,
        IncludeClassVariables = true,
        IncludeParameters = true,
        IncludeReturnTypes = true,
        MinimumConfidence = GDTypeConfidence.High
    };
}
