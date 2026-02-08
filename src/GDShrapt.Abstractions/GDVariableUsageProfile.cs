using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Abstractions;

/// <summary>
/// Profile of a Variant variable's usage - tracks all assignments for Union type inference.
/// </summary>
public class GDVariableUsageProfile
{
    /// <summary>
    /// Name of the variable.
    /// </summary>
    public string VariableName { get; }

    /// <summary>
    /// All assignments to this variable.
    /// </summary>
    public List<GDAssignmentObservation> Assignments { get; } = new List<GDAssignmentObservation>();

    /// <summary>
    /// Line where the variable was declared.
    /// </summary>
    public int DeclarationLine { get; set; }

    /// <summary>
    /// Column where the variable was declared.
    /// </summary>
    public int DeclarationColumn { get; set; }

    /// <summary>
    /// Whether this is a class-level variable (vs local).
    /// </summary>
    public bool IsClassLevel { get; set; }

    public GDVariableUsageProfile(string variableName)
    {
        VariableName = variableName;
    }

    /// <summary>
    /// Computes the Union type based on all observed assignments.
    /// </summary>
    public GDUnionType ComputeUnionType()
    {
        var union = new GDUnionType();

        foreach (var assignment in Assignments)
        {
            if (assignment.InferredType != null)
            {
                union.AddType(assignment.InferredType, assignment.IsHighConfidence);
            }
        }

        if (union.IsSingleType)
        {
            union.ConfidenceReason = "Single type observed";
        }
        else if (union.IsUnion)
        {
            union.ConfidenceReason = $"Union of: {string.Join(", ", union.Types.Select(t => t.DisplayName))}";
        }

        return union;
    }

    /// <summary>
    /// Gets all unique types assigned to this variable.
    /// </summary>
    public IEnumerable<GDSemanticType> GetAssignedTypes()
    {
        return Assignments
            .Where(a => a.InferredType != null)
            .Select(a => a.InferredType!)
            .Distinct();
    }

    /// <summary>
    /// Whether all assignments have high confidence.
    /// </summary>
    public bool AllHighConfidence => Assignments.All(a => a.IsHighConfidence);

    /// <summary>
    /// Number of assignments.
    /// </summary>
    public int AssignmentCount => Assignments.Count;

    public override string ToString()
    {
        var union = ComputeUnionType();
        return $"Variable '{VariableName}': {union} ({AssignmentCount} assignments)";
    }
}
