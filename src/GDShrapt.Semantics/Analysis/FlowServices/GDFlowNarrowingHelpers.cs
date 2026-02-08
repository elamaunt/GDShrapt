using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Static helper methods for flow-sensitive type narrowing.
/// Extracted from GDFlowAnalyzer to reduce its size.
/// </summary>
internal static class GDFlowNarrowingHelpers
{
    /// <summary>
    /// Extracts element/key type from a type name.
    /// Array[int] -> int, Dictionary[String, int] -> String, String -> String
    /// </summary>
    public static string? ExtractElementTypeFromTypeName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        // Array[T] -> T
        var arrayElement = GDGenericTypeHelper.ExtractArrayElementType(typeName);
        if (arrayElement != null)
            return arrayElement;

        // Dictionary[K, V] -> K (key type only)
        var (keyType, _) = GDGenericTypeHelper.ExtractDictionaryTypes(typeName);
        if (keyType != null)
            return keyType;

        // String -> String
        if (typeName == GDWellKnownTypes.Strings.String)
            return GDWellKnownTypes.Strings.String;

        // Range -> int
        if (typeName == GDWellKnownTypes.Other.Range)
            return GDWellKnownTypes.Numeric.Int;

        // PackedArrays
        return GDPackedArrayTypes.GetElementType(typeName);
    }

    /// <summary>
    /// Extracts the generic type parameter from a generic type string.
    /// For example: "Array[int]" -> "int", "Dictionary[String, int]" -> "String, int"
    /// </summary>
    public static string? ExtractGenericTypeParameter(string genericType, string prefix)
    {
        if (string.IsNullOrEmpty(genericType) ||
            !genericType.StartsWith(prefix) ||
            !genericType.EndsWith("]"))
        {
            return null;
        }

        return genericType.Substring(prefix.Length, genericType.Length - prefix.Length - 1);
    }

    /// <summary>
    /// Gets the root variable name from an expression chain.
    /// </summary>
    public static string? GetRootVariableName(GDExpression? expr)
    {
        while (expr is GDMemberOperatorExpression member)
            expr = member.CallerExpression;
        while (expr is GDIndexerExpression indexer)
            expr = indexer.CallerExpression;

        if (expr is GDIdentifierExpression ident)
            return ident.Identifier?.Sequence;

        return null;
    }

    /// <summary>
    /// Applies narrowing from is_instance_valid() checks.
    /// is_instance_valid(x) -> x is guaranteed non-null and valid.
    /// </summary>
    public static void ApplyIsInstanceValidNarrowing(GDCallExpression callExpr, GDFlowState state)
    {
        // Handle both is_instance_valid(x) (global function) and obj.is_instance_valid() patterns
        string? funcName = null;
        GDExpression? checkedExpr = null;

        if (callExpr.CallerExpression is GDIdentifierExpression funcIdent)
        {
            // Global function: is_instance_valid(x)
            funcName = funcIdent.Identifier?.Sequence;
            var args = callExpr.Parameters?.ToList();
            if (args != null && args.Count > 0)
                checkedExpr = args[0];
        }
        else if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
        {
            // Member method (less common, but handle it): obj.is_instance_valid() - not standard but just in case
            funcName = memberOp.Identifier?.Sequence;
        }

        if (funcName != "is_instance_valid")
            return;

        if (checkedExpr is GDIdentifierExpression checkedIdent)
        {
            var varName = checkedIdent.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
                state.MarkNonNull(varName);
        }
    }

    /// <summary>
    /// Applies truthiness narrowing.
    /// if x: -> x is truthy (non-null, non-zero, non-empty)
    /// </summary>
    public static void ApplyTruthinessNarrowing(GDIdentifierExpression identExpr, GDFlowState state)
    {
        var varName = identExpr.Identifier?.Sequence;
        if (string.IsNullOrEmpty(varName))
            return;

        // Truthiness check implies non-null
        state.MarkNonNull(varName);
    }

    /// <summary>
    /// Applies null comparison narrowing.
    /// x != null -> MarkNonNull
    /// x == null -> Mark as definitely null (inverse - for else branch)
    /// </summary>
    public static void ApplyNullComparisonNarrowing(GDDualOperatorExpression eqOp, GDFlowState state)
    {
        var opType = eqOp.Operator?.OperatorType;
        var leftExpr = eqOp.LeftExpression;
        var rightExpr = eqOp.RightExpression;

        string? varName = null;
        bool rightIsNull = false;
        bool leftIsNull = false;

        // Check if right side is null (represented as GDIdentifierExpression with "null")
        if (GDLiteralTypeResolver.IsNullLiteral(rightExpr))
        {
            rightIsNull = true;
            if (leftExpr is GDIdentifierExpression leftIdent)
                varName = leftIdent.Identifier?.Sequence;
        }
        // Check if left side is null
        else if (GDLiteralTypeResolver.IsNullLiteral(leftExpr))
        {
            leftIsNull = true;
            if (rightExpr is GDIdentifierExpression rightIdent)
                varName = rightIdent.Identifier?.Sequence;
        }

        if (string.IsNullOrEmpty(varName))
            return;

        if (rightIsNull || leftIsNull)
        {
            if (opType == GDDualOperatorType.NotEqual)
            {
                // x != null -> x is guaranteed non-null
                state.MarkNonNull(varName);
            }
            else if (opType == GDDualOperatorType.Equal)
            {
                // x == null -> x is definitely null in this branch
                state.MarkPotentiallyNull(varName);
            }
        }
    }

    /// <summary>
    /// Applies literal comparison narrowing.
    /// x == 42 -> x is narrowed to int
    /// x == "hello" -> x is narrowed to String
    /// </summary>
    public static void ApplyLiteralComparisonNarrowing(GDDualOperatorExpression eqOp, GDFlowState state)
    {
        string? varName = null;
        string? literalType = null;

        // variable == literal
        if (eqOp.LeftExpression is GDIdentifierExpression leftIdent &&
            GDLiteralTypeResolver.IsLiteralExpression(eqOp.RightExpression))
        {
            varName = leftIdent.Identifier?.Sequence;
            literalType = GDLiteralTypeResolver.GetLiteralType(eqOp.RightExpression);
        }
        // literal == variable
        else if (eqOp.RightExpression is GDIdentifierExpression rightIdent &&
                 GDLiteralTypeResolver.IsLiteralExpression(eqOp.LeftExpression))
        {
            varName = rightIdent.Identifier?.Sequence;
            literalType = GDLiteralTypeResolver.GetLiteralType(eqOp.LeftExpression);
        }

        if (!string.IsNullOrEmpty(varName) && !string.IsNullOrEmpty(literalType))
        {
            state.NarrowType(varName, GDSemanticType.FromRuntimeTypeName(literalType));
            // Non-null literals mark variable as non-null
            if (literalType != "null")
                state.MarkNonNull(varName);
        }
    }
}
