using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Collects Union types for class-level Variant variables.
/// Tracks initializers and assignments from all methods.
/// </summary>
internal class GDClassVariableCollector : GDVisitor
{
    private readonly GDTypeInferenceEngine? _typeEngine;
    private readonly Dictionary<string, GDVariableUsageProfile> _profiles = new();

    /// <summary>
    /// Collected class-level variable usage profiles.
    /// </summary>
    public IReadOnlyDictionary<string, GDVariableUsageProfile> Profiles => _profiles;

    public GDClassVariableCollector(GDTypeInferenceEngine? typeEngine)
    {
        _typeEngine = typeEngine;
    }

    /// <summary>
    /// Collects profiles for all class-level Variant variables.
    /// </summary>
    public void Collect(GDClassDeclaration? classDecl)
    {
        if (classDecl?.Members == null)
            return;

        // Pass 1: Identify Variant class variables and collect initializers
        foreach (var member in classDecl.Members)
        {
            if (member is GDVariableDeclaration varDecl)
            {
                var name = varDecl.Identifier?.Sequence;
                if (string.IsNullOrEmpty(name))
                    continue;

                // Skip typed variables
                if (varDecl.Type != null)
                    continue;

                // Skip constants (they have explicit type from value)
                if (varDecl.ConstKeyword != null)
                    continue;

                var token = varDecl.AllTokens.FirstOrDefault();
                var profile = new GDVariableUsageProfile(name)
                {
                    IsClassLevel = true,
                    DeclarationLine = token?.StartLine ?? 0,
                    DeclarationColumn = token?.StartColumn ?? 0
                };
                _profiles[name] = profile;

                // Record initializer if present
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
                        Line = initToken?.StartLine ?? profile.DeclarationLine,
                        Column = initToken?.StartColumn ?? profile.DeclarationColumn,
                        Kind = GDAssignmentKind.Initialization
                    });
                }
            }
        }

        // Pass 2: Walk all methods to find assignments to class variables
        foreach (var member in classDecl.Members)
        {
            if (member is GDMethodDeclaration method)
            {
                method.WalkIn(this);
            }
        }
    }

    public override void Visit(GDDualOperatorExpression dualOp)
    {
        var opType = dualOp.Operator?.OperatorType;
        if (opType == null)
            return;

        var isAssignment = opType == GDDualOperatorType.Assignment;
        var isCompoundAssignment = IsCompoundAssignment(opType.Value);

        if (!isAssignment && !isCompoundAssignment)
            return;

        string? varName = null;

        // Check for: x = value (direct reference to class variable)
        if (dualOp.LeftExpression is GDIdentifierExpression identExpr)
        {
            varName = identExpr.Identifier?.Sequence;
        }
        // Check for: self.x = value
        else if (dualOp.LeftExpression is GDMemberOperatorExpression memberExpr)
        {
            if (memberExpr.CallerExpression is GDIdentifierExpression selfIdent &&
                selfIdent.Identifier?.Sequence == "self")
            {
                varName = memberExpr.Identifier?.Sequence;
            }
        }

        if (string.IsNullOrEmpty(varName) || !_profiles.TryGetValue(varName, out var profile))
            return;

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
