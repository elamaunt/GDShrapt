using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Linq;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Validates container type annotations.
/// GD3025: Bare Array/Dictionary annotation could be specialized (var scores: Array = []).
/// GD7021: For-loop over untyped container with typed element usage.
/// </summary>
public class GDContainerSpecializationValidator : GDValidationVisitor
{
    private readonly GDSemanticModel _semanticModel;
    private readonly GDSemanticValidatorOptions _options;

    public GDContainerSpecializationValidator(
        GDValidationContext context,
        GDSemanticModel semanticModel,
        GDSemanticValidatorOptions options)
        : base(context)
    {
        _semanticModel = semanticModel;
        _options = options;
    }

    public void Validate(GDNode? node)
    {
        node?.WalkIn(this);
    }

    public override void Visit(GDVariableDeclaration varDecl)
    {
        if (_options.CheckContainerSpecialization)
            CheckContainerSpecialization(varDecl.Type, varDecl.Identifier?.Sequence, varDecl);
    }

    public override void Visit(GDVariableDeclarationStatement varStmt)
    {
        if (_options.CheckContainerSpecialization)
            CheckContainerSpecialization(varStmt.Type, varStmt.Identifier?.Sequence, varStmt);
    }

    public override void Visit(GDForStatement forStmt)
    {
        if (_options.CheckUntypedContainerAccess)
            CheckUntypedContainerElement(forStmt);
    }

    /// <summary>
    /// GD3025: Bare Array or Dictionary annotation could be specialized.
    /// Example: var scores: Array = [] — could be Array[int] based on usage.
    /// </summary>
    private void CheckContainerSpecialization(GDTypeNode? typeNode, string? varName, GDNode node)
    {
        if (typeNode == null || string.IsNullOrEmpty(varName))
            return;

        var typeName = typeNode.BuildName();

        // Only fire on bare Array or Dictionary (no brackets)
        if (typeName != "Array" && typeName != "Dictionary")
            return;

        // Get container element type from semantic model
        var containerType = _semanticModel.TypeSystem.GetContainerElementType(varName);
        if (containerType == null)
            return;

        var effectiveType = containerType.EffectiveElementType;
        if (effectiveType == null || effectiveType.IsVariant)
            return;

        var elementTypeName = effectiveType.DisplayName;
        if (elementTypeName == "Variant")
            return;

        // Build suggestion
        var suggestion = typeName == "Array"
            ? $"Array[{elementTypeName}]"
            : $"Dictionary with typed values";

        ReportDiagnostic(
            _options.ContainerSpecializationSeverity,
            GDDiagnosticCode.ContainerMissingSpecialization,
            $"'{typeName}' could be '{suggestion}' based on usage",
            typeNode);
    }

    /// <summary>
    /// GD7021: For-loop iterates untyped Array but uses elements as specific type.
    /// Example: for enemy in enemies: enemy.take_damage() — 'enemies' could be Array[Sprite2D].
    /// </summary>
    private void CheckUntypedContainerElement(GDForStatement forStmt)
    {
        // Get collection variable name
        var collectionVarName = GetVariableName(forStmt.Collection);
        if (string.IsNullOrEmpty(collectionVarName))
            return;

        // Check if collection has an explicit typed annotation
        var flowVar = _semanticModel.GetFlowVariableType(collectionVarName, forStmt);
        if (flowVar?.DeclaredType == null)
            return;

        var declaredTypeName = flowVar.DeclaredType.DisplayName;

        // Only fire on bare "Array" (no specialization)
        if (declaredTypeName != "Array")
            return;

        // Get element type from usage
        var containerType = _semanticModel.TypeSystem.GetContainerElementType(collectionVarName);
        if (containerType == null)
            return;

        var effectiveType = containerType.EffectiveElementType;
        if (effectiveType == null || effectiveType.IsVariant)
            return;

        var elementTypeName = effectiveType.DisplayName;
        if (elementTypeName == "Variant")
            return;

        ReportWarning(
            GDDiagnosticCode.UntypedContainerElementAccess,
            $"Elements of '{collectionVarName}' are used as '{elementTypeName}', consider Array[{elementTypeName}]",
            forStmt);
    }

    private static string? GetVariableName(GDExpression? expr)
    {
        if (expr is GDIdentifierExpression identExpr)
            return identExpr.Identifier?.Sequence;

        return null;
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
