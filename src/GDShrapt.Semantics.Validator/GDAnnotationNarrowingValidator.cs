using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Validates type annotation quality.
/// GD3022: Annotation wider than inferred type (var enemy: Node = Sprite2D.new()).
/// GD7022: Redundant annotation on literal (var x: int = 5).
/// </summary>
public class GDAnnotationNarrowingValidator : GDValidationVisitor
{
    private readonly GDSemanticModel _semanticModel;
    private readonly IGDRuntimeProvider? _runtimeProvider;
    private readonly GDSemanticValidatorOptions _options;

    public GDAnnotationNarrowingValidator(
        GDValidationContext context,
        GDSemanticModel semanticModel,
        GDSemanticValidatorOptions options)
        : base(context)
    {
        _semanticModel = semanticModel;
        _runtimeProvider = context.RuntimeProvider;
        _options = options;
    }

    public void Validate(GDNode? node)
    {
        node?.WalkIn(this);
    }

    public override void Visit(GDVariableDeclaration varDecl)
    {
        AnalyzeVariable(varDecl.Type, varDecl.Initializer, varDecl.Identifier?.Sequence, varDecl);
    }

    public override void Visit(GDVariableDeclarationStatement varStmt)
    {
        AnalyzeVariable(varStmt.Type, varStmt.Initializer, varStmt.Identifier?.Sequence, varStmt);
    }

    private void AnalyzeVariable(GDTypeNode? typeNode, GDExpression? initializer, string? varName, GDNode node)
    {
        if (typeNode == null || initializer == null)
            return;

        var declaredTypeName = typeNode.BuildName();
        var declaredSemType = GDSemanticType.FromRuntimeTypeName(declaredTypeName);
        if (string.IsNullOrEmpty(declaredTypeName) || declaredSemType.IsVariant)
            return;

        // GD7022: Redundant annotation on literal
        if (_options.CheckRedundantAnnotations)
        {
            CheckRedundantAnnotation(typeNode, initializer, declaredTypeName);
        }

        // GD3022: Annotation wider than inferred type
        if (_options.CheckAnnotationNarrowing)
        {
            CheckAnnotationWider(typeNode, initializer, declaredTypeName, varName, node);
        }
    }

    /// <summary>
    /// GD7022: Type annotation is redundant when initializer is a literal with the same type.
    /// Example: var x: int = 5 — annotation 'int' is obvious from the literal.
    /// </summary>
    private void CheckRedundantAnnotation(GDTypeNode typeNode, GDExpression initializer, string declaredTypeName)
    {
        var literalType = GetLiteralType(initializer);
        if (literalType == null)
            return;

        // Exact match only (not "var x: float = 5" — that's a conversion)
        if (literalType == declaredTypeName)
        {
            ReportHint(
                GDDiagnosticCode.RedundantAnnotation,
                $"Type annotation '{declaredTypeName}' is redundant (obvious from literal)",
                typeNode);
        }
    }

    /// <summary>
    /// GD3022: Annotation is wider than the inferred type.
    /// Example: var enemy: Node = Sprite2D.new() — annotation 'Node' is wider than 'Sprite2D'.
    /// Enriches message with origin provenance when available.
    /// </summary>
    private void CheckAnnotationWider(GDTypeNode typeNode, GDExpression initializer, string declaredTypeName, string? varName, GDNode node)
    {
        if (_runtimeProvider == null)
            return;

        // Skip null initializer — "var x: Node = null" is a standard pattern
        if (initializer is GDIdentifierExpression nullIdent &&
            nullIdent.Identifier?.Sequence == "null")
            return;

        // Get inferred type from the semantic model
        var inferredType = _semanticModel.TypeSystem.GetType(initializer);
        if (inferredType == null || inferredType.IsVariant)
            return;

        var inferredTypeName = inferredType.DisplayName;
        if (inferredType.IsNull)
            return;

        // Skip if same type
        if (inferredTypeName == declaredTypeName)
            return;

        var declaredSemType = GDSemanticType.FromRuntimeTypeName(declaredTypeName);

        // Skip container types with specialization (handled by GD3025)
        if (declaredSemType.IsArray || declaredSemType.IsDictionary)
            return;

        // Skip numeric conversions (int ↔ float is not widening)
        if (declaredSemType.IsNumeric && inferredType.IsNumeric)
            return;

        // Check if inferred is assignable to declared (narrower), but not the reverse
        if (_runtimeProvider.IsAssignableTo(inferredTypeName, declaredTypeName) &&
            !_runtimeProvider.IsAssignableTo(declaredTypeName, inferredTypeName))
        {
            var message = BuildAnnotationWiderMessage(declaredTypeName, inferredTypeName, inferredType, varName, node);
            ReportDiagnostic(
                _options.AnnotationNarrowingSeverity,
                GDDiagnosticCode.AnnotationWiderThanInferred,
                message,
                typeNode);
        }
    }

    private string BuildAnnotationWiderMessage(string declaredTypeName, string inferredTypeName,
        GDSemanticType inferredType, string? varName, GDNode node)
    {
        if (varName != null)
        {
            var flowVar = _semanticModel.GetFlowVariableType(varName, node);
            if (flowVar != null)
            {
                var origins = flowVar.CurrentType.GetOrigins(inferredType);
                if (origins.Count > 0)
                {
                    var origin = origins[0];
                    var originDesc = origin.Description ?? origin.Kind.ToString();
                    return $"Annotation '{declaredTypeName}' is wider than inferred type '{inferredTypeName}' (from {originDesc})";
                }
            }
        }

        return $"Annotation '{declaredTypeName}' is wider than inferred type '{inferredTypeName}'";
    }

    private static string? GetLiteralType(GDExpression? expr)
    {
        return expr switch
        {
            GDNumberExpression num => num.Number?.ResolveNumberType() switch
            {
                GDNumberType.LongDecimal or GDNumberType.LongBinary or GDNumberType.LongHexadecimal => "int",
                GDNumberType.Double => "float",
                _ => null
            },
            GDStringExpression => "String",
            GDBoolExpression => "bool",
            _ => null
        };
    }

    private void ReportDiagnostic(GDDiagnosticSeverity severity, GDDiagnosticCode code, string message, GDNode node)
    {
        switch (severity)
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
