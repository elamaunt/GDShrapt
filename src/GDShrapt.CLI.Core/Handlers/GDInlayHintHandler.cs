using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for inlay hint operations.
/// Provides type hints for variables, parameters, and signals without explicit type annotations.
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

        // Collect hints for class-level variables and properties
        CollectVariableHints(script, semanticModel, startLine, endLine, hints);

        // Collect hints for local variables in methods
        CollectLocalVariableHints(script, semanticModel, startLine, endLine, hints);

        // Collect hints for method parameters without type annotations
        CollectParameterTypeHints(script, semanticModel, startLine, endLine, hints);

        // Collect hints for signal parameters without type annotations
        CollectSignalParameterTypeHints(script, semanticModel, startLine, endLine, hints);

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
        // Get all class-level variables and properties through SemanticModel.Symbols
        foreach (var variable in semanticModel.Symbols.Where(s =>
            s.Kind == GDSymbolKind.Variable || s.Kind == GDSymbolKind.Property))
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

            // Try declared type first, then infer from initializer
            var typeName = variable.TypeName;
            if (string.IsNullOrEmpty(typeName) || typeName == "Variant")
            {
                if (variable.DeclarationNode is GDVariableDeclaration varDecl && varDecl.Initializer != null)
                {
                    var typeInfo = semanticModel.TypeSystem.GetType(varDecl.Initializer);
                    typeName = typeInfo.IsVariant ? null : typeInfo.DisplayName;

                    // Enrich plain container types with usage-based generic parameters
                    if (typeName == "Dictionary" || typeName == "Array")
                    {
                        var containerType = semanticModel.TypeSystem.GetContainerElementType(variable.Name);
                        if (containerType != null && containerType.HasElementTypes)
                            typeName = containerType.ToString();
                    }
                }
            }
            if (string.IsNullOrEmpty(typeName) || typeName == "Variant")
                continue;

            // Find position to insert hint (after variable name)
            var position = GetHintPositionAfterName(variable.DeclarationNode, variable.Name);
            if (position == null)
                continue;

            var label = $": {typeName}";
            hints.Add(new GDInlayHint
            {
                Line = position.Value.Line,
                Column = position.Value.Column,
                Label = label,
                Kind = GDInlayHintKind.Type,
                PaddingLeft = false,
                PaddingRight = true,
                Tooltip = $"Inferred type: {typeName}",
                TextEdits = CreateInsertEdit(position.Value.Line, position.Value.Column, label)
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

                var label = $": {typeName}";
                hints.Add(new GDInlayHint
                {
                    Line = position.Value.Line,
                    Column = position.Value.Column,
                    Label = label,
                    Kind = GDInlayHintKind.Type,
                    PaddingLeft = false,
                    PaddingRight = true,
                    Tooltip = $"Inferred type: {typeName}",
                    TextEdits = CreateInsertEdit(position.Value.Line, position.Value.Column, label)
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
                        var label = $": {typeName}";
                        hints.Add(new GDInlayHint
                        {
                            Line = position.Value.Line,
                            Column = position.Value.Column,
                            Label = label,
                            Kind = GDInlayHintKind.Type,
                            PaddingLeft = false,
                            PaddingRight = true,
                            Tooltip = $"Iterator type: {typeName}",
                            TextEdits = CreateInsertEdit(position.Value.Line, position.Value.Column, label)
                        });
                    }
                }
            }
        }
    }

    /// <summary>
    /// Collects inlay hints for method parameters without explicit type annotations.
    /// Uses SemanticModel.InferParameterTypes() to infer types from usage.
    /// </summary>
    protected virtual void CollectParameterTypeHints(
        GDScriptFile script,
        GDSemanticModel semanticModel,
        int startLine,
        int endLine,
        List<GDInlayHint> hints)
    {
        if (script.Class == null)
            return;

        foreach (var node in script.Class.AllNodes)
        {
            if (hints.Count >= MaxHintsPerRequest)
                break;

            if (node is not GDMethodDeclaration methodDecl)
                continue;

            var parameters = methodDecl.Parameters;
            if (parameters == null || !parameters.Any())
                continue;

            // Check if any parameter in this method is in range and lacks type
            var hasUntypedInRange = false;
            foreach (var param in parameters)
            {
                if (param.Type != null || param.Identifier == null)
                    continue;
                var paramLine1 = param.Identifier.StartLine + 1;
                if (paramLine1 >= startLine && paramLine1 <= endLine)
                {
                    hasUntypedInRange = true;
                    break;
                }
            }
            if (!hasUntypedInRange)
                continue;

            var inferredTypes = semanticModel.InferParameterTypes(methodDecl);

            foreach (var param in parameters)
            {
                if (hints.Count >= MaxHintsPerRequest)
                    break;

                if (param.Type != null || param.Identifier == null)
                    continue;

                var paramLine1 = param.Identifier.StartLine + 1;
                if (paramLine1 < startLine || paramLine1 > endLine)
                    continue;

                var paramName = param.Identifier.Sequence;
                if (string.IsNullOrEmpty(paramName))
                    continue;

                if (!inferredTypes.TryGetValue(paramName, out var inferred))
                    continue;

                if (inferred.IsUnknown)
                    continue;

                var typeName = inferred.TypeName.DisplayName;
                if (string.IsNullOrEmpty(typeName) || typeName == "Variant")
                    continue;

                var position = GetHintPositionAfterIdentifier(param.Identifier);
                if (position == null)
                    continue;

                var label = $": {typeName}";
                hints.Add(new GDInlayHint
                {
                    Line = position.Value.Line,
                    Column = position.Value.Column,
                    Label = label,
                    Kind = GDInlayHintKind.Type,
                    PaddingLeft = false,
                    PaddingRight = true,
                    Tooltip = $"Inferred type: {typeName} ({inferred.Reason ?? "from usage"})",
                    TextEdits = CreateInsertEdit(position.Value.Line, position.Value.Column, label)
                });
            }
        }
    }

    /// <summary>
    /// Collects inlay hints for signal parameters without explicit type annotations.
    /// Infers types from emit call arguments.
    /// </summary>
    protected virtual void CollectSignalParameterTypeHints(
        GDScriptFile script,
        GDSemanticModel semanticModel,
        int startLine,
        int endLine,
        List<GDInlayHint> hints)
    {
        if (script.Class == null)
            return;

        foreach (var node in script.Class.AllNodes)
        {
            if (hints.Count >= MaxHintsPerRequest)
                break;

            if (node is not GDSignalDeclaration signalDecl)
                continue;

            var parameters = signalDecl.Parameters;
            if (parameters == null || !parameters.Any())
                continue;

            // Check if any parameter in range lacks type
            var hasUntypedInRange = false;
            foreach (var param in parameters)
            {
                if (param.Type != null || param.Identifier == null)
                    continue;
                var paramLine1 = param.Identifier.StartLine + 1;
                if (paramLine1 >= startLine && paramLine1 <= endLine)
                {
                    hasUntypedInRange = true;
                    break;
                }
            }
            if (!hasUntypedInRange)
                continue;

            // Try to infer signal parameter types from emit usages
            var signalName = signalDecl.Identifier?.Sequence;
            if (string.IsNullOrEmpty(signalName))
                continue;

            var signalSymbol = semanticModel.FindSymbol(signalName);
            if (signalSymbol == null)
                continue;

            // Collect argument types from emit references
            var refs = semanticModel.GetReferencesTo(signalSymbol);
            var paramTypes = InferSignalParameterTypesFromEmits(refs, parameters, semanticModel, script);

            var paramIndex = 0;
            foreach (var param in parameters)
            {
                if (hints.Count >= MaxHintsPerRequest)
                    break;

                if (param.Type != null || param.Identifier == null)
                {
                    paramIndex++;
                    continue;
                }

                var paramLine1 = param.Identifier.StartLine + 1;
                if (paramLine1 < startLine || paramLine1 > endLine)
                {
                    paramIndex++;
                    continue;
                }

                if (paramIndex < paramTypes.Count && paramTypes[paramIndex] != null)
                {
                    var typeName = paramTypes[paramIndex];
                    if (!string.IsNullOrEmpty(typeName) && typeName != "Variant")
                    {
                        var position = GetHintPositionAfterIdentifier(param.Identifier);
                        if (position != null)
                        {
                            var label = $": {typeName}";
                            hints.Add(new GDInlayHint
                            {
                                Line = position.Value.Line,
                                Column = position.Value.Column,
                                Label = label,
                                Kind = GDInlayHintKind.Type,
                                PaddingLeft = false,
                                PaddingRight = true,
                                Tooltip = $"Inferred from emit usage: {typeName}",
                                TextEdits = CreateInsertEdit(position.Value.Line, position.Value.Column, label)
                            });
                        }
                    }
                }

                paramIndex++;
            }
        }
    }

    /// <summary>
    /// Infers signal parameter types from emit() call argument types.
    /// </summary>
    private static List<string?> InferSignalParameterTypesFromEmits(
        IReadOnlyList<GDReference> refs,
        IEnumerable<GDParameterDeclaration> parameters,
        GDSemanticModel semanticModel,
        GDScriptFile script)
    {
        var paramCount = parameters.Count();
        var paramTypes = new List<string?>(new string?[paramCount]);

        foreach (var reference in refs)
        {
            if (reference.ReferenceNode.IsEmpty)
                continue;

            // Resolve handle to AST node for navigation
            var refNode = ResolveHandle(reference.ReferenceNode, script);
            if (refNode == null)
                continue;

            // Find emit() calls: signal.emit(arg1, arg2, ...)
            var callExpr = refNode.Parent as GDCallExpression
                        ?? refNode.Parent?.Parent as GDCallExpression;
            if (callExpr == null)
                continue;

            // Check if this is a .emit() call
            if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
            {
                var memberName = memberOp.Identifier?.Sequence;
                if (memberName != "emit")
                    continue;
            }
            else
                continue;

            var args = callExpr.Parameters;
            if (args == null)
                continue;

            var argIndex = 0;
            foreach (var arg in args)
            {
                if (argIndex >= paramCount)
                    break;

                if (paramTypes[argIndex] == null)
                {
                    var argType = semanticModel.TypeSystem.GetType(arg);
                    if (!argType.IsVariant)
                        paramTypes[argIndex] = argType.DisplayName;
                }
                argIndex++;
            }
        }

        return paramTypes;
    }

    /// <summary>
    /// Creates a single zero-width insert text edit at the given position.
    /// </summary>
    protected static GDInlayHintTextEdit[] CreateInsertEdit(int line, int column, string text)
    {
        return [new GDInlayHintTextEdit
        {
            Line = line,
            StartColumn = column,
            EndColumn = column,
            NewText = text
        }];
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

    private static GDNode? ResolveHandle(GDNodeHandle handle, GDScriptFile? script)
    {
        if (handle.IsEmpty || script?.SemanticModel == null)
            return null;

        return script.SemanticModel.GetNodeAtPosition(handle.StartLine, handle.StartColumn);
    }
}
