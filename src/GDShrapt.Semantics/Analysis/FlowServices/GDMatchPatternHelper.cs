using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

internal enum GDMatchBindingContext
{
    Direct,
    ArrayElement,
    DictionaryValue
}

internal static class GDMatchPatternHelper
{
    internal static GDMatchStatement? FindEnclosingMatchStatement(GDNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is GDMatchStatement match)
                return match;
            current = current.Parent;
        }
        return null;
    }

    internal static GDMatchCaseDeclaration? FindEnclosingMatchCase(GDNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is GDMatchCaseDeclaration matchCase)
                return matchCase;
            current = current.Parent;
        }
        return null;
    }

    internal static GDMatchBindingContext DetermineBindingContext(GDMatchCaseVariableExpression varExpr)
    {
        var current = varExpr.Parent;
        while (current != null && current is not GDMatchCaseDeclaration)
        {
            if (current is GDArrayInitializerExpression)
                return GDMatchBindingContext.ArrayElement;
            if (current is GDDictionaryKeyValueDeclaration)
                return GDMatchBindingContext.DictionaryValue;
            current = current.Parent;
        }
        return GDMatchBindingContext.Direct;
    }

    internal static (string? varName, string? typeName) ExtractGuardNarrowing(GDMatchCaseDeclaration matchCase)
    {
        if (matchCase.GuardCondition is GDDualOperatorExpression guard &&
            guard.Operator?.OperatorType == GDDualOperatorType.Is &&
            guard.LeftExpression is GDIdentifierExpression leftId &&
            guard.RightExpression is GDIdentifierExpression rightId)
        {
            return (leftId.Identifier?.Sequence, rightId.Identifier?.Sequence);
        }
        return (null, null);
    }

    internal static string? InferMatchBindingType(string? subjectType, GDMatchBindingContext context)
    {
        if (string.IsNullOrEmpty(subjectType))
            return null;

        return context switch
        {
            GDMatchBindingContext.ArrayElement => InferArrayElementType(subjectType),
            GDMatchBindingContext.DictionaryValue => InferDictValueType(subjectType),
            _ => subjectType
        };
    }

    private static string? InferArrayElementType(string subjectType)
    {
        var elementType = GDGenericTypeHelper.ExtractArrayElementType(subjectType);
        if (elementType != null)
            return elementType;

        var packedElement = GDPackedArrayTypes.GetElementType(subjectType);
        if (packedElement != null)
            return packedElement;

        return GDWellKnownTypes.Variant;
    }

    private static string? InferDictValueType(string subjectType)
    {
        var (_, valueType) = GDGenericTypeHelper.ExtractDictionaryTypes(subjectType);
        return valueType ?? GDWellKnownTypes.Variant;
    }
}
