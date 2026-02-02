namespace GDShrapt.Abstractions;

/// <summary>
/// Type annotation coverage report for a project.
/// </summary>
public class GDTypeCoverageReport
{
    // Variables
    /// <summary>
    /// Total number of variables (local + class-level).
    /// </summary>
    public int TotalVariables { get; set; }

    /// <summary>
    /// Variables with explicit type annotations.
    /// </summary>
    public int AnnotatedVariables { get; set; }

    /// <summary>
    /// Variables with inferred types (no annotation but type known).
    /// </summary>
    public int InferredVariables { get; set; }

    /// <summary>
    /// Variables that resolved to Variant (type unknown).
    /// </summary>
    public int VariantVariables { get; set; }

    // Parameters
    /// <summary>
    /// Total number of function parameters.
    /// </summary>
    public int TotalParameters { get; set; }

    /// <summary>
    /// Parameters with explicit type annotations.
    /// </summary>
    public int AnnotatedParameters { get; set; }

    /// <summary>
    /// Parameters with inferred types.
    /// </summary>
    public int InferredParameters { get; set; }

    // Return types
    /// <summary>
    /// Total number of functions with non-void returns.
    /// </summary>
    public int TotalReturnTypes { get; set; }

    /// <summary>
    /// Functions with explicit return type annotations.
    /// </summary>
    public int AnnotatedReturnTypes { get; set; }

    /// <summary>
    /// Functions with inferred return types.
    /// </summary>
    public int InferredReturnTypes { get; set; }

    // Computed metrics
    /// <summary>
    /// Percentage of variables with explicit annotations (0-100).
    /// </summary>
    public double AnnotationCoverage => TotalVariables > 0
        ? (double)AnnotatedVariables / TotalVariables * 100
        : 100;

    /// <summary>
    /// Percentage of variables with known types (annotated + inferred) (0-100).
    /// </summary>
    public double EffectiveCoverage => TotalVariables > 0
        ? (double)(AnnotatedVariables + InferredVariables) / TotalVariables * 100
        : 100;

    /// <summary>
    /// Percentage of variables that are Variant (unknown type) (0-100).
    /// Lower is better.
    /// </summary>
    public double VariantPercentage => TotalVariables > 0
        ? (double)VariantVariables / TotalVariables * 100
        : 0;

    /// <summary>
    /// Parameter annotation coverage (0-100).
    /// </summary>
    public double ParameterCoverage => TotalParameters > 0
        ? (double)AnnotatedParameters / TotalParameters * 100
        : 100;

    /// <summary>
    /// Return type annotation coverage (0-100).
    /// </summary>
    public double ReturnTypeCoverage => TotalReturnTypes > 0
        ? (double)AnnotatedReturnTypes / TotalReturnTypes * 100
        : 100;

    /// <summary>
    /// Overall type safety score (0-100).
    /// Weighted average of annotation coverages.
    /// </summary>
    public double TypeSafetyScore
    {
        get
        {
            var totalItems = TotalVariables + TotalParameters + TotalReturnTypes;
            if (totalItems == 0) return 100;

            var annotatedItems = AnnotatedVariables + AnnotatedParameters + AnnotatedReturnTypes;
            return (double)annotatedItems / totalItems * 100;
        }
    }

    /// <summary>
    /// Creates an empty report.
    /// </summary>
    public static GDTypeCoverageReport Empty => new GDTypeCoverageReport();

    /// <summary>
    /// Creates a report representing full coverage (all typed).
    /// </summary>
    public static GDTypeCoverageReport FullCoverage => new GDTypeCoverageReport();
}
