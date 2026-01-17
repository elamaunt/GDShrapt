namespace GDShrapt.Abstractions;

/// <summary>
/// Interface for analyzing member access confidence and expression types.
/// Used by GDMemberAccessValidator to determine validation behavior.
/// Implemented by GDSemanticModel in GDShrapt.Semantics.
/// </summary>
public interface IGDMemberAccessAnalyzer
{
    /// <summary>
    /// Gets the confidence level for a member access operation.
    /// </summary>
    /// <param name="memberAccess">The member access expression.</param>
    /// <returns>Confidence level indicating how certain we are about the member access.</returns>
    GDReferenceConfidence GetMemberAccessConfidence(object memberAccess);

    /// <summary>
    /// Gets the inferred type of an expression.
    /// </summary>
    /// <param name="expression">The expression to analyze.</param>
    /// <returns>The type name, or null if unknown.</returns>
    string? GetExpressionType(object expression);
}
