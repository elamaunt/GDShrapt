namespace GDShrapt.Abstractions;

/// <summary>
/// Describes the syntactic kind of an expression for diagnostic messages.
/// </summary>
public enum GDExpressionSourceKind
{
    StringLiteral,
    IntegerLiteral,
    FloatLiteral,
    BooleanLiteral,
    NullLiteral,
    ArrayLiteral,
    DictionaryLiteral,
    Variable,
    PropertyAccess,
    FunctionCallResult,
    IndexerAccess
}
