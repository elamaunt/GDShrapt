using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Validates that coroutine calls are awaited.
/// Detects calls to methods containing 'await' that are not themselves awaited.
/// Reports GD5011 (PossibleMissedAwait).
/// </summary>
public class GDAwaitConsistencyValidator : GDValidationVisitor
{
    private readonly GDSemanticModel _semanticModel;
    private readonly HashSet<string> _coroutineMethodNames;

    public GDAwaitConsistencyValidator(
        GDValidationContext context,
        GDSemanticModel semanticModel)
        : base(context)
    {
        _semanticModel = semanticModel;
        _coroutineMethodNames = CollectCoroutineMethods();
    }

    public void Validate(GDNode? node)
    {
        node?.WalkIn(this);
    }

    public override void Visit(GDCallExpression callExpr)
    {
        // Skip if this call is already inside an await expression
        if (IsInsideAwait(callExpr))
            return;

        var methodName = GetCalledMethodName(callExpr);
        if (string.IsNullOrEmpty(methodName))
            return;

        if (_coroutineMethodNames.Contains(methodName))
        {
            ReportWarning(
                GDDiagnosticCode.PossibleMissedAwait,
                $"Method '{methodName}' is a coroutine (contains 'await') but is called without 'await'",
                callExpr);
        }
    }

    private bool IsInsideAwait(GDNode node)
    {
        var parent = node.Parent;
        while (parent != null)
        {
            if (parent is GDAwaitExpression)
                return true;

            // Stop walking at statement boundary
            if (parent is GDStatement)
                return false;

            parent = parent.Parent;
        }

        return false;
    }

    private string? GetCalledMethodName(GDCallExpression callExpr)
    {
        // Direct call: method_name()
        if (callExpr.CallerExpression is GDIdentifierExpression idExpr)
            return idExpr.Identifier?.Sequence;

        // Member call: self.method_name() — only check if caller is "self"
        if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
        {
            if (memberOp.CallerExpression is GDIdentifierExpression selfExpr &&
                selfExpr.Identifier?.Sequence == "self")
            {
                return memberOp.Identifier?.Sequence;
            }
        }

        return null;
    }

    private HashSet<string> CollectCoroutineMethods()
    {
        var result = new HashSet<string>();

        foreach (var method in _semanticModel.GetMethods())
        {
            if (method.IsCoroutine)
                result.Add(method.Name);
        }

        return result;
    }
}
