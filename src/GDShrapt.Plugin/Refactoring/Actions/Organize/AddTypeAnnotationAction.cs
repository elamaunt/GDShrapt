using GDShrapt.Reader;
using System.Threading.Tasks;

namespace GDShrapt.Plugin.Refactoring.Actions.Organize;

/// <summary>
/// Adds type annotation to a variable or parameter declaration based on inferred type.
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
        {
            // Check if we can infer the type
            return CanInferType(varDecl, context);
        }

        // Check if on a local variable declaration
        var localVar = GetLocalVariableDeclaration(context);
        if (localVar != null && localVar.Type == null)
        {
            return CanInferType(localVar, context);
        }

        // Check if on a parameter declaration
        var param = GetParameterDeclaration(context);
        if (param != null && param.Type == null)
        {
            return true; // Parameters can always have type hints added
        }

        return false;
    }

    private bool CanInferType(GDVariableDeclaration varDecl, RefactoringContext context)
    {
        // Can infer if there's an initializer
        if (varDecl.Initializer != null)
            return true;

        // Check if analyzer has type info
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
        // Can infer if there's an initializer
        if (localVar.Initializer != null)
            return true;

        return false;
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
        var editor = context.Editor;

        // Try class-level variable
        var varDecl = GetVariableDeclaration(context);
        if (varDecl != null)
        {
            await AddTypeToVariableDeclaration(varDecl, context, editor);
            return;
        }

        // Try local variable
        var localVar = GetLocalVariableDeclaration(context);
        if (localVar != null)
        {
            await AddTypeToLocalVariable(localVar, context, editor);
            return;
        }

        // Try parameter
        var param = GetParameterDeclaration(context);
        if (param != null)
        {
            await AddTypeToParameter(param, context, editor);
            return;
        }

        Logger.Info("AddTypeAnnotationAction: No suitable declaration found");
    }

    private async Task AddTypeToVariableDeclaration(GDVariableDeclaration varDecl, RefactoringContext context, IScriptEditor editor)
    {
        var typeName = InferType(varDecl.Initializer, context);
        if (string.IsNullOrEmpty(typeName))
        {
            Logger.Info("AddTypeAnnotationAction: Could not infer type");
            return;
        }

        Logger.Info($"AddTypeAnnotationAction: Inferred type '{typeName}'");

        var identifier = varDecl.Identifier;
        if (identifier == null) return;

        // Insert ": TypeName" after the identifier
        var line = identifier.EndLine;
        var column = identifier.EndColumn;

        editor.CursorLine = line;
        editor.CursorColumn = column;
        editor.InsertTextAtCursor($": {typeName}");

        editor.ReloadScriptFromText();

        Logger.Info("AddTypeAnnotationAction: Completed successfully");
        await Task.CompletedTask;
    }

    private async Task AddTypeToLocalVariable(GDVariableDeclarationStatement localVar, RefactoringContext context, IScriptEditor editor)
    {
        var typeName = InferType(localVar.Initializer, context);
        if (string.IsNullOrEmpty(typeName))
        {
            Logger.Info("AddTypeAnnotationAction: Could not infer type");
            return;
        }

        Logger.Info($"AddTypeAnnotationAction: Inferred type '{typeName}'");

        var identifier = localVar.Identifier;
        if (identifier == null) return;

        // Insert ": TypeName" after the identifier
        var line = identifier.EndLine;
        var column = identifier.EndColumn;

        editor.CursorLine = line;
        editor.CursorColumn = column;
        editor.InsertTextAtCursor($": {typeName}");

        editor.ReloadScriptFromText();

        Logger.Info("AddTypeAnnotationAction: Completed successfully");
        await Task.CompletedTask;
    }

    private async Task AddTypeToParameter(GDParameterDeclaration param, RefactoringContext context, IScriptEditor editor)
    {
        // For parameters without a default value, we can't infer the type
        // Prompt user or use Variant
        var typeName = "Variant";

        if (param.DefaultValue != null)
        {
            typeName = InferType(param.DefaultValue, context) ?? "Variant";
        }

        Logger.Info($"AddTypeAnnotationAction: Using type '{typeName}' for parameter");

        var identifier = param.Identifier;
        if (identifier == null) return;

        // Insert ": TypeName" after the identifier
        var line = identifier.EndLine;
        var column = identifier.EndColumn;

        editor.CursorLine = line;
        editor.CursorColumn = column;
        editor.InsertTextAtCursor($": {typeName}");

        editor.ReloadScriptFromText();

        Logger.Info("AddTypeAnnotationAction: Completed successfully");
        await Task.CompletedTask;
    }

    private string InferType(GDExpression expr, RefactoringContext context)
    {
        if (expr == null)
            return null;

        // Check literals
        if (expr is GDNumberExpression numExpr)
        {
            var numStr = numExpr.ToString();
            if (numStr.Contains("."))
                return "float";
            return "int";
        }

        if (expr is GDStringExpression)
            return "String";

        if (expr is GDBoolExpression)
            return "bool";

        if (expr is GDArrayInitializerExpression)
            return "Array";

        if (expr is GDDictionaryInitializerExpression)
            return "Dictionary";

        if (expr is GDNodePathExpression)
            return "NodePath";

        // Check constructor calls like Vector2(), Color(), etc.
        if (expr is GDCallExpression call)
        {
            var methodName = GetCallMethodName(call);
            if (IsGodotType(methodName))
                return methodName;

            // Check if it's a typed array: Array[int]()
            if (methodName == "Array" || methodName == "Dictionary")
                return methodName;
        }

        // Try to get type from analyzer
        var analyzer = context.ScriptMap?.Analyzer;
        if (analyzer != null)
        {
            var typeName = analyzer.GetTypeForNode(expr);
            if (!string.IsNullOrEmpty(typeName))
                return typeName;
        }

        return null;
    }

    private string GetCallMethodName(GDCallExpression call)
    {
        if (call.CallerExpression is GDIdentifierExpression idExpr)
            return idExpr.Identifier?.Sequence;
        return null;
    }

    private bool IsGodotType(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        // Common Godot types
        var godotTypes = new[]
        {
            "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i",
            "Rect2", "Rect2i", "Transform2D", "Transform3D",
            "Color", "Plane", "Quaternion", "AABB", "Basis",
            "RID", "Callable", "Signal", "StringName", "NodePath",
            "PackedByteArray", "PackedInt32Array", "PackedInt64Array",
            "PackedFloat32Array", "PackedFloat64Array",
            "PackedStringArray", "PackedVector2Array", "PackedVector3Array",
            "PackedColorArray"
        };

        return System.Array.Exists(godotTypes, t => t == name);
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
