using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for generating @onready variables from get_node() calls or $NodePath expressions.
/// </summary>
public class GDGenerateOnreadyService
{
    /// <summary>
    /// Checks if the generate @onready refactoring can be executed at the given context.
    /// </summary>
    public bool CanExecute(GDRefactoringContext context)
    {
        if (context?.ClassDeclaration == null)
            return false;

        // Must be on a get_node() call or $NodePath expression
        return context.IsOnGetNodeCall || context.IsOnNodePath;
    }

    /// <summary>
    /// Plans the generate @onready refactoring without applying changes.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="variableName">Name for the new variable (optional)</param>
    /// <returns>Plan result with preview information</returns>
    public GDGenerateOnreadyResult Plan(GDRefactoringContext context, string variableName = null)
    {
        if (!CanExecute(context))
            return GDGenerateOnreadyResult.Failed("Cannot generate @onready at this position");

        var nodePath = ExtractNodePath(context);
        if (string.IsNullOrEmpty(nodePath))
            return GDGenerateOnreadyResult.Failed("Could not determine node path");

        var normalizedName = NormalizeVariableName(variableName, nodePath);
        var inferredType = InferNodeType(context, nodePath);

        // Use the overload that accepts GDInferredType with confidence
        return GDGenerateOnreadyResult.Planned(
            normalizedName,
            nodePath,
            inferredType);  // Now returns GDInferredType with confidence
    }

    /// <summary>
    /// Executes the generate @onready refactoring.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="variableName">Name for the new variable</param>
    /// <returns>Result with text edits to apply</returns>
    public GDRefactoringResult Execute(GDRefactoringContext context, string variableName)
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot generate @onready at this position");

        var nodePath = ExtractNodePath(context);
        if (string.IsNullOrEmpty(nodePath))
            return GDRefactoringResult.Failed("Could not determine node path");

        var normalizedName = NormalizeVariableName(variableName, nodePath);
        var filePath = context.Script.Reference.FullPath;
        var inferredType = InferNodeType(context, nodePath);

        // Find the expression to replace
        var nodeExpression = FindNodeExpression(context);
        if (nodeExpression == null)
            return GDRefactoringResult.Failed("Could not find node expression");

        var edits = new List<GDTextEdit>();

        // Build the @onready declaration using the type name from GDInferredType
        var onreadyDecl = BuildOnreadyDeclaration(normalizedName, inferredType.TypeName, nodePath);

        // Find insertion point for @onready (after class declarations, before methods)
        var insertionLine = FindOnreadyInsertionLine(context.ClassDeclaration);

        // Edit 1: Insert @onready declaration at class level
        var insertEdit = new GDTextEdit(
            filePath,
            insertionLine,
            0,
            "",
            onreadyDecl + "\n");
        edits.Add(insertEdit);

        // Edit 2: Replace the get_node/$NodePath expression with variable reference
        var replaceEdit = new GDTextEdit(
            filePath,
            nodeExpression.StartLine,
            nodeExpression.StartColumn,
            nodeExpression.ToString(),
            normalizedName);
        edits.Add(replaceEdit);

        return GDRefactoringResult.Succeeded(edits);
    }

    /// <summary>
    /// Converts an existing variable assignment to @onready.
    /// </summary>
    public GDRefactoringResult ConvertToOnready(GDRefactoringContext context)
    {
        if (context?.ClassDeclaration == null)
            return GDRefactoringResult.Failed("Invalid context");

        // Find variable declaration at cursor
        var varDecl = context.GetVariableDeclaration();
        if (varDecl == null)
            return GDRefactoringResult.Failed("No variable declaration at cursor");

        // Check if it already has @onready
        var hasOnready = varDecl.AttributesDeclaredBefore
            .Any(a => a.Attribute?.Name?.Sequence == "onready");

        if (hasOnready)
            return GDRefactoringResult.Failed("Variable already has @onready");

        // Check if the initializer is a get_node() or $NodePath
        var initializer = varDecl.Initializer;
        if (initializer == null)
            return GDRefactoringResult.Failed("Variable has no initializer");

        if (!IsGetNodeExpression(initializer) && !IsNodePathExpression(initializer))
            return GDRefactoringResult.Failed("Initializer is not a get_node() or $NodePath expression");

        var filePath = context.Script.Reference.FullPath;

        // Insert @onready before var
        var varKeyword = varDecl.VarKeyword;
        if (varKeyword == null)
            return GDRefactoringResult.Failed("Could not find var keyword");

        var edit = new GDTextEdit(
            filePath,
            varKeyword.StartLine,
            varKeyword.StartColumn,
            "var",
            "@onready var");

        return GDRefactoringResult.Succeeded(edit);
    }

    #region Helper Methods

    private string ExtractNodePath(GDRefactoringContext context)
    {
        // First try $NodePath expression
        if (context.IsOnNodePath)
        {
            var nodePathExpr = context.FindParent<GDNodePathExpression>();
            if (nodePathExpr != null)
            {
                return nodePathExpr.Path?.ToString()?.Trim('"') ?? "";
            }

            var getNodeExpr = context.FindParent<GDGetNodeExpression>();
            if (getNodeExpr != null)
            {
                return getNodeExpr.Path?.ToString()?.Trim('"') ?? "";
            }
        }

        // Try get_node() call
        if (context.IsOnGetNodeCall)
        {
            var callExpr = context.FindParent<GDCallExpression>();
            if (callExpr != null && IsGetNodeCall(callExpr))
            {
                var firstParam = callExpr.Parameters.FirstOrDefault();
                if (firstParam is GDStringExpression strExpr)
                {
                    return strExpr.String?.Sequence?.Trim('"') ?? "";
                }
            }
        }

        return null;
    }

    private GDNode FindNodeExpression(GDRefactoringContext context)
    {
        if (context.IsOnNodePath)
        {
            var nodePathExpr = context.FindParent<GDNodePathExpression>();
            if (nodePathExpr != null)
                return nodePathExpr;

            var getNodeExpr = context.FindParent<GDGetNodeExpression>();
            if (getNodeExpr != null)
                return getNodeExpr;
        }

        if (context.IsOnGetNodeCall)
        {
            return context.FindParent<GDCallExpression>();
        }

        return null;
    }

    private bool IsGetNodeCall(GDCallExpression call)
    {
        var callerStr = call.CallerExpression?.ToString();
        return callerStr == "get_node" || callerStr == "get_node_or_null";
    }

    private bool IsGetNodeExpression(GDExpression expr)
    {
        if (expr is GDCallExpression call)
        {
            return IsGetNodeCall(call);
        }
        return false;
    }

    private bool IsNodePathExpression(GDExpression expr)
    {
        return expr is GDNodePathExpression || expr is GDGetNodeExpression;
    }

    private GDInferredType InferNodeType(GDRefactoringContext context, string nodePath)
    {
        // Try to infer type from analyzer (would have scene information)
        if (context.Script?.Analyzer != null)
        {
            // The analyzer might have scene information - high confidence
            // Check if there's scene type resolution available
            var nodeExpr = FindNodeExpression(context);
            if (nodeExpr != null)
            {
                var analyzerType = context.Script.Analyzer.GetTypeForNode(nodeExpr);
                if (!string.IsNullOrEmpty(analyzerType) && analyzerType != "Variant" && analyzerType != "Node")
                {
                    return GDInferredType.High(analyzerType, "From scene type resolution");
                }
            }
        }

        // Derive from node name if possible - low confidence (heuristic)
        var nodeName = nodePath?.Split('/')?.LastOrDefault();
        if (!string.IsNullOrEmpty(nodeName))
        {
            // Common patterns - Low confidence since it's just a naming heuristic
            if (nodeName.EndsWith("Button"))
                return GDInferredType.Low("Button", "Inferred from node name suffix 'Button'");
            if (nodeName.EndsWith("Label"))
                return GDInferredType.Low("Label", "Inferred from node name suffix 'Label'");
            if (nodeName.EndsWith("Sprite") || nodeName.EndsWith("Sprite2D"))
                return GDInferredType.Low("Sprite2D", "Inferred from node name suffix 'Sprite'");
            if (nodeName.EndsWith("Body2D"))
                return GDInferredType.Low("CharacterBody2D", "Inferred from node name suffix 'Body2D'");
            if (nodeName.EndsWith("Area2D"))
                return GDInferredType.Low("Area2D", "Inferred from node name suffix 'Area2D'");
            if (nodeName.EndsWith("Timer"))
                return GDInferredType.Low("Timer", "Inferred from node name suffix 'Timer'");
            if (nodeName.EndsWith("AnimationPlayer"))
                return GDInferredType.Low("AnimationPlayer", "Inferred from node name suffix 'AnimationPlayer'");
        }

        return GDInferredType.Low("Node", "Default node type - actual type depends on scene");
    }

    private static string NormalizeVariableName(string? name, string? nodePath)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return GDNamingUtilities.ToSnakeCase(name);
        }

        // Derive from node path using utility
        return GDNamingUtilities.SuggestVariableFromNodePath(nodePath ?? "");
    }

    private string BuildOnreadyDeclaration(string name, string type, string nodePath)
    {
        if (!string.IsNullOrEmpty(type) && type != "Node")
        {
            return $"@onready var {name}: {type} = ${nodePath}";
        }
        return $"@onready var {name} = ${nodePath}";
    }

    private static int FindOnreadyInsertionLine(GDClassDeclaration classDecl)
    {
        return GDIndentationUtilities.FindOnreadyInsertionLine(classDecl);
    }

    #endregion
}

/// <summary>
/// Result of generate @onready planning operation.
/// </summary>
public class GDGenerateOnreadyResult : GDRefactoringResult
{
    /// <summary>
    /// The variable name that will be used.
    /// </summary>
    public string VariableName { get; }

    /// <summary>
    /// The node path extracted from the expression.
    /// </summary>
    public string NodePath { get; }

    /// <summary>
    /// The inferred type of the node (e.g., "Sprite2D", "Button").
    /// </summary>
    public string InferredType { get; }

    /// <summary>
    /// Confidence level of the type inference.
    /// </summary>
    public GDTypeConfidence TypeConfidence { get; }

    /// <summary>
    /// Reason for the confidence level.
    /// </summary>
    public string? TypeConfidenceReason { get; }

    private GDGenerateOnreadyResult(
        bool success,
        string errorMessage,
        IReadOnlyList<GDTextEdit> edits,
        string variableName,
        string nodePath,
        string inferredType,
        GDTypeConfidence typeConfidence,
        string? typeConfidenceReason)
        : base(success, errorMessage, edits)
    {
        VariableName = variableName;
        NodePath = nodePath;
        InferredType = inferredType;
        TypeConfidence = typeConfidence;
        TypeConfidenceReason = typeConfidenceReason;
    }

    /// <summary>
    /// Creates a planned result with preview information.
    /// </summary>
    public static GDGenerateOnreadyResult Planned(
        string variableName,
        string nodePath,
        string inferredType)
    {
        return new GDGenerateOnreadyResult(
            true, null, null,
            variableName, nodePath, inferredType,
            GDTypeConfidence.Unknown, null);
    }

    /// <summary>
    /// Creates a planned result with preview information and confidence level.
    /// </summary>
    public static GDGenerateOnreadyResult Planned(
        string variableName,
        string nodePath,
        GDInferredType inferredType)
    {
        return new GDGenerateOnreadyResult(
            true, null, null,
            variableName, nodePath,
            inferredType?.TypeName ?? "Node",
            inferredType?.Confidence ?? GDTypeConfidence.Unknown,
            inferredType?.Reason);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public new static GDGenerateOnreadyResult Failed(string errorMessage)
    {
        return new GDGenerateOnreadyResult(
            false, errorMessage, null,
            null, null, null,
            GDTypeConfidence.Unknown, null);
    }
}
