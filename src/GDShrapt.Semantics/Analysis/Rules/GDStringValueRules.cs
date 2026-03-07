using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

internal sealed class GDStringValueRules : IGDStaticValueRules
{
    public static readonly GDStringValueRules Instance = new();

    private readonly GDNodeRegistry _registry;

    public GDStringValueRules()
    {
    }

    public GDStringValueRules(GDNodeRegistry registry)
    {
        _registry = registry;
    }

    public object? TryExtractLiteral(GDNodeHandle handle)
    {
        var expr = _registry?.ResolveNode(handle) as GDExpression;
        if (expr == null)
            return null;

        return expr switch
        {
            GDStringExpression strExpr => strExpr.String?.Sequence,
            GDStringNameExpression snExpr => snExpr.String?.Sequence,
            _ => null
        };
    }

    public object? TryEvaluateBinaryOp(string op, object left, object right)
    {
        if (op == nameof(GDDualOperatorType.Addition) && left is string l && right is string r)
            return l + r;
        return null;
    }

    public GDNodeHandle GetEditableSourceNode(GDNodeHandle handle)
    {
        var expr = _registry?.ResolveNode(handle) as GDExpression;
        if (expr == null)
            return GDNodeHandle.Empty;

        return expr switch
        {
            GDStringExpression => handle,
            GDStringNameExpression => handle,
            _ => GDNodeHandle.Empty
        };
    }
}
