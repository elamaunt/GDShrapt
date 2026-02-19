using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Detects assignments that widen a typed variable.
/// GD7019: Assignment widens typed variable (sprite = get_node("X") widens Sprite2D to Node).
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
        if (declaredTypeName == "Variant" || flowVar.DeclaredType.IsVariant)
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
        if (IsNumericType(declaredTypeName) && IsNumericType(rhsTypeName))
            return;

        // Check if declared type is assignable from RHS (RHS is wider/parent type)
        // That means assignment widens: declared is T, RHS returns U where T is subtype of U
        if (_runtimeProvider.IsAssignableTo(declaredTypeName, rhsTypeName) &&
            !_runtimeProvider.IsAssignableTo(rhsTypeName, declaredTypeName))
        {
            ReportWarning(
                GDDiagnosticCode.TypeWideningAssignment,
                $"Assignment widens '{declaredTypeName}' to '{rhsTypeName}'",
                dualOp);
        }
    }

    private static bool IsNumericType(string typeName)
    {
        return typeName is "int" or "float";
    }

    private static string? GetVariableName(GDExpression? expr)
    {
        if (expr is GDIdentifierExpression identExpr)
            return identExpr.Identifier?.Sequence;

        return null;
    }
}
