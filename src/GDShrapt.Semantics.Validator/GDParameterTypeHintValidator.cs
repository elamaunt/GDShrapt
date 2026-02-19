using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Linq;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Suggests type annotations for parameters when all call sites agree.
/// GD7020: Untyped parameter, but all callers pass the same type.
/// </summary>
public class GDParameterTypeHintValidator : GDValidationVisitor
{
    private readonly GDSemanticModel _semanticModel;
    private readonly GDDiagnosticSeverity _severity;

    public GDParameterTypeHintValidator(
        GDValidationContext context,
        GDSemanticModel semanticModel,
        GDDiagnosticSeverity severity = GDDiagnosticSeverity.Hint)
        : base(context)
    {
        _semanticModel = semanticModel;
        _severity = severity;
    }

    public void Validate(GDNode? node)
    {
        node?.WalkIn(this);
    }

    public override void Visit(GDMethodDeclaration method)
    {
        if (method.Parameters == null)
            return;

        foreach (var param in method.Parameters.OfType<GDParameterDeclaration>())
        {
            // Skip parameters that already have type annotations
            if (param.Type != null)
                continue;

            var paramName = param.Identifier?.Sequence;
            if (string.IsNullOrEmpty(paramName))
                continue;

            // Skip _ prefixed parameters (intentionally untyped)
            if (paramName.StartsWith("_"))
                continue;

            // Use semantic model to infer parameter type from call sites
            var inference = _semanticModel.TypeSystem.InferParameterType(param);
            if (inference == null)
                continue;

            // Only fire when confidence is high enough and not a union
            if (inference.IsUnknown || inference.IsUnion)
                continue;

            var inferredType = inference.TypeName;
            if (inferredType == null || inferredType.IsVariant)
                continue;

            var inferredTypeName = inferredType.DisplayName;
            if (inferredTypeName == "Variant")
                continue;

            ReportDiagnostic(
                GDDiagnosticCode.CallSiteParameterTypeConsensus,
                $"All callers pass '{inferredTypeName}' for parameter '{paramName}'",
                param);
        }
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
}
