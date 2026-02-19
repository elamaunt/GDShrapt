using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Linq;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Detects redundant type guards and null checks.
/// Reports hints for:
/// - Redundant 'is' checks when type is already known (var x: Array; if x is Array:)
/// - Redundant 'is' checks in nested conditions (if x is Array: if x is Array:)
/// - Redundant has_method() checks when type is known to have the method
/// - Redundant null checks when variable cannot be null
/// - Redundant truthiness checks when variable cannot be falsy
/// </summary>
public class GDRedundantGuardValidator : GDValidationVisitor
{
    private readonly GDSemanticModel _semanticModel;
    private readonly IGDRuntimeProvider? _runtimeProvider;
    private readonly GDDiagnosticSeverity _severity;

    public GDRedundantGuardValidator(
        GDValidationContext context,
        GDSemanticModel semanticModel,
        GDDiagnosticSeverity severity = GDDiagnosticSeverity.Hint)
        : base(context)
    {
        _semanticModel = semanticModel;
        _runtimeProvider = context.RuntimeProvider;
        _severity = severity;
    }

    public void Validate(GDNode? node)
    {
        node?.WalkIn(this);
    }

    public override void Visit(GDIfBranch ifBranch)
    {
        AnalyzeCondition(ifBranch.Condition, ifBranch);
    }

    public override void Visit(GDElifBranch elifBranch)
    {
        AnalyzeCondition(elifBranch.Condition, elifBranch);
    }

    public override void Visit(GDWhileStatement whileStmt)
    {
        AnalyzeCondition(whileStmt.Condition, whileStmt);
    }

    private void AnalyzeCondition(GDExpression? condition, GDNode contextNode)
    {
        if (condition == null)
            return;

        // Handle: x is Type
        if (condition is GDDualOperatorExpression dualOp &&
            dualOp.Operator?.OperatorType == GDDualOperatorType.Is)
        {
            CheckRedundantIsCheck(dualOp, contextNode);
        }

        // Handle: x != null / x == null
        if (condition is GDDualOperatorExpression eqOp)
        {
            var opType = eqOp.Operator?.OperatorType;
            if (opType == GDDualOperatorType.NotEqual || opType == GDDualOperatorType.Equal)
            {
                CheckRedundantNullCheck(eqOp, contextNode);
            }
        }

        // Handle: if x (truthiness)
        if (condition is GDIdentifierExpression identExpr)
        {
            CheckRedundantTruthinessCheck(identExpr, contextNode);
        }

        // Handle: has_method(), has(), has_signal()
        if (condition is GDCallExpression callExpr)
        {
            CheckRedundantHasMethodCheck(callExpr, contextNode);
        }

        // Handle: x and y (recursively check both sides)
        if (condition is GDDualOperatorExpression andOp &&
            andOp.Operator?.OperatorType == GDDualOperatorType.And)
        {
            AnalyzeCondition(andOp.LeftExpression, contextNode);
            AnalyzeCondition(andOp.RightExpression, contextNode);
        }
    }

    /// <summary>
    /// Checks for redundant 'is' type checks.
    /// GD7010: Type already declared (var x: Array; if x is Array:)
    /// GD7011: Type already narrowed in outer scope (if x is Array: if x is Array:)
    /// </summary>
    private void CheckRedundantIsCheck(GDDualOperatorExpression isExpr, GDNode contextNode)
    {
        if (isExpr.LeftExpression is not GDIdentifierExpression identExpr)
            return;

        var varName = identExpr.Identifier?.Sequence;
        if (string.IsNullOrEmpty(varName))
            return;

        var checkedType = GetTypeNameFromExpression(isExpr.RightExpression);
        if (string.IsNullOrEmpty(checkedType))
            return;

        // Get the flow state at current location for declared type check
        var flowVarType = _semanticModel.GetFlowVariableType(varName, contextNode);
        if (flowVarType == null)
            return;

        // Check if declared type matches (GD7010)
        // DeclaredType is set at variable declaration and doesn't change with narrowing
        if (flowVarType.DeclaredType != null &&
            IsTypeMatch(flowVarType.DeclaredType.DisplayName, checkedType))
        {
            ReportDiagnostic(
                GDDiagnosticCode.RedundantTypeGuard,
                $"Redundant type check: '{varName}' is already declared as '{flowVarType.DeclaredType.DisplayName}'",
                isExpr);
            return;
        }

        // For narrowed type check (GD7011), we need to check the parent scope
        // to see if the variable was already narrowed before this if branch
        var parentNode = GetParentScopeNode(contextNode);
        if (parentNode != null)
        {
            // When parent scope is a method/class declaration, use initial flow state to avoid
            // seeing narrowing from this very check in the final state
            GDFlowVariableType? parentFlowVarType;
            if (parentNode is GDMethodDeclaration or GDClassDeclaration)
                parentFlowVarType = _semanticModel.GetInitialFlowVariableType(varName, contextNode);
            else
                parentFlowVarType = _semanticModel.GetFlowVariableType(varName, parentNode);

            if (parentFlowVarType != null &&
                parentFlowVarType.IsNarrowed &&
                parentFlowVarType.NarrowedFromType != null &&
                IsTypeMatch(parentFlowVarType.NarrowedFromType.DisplayName, checkedType))
            {
                ReportDiagnostic(
                    GDDiagnosticCode.RedundantNarrowedTypeGuard,
                    $"Redundant type check: '{varName}' is already narrowed to '{parentFlowVarType.NarrowedFromType.DisplayName}'",
                    isExpr);
            }
        }
    }

    /// <summary>
    /// Checks for redundant null checks.
    /// GD7013: Variable cannot be null (var x: int; if x != null:)
    /// </summary>
    private void CheckRedundantNullCheck(GDDualOperatorExpression eqOp, GDNode contextNode)
    {
        var leftExpr = eqOp.LeftExpression;
        var rightExpr = eqOp.RightExpression;

        string? varName = null;

        // Check if comparing to null
        if (IsNullLiteral(rightExpr) && leftExpr is GDIdentifierExpression leftIdent)
        {
            varName = leftIdent.Identifier?.Sequence;
        }
        else if (IsNullLiteral(leftExpr) && rightExpr is GDIdentifierExpression rightIdent)
        {
            varName = rightIdent.Identifier?.Sequence;
        }

        if (string.IsNullOrEmpty(varName))
            return;

        // Get the flow state at current location for declared type check
        var flowVarType = _semanticModel.GetFlowVariableType(varName, contextNode);
        if (flowVarType == null)
            return;

        // Check if type is never-null (primitives) - uses declared type which doesn't change
        if (IsNeverNullType(flowVarType.DeclaredType?.DisplayName))
        {
            var typeName = flowVarType.DeclaredType?.DisplayName ?? "primitive";
            ReportDiagnostic(
                GDDiagnosticCode.RedundantNullCheck,
                $"Redundant null check: '{varName}' of type '{typeName}' cannot be null",
                eqOp);
            return;
        }

        // For flow-based non-null check, we need to check the parent scope
        // to see if variable was already marked non-null before this if branch
        var parentNode = GetParentScopeNode(contextNode);
        if (parentNode != null)
        {
            // When parent scope is a method/class declaration, use initial flow state
            GDFlowVariableType? parentFlowVarType;
            if (parentNode is GDMethodDeclaration or GDClassDeclaration)
                parentFlowVarType = _semanticModel.GetInitialFlowVariableType(varName, contextNode);
            else
                parentFlowVarType = _semanticModel.GetFlowVariableType(varName, parentNode);

            if (parentFlowVarType?.IsGuaranteedNonNull == true)
            {
                ReportDiagnostic(
                    GDDiagnosticCode.RedundantNullCheck,
                    $"Redundant null check: '{varName}' is already known to be non-null",
                    eqOp);
            }
        }
    }

    /// <summary>
    /// Checks for redundant truthiness checks.
    /// GD7014: Variable cannot be falsy (var node: Node; if node:)
    /// </summary>
    private void CheckRedundantTruthinessCheck(GDIdentifierExpression identExpr, GDNode contextNode)
    {
        var varName = identExpr.Identifier?.Sequence;
        if (string.IsNullOrEmpty(varName))
            return;

        // Get the flow state BEFORE the current if/elif/while applies its narrowing
        var parentNode = GetParentScopeNode(contextNode);

        // When parent scope is a method/class declaration, use initial flow state to avoid
        // circular logic (the final state may include narrowing from this very check)
        GDFlowVariableType? flowVarType;
        if (parentNode is GDMethodDeclaration or GDClassDeclaration)
            flowVarType = _semanticModel.GetInitialFlowVariableType(varName, contextNode);
        else
            flowVarType = _semanticModel.GetFlowVariableType(varName, parentNode ?? contextNode);

        if (flowVarType == null)
            return;

        // Check if type cannot be falsy (non-nullable reference types with no zero value)
        // For GDScript, most reference types can be falsy (null), but if we know it's non-null...
        if (flowVarType.IsGuaranteedNonNull && IsNonZeroType(flowVarType.EffectiveType.DisplayName))
        {
            ReportDiagnostic(
                GDDiagnosticCode.RedundantTruthinessCheck,
                $"Redundant truthiness check: '{varName}' is known to be non-null and cannot be falsy",
                identExpr);
        }
    }

    /// <summary>
    /// Checks for redundant has_method()/has()/has_signal() checks.
    /// GD7012: Type is known to have the method/property/signal
    /// </summary>
    private void CheckRedundantHasMethodCheck(GDCallExpression callExpr, GDNode contextNode)
    {
        if (callExpr.CallerExpression is not GDMemberOperatorExpression memberOp)
            return;

        var methodName = memberOp.Identifier?.Sequence;
        if (string.IsNullOrEmpty(methodName))
            return;

        if (methodName != "has_method" && methodName != "has" && methodName != "has_signal")
            return;

        // Get the variable being checked
        var callerVar = GetRootVariableName(memberOp.CallerExpression);
        if (string.IsNullOrEmpty(callerVar))
            return;

        // Get the checked member name from first string argument
        var args = callExpr.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return;

        var checkedName = GetStringLiteralValue(args[0]);
        if (string.IsNullOrEmpty(checkedName))
            return;

        // Get type info (use parent scope for redundancy check)
        var parentNode = GetParentScopeNode(contextNode);
        var flowVarType = _semanticModel.GetFlowVariableType(callerVar, parentNode ?? contextNode);
        var effectiveSemanticType = flowVarType?.EffectiveType;
        string? effectiveType = effectiveSemanticType?.DisplayName;
        if (string.IsNullOrEmpty(effectiveType))
        {
            var callerTypeInfo = _semanticModel.TypeSystem.GetType(memberOp.CallerExpression);
            effectiveType = callerTypeInfo.IsVariant ? null : callerTypeInfo.DisplayName;
        }

        if (string.IsNullOrEmpty(effectiveType) || effectiveType == "Variant")
            return;

        // Check if type is known to have this member
        bool hasMember = methodName switch
        {
            "has_method" => TypeHasMethod(effectiveType, checkedName),
            "has" => TypeHasProperty(effectiveType, checkedName),
            "has_signal" => TypeHasSignal(effectiveType, checkedName),
            _ => false
        };

        if (hasMember)
        {
            var memberType = methodName switch
            {
                "has_method" => "method",
                "has" => "property",
                "has_signal" => "signal",
                _ => "member"
            };

            ReportDiagnostic(
                GDDiagnosticCode.RedundantHasMethodCheck,
                $"Redundant {methodName}() check: '{effectiveType}' is known to have {memberType} '{checkedName}'",
                callExpr);
            return;
        }

        // Check if already required by duck type constraints
        if (flowVarType?.DuckType != null)
        {
            bool alreadyRequired = methodName switch
            {
                "has_method" => flowVarType.DuckType.RequiredMethods.ContainsKey(checkedName),
                "has" => flowVarType.DuckType.RequiredProperties.ContainsKey(checkedName),
                "has_signal" => flowVarType.DuckType.RequiredSignals.Contains(checkedName),
                _ => false
            };

            if (alreadyRequired)
            {
                ReportDiagnostic(
                    GDDiagnosticCode.RedundantHasMethodCheck,
                    $"Redundant {methodName}() check: already verified in outer scope",
                    callExpr);
            }
        }
    }

    /// <summary>
    /// Gets the enclosing branch node for checking pre-narrowing state.
    /// For nested if statements, we need to find the outer branch that has the narrowing applied.
    /// </summary>
    private static GDNode? GetParentScopeNode(GDNode node)
    {
        if (node == null)
            return null;

        // Walk up past the containing GDIfStatement to find the enclosing branch or scope
        var current = node.Parent as GDNode;

        while (current != null)
        {
            // Skip the immediate containing GDIfStatement (we need to go past it)
            if (current is GDIfStatement)
            {
                current = current.Parent as GDNode;
                continue;
            }

            // Found an enclosing branch (outer if/elif/else/while body)
            if (current is GDIfBranch || current is GDElifBranch || current is GDElseBranch ||
                current is GDWhileStatement || current is GDForStatement)
            {
                return current;
            }

            // Found a method or class - stop here
            if (current is GDMethodDeclaration || current is GDClassDeclaration)
            {
                return current;
            }

            current = current.Parent as GDNode;
        }

        return null;
    }

    private void ReportDiagnostic(GDDiagnosticCode code, string message, GDNode node)
    {
        switch (_severity)
        {
            case GDDiagnosticSeverity.Error:
                ReportError(code, message, node);
                break;
            case GDDiagnosticSeverity.Warning:
                ReportWarning(code, message, node);
                break;
            case GDDiagnosticSeverity.Hint:
                ReportHint(code, message, node);
                break;
        }
    }

    private static bool IsTypeMatch(string? declaredType, string? checkedType)
    {
        if (string.IsNullOrEmpty(declaredType) || string.IsNullOrEmpty(checkedType))
            return false;

        // Exact match
        if (declaredType == checkedType)
            return true;

        // Handle generics: Array[int] matches Array
        if (declaredType.StartsWith(checkedType + "["))
            return true;

        return false;
    }

    private static bool IsNullLiteral(GDExpression? expr)
    {
        if (expr is GDIdentifierExpression identExpr)
        {
            return identExpr.Identifier?.Sequence == "null";
        }
        return false;
    }

    private static bool IsNeverNullType(string? typeName)
    {
        return typeName is "int" or "float" or "bool" or "String" or "Vector2"
            or "Vector3" or "Vector4" or "Color" or "Rect2" or "Rect2i"
            or "Transform2D" or "Transform3D" or "Basis" or "Quaternion"
            or "AABB" or "Plane" or "RID" or "StringName";
    }

    private static bool IsNonZeroType(string? typeName)
    {
        // Types that can have a zero/empty value are not "non-zero types"
        // For reference types that are non-null, they are considered non-zero
        return typeName is not null and not (
            "int" or "float" or "bool" or "String" or "Array" or "Dictionary"
            or "PackedByteArray" or "PackedInt32Array" or "PackedInt64Array"
            or "PackedFloat32Array" or "PackedFloat64Array" or "PackedStringArray"
            or "PackedVector2Array" or "PackedVector3Array" or "PackedColorArray"
        );
    }

    private static string? GetTypeNameFromExpression(GDExpression? expr)
    {
        if (expr == null)
            return null;

        if (expr is GDIdentifierExpression identExpr)
            return identExpr.Identifier?.Sequence;

        if (expr is GDMemberOperatorExpression memberExpr)
        {
            var caller = GetTypeNameFromExpression(memberExpr.CallerExpression);
            var member = memberExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(caller) && !string.IsNullOrEmpty(member))
                return $"{caller}.{member}";
        }

        return expr.ToString();
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

    private static string? GetStringLiteralValue(GDExpression? expr)
    {
        if (expr is GDStringExpression strExpr)
        {
            return strExpr.String?.Sequence;
        }
        return null;
    }

    private bool TypeHasMethod(string typeName, string methodName)
    {
        if (_runtimeProvider == null)
            return false;

        return FindMember(typeName, methodName)?.Kind == GDRuntimeMemberKind.Method;
    }

    private bool TypeHasProperty(string typeName, string propertyName)
    {
        if (_runtimeProvider == null)
            return false;

        var member = FindMember(typeName, propertyName);
        return member?.Kind == GDRuntimeMemberKind.Property;
    }

    private bool TypeHasSignal(string typeName, string signalName)
    {
        if (_runtimeProvider == null)
            return false;

        var member = FindMember(typeName, signalName);
        return member?.Kind == GDRuntimeMemberKind.Signal;
    }

    private GDRuntimeMemberInfo? FindMember(string typeName, string memberName)
    {
        if (_runtimeProvider == null)
            return null;

        // Extract base type for generics
        var baseTypeName = ExtractBaseTypeName(typeName);

        // Check direct member
        var memberInfo = _runtimeProvider.GetMember(baseTypeName, memberName);
        if (memberInfo != null)
            return memberInfo;

        // Check inherited members
        var baseType = _runtimeProvider.GetBaseType(baseTypeName);
        while (!string.IsNullOrEmpty(baseType))
        {
            memberInfo = _runtimeProvider.GetMember(baseType, memberName);
            if (memberInfo != null)
                return memberInfo;

            baseType = _runtimeProvider.GetBaseType(baseType);
        }

        return null;
    }

    private static string ExtractBaseTypeName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return typeName;

        var bracketIndex = typeName.IndexOf('[');
        if (bracketIndex > 0)
            return typeName.Substring(0, bracketIndex);

        return typeName;
    }
}
