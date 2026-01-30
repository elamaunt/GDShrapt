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

    /// <summary>
    /// Checks if the type name refers to a local enum.
    /// </summary>
    /// <param name="typeName">The type name to check.</param>
    /// <returns>True if the type name refers to a local enum declaration.</returns>
    bool IsLocalEnum(string typeName);

    /// <summary>
    /// Checks if a member name is a valid value for a local enum.
    /// </summary>
    /// <param name="enumTypeName">The enum type name.</param>
    /// <param name="memberName">The member name to check.</param>
    /// <returns>True if the member is a valid enum value.</returns>
    bool IsLocalEnumValue(string enumTypeName, string memberName);

    /// <summary>
    /// Checks if the type name refers to a local inner class.
    /// </summary>
    /// <param name="typeName">The type name to check.</param>
    /// <returns>True if the type name refers to a local inner class declaration.</returns>
    bool IsLocalInnerClass(string typeName);

    /// <summary>
    /// Gets a member from a local inner class.
    /// </summary>
    /// <param name="innerClassName">The inner class name.</param>
    /// <param name="memberName">The member name to find.</param>
    /// <returns>Member info if found, null otherwise.</returns>
    GDRuntimeMemberInfo? GetInnerClassMember(string innerClassName, string memberName);
}
