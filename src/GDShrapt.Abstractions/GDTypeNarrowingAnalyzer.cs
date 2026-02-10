using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Abstractions;

/// <summary>
/// Analyzes control flow to extract type narrowing information.
/// Detects patterns like: if obj is Player, if obj.has_method("foo"), etc.
/// </summary>
public class GDTypeNarrowingAnalyzer
{
    private readonly IGDRuntimeProvider? _runtimeProvider;
    private Func<string, string?>? _variableTypeResolver;

    public GDTypeNarrowingAnalyzer(
        IGDRuntimeProvider? runtimeProvider,
        Func<string, string?>? variableTypeResolver = null)
    {
        _runtimeProvider = runtimeProvider;
        _variableTypeResolver = variableTypeResolver;
    }

    /// <summary>
    /// Sets the variable type resolver for scope-aware type lookup.
    /// Called before each AnalyzeCondition to provide current scope context.
    /// </summary>
    public void SetVariableTypeResolver(Func<string, string?>? resolver)
    {
        _variableTypeResolver = resolver;
    }

    /// <summary>
    /// Analyzes a condition expression and returns type narrowing information.
    /// </summary>
    /// <param name="condition">The condition expression</param>
    /// <param name="isNegated">True if in else branch (condition is false)</param>
    /// <returns>Type narrowing context for the branch</returns>
    public GDTypeNarrowingContext AnalyzeCondition(GDExpression? condition, bool isNegated = false)
    {
        var context = new GDTypeNarrowingContext();

        if (condition == null)
            return context;

        AnalyzeConditionInto(condition, context, isNegated);
        return context;
    }

    private void AnalyzeConditionInto(GDExpression condition, GDTypeNarrowingContext context, bool isNegated)
    {
        switch (condition)
        {
            // obj is Type
            case GDDualOperatorExpression dualOp when dualOp.Operator?.OperatorType == GDDualOperatorType.Is:
                AnalyzeIsExpression(dualOp, context, isNegated);
                break;

            // P1: "method" in obj - duck typing check
            case GDDualOperatorExpression inOp when inOp.Operator?.OperatorType == GDDualOperatorType.In:
                AnalyzeInExpression(inOp, context, isNegated);
                break;

            case GDDualOperatorExpression notInOp when notInOp.Operator?.OperatorType == GDDualOperatorType.In && notInOp.NotKeyword != null:
                AnalyzeInExpression(notInOp, context, !isNegated);
                break;

            // P10: obj == null / obj != null - null check
            // Also: obj == literal (type narrowing)
            case GDDualOperatorExpression eqOp when eqOp.Operator?.OperatorType == GDDualOperatorType.Equal ||
                                                    eqOp.Operator?.OperatorType == GDDualOperatorType.NotEqual:
                AnalyzeNullComparison(eqOp, context, isNegated);
                // Also check for literal type narrowing (x == 42)
                if (eqOp.Operator?.OperatorType == GDDualOperatorType.Equal && !isNegated)
                {
                    AnalyzeLiteralComparison(eqOp, context);
                }
                break;

            // obj.has_method("name") / obj.has_signal("name")
            case GDCallExpression callExpr:
                AnalyzeCallCondition(callExpr, context, isNegated);
                break;

            // not condition
            case GDSingleOperatorExpression singleOp
                when singleOp.Operator?.OperatorType == GDSingleOperatorType.Not ||
                     singleOp.Operator?.OperatorType == GDSingleOperatorType.Not2:
                if (singleOp.TargetExpression != null)
                    AnalyzeConditionInto(singleOp.TargetExpression, context, !isNegated);
                break;

            // condition and condition (both && and 'and' keyword)
            case GDDualOperatorExpression andOp when andOp.Operator?.OperatorType == GDDualOperatorType.And ||
                                                     andOp.Operator?.OperatorType == GDDualOperatorType.And2:
                if (!isNegated)
                {
                    // Both must be true
                    if (andOp.LeftExpression != null)
                        AnalyzeConditionInto(andOp.LeftExpression, context, false);
                    if (andOp.RightExpression != null)
                        AnalyzeConditionInto(andOp.RightExpression, context, false);
                }
                // In negated case (else branch), we can't conclude much
                break;

            // condition or condition (both || and 'or' keyword)
            case GDDualOperatorExpression orOp when orOp.Operator?.OperatorType == GDDualOperatorType.Or ||
                                                    orOp.Operator?.OperatorType == GDDualOperatorType.Or2:
                if (isNegated)
                {
                    // Both must be false
                    if (orOp.LeftExpression != null)
                        AnalyzeConditionInto(orOp.LeftExpression, context, true);
                    if (orOp.RightExpression != null)
                        AnalyzeConditionInto(orOp.RightExpression, context, true);
                }
                // In non-negated case, we can't conclude much (either could be true)
                break;

            // (condition)
            case GDBracketExpression bracket:
                if (bracket.InnerExpression != null)
                    AnalyzeConditionInto(bracket.InnerExpression, context, isNegated);
                break;

            // P8: if variable: (truthiness check) - variable is not null/false/empty
            case GDIdentifierExpression ident:
                AnalyzeTruthinessCheck(ident, context, isNegated);
                break;
        }
    }

    private void AnalyzeIsExpression(GDDualOperatorExpression isExpr, GDTypeNarrowingContext context, bool isNegated)
    {
        // Get the variable name
        var varName = GetVariableName(isExpr.LeftExpression);
        if (varName == null)
            return;

        // Get the type name
        var typeName = GetTypeName(isExpr.RightExpression);
        if (typeName == null)
            return;

        // Skip tautological narrowing: variable already has this exact type
        var declaredType = _variableTypeResolver?.Invoke(varName);
        if (declaredType != null && declaredType == typeName)
            return;

        if (isNegated)
        {
            // In else branch: obj is NOT this type
            context.ExcludeType(varName, GDSemanticType.FromRuntimeTypeName(typeName));
        }
        else
        {
            // In if branch: obj IS this type
            context.NarrowType(varName, GDSemanticType.FromRuntimeTypeName(typeName));
        }
    }

    private void AnalyzeCallCondition(GDCallExpression callExpr, GDTypeNarrowingContext context, bool isNegated)
    {
        // Check for has_method/has_signal/is_valid/is_null pattern
        if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
        {
            var methodName = memberOp.Identifier?.Sequence;
            var varName = GetVariableName(memberOp.CallerExpression);

            if (varName == null)
                return;

            // has_method("name")
            if (methodName == "has_method" && !isNegated)
            {
                var argName = GetStringArgument(callExpr);
                if (argName != null)
                    context.RequireMethod(varName, argName);
            }
            // has_signal("name")
            else if (methodName == "has_signal" && !isNegated)
            {
                var argName = GetStringArgument(callExpr);
                if (argName != null)
                    context.RequireSignal(varName, argName);
            }
            // has("property")
            else if (methodName == "has" && !isNegated)
            {
                var argName = GetStringArgument(callExpr);
                if (argName != null)
                    context.RequireProperty(varName, argName);
            }
            // Callable.is_valid() - returns true if callable is valid (not null)
            // if cb.is_valid(): → cb is valid (non-null)
            // if not cb.is_valid(): → cb may be null
            else if (methodName == "is_valid")
            {
                if (!isNegated)
                {
                    // In true branch: Callable is valid (not null)
                    context.MarkValidated(varName);
                    context.SetNotNull(varName);
                }
                else
                {
                    // In else branch (or negated): Callable may be null/invalid
                    context.SetMayBeNull(varName);
                }
            }
            // Callable.is_null() - returns true if callable is null
            // if cb.is_null(): → cb is null
            // if not cb.is_null(): → cb is valid
            else if (methodName == "is_null")
            {
                if (!isNegated)
                {
                    // In true branch: Callable is null
                    context.SetConcreteType(varName, GDNullSemanticType.Instance);
                    context.SetMayBeNull(varName);
                }
                else
                {
                    // In else branch (or negated): Callable is not null
                    context.MarkValidated(varName);
                    context.SetNotNull(varName);
                }
            }
        }

        // Check for is_instance_valid(obj)
        if (callExpr.CallerExpression is GDIdentifierExpression funcIdent)
        {
            var funcName = funcIdent.Identifier?.Sequence;
            if (funcName == "is_instance_valid" && !isNegated)
            {
                // Object is valid, but we don't learn much about type
            }
        }
    }

    /// <summary>
    /// P1: Analyzes "method" in obj pattern for duck typing.
    /// Also handles x in container pattern for type narrowing.
    /// When condition is true, obj is guaranteed to have that method, or x is element type.
    /// </summary>
    private void AnalyzeInExpression(GDDualOperatorExpression inExpr, GDTypeNarrowingContext context, bool isNegated)
    {
        // Pattern: "method_name" in obj (duck typing)
        if (inExpr.LeftExpression is GDStringExpression strExpr)
        {
            var memberName = strExpr.String?.Sequence;
            var varName = GetVariableName(inExpr.RightExpression);

            if (string.IsNullOrEmpty(memberName) || string.IsNullOrEmpty(varName))
                return;

            if (!isNegated)
            {
                // In if branch: obj has this member (could be method or property)
                context.RequireMethod(varName, memberName);
            }
        }

        // Pattern: x in container (type narrowing)
        if (!isNegated)
        {
            AnalyzeContainerMembershipCheck(inExpr, context);
        }
    }

    /// <summary>
    /// Analyzes x in container patterns for type narrowing.
    /// x in [1, 2, 3] -> x is int
    /// x in {"a": 1} -> x is String (key type)
    /// x in "hello" -> x is String
    /// </summary>
    private void AnalyzeContainerMembershipCheck(GDDualOperatorExpression inExpr, GDTypeNarrowingContext context)
    {
        var varName = GetVariableName(inExpr.LeftExpression);
        if (string.IsNullOrEmpty(varName))
            return;

        var container = inExpr.RightExpression;
        if (container == null)
            return;

        // Infer element/key type from container
        var elementType = InferContainerElementType(container);
        if (!string.IsNullOrEmpty(elementType) && elementType != "Variant" && elementType != "Unknown")
        {
            context.SetConcreteType(varName, GDSemanticType.FromRuntimeTypeName(elementType));
        }
    }

    /// <summary>
    /// Infers the element/key type from a container expression.
    /// </summary>
    private string? InferContainerElementType(GDExpression container)
    {
        // Array literal: [1, 2, 3] or ["a", "b"]
        if (container is GDArrayInitializerExpression arrayInit)
        {
            return InferArrayLiteralElementType(arrayInit);
        }

        // Dictionary literal: {"a": 1}
        if (container is GDDictionaryInitializerExpression dictInit)
        {
            return InferDictionaryLiteralKeyType(dictInit);
        }

        // String literal: "hello"
        if (container is GDStringExpression)
        {
            return "String";
        }

        // range() call
        if (container is GDCallExpression callExpr &&
            callExpr.CallerExpression is GDIdentifierExpression funcIdent &&
            funcIdent.Identifier?.Sequence == "range")
        {
            return "int";
        }

        // Variable with known type via resolver
        if (container is GDIdentifierExpression identExpr && _variableTypeResolver != null)
        {
            var varName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                var typeName = _variableTypeResolver(varName);
                if (!string.IsNullOrEmpty(typeName))
                {
                    return ExtractElementTypeFromTypeName(typeName);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the element/key type from a container type name.
    /// Array[int] -> int, Dictionary[String, int] -> String, PackedInt32Array -> int
    /// </summary>
    private static string? ExtractElementTypeFromTypeName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        // Array[T] -> T
        var arrayElement = GDGenericTypeHelper.ExtractArrayElementType(typeName);
        if (arrayElement != null)
            return arrayElement;

        // Dictionary[K, V] -> K (key type for 'in')
        var (keyType, _) = GDGenericTypeHelper.ExtractDictionaryTypes(typeName);
        if (keyType != null)
            return keyType;

        // String -> String
        if (typeName == "String")
            return "String";

        // Range -> int
        if (typeName == "Range")
            return "int";

        // PackedArrays
        return GDPackedArrayTypes.GetElementType(typeName);
    }

    /// <summary>
    /// Infers element type from array literal.
    /// </summary>
    private static string? InferArrayLiteralElementType(GDArrayInitializerExpression arrayInit)
    {
        var values = arrayInit.Values?.ToList();
        if (values == null || values.Count == 0)
            return null;

        string? commonType = null;
        foreach (var value in values)
        {
            var elementType = GetLiteralType(value);
            if (string.IsNullOrEmpty(elementType))
                return "Variant"; // Non-literal element

            if (commonType == null)
            {
                commonType = elementType;
            }
            else if (commonType != elementType)
            {
                // Mixed int/float -> keep as numeric
                if ((commonType == "int" || commonType == "float") &&
                    (elementType == "int" || elementType == "float"))
                {
                    commonType = "float";
                }
                else
                {
                    return "Variant"; // Truly mixed types
                }
            }
        }

        return commonType;
    }

    /// <summary>
    /// Infers key type from dictionary literal.
    /// </summary>
    private static string? InferDictionaryLiteralKeyType(GDDictionaryInitializerExpression dictInit)
    {
        var keyValues = dictInit.KeyValues?.ToList();
        if (keyValues == null || keyValues.Count == 0)
            return null;

        string? commonKeyType = null;
        foreach (var kv in keyValues)
        {
            var keyType = GetLiteralType(kv.Key);
            if (string.IsNullOrEmpty(keyType))
                return "Variant";

            if (commonKeyType == null)
            {
                commonKeyType = keyType;
            }
            else if (commonKeyType != keyType)
            {
                return "Variant"; // Mixed key types
            }
        }

        return commonKeyType;
    }

    /// <summary>
    /// Gets the type of a literal expression.
    /// </summary>
    private static string? GetLiteralType(GDExpression? expr)
    {
        return expr switch
        {
            GDNumberExpression numExpr => IsIntegerNumber(numExpr) ? "int" : "float",
            GDStringExpression => "String",
            GDBoolExpression => "bool",
            _ when IsNullLiteral(expr) => "null",
            _ => null
        };
    }

    /// <summary>
    /// Checks if a number expression represents an integer.
    /// </summary>
    private static bool IsIntegerNumber(GDNumberExpression numExpr)
    {
        var sequence = numExpr.Number?.Sequence;
        if (string.IsNullOrEmpty(sequence))
            return true;
        return !sequence.Contains('.');
    }

    /// <summary>
    /// Analyzes x == literal patterns for type narrowing.
    /// x == 42 -> x is int
    /// x == "hello" -> x is String
    /// x == null -> x is null
    /// </summary>
    private void AnalyzeLiteralComparison(GDDualOperatorExpression eqExpr, GDTypeNarrowingContext context)
    {
        string? varName = null;
        string? literalType = null;

        // variable == literal (including null)
        if (eqExpr.LeftExpression is GDIdentifierExpression leftIdent)
        {
            // Don't treat 'null' identifier as a variable
            if (leftIdent.Identifier?.Sequence != "null")
            {
                var type = GetLiteralType(eqExpr.RightExpression);
                if (!string.IsNullOrEmpty(type))
                {
                    varName = leftIdent.Identifier?.Sequence;
                    literalType = type;
                }
            }
        }
        // literal == variable (including null)
        else if (eqExpr.RightExpression is GDIdentifierExpression rightIdent)
        {
            // Don't treat 'null' identifier as a variable
            if (rightIdent.Identifier?.Sequence != "null")
            {
                var type = GetLiteralType(eqExpr.LeftExpression);
                if (!string.IsNullOrEmpty(type))
                {
                    varName = rightIdent.Identifier?.Sequence;
                    literalType = type;
                }
            }
        }

        if (!string.IsNullOrEmpty(varName) && !string.IsNullOrEmpty(literalType))
        {
            // Skip tautological narrowing: variable already has this type
            var declaredType = _variableTypeResolver?.Invoke(varName);
            if (declaredType != null && declaredType == literalType)
                return;

            context.SetConcreteType(varName, GDSemanticType.FromRuntimeTypeName(literalType));
        }
    }

    /// <summary>
    /// P10: Analyzes null comparison (obj == null or obj != null).
    /// </summary>
    private void AnalyzeNullComparison(GDDualOperatorExpression eqExpr, GDTypeNarrowingContext context, bool isNegated)
    {
        var isEqualOp = eqExpr.Operator?.OperatorType == GDDualOperatorType.Equal;

        // Determine which side is the null literal and which is the variable
        string? varName = null;
        bool isNullOnRight = false;

        if (IsNullLiteral(eqExpr.RightExpression))
        {
            varName = GetVariableName(eqExpr.LeftExpression);
            isNullOnRight = true;
        }
        else if (IsNullLiteral(eqExpr.LeftExpression))
        {
            varName = GetVariableName(eqExpr.RightExpression);
            isNullOnRight = false;
        }

        if (string.IsNullOrEmpty(varName))
            return;

        // Logic:
        // - "x == null" with isNegated=false: x IS null in the if branch -> nothing useful
        // - "x == null" with isNegated=true (else branch): x IS NOT null -> narrow to non-null
        // - "x != null" with isNegated=false: x IS NOT null -> narrow to non-null
        // - "x != null" with isNegated=true (else branch): x IS null -> nothing useful
        bool narrowsToNonNull = (isEqualOp && isNegated) || (!isEqualOp && !isNegated);

        if (narrowsToNonNull)
        {
            // Mark variable as guaranteed non-null (narrowed away from null)
            context.ExcludeType(varName, GDNullSemanticType.Instance);
        }
    }

    /// <summary>
    /// P8: Analyzes truthiness check (if variable:).
    /// If variable is used directly as condition, it's truthy (non-null, non-empty, non-false).
    /// </summary>
    private void AnalyzeTruthinessCheck(GDIdentifierExpression ident, GDTypeNarrowingContext context, bool isNegated)
    {
        var varName = ident.Identifier?.Sequence;
        if (string.IsNullOrEmpty(varName))
            return;

        if (!isNegated)
        {
            // if variable: -> variable is truthy (not null, not false, not 0, not empty)
            // For type narrowing, this means the variable is at least non-null
            context.ExcludeType(varName, GDNullSemanticType.Instance);
            context.SetNotNull(varName);
            // For Callable, truthy also means valid
            context.MarkValidated(varName);
        }
        else
        {
            // if not variable: -> variable is falsy (null, false, 0, empty)
            // For bool variables, "not bool_var" means false, not null - skip narrowing
            var declaredType = _variableTypeResolver?.Invoke(varName);
            if (declaredType == "bool")
                return;

            // For Callable and other types, this means it's invalid/null
            context.SetMayBeNull(varName);
            context.SetConcreteType(varName, GDNullSemanticType.Instance);
        }
    }

    private static bool IsNullLiteral(GDExpression? expr)
    {
        if (expr is GDIdentifierExpression ident)
            return ident.Identifier?.Sequence == "null";
        return false;
    }

    private static string? GetVariableName(GDExpression? expr)
    {
        if (expr is GDIdentifierExpression ident)
            return ident.Identifier?.Sequence;
        return null;
    }

    private static string? GetTypeName(GDExpression? expr)
    {
        if (expr is GDIdentifierExpression ident)
            return ident.Identifier?.Sequence;
        return null;
    }

    private static string? GetStringArgument(GDCallExpression callExpr)
    {
        var args = callExpr.Parameters;
        if (args == null || args.Count == 0)
            return null;

        var firstArg = args.FirstOrDefault();
        if (firstArg is GDStringExpression strExpr)
        {
            // GDStringNode.Sequence returns content without quotes
            return strExpr.String?.Sequence;
        }

        return null;
    }

}
