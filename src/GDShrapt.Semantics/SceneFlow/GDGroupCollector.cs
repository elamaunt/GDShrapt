using GDShrapt.Reader;
using System;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// AST visitor that collects add_to_group() calls to build group membership information.
/// </summary>
internal class GDGroupCollector : GDVisitor
{
    public List<GDAddToGroupInfo> AddToGroupCalls { get; } = new();

    private readonly Func<string, GDExpression?>? _resolveVariable;
    private readonly Func<string, string, GDExpression?>? _resolveCrossClass;

    public GDGroupCollector(
        Func<string, GDExpression?>? resolveVariable = null,
        Func<string, string, GDExpression?>? resolveCrossClass = null)
    {
        _resolveVariable = resolveVariable;
        _resolveCrossClass = resolveCrossClass;
    }

    public override void Visit(GDCallExpression node)
    {
        var callName = GDNodePathExtractor.GetCallName(node);

        if (callName == GDWellKnownFunctions.AddToGroup)
            TryCollectAddToGroup(node);

        base.Visit(node);
    }

    private void TryCollectAddToGroup(GDCallExpression call)
    {
        var args = call.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return;

        var firstArg = args[0] as GDExpression;
        var groupName = GDStaticStringExtractor.TryExtractString(
            firstArg, _resolveVariable, _resolveCrossClass);

        if (string.IsNullOrEmpty(groupName))
            return;

        AddToGroupCalls.Add(new GDAddToGroupInfo
        {
            GroupName = groupName,
            IsOnSelf = IsCallOnSelf(call)
        });
    }

    private static bool IsCallOnSelf(GDCallExpression call)
    {
        // Bare call: add_to_group(...)
        if (call.CallerExpression is GDIdentifierExpression)
            return true;

        // self.add_to_group(...)
        if (call.CallerExpression is GDMemberOperatorExpression memberExpr &&
            memberExpr.CallerExpression is GDIdentifierExpression selfIdent &&
            selfIdent.Identifier?.Sequence == "self")
            return true;

        return false;
    }
}

internal class GDAddToGroupInfo
{
    public string GroupName { get; init; } = "";
    public bool IsOnSelf { get; init; }
}
