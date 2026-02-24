using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// The scope where a symbol is defined.
/// </summary>
public enum GDSymbolScopeType
{
    /// <summary>Local variable in method body.</summary>
    LocalVariable,

    /// <summary>Method parameter.</summary>
    MethodParameter,

    /// <summary>For loop iterator variable.</summary>
    ForLoopVariable,

    /// <summary>Match case bound variable.</summary>
    MatchCaseVariable,

    /// <summary>Class member (var, const, signal, method, enum).</summary>
    ClassMember,

    /// <summary>Member from another class in project.</summary>
    ExternalMember,

    /// <summary>Symbol spans entire project (class_name, global).</summary>
    ProjectWide
}

/// <summary>
/// The kind of reference (declaration, read, write, call).
/// </summary>
public enum GDReferenceKind
{
    /// <summary>Symbol declaration.</summary>
    Declaration,

    /// <summary>Reading the symbol's value.</summary>
    Read,

    /// <summary>Writing/assigning to the symbol.</summary>
    Write,

    /// <summary>Calling the symbol as a function.</summary>
    Call
}

/// <summary>
/// A single reference location found by GDFindReferencesService.
/// </summary>
public class GDReferenceLocation
{
    /// <summary>
    /// The symbol name that was found.
    /// </summary>
    public string SymbolName { get; }

    /// <summary>
    /// File path of the reference.
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Line number (0-based).
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Column number (0-based).
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// End column of the identifier.
    /// </summary>
    public int EndColumn { get; }

    /// <summary>
    /// The kind of reference.
    /// </summary>
    public GDReferenceKind Kind { get; }

    /// <summary>
    /// Confidence level of this reference.
    /// </summary>
    public GDReferenceConfidence Confidence { get; }

    /// <summary>
    /// Reason for the confidence level.
    /// </summary>
    public string? ConfidenceReason { get; }

    /// <summary>
    /// The AST node of the reference.
    /// </summary>
    public GDNode Node { get; }

    /// <summary>
    /// Context text showing the reference in code.
    /// </summary>
    public string ContextText { get; }

    /// <summary>
    /// Start position of highlight within ContextText.
    /// </summary>
    public int HighlightStart { get; }

    /// <summary>
    /// End position of highlight within ContextText.
    /// </summary>
    public int HighlightEnd { get; }

    public GDReferenceLocation(
        string symbolName,
        string? filePath,
        int line,
        int column,
        int endColumn,
        GDReferenceKind kind,
        GDReferenceConfidence confidence,
        GDNode node,
        string contextText,
        int highlightStart,
        int highlightEnd,
        string? confidenceReason = null)
    {
        SymbolName = symbolName;
        FilePath = filePath;
        Line = line;
        Column = column;
        EndColumn = endColumn;
        Kind = kind;
        Confidence = confidence;
        Node = node;
        ContextText = contextText;
        HighlightStart = highlightStart;
        HighlightEnd = highlightEnd;
        ConfidenceReason = confidenceReason;
    }
}

/// <summary>
/// Result of find references operation.
/// </summary>
public class GDFindReferencesResult : GDRefactoringResult
{
    /// <summary>
    /// The symbol that was searched for.
    /// </summary>
    public GDSymbolInfo? Symbol { get; }

    /// <summary>
    /// Strict references - type-confirmed.
    /// </summary>
    public IReadOnlyList<GDReferenceLocation> StrictReferences { get; }

    /// <summary>
    /// Potential references - may be references but type unknown.
    /// </summary>
    public IReadOnlyList<GDReferenceLocation> PotentialReferences { get; }

    /// <summary>
    /// All references combined.
    /// </summary>
    public IReadOnlyList<GDReferenceLocation> AllReferences { get; }

    /// <summary>
    /// Total count of references.
    /// </summary>
    public int TotalCount => StrictReferences.Count + PotentialReferences.Count;

    private GDFindReferencesResult(
        bool success,
        string? errorMessage,
        GDSymbolInfo? symbol,
        IReadOnlyList<GDReferenceLocation> strictReferences,
        IReadOnlyList<GDReferenceLocation> potentialReferences)
        : base(success, errorMessage, null)
    {
        Symbol = symbol;
        StrictReferences = strictReferences;
        PotentialReferences = potentialReferences;
        AllReferences = strictReferences.Concat(potentialReferences).ToList();
    }

    /// <summary>
    /// Creates a successful result with references.
    /// </summary>
    public static GDFindReferencesResult Succeeded(
        GDSymbolInfo symbol,
        IReadOnlyList<GDReferenceLocation> strictReferences,
        IReadOnlyList<GDReferenceLocation> potentialReferences)
    {
        return new GDFindReferencesResult(true, null, symbol, strictReferences, potentialReferences);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public new static GDFindReferencesResult Failed(string errorMessage)
    {
        return new GDFindReferencesResult(false, errorMessage, null,
            Array.Empty<GDReferenceLocation>(), Array.Empty<GDReferenceLocation>());
    }

    /// <summary>
    /// Gets references grouped by file.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<GDReferenceLocation>> GetByFile()
    {
        return AllReferences
            .Where(r => r.FilePath != null)
            .GroupBy(r => r.FilePath!)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<GDReferenceLocation>)g.OrderBy(r => r.Line).ThenBy(r => r.Column).ToList());
    }
}

/// <summary>
/// Service for finding all references to a symbol.
/// Uses GDSemanticModel for symbol resolution when available.
/// When project context is provided, delegates cross-file collection to GDSymbolReferenceCollector.
/// </summary>
public class GDFindReferencesService : GDRefactoringServiceBase
{
    private readonly GDScriptProject? _project;
    private readonly GDProjectSemanticModel? _projectModel;

    public GDFindReferencesService()
    {
    }

    public GDFindReferencesService(GDScriptProject project, GDProjectSemanticModel? projectModel = null)
    {
        _project = project;
        _projectModel = projectModel;
    }

    /// <summary>
    /// Determines the symbol at the given cursor position.
    /// </summary>
    public GDSymbolInfo? DetermineSymbolScope(GDRefactoringContext context)
    {
        if (!IsContextValid(context))
            return null;

        var semanticModel = context.Script?.SemanticModel
            ?? (context.Script != null ? GDSemanticModel.Create(context.Script) : null);

        return semanticModel?.GetSymbolAtPosition(context.Cursor.Line, context.Cursor.Column);
    }

    /// <summary>
    /// Determines the symbol for a specific identifier.
    /// Builds a SemanticModel on demand if one is not already available.
    /// </summary>
    public GDSymbolInfo? DetermineSymbolScope(GDIdentifier identifier, GDRefactoringContext context)
    {
        if (identifier == null || context == null)
            return null;

        var semanticModel = context.Script?.SemanticModel
            ?? (context.Script != null ? GDSemanticModel.Create(context.Script) : null);

        return semanticModel?.GetSymbolAtPosition(identifier.StartLine, identifier.StartColumn);
    }

    /// <summary>
    /// Finds all references to the symbol at cursor position.
    /// </summary>
    public GDFindReferencesResult FindReferences(GDRefactoringContext context)
    {
        var scope = DetermineSymbolScope(context);
        if (scope == null)
            return GDFindReferencesResult.Failed("No symbol at cursor");

        return FindReferencesForScope(context, scope);
    }

    /// <summary>
    /// Finds all references for a known symbol scope.
    /// When project context is available and the scope is cross-file (class member, external, project-wide),
    /// delegates to GDSymbolReferenceCollector for unified cross-file collection.
    /// </summary>
    public GDFindReferencesResult FindReferencesForScope(GDRefactoringContext context, GDSymbolInfo scope)
    {
        // For cross-file scopes, delegate to the unified collector when project context is available
        if (_project != null && IsCrossFileScope(scope))
        {
            var result = CollectViaUnifiedCollector(scope);
            if (result != null)
                return result;
        }

        var strictRefs = new List<GDReferenceLocation>();
        var potentialRefs = new List<GDReferenceLocation>();

        // Use SemanticModel for all scope types (build on demand if needed)
        var semanticModel = context.Script?.SemanticModel
            ?? (context.Script != null ? GDSemanticModel.Create(context.Script) : null);

        if (semanticModel != null)
        {
            var collected = CollectReferencesViaSemanticModel(semanticModel, scope);
            if (collected != null)
            {
                strictRefs.AddRange(collected.Where(r => r.Confidence == GDReferenceConfidence.Strict));
                potentialRefs.AddRange(collected.Where(r => r.Confidence == GDReferenceConfidence.Potential));
                return GDFindReferencesResult.Succeeded(scope, strictRefs, potentialRefs);
            }
        }

        return GDFindReferencesResult.Succeeded(scope, strictRefs, potentialRefs);
    }

    private static bool IsCrossFileScope(GDSymbolInfo scope)
    {
        return scope.ScopeType == GDSymbolScopeType.ClassMember
            || scope.ScopeType == GDSymbolScopeType.ExternalMember
            || scope.ScopeType == GDSymbolScopeType.ProjectWide;
    }

    /// <summary>
    /// Delegates to GDSymbolReferenceCollector and converts results to GDReferenceLocation.
    /// Returns null if the symbol cannot be resolved.
    /// </summary>
    private GDFindReferencesResult? CollectViaUnifiedCollector(GDSymbolInfo scope)
    {
        var collector = new GDSymbolReferenceCollector(_project!, _projectModel);
        var filterFile = scope.DeclaringScript?.FullPath;
        var collected = collector.CollectReferences(scope.Name, filterFile);

        if (collected.References.Count == 0)
            return null;

        var strictRefs = new List<GDReferenceLocation>();
        var potentialRefs = new List<GDReferenceLocation>();

        foreach (var symRef in collected.References)
        {
            var refKind = ConvertReferenceKind(symRef);
            var (contextText, hlStart, hlEnd) = GetContextForSymbolReference(symRef, scope.Name);

            var location = new GDReferenceLocation(
                scope.Name,
                symRef.FilePath,
                symRef.Line,
                symRef.Column,
                symRef.Column + scope.Name.Length,
                refKind,
                symRef.Confidence,
                symRef.Node ?? symRef.Script.Class as GDNode ?? new GDClassDeclaration(),
                contextText,
                hlStart,
                hlEnd,
                symRef.ConfidenceReason);

            if (symRef.Confidence == GDReferenceConfidence.Strict)
                strictRefs.Add(location);
            else if (symRef.Confidence == GDReferenceConfidence.Potential)
                potentialRefs.Add(location);
        }

        EnrichWithCSharpInteropNote(scope, strictRefs);

        return GDFindReferencesResult.Succeeded(scope, strictRefs, potentialRefs);
    }

    /// <summary>
    /// Enriches declaration references with C# interop notes when the symbol
    /// is on an autoload in a mixed GDScript/C# project.
    /// </summary>
    private void EnrichWithCSharpInteropNote(GDSymbolInfo scope, List<GDReferenceLocation> strictRefs)
    {
        if (_projectModel == null || _project == null)
            return;

        if (!_projectModel.CSharpInterop.HasCSharpCode)
            return;

        // Find the declaring script
        var declaringScript = scope.DeclaringScript;
        if (declaringScript == null)
            return;

        // Check if declaring script is an autoload
        var isAutoload = _project.AutoloadEntries.Any(a =>
        {
            var resPath = a.Path;
            if (resPath.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
                resPath = resPath.Substring(6);
            var fullPath = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(_project.ProjectPath, resPath))
                .Replace('\\', '/').TrimEnd('/');
            return declaringScript.FullPath != null &&
                fullPath.Equals(declaringScript.FullPath, StringComparison.OrdinalIgnoreCase);
        });

        if (!isAutoload)
            return;

        // Find the declaration reference and replace it with an enriched version
        for (int i = 0; i < strictRefs.Count; i++)
        {
            if (strictRefs[i].Kind == GDReferenceKind.Declaration)
            {
                var orig = strictRefs[i];
                var interopReason = orig.ConfidenceReason != null
                    ? $"{orig.ConfidenceReason}; may be called from C# via Call()"
                    : "May be called from C# via Call()";

                strictRefs[i] = new GDReferenceLocation(
                    orig.SymbolName,
                    orig.FilePath,
                    orig.Line,
                    orig.Column,
                    orig.EndColumn,
                    orig.Kind,
                    orig.Confidence,
                    orig.Node,
                    orig.ContextText,
                    orig.HighlightStart,
                    orig.HighlightEnd,
                    interopReason);
                break;
            }
        }
    }

    private static GDReferenceKind ConvertReferenceKind(GDSymbolReference symRef)
    {
        return symRef.Kind switch
        {
            GDSymbolReferenceKind.Declaration => GDReferenceKind.Declaration,
            GDSymbolReferenceKind.Write => GDReferenceKind.Write,
            GDSymbolReferenceKind.Call => GDReferenceKind.Call,
            GDSymbolReferenceKind.Override => GDReferenceKind.Declaration,
            _ => GDReferenceKind.Read
        };
    }

    private (string contextText, int hlStart, int hlEnd) GetContextForSymbolReference(
        GDSymbolReference symRef, string symbolName)
    {
        // Try to get context from the identifier token
        if (symRef.IdentifierToken is GDIdentifier identifier)
            return GetContextWithHighlight(identifier);

        // Try to get context from the node
        if (symRef.Node != null)
        {
            var text = symRef.Node.ToString() ?? symbolName;
            if (text.Length > 60)
                text = text.Substring(0, 57) + "...";
            text = text.Trim().Replace("\n", " ").Replace("\r", "");

            var idx = text.IndexOf(symbolName, StringComparison.Ordinal);
            if (idx >= 0)
                return (text, idx, idx + symbolName.Length);
            return (text, 0, 0);
        }

        return (symbolName, 0, symbolName.Length);
    }

    /// <summary>
    /// Collects references using the SemanticModel when available.
    /// Returns null if SemanticModel doesn't have the symbol.
    /// </summary>
    private List<GDReferenceLocation>? CollectReferencesViaSemanticModel(GDSemanticModel semanticModel, GDSymbolInfo scope)
    {
        // Prefer exact node lookup to handle same-named symbols in different scopes
        var symbolInfo = scope.DeclarationNode != null
            ? semanticModel.GetSymbolForNode(scope.DeclarationNode)
            : null;
        symbolInfo ??= semanticModel.FindSymbol(scope.Name);
        if (symbolInfo == null)
            return null;

        var references = semanticModel.GetReferencesTo(symbolInfo);
        if (references.Count == 0)
            return null;

        var filePath = scope.DeclaringScript?.FullPath;
        var results = new List<GDReferenceLocation>();

        foreach (var gdRef in references)
        {
            var node = gdRef.ReferenceNode;
            if (node == null)
                continue;

            var identifier = gdRef.IdentifierToken as GDIdentifier;
            if (identifier == null)
                continue;

            var (contextText, hlStart, hlEnd) = GetContextWithHighlight(identifier);
            var refKind = DetermineReferenceKind(identifier);
            var confidenceReason = semanticModel.GetConfidenceReason(identifier) ?? "Resolved via SemanticModel";

            results.Add(new GDReferenceLocation(
                scope.Name,
                filePath,
                identifier.StartLine,
                identifier.StartColumn,
                identifier.EndColumn,
                refKind,
                gdRef.Confidence,
                node,
                contextText,
                hlStart,
                hlEnd,
                confidenceReason));
        }

        // Also add the declaration if not already included
        if (symbolInfo.DeclarationIdentifier is GDIdentifier declId)
        {
            if (!results.Any(r => r.Line == declId.StartLine && r.Column == declId.StartColumn))
            {
                var (contextText, hlStart, hlEnd) = GetContextWithHighlight(declId);
                results.Add(new GDReferenceLocation(
                    scope.Name,
                    filePath,
                    declId.StartLine,
                    declId.StartColumn,
                    declId.EndColumn,
                    GDReferenceKind.Declaration,
                    GDReferenceConfidence.Strict,
                    symbolInfo.DeclarationNode!,
                    contextText,
                    hlStart,
                    hlEnd,
                    "Symbol declaration"));
            }
        }

        return results;
    }

    /// <summary>
    /// Finds an identifier with the given name within a node.
    /// Uses fast-path checks for common node types before falling back to full token search.
    /// </summary>
    private GDIdentifier? FindIdentifierInNode(GDNode node, string name)
    {
        // Fast-path 1: Simple identifier expression (most common)
        if (node is GDIdentifierExpression idExpr && idExpr.Identifier?.Sequence == name)
            return idExpr.Identifier;

        // Fast-path 2: Member access (obj.member)
        if (node is GDMemberOperatorExpression memberOp && memberOp.Identifier?.Sequence == name)
            return memberOp.Identifier;

        // Fast-path 3: Match case variable binding
        if (node is GDMatchCaseVariableExpression matchVar && matchVar.Identifier?.Sequence == name)
            return matchVar.Identifier;

        // Fast-path 4: Method declaration
        if (node is GDMethodDeclaration methodDecl && methodDecl.Identifier?.Sequence == name)
            return methodDecl.Identifier;

        // Fast-path 5: Variable declaration
        if (node is GDVariableDeclaration varDecl && varDecl.Identifier?.Sequence == name)
            return varDecl.Identifier;

        // Fast-path 6: Parameter declaration
        if (node is GDParameterDeclaration param && param.Identifier?.Sequence == name)
            return param.Identifier;

        // Fast-path 7: Signal declaration
        if (node is GDSignalDeclaration signalDecl && signalDecl.Identifier?.Sequence == name)
            return signalDecl.Identifier;

        // Fast-path 8: Enum declaration
        if (node is GDEnumDeclaration enumDecl && enumDecl.Identifier?.Sequence == name)
            return enumDecl.Identifier;

        // Fallback: Full search in all tokens
        return node.AllTokens.OfType<GDIdentifier>().FirstOrDefault(i => i.Sequence == name);
    }

    #region Helper Methods

    private GDReferenceKind DetermineReferenceKind(GDIdentifier identifier)
    {
        var parent = identifier.Parent;

        // Check if it's a declaration
        if (parent is GDMethodDeclaration ||
            parent is GDVariableDeclaration ||
            parent is GDVariableDeclarationStatement ||
            parent is GDSignalDeclaration ||
            parent is GDParameterDeclaration ||
            parent is GDEnumDeclaration ||
            parent is GDInnerClassDeclaration)
        {
            return GDReferenceKind.Declaration;
        }

        // Check if it's a call
        if (parent is GDIdentifierExpression idExpr)
        {
            if (idExpr.Parent is GDCallExpression)
                return GDReferenceKind.Call;
        }

        if (parent is GDMemberOperatorExpression memberOp)
        {
            if (memberOp.Parent is GDCallExpression)
                return GDReferenceKind.Call;
        }

        // Check if it's a write (assignment target)
        if (parent is GDIdentifierExpression idExpr2)
        {
            if (idExpr2.Parent is GDDualOperatorExpression dualOp)
            {
                // Check if this is the left side of an assignment
                if (dualOp.LeftExpression == idExpr2 && IsAssignmentOperator(dualOp.OperatorType))
                {
                    return GDReferenceKind.Write;
                }
            }
        }

        // Default to Read
        return GDReferenceKind.Read;
    }

    private bool IsAssignmentOperator(GDDualOperatorType opType)
    {
        return opType == GDDualOperatorType.Assignment ||
               opType == GDDualOperatorType.AddAndAssign ||
               opType == GDDualOperatorType.SubtractAndAssign ||
               opType == GDDualOperatorType.MultiplyAndAssign ||
               opType == GDDualOperatorType.DivideAndAssign ||
               opType == GDDualOperatorType.ModAndAssign ||
               opType == GDDualOperatorType.BitwiseAndAndAssign ||
               opType == GDDualOperatorType.BitwiseOrAndAssign ||
               opType == GDDualOperatorType.XorAndAssign ||
               opType == GDDualOperatorType.BitShiftLeftAndAssign ||
               opType == GDDualOperatorType.BitShiftRightAndAssign ||
               opType == GDDualOperatorType.PowerAndAssign;
    }

    private (string context, int highlightStart, int highlightEnd) GetContextWithHighlight(GDIdentifier identifier)
    {
        if (identifier == null)
            return ("", 0, 0);

        var symbolName = identifier.Sequence ?? "";
        var parent = identifier.Parent;

        if (parent is GDMethodDeclaration method)
        {
            var text = $"func {method.Identifier?.Sequence ?? ""}(...)";
            var hlStart = 5;
            var hlEnd = hlStart + (method.Identifier?.Sequence?.Length ?? 0);
            return (text, hlStart, hlEnd);
        }

        if (parent is GDVariableDeclaration variable)
        {
            var text = $"var {variable.Identifier?.Sequence ?? ""}";
            var hlStart = 4;
            var hlEnd = hlStart + (variable.Identifier?.Sequence?.Length ?? 0);
            return (text, hlStart, hlEnd);
        }

        if (parent is GDVariableDeclarationStatement localVar)
        {
            var text = $"var {localVar.Identifier?.Sequence ?? ""}";
            var hlStart = 4;
            var hlEnd = hlStart + (localVar.Identifier?.Sequence?.Length ?? 0);
            return (text, hlStart, hlEnd);
        }

        if (parent is GDSignalDeclaration signal)
        {
            var text = $"signal {signal.Identifier?.Sequence ?? ""}";
            var hlStart = 7;
            var hlEnd = hlStart + (signal.Identifier?.Sequence?.Length ?? 0);
            return (text, hlStart, hlEnd);
        }

        if (parent is GDParameterDeclaration param)
        {
            var text = $"param {param.Identifier?.Sequence ?? ""}";
            var hlStart = 6;
            var hlEnd = hlStart + (param.Identifier?.Sequence?.Length ?? 0);
            return (text, hlStart, hlEnd);
        }

        // For expressions, try to get statement context
        var current = parent;
        while (current != null && !(current is GDStatement) && !(current is GDClassMember))
        {
            current = current.Parent;
        }

        if (current != null)
        {
            var text = current.ToString() ?? "";
            var wasTruncated = false;

            if (text.Length > 60)
            {
                text = text.Substring(0, 57) + "...";
                wasTruncated = true;
            }

            text = text.Trim().Replace("\n", " ").Replace("\r", "");

            var idx = text.IndexOf(symbolName, StringComparison.Ordinal);
            if (idx >= 0 && (!wasTruncated || idx + symbolName.Length <= 57))
            {
                return (text, idx, idx + symbolName.Length);
            }

            return (text, 0, 0);
        }

        return (symbolName, 0, symbolName.Length);
    }

    #endregion
}
