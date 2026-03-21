using GDShrapt.Reader;

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
    GDReferenceConfidence GetMemberAccessConfidence(GDMemberOperatorExpression memberAccess);

    /// <summary>
    /// Gets the inferred type of an expression.
    /// Flow analysis is the primary source; narrowing is already applied.
    /// </summary>
    GDSemanticType? GetExpressionType(GDExpression expression);

    /// <summary>
    /// Checks if the type name refers to a local enum.
    /// </summary>
    bool IsLocalEnum(string typeName);

    /// <summary>
    /// Checks if a member name is a valid value for a local enum.
    /// </summary>
    bool IsLocalEnumValue(string enumTypeName, string memberName);

    /// <summary>
    /// Checks if the type name refers to a local inner class.
    /// </summary>
    bool IsLocalInnerClass(string typeName);

    /// <summary>
    /// Gets a member from a local inner class.
    /// </summary>
    GDRuntimeMemberInfo? GetInnerClassMember(string innerClassName, string memberName);
}
