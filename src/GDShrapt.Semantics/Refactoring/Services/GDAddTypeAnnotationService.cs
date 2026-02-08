using GDShrapt.Reader;
using System;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for adding type annotations to variable and parameter declarations.
/// </summary>
public class GDAddTypeAnnotationService : GDRefactoringServiceBase
{
    private static readonly string[] GodotTypes = new[]
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

    /// <summary>
    /// Checks if the add type annotation refactoring can be executed at the given context.
    /// </summary>
    public bool CanExecute(GDRefactoringContext context)
    {
        if (!IsContextValid(context))
            return false;

        // Check if on a variable declaration without type annotation
        var varDecl = GetVariableDeclaration(context);
        if (varDecl != null && varDecl.Type == null && CanInferType(varDecl, context))
            return true;

        // Check if on a local variable declaration
        var localVar = GetLocalVariableDeclaration(context);
        if (localVar != null && localVar.Type == null && CanInferType(localVar, context))
            return true;

        // Check if on a parameter declaration
        var param = GetParameterDeclaration(context);
        if (param != null && param.Type == null)
            return true; // Parameters can always have type hints added

        return false;
    }

    /// <summary>
    /// Executes the add type annotation refactoring.
    /// Returns edits to apply or an error result.
    /// </summary>
    public GDRefactoringResult Execute(GDRefactoringContext context)
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot add type annotation at this position");

        // Try class-level variable
        var varDecl = GetVariableDeclaration(context);
        if (varDecl != null && varDecl.Type == null)
        {
            return AddTypeToVariableDeclaration(varDecl, context);
        }

        // Try local variable
        var localVar = GetLocalVariableDeclaration(context);
        if (localVar != null && localVar.Type == null)
        {
            return AddTypeToLocalVariable(localVar, context);
        }

        // Try parameter
        var param = GetParameterDeclaration(context);
        if (param != null && param.Type == null)
        {
            return AddTypeToParameter(param, context);
        }

        return GDRefactoringResult.Failed("No suitable declaration found");
    }

    /// <summary>
    /// Plans the type annotation addition without applying it.
    /// </summary>
    public GDAddTypeAnnotationResult Plan(GDRefactoringContext context)
    {
        if (!CanExecute(context))
            return GDAddTypeAnnotationResult.Failed("Cannot add type annotation at this position");

        var helper = new GDTypeInferenceHelper(context.GetSemanticModel());

        // Try class-level variable
        var varDecl = GetVariableDeclaration(context);
        if (varDecl?.Type == null && varDecl?.Identifier != null)
        {
            var inferredType = helper.InferVariableType(varDecl);
            if (!inferredType.IsUnknown)
            {
                return GDAddTypeAnnotationResult.FromInferredType(
                    varDecl.Identifier.Sequence ?? "variable",
                    inferredType,
                    TypeAnnotationTarget.ClassVariable);
            }
        }

        // Try local variable
        var localVar = GetLocalVariableDeclaration(context);
        if (localVar?.Type == null && localVar?.Identifier != null)
        {
            var inferredType = helper.InferVariableType(localVar);
            if (!inferredType.IsUnknown)
            {
                return GDAddTypeAnnotationResult.FromInferredType(
                    localVar.Identifier.Sequence ?? "variable",
                    inferredType,
                    TypeAnnotationTarget.LocalVariable);
            }
        }

        // Try parameter
        var param = GetParameterDeclaration(context);
        if (param?.Type == null && param?.Identifier != null)
        {
            var inferredType = helper.InferParameterType(param);

            // For parameters without default values, we return Variant with Unknown confidence
            return GDAddTypeAnnotationResult.FromInferredType(
                param.Identifier.Sequence ?? "parameter",
                inferredType.IsUnknown
                    ? GDInferredType.Low("Variant", "Parameter has no type annotation or default value")
                    : inferredType,
                TypeAnnotationTarget.Parameter);
        }

        return GDAddTypeAnnotationResult.Failed("Could not determine type annotation");
    }

    private GDRefactoringResult AddTypeToVariableDeclaration(GDVariableDeclaration varDecl, GDRefactoringContext context)
    {
        var typeName = InferType(varDecl.Initializer, context);
        if (string.IsNullOrEmpty(typeName))
            return GDRefactoringResult.Failed("Could not infer type for variable");

        var identifier = varDecl.Identifier;
        if (identifier == null)
            return GDRefactoringResult.Failed("Variable has no identifier");

        return CreateTypeAnnotationEdit(context, identifier, typeName);
    }

    private GDRefactoringResult AddTypeToLocalVariable(GDVariableDeclarationStatement localVar, GDRefactoringContext context)
    {
        var typeName = InferType(localVar.Initializer, context);
        if (string.IsNullOrEmpty(typeName))
            return GDRefactoringResult.Failed("Could not infer type for variable");

        var identifier = localVar.Identifier;
        if (identifier == null)
            return GDRefactoringResult.Failed("Variable has no identifier");

        return CreateTypeAnnotationEdit(context, identifier, typeName);
    }

    private GDRefactoringResult AddTypeToParameter(GDParameterDeclaration param, GDRefactoringContext context)
    {
        var typeName = param.DefaultValue != null
            ? InferType(param.DefaultValue, context) ?? "Variant"
            : "Variant";

        var identifier = param.Identifier;
        if (identifier == null)
            return GDRefactoringResult.Failed("Parameter has no identifier");

        return CreateTypeAnnotationEdit(context, identifier, typeName);
    }

    private GDRefactoringResult CreateTypeAnnotationEdit(GDRefactoringContext context, GDIdentifier identifier, string typeName)
    {
        var filePath = context.Script.Reference.FullPath;

        // Insert ": TypeName" after the identifier
        var edit = new GDTextEdit(
            filePath,
            identifier.EndLine,
            identifier.EndColumn,
            "",  // No text to replace, this is an insertion
            $": {typeName}");

        return GDRefactoringResult.Succeeded(edit);
    }

    private bool CanInferType(GDVariableDeclaration? varDecl, GDRefactoringContext context)
    {
        if (varDecl == null)
            return false;

        // Can infer if there's an initializer
        if (varDecl.Initializer != null)
            return true;

        // Check if semantic model has type info
        var semanticModel = context.Script?.SemanticModel;
        if (semanticModel != null)
        {
            var typeName = semanticModel.GetTypeForNode(varDecl);
            if (!string.IsNullOrEmpty(typeName))
                return true;
        }

        return false;
    }

    private bool CanInferType(GDVariableDeclarationStatement? localVar, GDRefactoringContext context)
    {
        if (localVar == null)
            return false;

        // Can infer if there's an initializer
        return localVar.Initializer != null;
    }

    /// <summary>
    /// Infers the type of an expression.
    /// </summary>
    public string? InferType(GDExpression? expr, GDRefactoringContext context)
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

        // Try to get type from semantic model
        var semanticModel = context.Script?.SemanticModel;
        if (semanticModel != null)
        {
            var typeName = semanticModel.GetTypeForNode(expr);
            if (!string.IsNullOrEmpty(typeName))
                return typeName;
        }

        return null;
    }

    private static string? GetCallMethodName(GDCallExpression call)
    {
        if (call.CallerExpression is GDIdentifierExpression idExpr)
            return idExpr.Identifier?.Sequence;
        return null;
    }

    private static bool IsGodotType(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        return Array.Exists(GodotTypes, t => t == name);
    }

    private GDVariableDeclaration? GetVariableDeclaration(GDRefactoringContext context)
    {
        if (context.NodeAtCursor is GDVariableDeclaration varDecl)
            return varDecl;
        return context.FindParent<GDVariableDeclaration>();
    }

    private GDVariableDeclarationStatement? GetLocalVariableDeclaration(GDRefactoringContext context)
    {
        if (context.NodeAtCursor is GDVariableDeclarationStatement localVar)
            return localVar;
        return context.FindParent<GDVariableDeclarationStatement>();
    }

    private GDParameterDeclaration? GetParameterDeclaration(GDRefactoringContext context)
    {
        if (context.NodeAtCursor is GDParameterDeclaration param)
            return param;
        return context.FindParent<GDParameterDeclaration>();
    }
}

/// <summary>
/// The target type for type annotation.
/// </summary>
public enum TypeAnnotationTarget
{
    /// <summary>
    /// Class-level variable.
    /// </summary>
    ClassVariable,

    /// <summary>
    /// Local variable inside a method.
    /// </summary>
    LocalVariable,

    /// <summary>
    /// Function parameter.
    /// </summary>
    Parameter
}

/// <summary>
/// Result of type annotation planning.
/// </summary>
public class GDAddTypeAnnotationResult
{
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public string IdentifierName { get; }
    public string TypeName { get; }
    public TypeAnnotationTarget Target { get; }

    /// <summary>
    /// Confidence level of the type inference.
    /// </summary>
    public GDTypeConfidence Confidence { get; }

    /// <summary>
    /// Reason for the confidence level.
    /// </summary>
    public string? ConfidenceReason { get; }

    public GDAddTypeAnnotationResult(
        bool success,
        string? errorMessage,
        string identifierName,
        string typeName,
        TypeAnnotationTarget target,
        GDTypeConfidence confidence = GDTypeConfidence.Unknown,
        string? confidenceReason = null)
    {
        Success = success;
        ErrorMessage = errorMessage;
        IdentifierName = identifierName;
        TypeName = typeName;
        Target = target;
        Confidence = confidence;
        ConfidenceReason = confidenceReason;
    }

    /// <summary>
    /// Creates a result from an inferred type with confidence.
    /// </summary>
    public static GDAddTypeAnnotationResult FromInferredType(
        string identifierName,
        GDInferredType inferredType,
        TypeAnnotationTarget target)
    {
        return new GDAddTypeAnnotationResult(
            true, null,
            identifierName,
            inferredType?.TypeName?.DisplayName ?? "Variant",
            target,
            inferredType?.Confidence ?? GDTypeConfidence.Unknown,
            inferredType?.Reason);
    }

    public static GDAddTypeAnnotationResult Failed(string errorMessage)
    {
        return new GDAddTypeAnnotationResult(
            false, errorMessage, "", "", TypeAnnotationTarget.ClassVariable);
    }
}
