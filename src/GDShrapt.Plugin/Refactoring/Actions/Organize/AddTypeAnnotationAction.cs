using GDShrapt.Reader;
using GDShrapt.Semantics;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Adds type annotation to a variable or parameter declaration based on inferred type.
/// Uses GDTypeInferenceHelper for type inference and shows preview before applying.
/// </summary>
internal class AddTypeAnnotationAction : RefactoringActionBase
{
    public override string Id => "add_type_annotation";
    public override string DisplayName => "Add Type Annotation";
    public override RefactoringCategory Category => RefactoringCategory.Organize;
    public override int Priority => 10;

    public override bool IsAvailable(RefactoringContext context)
    {
        if (context?.ContainingClass == null)
            return false;

        // Check if cursor is on a variable declaration without type annotation
        var varDecl = GetVariableDeclaration(context);
        if (varDecl != null && varDecl.Type == null)
            return CanInferType(varDecl, context);

        // Check if on a local variable declaration
        var localVar = GetLocalVariableDeclaration(context);
        if (localVar != null && localVar.Type == null)
            return CanInferType(localVar, context);

        // Check if on a parameter declaration
        var param = GetParameterDeclaration(context);
        if (param != null && param.Type == null)
            return true;

        return false;
    }

    private bool CanInferType(GDVariableDeclaration varDecl, RefactoringContext context)
    {
        if (varDecl.Initializer != null)
            return true;

        var analyzer = context.ScriptMap?.Analyzer;
        if (analyzer != null)
        {
            var typeName = analyzer.GetTypeForNode(varDecl);
            if (!string.IsNullOrEmpty(typeName))
                return true;
        }

        return false;
    }

    private bool CanInferType(GDVariableDeclarationStatement localVar, RefactoringContext context)
    {
        return localVar.Initializer != null;
    }

    protected override string ValidateContext(RefactoringContext context)
    {
        if (context.Editor == null)
            return "No editor available";

        var varDecl = GetVariableDeclaration(context);
        var localVar = GetLocalVariableDeclaration(context);
        var param = GetParameterDeclaration(context);

        if (varDecl == null && localVar == null && param == null)
            return "No declaration found at cursor";

        return null;
    }

    protected override async Task ExecuteInternalAsync(RefactoringContext context)
    {
        // Try class-level variable
        var varDecl = GetVariableDeclaration(context);
        if (varDecl != null)
        {
            await AddTypeWithPreview(varDecl, context);
            return;
        }

        // Try local variable
        var localVar = GetLocalVariableDeclaration(context);
        if (localVar != null)
        {
            await AddTypeWithPreview(localVar, context);
            return;
        }

        // Try parameter
        var param = GetParameterDeclaration(context);
        if (param != null)
        {
            await AddTypeWithPreview(param, context);
            return;
        }

        Logger.Info("AddTypeAnnotationAction: No suitable declaration found");
    }

    private async Task AddTypeWithPreview(GDVariableDeclaration varDecl, RefactoringContext context)
    {
        // Use null analyzer - GDTypeInferenceHelper works with expression-based inference
        var helper = new GDTypeInferenceHelper((GDShrapt.Semantics.GDScriptAnalyzer?)null);
        var inferredType = varDecl.Initializer != null
            ? helper.InferExpressionType(varDecl.Initializer)
            : GDInferredType.Unknown("No initializer");

        if (inferredType.IsUnknown)
        {
            Logger.Info("AddTypeAnnotationAction: Could not infer type");
            return;
        }

        var identifier = varDecl.Identifier;
        if (identifier == null) return;

        var originalCode = varDecl.ToString();
        var typeName = inferredType.TypeName ?? "Variant";
        var resultCode = BuildResultCode(varDecl, typeName);

        await ShowPreviewAndApply(context, identifier, typeName, originalCode, resultCode, inferredType);
    }

    private async Task AddTypeWithPreview(GDVariableDeclarationStatement localVar, RefactoringContext context)
    {
        // Use null analyzer - GDTypeInferenceHelper works with expression-based inference
        var helper = new GDTypeInferenceHelper((GDShrapt.Semantics.GDScriptAnalyzer?)null);
        var inferredType = localVar.Initializer != null
            ? helper.InferExpressionType(localVar.Initializer)
            : GDInferredType.Unknown("No initializer");

        if (inferredType.IsUnknown)
        {
            Logger.Info("AddTypeAnnotationAction: Could not infer type");
            return;
        }

        var identifier = localVar.Identifier;
        if (identifier == null) return;

        var originalCode = localVar.ToString();
        var typeName = inferredType.TypeName ?? "Variant";
        var resultCode = BuildResultCode(localVar, typeName);

        await ShowPreviewAndApply(context, identifier, typeName, originalCode, resultCode, inferredType);
    }

    private async Task AddTypeWithPreview(GDParameterDeclaration param, RefactoringContext context)
    {
        // Use null analyzer - GDTypeInferenceHelper works with expression-based inference
        var helper = new GDTypeInferenceHelper((GDShrapt.Semantics.GDScriptAnalyzer?)null);
        var inferredType = param.DefaultValue != null
            ? helper.InferExpressionType(param.DefaultValue)
            : GDInferredType.FromType("Variant", GDTypeConfidence.Low, "No default value");

        var identifier = param.Identifier;
        if (identifier == null) return;

        var originalCode = param.ToString();
        var typeName = inferredType.TypeName ?? "Variant";
        var resultCode = BuildResultCode(param, typeName);

        await ShowPreviewAndApply(context, identifier, typeName, originalCode, resultCode, inferredType);
    }

    private async Task ShowPreviewAndApply(
        RefactoringContext context,
        GDIdentifier identifier,
        string typeName,
        string originalCode,
        string resultCode,
        GDInferredType inferredType)
    {
        var previewDialog = new RefactoringPreviewDialog();
        context.DialogParent?.AddChild(previewDialog);

        try
        {
            var title = $"Add Type Annotation ({inferredType.Confidence})";

            // In Base Plugin: Apply is disabled (Pro required)
            var canApply = false;
            var proMessage = "GDShrapt Pro required to apply this refactoring";

            var result = await previewDialog.ShowForResult(
                title,
                $"{originalCode}\n\n// Inferred type: {typeName}\n// Confidence: {inferredType.Confidence}\n// Reason: {inferredType.Reason ?? "Type inference"}",
                resultCode,
                canApply,
                "Apply",
                proMessage);

            if (result.ShouldApply && canApply)
            {
                // Apply the annotation
                var editor = context.Editor;
                editor.CursorLine = identifier.EndLine;
                editor.CursorColumn = identifier.EndColumn;
                editor.InsertTextAtCursor($": {typeName}");
                editor.ReloadScriptFromText();

                Logger.Info("AddTypeAnnotationAction: Completed successfully");
            }
        }
        finally
        {
            previewDialog.QueueFree();
        }
    }

    private string BuildResultCode(GDVariableDeclaration varDecl, string typeName)
    {
        var sb = new System.Text.StringBuilder();

        if (varDecl.ConstKeyword != null)
            sb.Append("const ");
        else if (varDecl.VarKeyword != null)
            sb.Append("var ");

        sb.Append(varDecl.Identifier?.Sequence ?? "variable");
        sb.Append($": {typeName}");

        if (varDecl.Initializer != null)
            sb.Append($" = {varDecl.Initializer}");

        return sb.ToString();
    }

    private string BuildResultCode(GDVariableDeclarationStatement localVar, string typeName)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("var ");
        sb.Append(localVar.Identifier?.Sequence ?? "variable");
        sb.Append($": {typeName}");

        if (localVar.Initializer != null)
            sb.Append($" = {localVar.Initializer}");

        return sb.ToString();
    }

    private string BuildResultCode(GDParameterDeclaration param, string typeName)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(param.Identifier?.Sequence ?? "param");
        sb.Append($": {typeName}");

        if (param.DefaultValue != null)
            sb.Append($" = {param.DefaultValue}");

        return sb.ToString();
    }

    private GDVariableDeclaration GetVariableDeclaration(RefactoringContext context)
    {
        if (context.NodeAtCursor is GDVariableDeclaration varDecl)
            return varDecl;
        return context.FindParent<GDVariableDeclaration>();
    }

    private GDVariableDeclarationStatement GetLocalVariableDeclaration(RefactoringContext context)
    {
        if (context.NodeAtCursor is GDVariableDeclarationStatement localVar)
            return localVar;
        return context.FindParent<GDVariableDeclarationStatement>();
    }

    private GDParameterDeclaration GetParameterDeclaration(RefactoringContext context)
    {
        if (context.NodeAtCursor is GDParameterDeclaration param)
            return param;
        return context.FindParent<GDParameterDeclaration>();
    }
}
