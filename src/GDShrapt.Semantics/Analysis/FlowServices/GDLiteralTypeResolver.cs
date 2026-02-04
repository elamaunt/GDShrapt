using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Resolves types of literal expressions.
/// Extracted from GDFlowAnalyzer to reduce its size.
/// </summary>
internal static class GDLiteralTypeResolver
{
    /// <summary>
    /// Checks if an expression is a literal (number, string, bool, array, dictionary, or null).
    /// </summary>
    public static bool IsLiteralExpression(GDExpression? expr)
    {
        return expr is GDNumberExpression ||
               expr is GDStringExpression ||
               expr is GDBoolExpression ||
               expr is GDArrayInitializerExpression ||
               expr is GDDictionaryInitializerExpression ||
               IsNullLiteral(expr);
    }

    /// <summary>
    /// Gets the type of a literal expression.
    /// </summary>
    public static string? GetLiteralType(GDExpression? expr)
    {
        return expr switch
        {
            GDNumberExpression numExpr => IsIntegerNumber(numExpr) ? "int" : "float",
            GDStringExpression => "String",
            GDBoolExpression => "bool",
            GDArrayInitializerExpression => "Array",
            GDDictionaryInitializerExpression => "Dictionary",
            _ when IsNullLiteral(expr) => "null",
            _ => null
        };
    }

    /// <summary>
    /// Checks if a number expression represents an integer (no decimal point).
    /// </summary>
    public static bool IsIntegerNumber(GDNumberExpression numExpr)
    {
        var sequence = numExpr.Number?.Sequence;
        if (string.IsNullOrEmpty(sequence))
            return true; // Default to int if unknown

        // If it contains a dot, it's a float
        return !sequence.Contains('.');
    }

    /// <summary>
    /// Checks if an expression is a null literal.
    /// In GDScript, null is represented as GDIdentifierExpression with "null" identifier.
    /// </summary>
    public static bool IsNullLiteral(GDExpression? expr)
    {
        if (expr is GDIdentifierExpression identExpr)
        {
            return identExpr.Identifier?.Sequence == "null";
        }
        return false;
    }

    /// <summary>
    /// Extracts string literal value from an expression.
    /// </summary>
    public static string? GetStringLiteralValue(GDExpression? expr)
    {
        if (expr is GDStringExpression strExpr)
        {
            var str = strExpr.String?.Sequence;
            if (!string.IsNullOrEmpty(str))
                return str;
        }

        return null;
    }

    /// <summary>
    /// Extracts type name from an expression (for type checks like 'is Type').
    /// </summary>
    public static string? GetTypeNameFromExpression(GDExpression? expr)
    {
        if (expr == null)
            return null;

        // Simple identifier: Dictionary, Array, Node, etc.
        if (expr is GDIdentifierExpression identExpr)
            return identExpr.Identifier?.Sequence;

        // Could also handle member expressions like SomeClass.InnerType
        if (expr is GDMemberOperatorExpression memberExpr)
        {
            var caller = GetTypeNameFromExpression(memberExpr.CallerExpression);
            var member = memberExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(caller) && !string.IsNullOrEmpty(member))
                return $"{caller}.{member}";
        }

        return expr.ToString();
    }
}
