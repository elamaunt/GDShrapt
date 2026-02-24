using GDShrapt.Reader;

namespace GDShrapt.Abstractions;

/// <summary>
/// Type-specific rules for compile-time value evaluation.
/// Each tracked GDScript type (String, int, enum...) has its own implementation.
/// </summary>
public interface IGDStaticValueRules
{
    /// <summary>
    /// Try to extract a compile-time value from a literal expression.
    /// </summary>
    object? TryExtractLiteral(GDExpression expr);

    /// <summary>
    /// Try to evaluate a binary operation on two known values.
    /// </summary>
    object? TryEvaluateBinaryOp(GDDualOperatorType op, object left, object right);

    /// <summary>
    /// Get the AST node suitable for rename/edit. Null if value is computed.
    /// </summary>
    GDExpression? GetEditableSourceNode(GDExpression expr);
}
