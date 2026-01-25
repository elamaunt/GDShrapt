using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Interface for analyzing argument types at call sites.
/// Used by GDCallValidator to check argument/parameter type compatibility.
/// Implemented by GDSemanticModel in GDShrapt.Semantics.
/// </summary>
public interface IGDArgumentTypeAnalyzer
{
    /// <summary>
    /// Gets the type diff for a call expression argument at the given index.
    /// Returns null if the called function/method is unknown or has no type info.
    /// </summary>
    /// <param name="callExpression">The call expression node (GDCallExpression).</param>
    /// <param name="argumentIndex">The 0-based index of the argument.</param>
    /// <returns>Type diff comparing argument type vs parameter type, or null if cannot analyze.</returns>
    GDArgumentTypeDiff? GetArgumentTypeDiff(object callExpression, int argumentIndex);

    /// <summary>
    /// Gets all argument type diffs for a call expression.
    /// </summary>
    /// <param name="callExpression">The call expression node (GDCallExpression).</param>
    /// <returns>Enumerable of type diffs for each argument.</returns>
    IEnumerable<GDArgumentTypeDiff> GetAllArgumentTypeDiffs(object callExpression);

    /// <summary>
    /// Gets the inferred type of an expression.
    /// </summary>
    /// <param name="expression">The expression to analyze.</param>
    /// <returns>The type name, or null if unknown.</returns>
    string? GetExpressionType(object expression);

    /// <summary>
    /// Gets the source description for an expression type.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns>Description like "string literal", "variable 'x'", "function call result".</returns>
    string? GetExpressionTypeSource(object expression);
}
