using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for inlay hint operations.
/// Provides type hints for variables without explicit type annotations.
/// All type information is accessed through GDSemanticModel as the single API entry point.
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
        var semanticModel = script?.SemanticModel;
        if (script?.Class == null || semanticModel == null)
            return [];

        var hints = new List<GDInlayHint>();

        // Collect hints for class-level variables
        CollectVariableHints(script, semanticModel, startLine, endLine, hints);

        // Collect hints for local variables in methods
        CollectLocalVariableHints(script, semanticModel, startLine, endLine, hints);

        // Limit hints count
        if (hints.Count > MaxHintsPerRequest)
        {
            hints.RemoveRange(MaxHintsPerRequest, hints.Count - MaxHintsPerRequest);
        }

        return hints;
    }

    /// <summary>
    /// Collects inlay hints for class-level variables.
    /// Uses GDSemanticModel.Symbols as the single API entry point.
    /// </summary>
    protected virtual void CollectVariableHints(
        GDScriptFile script,
        GDSemanticModel semanticModel,
        int startLine,
        int endLine,
        List<GDInlayHint> hints)
    {
        // Get all class-level variables through SemanticModel.Symbols
        foreach (var variable in semanticModel.Symbols.Where(s => s.Kind == GDSymbolKind.Variable))
        {
            if (hints.Count >= MaxHintsPerRequest)
                break;

            // Check if in range
            if (variable.DeclarationNode == null)
                continue;

            // AST StartLine is 0-based, startLine/endLine are 1-based
            var line1 = variable.DeclarationNode.StartLine + 1;
            if (line1 < startLine || line1 > endLine)
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
    /// Uses GDSemanticModel for all type inference.
    /// </summary>
    protected virtual void CollectLocalVariableHints(
        GDScriptFile script,
        GDSemanticModel semanticModel,
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

            // AST StartLine is 0-based, startLine/endLine are 1-based
            var nodeLine1 = node.StartLine + 1;
            if (nodeLine1 < startLine || nodeLine1 > endLine)
                continue;

            // Handle local variable declarations (var statements)
            if (node is GDVariableDeclarationStatement varStmt)
            {
                // Skip if has explicit type
                if (varStmt.Type != null)
                    continue;

                // Try to infer type from initializer via SemanticModel
                string? typeName = null;
                if (varStmt.Initializer != null)
                {
                    var typeInfo = semanticModel.TypeSystem.GetType(varStmt.Initializer);
                    typeName = typeInfo.IsVariant ? null : typeInfo.DisplayName;
                }
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
                // Get iterator type via SemanticModel flow analysis
                var iteratorName = forStmt.Variable.Sequence;
                var typeNameSemantic = !string.IsNullOrEmpty(iteratorName)
                    ? semanticModel.GetFlowVariableType(iteratorName, forStmt)?.EffectiveType
                    : null;
                var typeName = typeNameSemantic?.DisplayName;

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
                // AST EndLine/EndColumn are 0-based, convert to 1-based
                return (id.EndLine + 1, id.EndColumn + 1);
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

        // AST EndLine/EndColumn are 0-based, convert to 1-based
        return (identifier.EndLine + 1, identifier.EndColumn + 1);
    }

}
