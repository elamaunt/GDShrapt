using System.Collections.Generic;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for inlay hint operations.
/// Provides type hints for variables without explicit type annotations.
/// </summary>
public class GDInlayHintHandler : IGDInlayHintHandler
{
    protected readonly GDScriptProject _project;

    /// <summary>
    /// Maximum number of hints to return per request (for performance).
    /// </summary>
    protected const int MaxHintsPerRequest = 500;

    public GDInlayHintHandler(GDScriptProject project)
    {
        _project = project;
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDInlayHint> GetInlayHints(string filePath, int startLine, int endLine)
    {
        var script = _project.GetScript(filePath);
        if (script?.Class == null || script.Analyzer == null)
            return [];

        var hints = new List<GDInlayHint>();

        // Collect hints for class-level variables
        CollectVariableHints(script, startLine, endLine, hints);

        // Collect hints for local variables in methods
        CollectLocalVariableHints(script, startLine, endLine, hints);

        // Limit hints count
        if (hints.Count > MaxHintsPerRequest)
        {
            hints.RemoveRange(MaxHintsPerRequest, hints.Count - MaxHintsPerRequest);
        }

        return hints;
    }

    /// <summary>
    /// Collects inlay hints for class-level variables.
    /// </summary>
    protected virtual void CollectVariableHints(
        GDScriptFile script,
        int startLine,
        int endLine,
        List<GDInlayHint> hints)
    {
        // Get all class-level variables
        foreach (var variable in script.Analyzer!.GetVariables())
        {
            if (hints.Count >= MaxHintsPerRequest)
                break;

            // Check if in range
            if (variable.DeclarationNode == null)
                continue;

            var line = variable.DeclarationNode.StartLine;
            if (line < startLine || line > endLine)
                continue;

            // Skip if already has explicit type
            if (variable.TypeNode != null)
                continue;

            // Skip if no inferred type or type is Variant
            var typeName = variable.TypeName;
            if (string.IsNullOrEmpty(typeName) || typeName == "Variant")
                continue;

            // Find position to insert hint (after variable name)
            var position = GetHintPositionAfterName(variable.DeclarationNode, variable.Name);
            if (position == null)
                continue;

            hints.Add(new GDInlayHint
            {
                Line = position.Value.Line,
                Column = position.Value.Column,
                Label = $": {typeName}",
                Kind = GDInlayHintKind.Type,
                PaddingLeft = false,
                PaddingRight = true,
                Tooltip = $"Inferred type: {typeName}"
            });
        }
    }

    /// <summary>
    /// Collects inlay hints for local variables within methods.
    /// </summary>
    protected virtual void CollectLocalVariableHints(
        GDScriptFile script,
        int startLine,
        int endLine,
        List<GDInlayHint> hints)
    {
        if (script.Class == null)
            return;

        // Iterate through all nodes to find variable declarations
        foreach (var node in script.Class.AllNodes)
        {
            if (hints.Count >= MaxHintsPerRequest)
                break;

            // Check if in range
            if (node.StartLine < startLine || node.StartLine > endLine)
                continue;

            // Handle local variable declarations (var statements)
            if (node is GDVariableDeclarationStatement varStmt)
            {
                // Skip if has explicit type
                if (varStmt.Type != null)
                    continue;

                // Try to infer type from initializer
                var typeName = InferTypeFromExpression(script, varStmt.Initializer);
                if (string.IsNullOrEmpty(typeName) || typeName == "Variant")
                    continue;

                var position = GetHintPositionAfterIdentifier(varStmt.Identifier);
                if (position == null)
                    continue;

                hints.Add(new GDInlayHint
                {
                    Line = position.Value.Line,
                    Column = position.Value.Column,
                    Label = $": {typeName}",
                    Kind = GDInlayHintKind.Type,
                    PaddingLeft = false,
                    PaddingRight = true,
                    Tooltip = $"Inferred type: {typeName}"
                });
            }

            // Handle for loop iterators
            if (node is GDForStatement forStmt && forStmt.Variable != null)
            {
                // Try to infer iterator element type
                var typeName = InferIteratorType(script, forStmt);
                if (!string.IsNullOrEmpty(typeName) && typeName != "Variant")
                {
                    var position = GetHintPositionAfterIdentifier(forStmt.Variable);
                    if (position != null)
                    {
                        hints.Add(new GDInlayHint
                        {
                            Line = position.Value.Line,
                            Column = position.Value.Column,
                            Label = $": {typeName}",
                            Kind = GDInlayHintKind.Type,
                            PaddingLeft = false,
                            PaddingRight = true,
                            Tooltip = $"Iterator type: {typeName}"
                        });
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets the position after a variable name in a declaration.
    /// Returns 1-based coordinates.
    /// </summary>
    protected static (int Line, int Column)? GetHintPositionAfterName(GDNode declaration, string name)
    {
        // Find the identifier token
        foreach (var token in declaration.AllTokens)
        {
            if (token is GDIdentifier id && id.ToString() == name)
            {
                // Position is after the identifier (1-based)
                return (id.EndLine, id.EndColumn + 1);
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the position after an identifier token.
    /// Returns 1-based coordinates.
    /// </summary>
    protected static (int Line, int Column)? GetHintPositionAfterIdentifier(GDIdentifier? identifier)
    {
        if (identifier == null)
            return null;

        // Position is after the identifier (1-based)
        return (identifier.EndLine, identifier.EndColumn + 1);
    }

    /// <summary>
    /// Infers the type from an expression.
    /// </summary>
    protected static string? InferTypeFromExpression(GDScriptFile script, GDExpression? expression)
    {
        if (expression == null)
            return null;

        // Literal types
        if (expression is GDStringExpression)
            return "String";

        if (expression is GDNumberExpression numExpr)
        {
            var text = numExpr.ToString();
            return text != null && text.Contains('.') ? "float" : "int";
        }

        if (expression is GDBoolExpression)
            return "bool";

        if (expression is GDArrayInitializerExpression)
            return "Array";

        if (expression is GDDictionaryInitializerExpression)
            return "Dictionary";

        if (expression is GDNodePathExpression)
            return "NodePath";

        // Constructor calls like Vector2(), Color(), etc.
        if (expression is GDCallExpression call)
        {
            var callerName = GetCallerName(call.CallerExpression);
            if (callerName != null && IsGodotType(callerName))
            {
                return callerName;
            }
        }

        // Try to get type from analyzer
        if (script.Analyzer != null)
        {
            var symbol = script.Analyzer.GetSymbolForNode(expression);
            if (symbol != null && !string.IsNullOrEmpty(symbol.TypeName))
            {
                return symbol.TypeName;
            }
        }

        return null;
    }

    /// <summary>
    /// Infers the element type for a for loop iterator.
    /// </summary>
    protected static string? InferIteratorType(GDScriptFile script, GDForStatement forStmt)
    {
        var collection = forStmt.Collection;
        if (collection == null)
            return null;

        // range() returns int
        if (collection is GDCallExpression call)
        {
            var callerName = GetCallerName(call.CallerExpression);
            if (callerName == "range")
                return "int";
        }

        // String iteration returns String (each character)
        if (collection is GDStringExpression)
            return "String";

        // Try to get collection type
        var collectionType = InferTypeFromExpression(script, collection);
        if (collectionType != null)
        {
            // Extract element type from typed arrays
            if (collectionType.StartsWith("Array[") && collectionType.EndsWith("]"))
            {
                return collectionType.Substring(6, collectionType.Length - 7);
            }

            // PackedStringArray -> String
            if (collectionType == "PackedStringArray")
                return "String";
            if (collectionType == "PackedInt32Array" || collectionType == "PackedInt64Array")
                return "int";
            if (collectionType == "PackedFloat32Array" || collectionType == "PackedFloat64Array")
                return "float";
            if (collectionType == "PackedVector2Array")
                return "Vector2";
            if (collectionType == "PackedVector3Array")
                return "Vector3";
            if (collectionType == "PackedColorArray")
                return "Color";
            if (collectionType == "PackedByteArray")
                return "int";
        }

        return null;
    }

    /// <summary>
    /// Gets the caller name from a call expression.
    /// </summary>
    protected static string? GetCallerName(GDExpression? caller)
    {
        if (caller is GDIdentifierExpression idExpr)
            return idExpr.Identifier?.ToString();

        return null;
    }

    /// <summary>
    /// Checks if a name is a Godot built-in type.
    /// </summary>
    protected static bool IsGodotType(string name)
    {
        return name switch
        {
            // Math types
            "Vector2" or "Vector2i" or "Vector3" or "Vector3i" or "Vector4" or "Vector4i" => true,
            "Rect2" or "Rect2i" => true,
            "Transform2D" or "Transform3D" => true,
            "Plane" or "Quaternion" or "AABB" or "Basis" => true,
            "Projection" => true,

            // Color
            "Color" => true,

            // String types
            "StringName" or "NodePath" => true,

            // Resources
            "RID" => true,

            // Callables
            "Callable" or "Signal" => true,

            _ => false
        };
    }
}
