using System;
using System.Collections.Generic;
using System.Text;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for generating getters and setters for class variables.
/// Supports both GDScript 4.x property syntax and traditional method syntax.
/// </summary>
public class GDGenerateGetterSetterService : GDRefactoringServiceBase
{
    /// <summary>
    /// Checks if the generate getter/setter refactoring can be executed at the given context.
    /// </summary>
    public bool CanExecute(GDRefactoringContext context)
    {
        if (!IsContextValid(context))
            return false;

        var varDecl = GetVariableDeclaration(context);
        if (varDecl == null)
            return false;

        // Don't offer for constants
        if (varDecl.ConstKeyword != null)
            return false;

        // Don't offer if already has getters/setters
        if (varDecl.FirstAccessorDeclarationNode != null || varDecl.SecondAccessorDeclarationNode != null)
            return false;

        return true;
    }

    /// <summary>
    /// Plans the generate getter/setter refactoring without applying changes.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="options">Generation options</param>
    /// <returns>Plan result with preview information</returns>
    public GDGenerateGetterSetterResult Plan(GDRefactoringContext context, GDGetterSetterOptions options = null)
    {
        if (!CanExecute(context))
            return GDGenerateGetterSetterResult.Failed("Cannot generate getter/setter at this position");

        var varDecl = GetVariableDeclaration(context);
        if (varDecl == null)
            return GDGenerateGetterSetterResult.Failed("No variable declaration found");

        options ??= new GDGetterSetterOptions();

        var varName = varDecl.Identifier?.ToString() ?? "value";
        var varType = varDecl.Type?.ToString();
        var hasInitializer = varDecl.Initializer != null;
        var initializerText = varDecl.Initializer?.ToString();

        // Get original code
        var originalCode = varDecl.ToString();

        // Generate result code
        string resultCode;
        if (options.UsePropertySyntax)
        {
            resultCode = GeneratePropertySyntax(varName, varType, initializerText, options);
        }
        else
        {
            resultCode = GenerateMethodSyntax(varName, varType, options);
        }

        return GDGenerateGetterSetterResult.Planned(
            varName,
            varType,
            originalCode,
            resultCode,
            options);
    }

    /// <summary>
    /// Executes the generate getter/setter refactoring.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="options">Generation options</param>
    /// <returns>Result with text edits to apply</returns>
    public GDRefactoringResult Execute(GDRefactoringContext context, GDGetterSetterOptions options)
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot generate getter/setter at this position");

        var varDecl = GetVariableDeclaration(context);
        if (varDecl == null)
            return GDRefactoringResult.Failed("No variable declaration found");

        options ??= new GDGetterSetterOptions();

        var filePath = context.Script.Reference.FullPath;
        var varName = varDecl.Identifier?.ToString() ?? "value";
        var varType = varDecl.Type?.ToString();
        var initializerText = varDecl.Initializer?.ToString();

        // Generate result code
        string resultCode;
        if (options.UsePropertySyntax)
        {
            resultCode = GeneratePropertySyntax(varName, varType, initializerText, options);
        }
        else
        {
            resultCode = GenerateMethodSyntax(varName, varType, options);
        }

        var edits = new List<GDTextEdit>();

        // Replace the variable declaration with generated code
        var edit = new GDTextEdit(
            filePath,
            varDecl.StartLine,
            varDecl.StartColumn,
            varDecl.ToString(),
            resultCode);
        edits.Add(edit);

        return GDRefactoringResult.Succeeded(edits);
    }

    #region Code Generation

    private string GeneratePropertySyntax(string varName, string varType, string initializer, GDGetterSetterOptions options)
    {
        var sb = new StringBuilder();
        var backingField = options.UseBackingField ? $"_{varName}" : null;

        // Generate backing field if needed
        if (options.UseBackingField)
        {
            sb.Append($"var {backingField}");
            if (!string.IsNullOrEmpty(varType))
                sb.Append($": {varType}");
            if (!string.IsNullOrEmpty(initializer))
                sb.Append($" = {initializer}");
            sb.AppendLine();
            sb.AppendLine();
        }

        // Generate property with get/set
        sb.Append($"var {varName}");
        if (!string.IsNullOrEmpty(varType))
            sb.Append($": {varType}");
        if (!options.UseBackingField && !string.IsNullOrEmpty(initializer))
            sb.Append($" = {initializer}");
        sb.AppendLine(":");

        // Getter
        if (options.GenerateGetter)
        {
            if (options.UseBackingField)
                sb.AppendLine($"\tget: return {backingField}");
            else
                sb.AppendLine($"\tget: return {varName}");
        }

        // Setter
        if (options.GenerateSetter)
        {
            if (options.UseBackingField)
                sb.AppendLine($"\tset(value): {backingField} = value");
            else
                sb.AppendLine($"\tset(value): {varName} = value");
        }

        return sb.ToString().TrimEnd();
    }

    private string GenerateMethodSyntax(string varName, string varType, GDGetterSetterOptions options)
    {
        var sb = new StringBuilder();
        var backingField = $"_{varName}";

        // Generate backing field (always needed for method syntax)
        sb.Append($"var {backingField}");
        if (!string.IsNullOrEmpty(varType))
            sb.Append($": {varType}");
        sb.AppendLine();

        // Getter method
        if (options.GenerateGetter)
        {
            sb.AppendLine();
            sb.Append($"func get_{varName}()");
            if (!string.IsNullOrEmpty(varType))
                sb.Append($" -> {varType}");
            sb.AppendLine(":");
            sb.AppendLine($"\treturn {backingField}");
        }

        // Setter method
        if (options.GenerateSetter)
        {
            sb.AppendLine();
            sb.Append($"func set_{varName}(value");
            if (!string.IsNullOrEmpty(varType))
                sb.Append($": {varType}");
            sb.AppendLine("):");
            sb.AppendLine($"\t{backingField} = value");
        }

        return sb.ToString().TrimEnd();
    }

    #endregion

    #region Helper Methods

    private GDVariableDeclaration GetVariableDeclaration(GDRefactoringContext context)
    {
        // Check if node at cursor is a variable declaration
        if (context.NodeAtCursor is GDVariableDeclaration varDecl)
            return varDecl;

        // Walk up the tree
        var node = context.NodeAtCursor;
        while (node != null)
        {
            if (node is GDVariableDeclaration v)
            {
                // Ensure it's a class-level variable
                if (v.Parent == context.ClassDeclaration ||
                    (v.Parent is GDClassMembersList && v.Parent.Parent == context.ClassDeclaration))
                    return v;
            }
            node = node.Parent as GDNode;
        }

        return null;
    }

    #endregion
}

/// <summary>
/// Options for getter/setter generation.
/// </summary>
public class GDGetterSetterOptions
{
    /// <summary>
    /// Whether to generate a getter.
    /// </summary>
    public bool GenerateGetter { get; set; } = true;

    /// <summary>
    /// Whether to generate a setter.
    /// </summary>
    public bool GenerateSetter { get; set; } = true;

    /// <summary>
    /// Whether to use GDScript 4.x property syntax vs traditional method syntax.
    /// </summary>
    public bool UsePropertySyntax { get; set; } = true;

    /// <summary>
    /// Whether to use a backing field (_var) for storing the value.
    /// </summary>
    public bool UseBackingField { get; set; } = true;
}

/// <summary>
/// Result of generate getter/setter planning operation.
/// </summary>
public class GDGenerateGetterSetterResult : GDRefactoringResult
{
    /// <summary>
    /// The variable name.
    /// </summary>
    public string VariableName { get; }

    /// <summary>
    /// The variable type (if specified).
    /// </summary>
    public string VariableType { get; }

    /// <summary>
    /// The original code that will be replaced.
    /// </summary>
    public string OriginalCode { get; }

    /// <summary>
    /// The generated code.
    /// </summary>
    public string ResultCode { get; }

    /// <summary>
    /// The options used for generation.
    /// </summary>
    public GDGetterSetterOptions Options { get; }

    private GDGenerateGetterSetterResult(
        bool success,
        string errorMessage,
        IReadOnlyList<GDTextEdit> edits,
        string variableName,
        string variableType,
        string originalCode,
        string resultCode,
        GDGetterSetterOptions options)
        : base(success, errorMessage, edits)
    {
        VariableName = variableName;
        VariableType = variableType;
        OriginalCode = originalCode;
        ResultCode = resultCode;
        Options = options;
    }

    /// <summary>
    /// Creates a planned result with preview information.
    /// </summary>
    public static GDGenerateGetterSetterResult Planned(
        string variableName,
        string variableType,
        string originalCode,
        string resultCode,
        GDGetterSetterOptions options)
    {
        return new GDGenerateGetterSetterResult(
            true, null, null,
            variableName, variableType, originalCode, resultCode, options);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public new static GDGenerateGetterSetterResult Failed(string errorMessage)
    {
        return new GDGenerateGetterSetterResult(
            false, errorMessage, null,
            null, null, null, null, null);
    }
}
