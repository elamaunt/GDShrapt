using System.Collections.Generic;
using GDShrapt.Reader;

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
    GDArgumentTypeDiff? GetArgumentTypeDiff(GDCallExpression callExpression, int argumentIndex);

    /// <summary>
    /// Gets all argument type diffs for a call expression.
    /// </summary>
    IEnumerable<GDArgumentTypeDiff> GetAllArgumentTypeDiffs(GDCallExpression callExpression);

    /// <summary>
    /// Gets the inferred type of an expression.
    /// </summary>
    GDSemanticType? GetExpressionType(GDExpression expression);

    /// <summary>
    /// Gets the syntactic kind of an expression for diagnostic messages.
    /// </summary>
    GDExpressionSourceKind? GetExpressionSourceKind(GDExpression expression);
}
