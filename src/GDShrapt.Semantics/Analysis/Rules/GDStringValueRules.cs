using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

internal sealed class GDStringValueRules : IGDStaticValueRules
{
    public static readonly GDStringValueRules Instance = new();

    public object? TryExtractLiteral(GDExpression expr) => expr switch
    {
        GDStringExpression strExpr => strExpr.String?.Sequence,
        GDStringNameExpression snExpr => snExpr.String?.Sequence,
        _ => null
    };

    public object? TryEvaluateBinaryOp(GDDualOperatorType op, object left, object right)
    {
        if (op == GDDualOperatorType.Addition && left is string l && right is string r)
            return l + r;
        return null;
    }

    public GDExpression? GetEditableSourceNode(GDExpression expr) => expr switch
    {
        GDStringExpression => expr,
        GDStringNameExpression => expr,
        _ => null
    };
}
