using GDShrapt.Reader;
using System;
using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Collects duck type information by analyzing member accesses and method calls.
/// Filters out universal Object members that exist on all objects.
/// </summary>
public class GDDuckTypeCollector : GDVisitor
{
    private readonly Dictionary<string, GDDuckType> _variableDuckTypes;
    private readonly GDScopeStack? _scopes;
    private readonly HashSet<string>? _objectMembers;

    /// <summary>
    /// Duck types collected for each variable.
    /// </summary>
    public IReadOnlyDictionary<string, GDDuckType> VariableDuckTypes => _variableDuckTypes;

    /// <summary>
    /// Creates a new duck type collector.
    /// </summary>
    /// <param name="scopes">Scope stack for checking variable types</param>
    /// <param name="runtimeProvider">Runtime provider for getting Object members to filter</param>
    public GDDuckTypeCollector(GDScopeStack? scopes, IGDRuntimeProvider? runtimeProvider = null)
    {
        _variableDuckTypes = new Dictionary<string, GDDuckType>();
        _scopes = scopes;
        _objectMembers = BuildObjectMembersCache(runtimeProvider);
    }

    /// <summary>
    /// Builds a cache of all Object class members to filter from duck-typing.
    /// These are universal methods/properties available on ALL objects.
    /// </summary>
    private static HashSet<string>? BuildObjectMembersCache(IGDRuntimeProvider? runtimeProvider)
    {
        if (runtimeProvider == null)
            return null;

        var objectType = runtimeProvider.GetTypeInfo("Object");
        if (objectType?.Members == null)
            return null;

        var members = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in objectType.Members)
        {
            if (!string.IsNullOrEmpty(member.Name))
                members.Add(member.Name);
        }
        return members.Count > 0 ? members : null;
    }

    /// <summary>
    /// Checks if a member name is a universal Object member that should be excluded from duck-typing.
    /// </summary>
    private bool IsObjectMember(string memberName)
    {
        return _objectMembers?.Contains(memberName) ?? false;
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

        // Skip Object members - they exist on all objects
        if (IsObjectMember(memberName))
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

            // Skip Object methods - they exist on all objects
            if (IsObjectMember(methodName))
                return;

            EnsureDuckType(varName).RequireMethod(methodName);
        }
    }

    public override void Visit(GDDualOperatorExpression dualOp)
    {
        var opType = dualOp.Operator?.OperatorType;
        if (opType == null)
            return;

        // Only collect for arithmetic operators
        if (!IsArithmeticOperator(opType.Value))
            return;

        // Left operand - if it's an untyped variable, record the operator requirement
        var leftVar = GetRootVariableName(dualOp.LeftExpression);
        if (leftVar != null && IsUntypedVariable(leftVar))
        {
            var rightType = InferSimpleType(dualOp.RightExpression);
            EnsureDuckType(leftVar).RequireOperator(opType.Value, rightType);
        }

        // Right operand - if it's an untyped variable, record the operator requirement
        var rightVar = GetRootVariableName(dualOp.RightExpression);
        if (rightVar != null && IsUntypedVariable(rightVar))
        {
            var leftType = InferSimpleType(dualOp.LeftExpression);
            EnsureDuckType(rightVar).RequireOperator(opType.Value, leftType);
        }
    }

    /// <summary>
    /// Checks if the operator is an arithmetic operator that restricts types.
    /// </summary>
    private static bool IsArithmeticOperator(GDDualOperatorType op)
    {
        return op == GDDualOperatorType.Addition ||
               op == GDDualOperatorType.Subtraction ||
               op == GDDualOperatorType.Multiply ||
               op == GDDualOperatorType.Division ||
               op == GDDualOperatorType.Mod;
    }

    /// <summary>
    /// Checks if a variable is untyped (no type annotation or Variant type).
    /// </summary>
    private bool IsUntypedVariable(string varName)
    {
        var symbol = _scopes?.Lookup(varName);
        return symbol == null || string.IsNullOrEmpty(symbol.TypeName) || symbol.TypeName == "Variant";
    }

    /// <summary>
    /// Simple type inference for literal expressions.
    /// </summary>
    private static string? InferSimpleType(GDExpression? expr)
    {
        if (expr == null)
            return null;

        switch (expr)
        {
            case GDNumberExpression numExpr:
                return numExpr.Number?.Sequence?.Contains(".") == true ? "float" : "int";
            case GDStringExpression _:
                return "String";
            case GDBoolExpression _:
                return "bool";
            case GDArrayInitializerExpression _:
                return "Array";
            case GDDictionaryInitializerExpression _:
                return "Dictionary";
            default:
                return null;
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
