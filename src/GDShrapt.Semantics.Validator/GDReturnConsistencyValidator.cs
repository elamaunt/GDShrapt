using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Linq;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Validates return type consistency in methods.
/// GD3023: Inconsistent return types across branches.
/// GD3024: Missing return in branch (non-void function has implicit return path).
/// </summary>
public class GDReturnConsistencyValidator : GDValidationVisitor
{
    private readonly GDSemanticModel _semanticModel;

    public GDReturnConsistencyValidator(
        GDValidationContext context,
        GDSemanticModel semanticModel)
        : base(context)
    {
        _semanticModel = semanticModel;
    }

    public void Validate(GDNode? node)
    {
        node?.WalkIn(this);
    }

    public override void Visit(GDMethodDeclaration method)
    {
        var analysis = _semanticModel.AnalyzeMethodReturns(method);
        if (analysis == null)
            return;

        CheckMissingReturn(method, analysis);
        CheckInconsistentTypes(method, analysis);
    }

    /// <summary>
    /// GD3024: Non-void function with declared return type has a code path without return.
    /// </summary>
    private void CheckMissingReturn(GDMethodDeclaration method, GDMethodReturnAnalysis analysis)
    {
        if (analysis.DeclaredReturnType == null)
            return;

        // Skip void and Variant return types
        if (analysis.DeclaredReturnType.IsVariant)
            return;

        var returnTypeName = analysis.DeclaredReturnType.DisplayName;
        if (returnTypeName == "void")
            return;

        // Skip methods with empty body (abstract or stub)
        if (method.Statements == null || !method.Statements.Any())
        {
            // Also skip if it's a single-expression function
            if (method.Expression == null)
                return;
        }

        if (analysis.HasImplicitReturn)
        {
            var methodName = method.Identifier?.Sequence ?? "<anonymous>";
            ReportWarning(
                GDDiagnosticCode.MissingReturnInBranch,
                $"Not all code paths return a value in '{methodName}' (declared -> {returnTypeName})",
                method);
        }
    }

    /// <summary>
    /// GD3023: Function has return statements with incompatible types.
    /// </summary>
    private void CheckInconsistentTypes(GDMethodDeclaration method, GDMethodReturnAnalysis analysis)
    {
        if (!analysis.HasInconsistentTypes)
            return;

        // Get the conflicting types for the message
        var conflictingTypes = analysis.ReturnPaths
            .Where(r => !r.IsImplicit && r.InferredType != null && r.IsHighConfidence && !r.InferredType.IsVariant)
            .Select(r => r.InferredType!.DisplayName)
            .Distinct()
            .ToList();

        if (conflictingTypes.Count < 2)
            return;

        var typeList = string.Join(", ", conflictingTypes);
        var methodName = method.Identifier?.Sequence ?? "<anonymous>";
        ReportWarning(
            GDDiagnosticCode.InconsistentReturnTypes,
            $"Function '{methodName}' returns inconsistent types: {typeList}",
            method);
    }
}
