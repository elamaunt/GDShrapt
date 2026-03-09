using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Detects assignments that widen a typed variable.
/// GD7019: Assignment widens typed variable (sprite = get_node("X") widens Sprite2D to Node).
/// Enriches diagnostic messages with origin provenance when available.
/// </summary>
public class GDTypeWideningValidator : GDValidationVisitor
{
    private readonly GDSemanticModel _semanticModel;
    private readonly IGDRuntimeProvider? _runtimeProvider;

    public GDTypeWideningValidator(
        GDValidationContext context,
        GDSemanticModel semanticModel)
        : base(context)
    {
        _semanticModel = semanticModel;
        _runtimeProvider = context.RuntimeProvider;
    }

    public void Validate(GDNode? node)
    {
        node?.WalkIn(this);
    }

    public override void Visit(GDDualOperatorExpression dualOp)
    {
        if (dualOp.Operator?.OperatorType != GDDualOperatorType.Assignment)
            return;

        if (_runtimeProvider == null)
            return;

        // Get LHS variable name
        var varName = GetVariableName(dualOp.LeftExpression);
        if (string.IsNullOrEmpty(varName))
            return;

        // Get declared type from flow analysis
        var flowVar = _semanticModel.GetFlowVariableType(varName, dualOp);
        if (flowVar?.DeclaredType == null)
            return;

        var declaredTypeName = flowVar.DeclaredType.DisplayName;

        // Skip Variant — no widening possible
        if (flowVar.DeclaredType.IsVariant)
            return;

        // Get RHS type
        var rhsType = _semanticModel.TypeSystem.GetType(dualOp.RightExpression);
        if (rhsType == null || rhsType.IsVariant)
            return;

        var rhsTypeName = rhsType.DisplayName;

        // Skip if same type
        if (rhsTypeName == declaredTypeName)
            return;

        // Skip numeric conversions (int ↔ float is implicit in GDScript)
        if (flowVar.DeclaredType.IsNumeric && rhsType.IsNumeric)
            return;

        // Check if declared type is assignable from RHS (RHS is wider/parent type)
        // That means assignment widens: declared is T, RHS returns U where T is subtype of U
        if (_runtimeProvider.IsAssignableTo(declaredTypeName, rhsTypeName) &&
            !_runtimeProvider.IsAssignableTo(rhsTypeName, declaredTypeName))
        {
            var message = BuildWideningMessage(declaredTypeName, rhsTypeName, flowVar, rhsType);
            ReportWarning(GDDiagnosticCode.TypeWideningAssignment, message, dualOp);
        }
    }

    private static string BuildWideningMessage(
        string declaredTypeName, string rhsTypeName,
        GDFlowVariableType flowVar, GDSemanticType rhsType)
    {
        // Try to find origin for the RHS type in flow data
        var origins = flowVar.CurrentType.GetOrigins(rhsType);
        if (origins.Count > 0)
        {
            var origin = origins[0];
            var originDesc = origin.Description ?? origin.Kind.ToString();
            var locationSuffix = origin.Location.Line > 0
                ? $" at line {origin.Location.Line + 1}"
                : "";
            return $"Assignment widens '{declaredTypeName}' to '{rhsTypeName}' (from {originDesc}{locationSuffix})";
        }

        return $"Assignment widens '{declaredTypeName}' to '{rhsTypeName}'";
    }

    private static string? GetVariableName(GDExpression? expr)
    {
        if (expr is GDIdentifierExpression identExpr)
            return identExpr.Identifier?.Sequence;

        return null;
    }
}
