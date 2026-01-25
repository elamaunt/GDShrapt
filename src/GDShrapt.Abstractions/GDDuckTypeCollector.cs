using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Collects duck type information by analyzing member accesses and method calls.
/// </summary>
public class GDDuckTypeCollector : GDVisitor
{
    private readonly Dictionary<string, GDDuckType> _variableDuckTypes;
    private readonly GDScopeStack? _scopes;

    /// <summary>
    /// Duck types collected for each variable.
    /// </summary>
    public IReadOnlyDictionary<string, GDDuckType> VariableDuckTypes => _variableDuckTypes;

    public GDDuckTypeCollector(GDScopeStack? scopes)
    {
        _variableDuckTypes = new Dictionary<string, GDDuckType>();
        _scopes = scopes;
    }

    /// <summary>
    /// Collects duck type information from an AST.
    /// </summary>
    public void Collect(GDNode? node)
    {
        node?.WalkIn(this);
    }

    public override void Visit(GDMemberOperatorExpression memberOp)
    {
        var varName = GetRootVariableName(memberOp.CallerExpression);
        if (varName == null)
            return;

        // Check if variable is untyped (no type or Variant type)
        var symbol = _scopes?.Lookup(varName);
        if (symbol != null && !string.IsNullOrEmpty(symbol.TypeName) && symbol.TypeName != "Variant")
            return; // Already has a known concrete type

        var memberName = memberOp.Identifier?.Sequence;
        if (string.IsNullOrEmpty(memberName))
            return;

        EnsureDuckType(varName).RequireProperty(memberName);
    }

    public override void Visit(GDCallExpression callExpr)
    {
        if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
        {
            var varName = GetRootVariableName(memberOp.CallerExpression);
            if (varName == null)
                return;

            // Check if variable is untyped (no type or Variant type)
            var symbol = _scopes?.Lookup(varName);
            if (symbol != null && !string.IsNullOrEmpty(symbol.TypeName) && symbol.TypeName != "Variant")
                return; // Already has a known concrete type

            var methodName = memberOp.Identifier?.Sequence;
            if (string.IsNullOrEmpty(methodName))
                return;

            EnsureDuckType(varName).RequireMethod(methodName);
        }
    }

    private static string? GetRootVariableName(GDExpression? expr)
    {
        while (expr is GDMemberOperatorExpression member)
            expr = member.CallerExpression;
        while (expr is GDIndexerExpression indexer)
            expr = indexer.CallerExpression;

        if (expr is GDIdentifierExpression ident)
            return ident.Identifier?.Sequence;

        return null;
    }

    private GDDuckType EnsureDuckType(string varName)
    {
        if (!_variableDuckTypes.TryGetValue(varName, out var duckType))
        {
            duckType = new GDDuckType();
            _variableDuckTypes[varName] = duckType;
        }
        return duckType;
    }
}
