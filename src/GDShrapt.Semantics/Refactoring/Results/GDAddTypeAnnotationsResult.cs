using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents a planned type annotation with its source location and inferred type.
/// </summary>
public class GDTypeAnnotationPlan
{
    /// <summary>
    /// File path containing the variable.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Name of the variable, parameter, or local.
    /// </summary>
    public string IdentifierName { get; }

    /// <summary>
    /// Alias for IdentifierName.
    /// </summary>
    public string VariableName => IdentifierName;

    /// <summary>
    /// Line number (1-based).
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Column number (1-based).
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// The inferred type to add.
    /// </summary>
    public GDInferredType InferredType { get; }

    /// <summary>
    /// The target type (class variable, local, or parameter).
    /// </summary>
    public TypeAnnotationTarget Target { get; }

    /// <summary>
    /// The text edit to apply this annotation.
    /// </summary>
    public GDTextEdit Edit { get; }

    public GDTypeAnnotationPlan(
        string filePath,
        string identifierName,
        int line,
        int column,
        GDInferredType inferredType,
        TypeAnnotationTarget target,
        GDTextEdit edit)
    {
        FilePath = filePath;
        IdentifierName = identifierName;
        Line = line;
        Column = column;
        InferredType = inferredType;
        Target = target;
        Edit = edit;
    }

    public override string ToString() =>
        $"{IdentifierName}: {InferredType.TypeName.DisplayName} ({InferredType.Confidence}) at {FilePath}:{Line}";
}

/// <summary>
/// Result of single-file type annotation planning.
/// </summary>
public class GDAddTypeAnnotationsResult : GDRefactoringResult
{
    /// <summary>
    /// Planned type annotations.
    /// </summary>
    public IReadOnlyList<GDTypeAnnotationPlan> Annotations { get; }

    /// <summary>
    /// Statistics about annotations by confidence level.
    /// </summary>
    public GDTypeAnnotationStatistics Statistics { get; }

    /// <summary>
    /// Whether there are any annotations to add.
    /// </summary>
    public bool HasAnnotations => Annotations.Count > 0;

    /// <summary>
    /// Total number of annotations.
    /// </summary>
    public int TotalAnnotations => Annotations.Count;

    private GDAddTypeAnnotationsResult(
        bool success,
        string? errorMessage,
        IReadOnlyList<GDTextEdit>? edits,
        IReadOnlyList<GDTypeAnnotationPlan>? annotations,
        GDTypeAnnotationStatistics? statistics)
        : base(success, errorMessage, edits)
    {
        Annotations = annotations ?? new List<GDTypeAnnotationPlan>();
        Statistics = statistics ?? new GDTypeAnnotationStatistics();
    }

    /// <summary>
    /// Creates a successful planning result.
    /// </summary>
    public static GDAddTypeAnnotationsResult Planned(IReadOnlyList<GDTypeAnnotationPlan> annotations)
    {
        var edits = annotations.Select(a => a.Edit).ToList();
        var statistics = GDTypeAnnotationStatistics.FromAnnotations(annotations);
        return new GDAddTypeAnnotationsResult(true, null, edits, annotations, statistics);
    }

    /// <summary>
    /// Creates a result indicating no annotations are needed.
    /// </summary>
    public static new GDAddTypeAnnotationsResult Empty =>
        new(true, null, null, null, null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static new GDAddTypeAnnotationsResult Failed(string errorMessage) =>
        new(false, errorMessage, null, null, null);

    public override string ToString()
    {
        if (!Success)
            return $"Failed: {ErrorMessage}";
        return $"Planned {Annotations.Count} annotation(s)";
    }
}

/// <summary>
/// Statistics about planned type annotations by confidence level.
/// </summary>
public class GDTypeAnnotationStatistics
{
    /// <summary>
    /// Number of certain-confidence annotations.
    /// </summary>
    public int CertainCount { get; init; }

    /// <summary>
    /// Number of high-confidence annotations.
    /// </summary>
    public int HighCount { get; init; }

    /// <summary>
    /// Number of medium-confidence annotations.
    /// </summary>
    public int MediumCount { get; init; }

    /// <summary>
    /// Number of low-confidence annotations.
    /// </summary>
    public int LowCount { get; init; }

    /// <summary>
    /// Number of unknown/fallback annotations.
    /// </summary>
    public int UnknownCount { get; init; }

    /// <summary>
    /// Total number of annotations.
    /// </summary>
    public int TotalCount => CertainCount + HighCount + MediumCount + LowCount + UnknownCount;

    /// <summary>
    /// Number of class variable annotations.
    /// </summary>
    public int ClassVariableCount { get; init; }

    /// <summary>
    /// Number of local variable annotations.
    /// </summary>
    public int LocalVariableCount { get; init; }

    /// <summary>
    /// Number of parameter annotations.
    /// </summary>
    public int ParameterCount { get; init; }

    /// <summary>
    /// Creates statistics from a list of annotations.
    /// </summary>
    public static GDTypeAnnotationStatistics FromAnnotations(IReadOnlyList<GDTypeAnnotationPlan> annotations)
    {
        return new GDTypeAnnotationStatistics
        {
            CertainCount = annotations.Count(a => a.InferredType.Confidence == GDTypeConfidence.Certain),
            HighCount = annotations.Count(a => a.InferredType.Confidence == GDTypeConfidence.High),
            MediumCount = annotations.Count(a => a.InferredType.Confidence == GDTypeConfidence.Medium),
            LowCount = annotations.Count(a => a.InferredType.Confidence == GDTypeConfidence.Low),
            UnknownCount = annotations.Count(a => a.InferredType.Confidence == GDTypeConfidence.Unknown),
            ClassVariableCount = annotations.Count(a => a.Target == TypeAnnotationTarget.ClassVariable),
            LocalVariableCount = annotations.Count(a => a.Target == TypeAnnotationTarget.LocalVariable),
            ParameterCount = annotations.Count(a => a.Target == TypeAnnotationTarget.Parameter)
        };
    }
}
