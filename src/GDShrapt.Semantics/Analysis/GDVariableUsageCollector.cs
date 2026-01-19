using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Collects assignment observations for Variant variables to infer Union types.
/// </summary>
internal class GDVariableUsageCollector : GDVisitor
{
    private readonly GDScopeStack? _scopes;
    private readonly GDTypeInferenceEngine? _typeEngine;
    private readonly Dictionary<string, GDVariableUsageProfile> _profiles = new();

    /// <summary>
    /// Collected variable usage profiles.
    /// </summary>
    public IReadOnlyDictionary<string, GDVariableUsageProfile> Profiles => _profiles;

    public GDVariableUsageCollector(GDScopeStack? scopes, GDTypeInferenceEngine? typeEngine)
    {
        _scopes = scopes;
        _typeEngine = typeEngine;
    }

    /// <summary>
    /// Collects variable usage profiles from a method declaration.
    /// </summary>
    public void Collect(GDMethodDeclaration method)
    {
        method?.WalkIn(this);
    }

    public override void Visit(GDVariableDeclarationStatement varDecl)
    {
        var varName = varDecl.Identifier?.Sequence;
        if (string.IsNullOrEmpty(varName))
            return;

        // Only track Variant variables (no explicit type annotation)
        if (varDecl.Type != null)
            return;

        var token = varDecl.AllTokens.FirstOrDefault();
        var profile = new GDVariableUsageProfile(varName)
        {
            DeclarationLine = token?.StartLine ?? 0,
            DeclarationColumn = token?.StartColumn ?? 0
        };
        _profiles[varName] = profile;

        // If there's an initializer, record it as the first assignment
        if (varDecl.Initializer != null)
        {
            var initType = _typeEngine?.InferType(varDecl.Initializer);
            var isHighConfidence = DetermineHighConfidence(varDecl.Initializer, initType);

            var initToken = varDecl.Initializer.AllTokens.FirstOrDefault();
            profile.Assignments.Add(new GDAssignmentObservation
            {
                InferredType = initType,
                IsHighConfidence = isHighConfidence,
                Node = varDecl,
                Line = initToken?.StartLine ?? token?.StartLine ?? 0,
                Column = initToken?.StartColumn ?? token?.StartColumn ?? 0,
                Kind = GDAssignmentKind.Initialization
            });
        }
    }

    public override void Visit(GDDualOperatorExpression dualOp)
    {
        // Check for assignment operators
        var opType = dualOp.Operator?.OperatorType;
        if (opType == null)
            return;

        var isAssignment = opType == GDDualOperatorType.Assignment;
        var isCompoundAssignment = IsCompoundAssignment(opType.Value);

        if (!isAssignment && !isCompoundAssignment)
            return;

        // Get the variable being assigned
        if (dualOp.LeftExpression is GDIdentifierExpression identExpr)
        {
            var varName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName) && _profiles.TryGetValue(varName, out var profile))
            {
                var valueType = _typeEngine?.InferType(dualOp.RightExpression);
                var isHighConfidence = DetermineHighConfidence(dualOp.RightExpression, valueType);

                var token = dualOp.AllTokens.FirstOrDefault();
                profile.Assignments.Add(new GDAssignmentObservation
                {
                    InferredType = valueType,
                    IsHighConfidence = isHighConfidence,
                    Node = dualOp,
                    Line = token?.StartLine ?? 0,
                    Column = token?.StartColumn ?? 0,
                    Kind = isCompoundAssignment ? GDAssignmentKind.CompoundAssignment : GDAssignmentKind.DirectAssignment
                });
            }
        }
    }

    private bool DetermineHighConfidence(GDExpression? expr, string? inferredType)
    {
        if (expr == null || string.IsNullOrEmpty(inferredType) || inferredType == "Variant")
            return false;

        // Literals have certain confidence
        if (expr is GDNumberExpression or GDStringExpression or GDBoolExpression)
            return true;

        // ClassName.new() has high confidence
        if (expr is GDCallExpression call &&
            call.CallerExpression is GDMemberOperatorExpression member &&
            member.Identifier?.Sequence == "new")
            return true;

        // Array/Dictionary initializers have certain confidence for container type
        if (expr is GDArrayInitializerExpression or GDDictionaryInitializerExpression)
            return true;

        // Known typed identifier has high confidence
        if (expr is GDIdentifierExpression identExpr)
        {
            var identName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(identName))
            {
                var symbol = _scopes?.Lookup(identName);
                if (symbol != null && !string.IsNullOrEmpty(symbol.TypeName) && symbol.TypeName != "Variant")
                    return true;
            }
        }

        // If we got a concrete type, treat as high confidence
        if (!string.IsNullOrEmpty(inferredType) && inferredType != "Variant" && !inferredType.StartsWith("Unknown"))
            return true;

        return false;
    }

    private static bool IsCompoundAssignment(GDDualOperatorType opType)
    {
        return opType == GDDualOperatorType.AddAndAssign ||
               opType == GDDualOperatorType.SubtractAndAssign ||
               opType == GDDualOperatorType.MultiplyAndAssign ||
               opType == GDDualOperatorType.DivideAndAssign ||
               opType == GDDualOperatorType.ModAndAssign ||
               opType == GDDualOperatorType.BitwiseAndAndAssign ||
               opType == GDDualOperatorType.BitwiseOrAndAssign ||
               opType == GDDualOperatorType.XorAndAssign ||
               opType == GDDualOperatorType.BitShiftLeftAndAssign ||
               opType == GDDualOperatorType.BitShiftRightAndAssign ||
               opType == GDDualOperatorType.PowerAndAssign;
    }
}
