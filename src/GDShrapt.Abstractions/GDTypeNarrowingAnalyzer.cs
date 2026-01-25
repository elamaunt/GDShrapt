using GDShrapt.Reader;
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

    public GDTypeNarrowingAnalyzer(IGDRuntimeProvider? runtimeProvider)
    {
        _runtimeProvider = runtimeProvider;
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

            // P10: obj == null / obj != null - null check
            case GDDualOperatorExpression eqOp when eqOp.Operator?.OperatorType == GDDualOperatorType.Equal ||
                                                    eqOp.Operator?.OperatorType == GDDualOperatorType.NotEqual:
                AnalyzeNullComparison(eqOp, context, isNegated);
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

        if (isNegated)
        {
            // In else branch: obj is NOT this type
            context.ExcludeType(varName, typeName);
        }
        else
        {
            // In if branch: obj IS this type
            context.NarrowType(varName, typeName);
        }
    }

    private void AnalyzeCallCondition(GDCallExpression callExpr, GDTypeNarrowingContext context, bool isNegated)
    {
        // Check for has_method/has_signal pattern
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
    /// When condition is true, obj is guaranteed to have that method.
    /// </summary>
    private void AnalyzeInExpression(GDDualOperatorExpression inExpr, GDTypeNarrowingContext context, bool isNegated)
    {
        // Pattern: "method_name" in obj
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
            context.ExcludeType(varName, "null");
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
            context.ExcludeType(varName, "null");
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
