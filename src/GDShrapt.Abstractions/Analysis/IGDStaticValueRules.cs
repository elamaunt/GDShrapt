namespace GDShrapt.Abstractions;

/// <summary>
/// Type-specific rules for compile-time value evaluation.
/// Each tracked GDScript type (String, int, enum...) has its own implementation.
/// </summary>
public interface IGDStaticValueRules
{
    /// <summary>
    /// Try to extract a compile-time value from a literal expression node.
    /// </summary>
    object? TryExtractLiteral(GDNodeHandle node);

    /// <summary>
    /// Try to evaluate a binary operation on two known values.
    /// </summary>
    object? TryEvaluateBinaryOp(string operatorName, object left, object right);

    /// <summary>
    /// Get the handle to the AST node suitable for rename/edit. Empty if value is computed.
    /// </summary>
    GDNodeHandle GetEditableSourceNode(GDNodeHandle node);
}
