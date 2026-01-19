using System;
using System.Collections.Generic;
using System.Linq;
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
/// Information about a symbol's scope.
/// </summary>
public class GDSymbolScope
{
    /// <summary>
    /// The type of symbol scope.
    /// </summary>
    public GDSymbolScopeType Type { get; }

    /// <summary>
    /// The name of the symbol.
    /// </summary>
    public string SymbolName { get; }

    /// <summary>
    /// The AST node where the symbol is declared.
    /// </summary>
    public GDNode? DeclarationNode { get; }

    /// <summary>
    /// The method containing this symbol (for local variables, parameters).
    /// </summary>
    public GDMethodDeclaration? ContainingMethod { get; }

    /// <summary>
    /// The for loop containing this symbol (for loop variables).
    /// </summary>
    public GDForStatement? ContainingForLoop { get; }

    /// <summary>
    /// The match case containing this symbol (match case variables).
    /// </summary>
    public GDMatchCaseDeclaration? ContainingMatchCase { get; }

    /// <summary>
    /// The class containing this symbol.
    /// </summary>
    public GDClassDeclaration? ContainingClass { get; }

    /// <summary>
    /// The script file containing this symbol.
    /// </summary>
    public GDScriptFile? ContainingScript { get; }

    /// <summary>
    /// For external members, the expression accessing the member.
    /// </summary>
    public GDMemberOperatorExpression? MemberExpression { get; }

    /// <summary>
    /// For external members, the resolved type name of the caller (if known).
    /// </summary>
    public string? CallerTypeName { get; }

    /// <summary>
    /// Line where the symbol is declared.
    /// </summary>
    public int DeclarationLine { get; }

    /// <summary>
    /// Whether the symbol is public (doesn't start with underscore).
    /// </summary>
    public bool IsPublic { get; }

    public GDSymbolScope(
        GDSymbolScopeType type,
        string symbolName,
        GDNode? declarationNode = null,
        GDMethodDeclaration? containingMethod = null,
        GDForStatement? containingForLoop = null,
        GDMatchCaseDeclaration? containingMatchCase = null,
        GDClassDeclaration? containingClass = null,
        GDScriptFile? containingScript = null,
        GDMemberOperatorExpression? memberExpression = null,
        string? callerTypeName = null,
        int declarationLine = 0,
        bool isPublic = true)
    {
        Type = type;
        SymbolName = symbolName;
        DeclarationNode = declarationNode;
        ContainingMethod = containingMethod;
        ContainingForLoop = containingForLoop;
        ContainingMatchCase = containingMatchCase;
        ContainingClass = containingClass;
        ContainingScript = containingScript;
        MemberExpression = memberExpression;
        CallerTypeName = callerTypeName;
        DeclarationLine = declarationLine;
        IsPublic = isPublic;
    }
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
/// A single reference found by GDFindReferencesService.
/// </summary>
public class GDFoundReference
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

    public GDFoundReference(
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
    public GDSymbolScope? Symbol { get; }

    /// <summary>
    /// Strict references - type-confirmed.
    /// </summary>
    public IReadOnlyList<GDFoundReference> StrictReferences { get; }

    /// <summary>
    /// Potential references - may be references but type unknown.
    /// </summary>
    public IReadOnlyList<GDFoundReference> PotentialReferences { get; }

    /// <summary>
    /// All references combined.
    /// </summary>
    public IReadOnlyList<GDFoundReference> AllReferences { get; }

    /// <summary>
    /// Total count of references.
    /// </summary>
    public int TotalCount => StrictReferences.Count + PotentialReferences.Count;

    private GDFindReferencesResult(
        bool success,
        string? errorMessage,
        GDSymbolScope? symbol,
        IReadOnlyList<GDFoundReference> strictReferences,
        IReadOnlyList<GDFoundReference> potentialReferences)
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
        GDSymbolScope symbol,
        IReadOnlyList<GDFoundReference> strictReferences,
        IReadOnlyList<GDFoundReference> potentialReferences)
    {
        return new GDFindReferencesResult(true, null, symbol, strictReferences, potentialReferences);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public new static GDFindReferencesResult Failed(string errorMessage)
    {
        return new GDFindReferencesResult(false, errorMessage, null,
            Array.Empty<GDFoundReference>(), Array.Empty<GDFoundReference>());
    }

    /// <summary>
    /// Gets references grouped by file.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<GDFoundReference>> GetByFile()
    {
        return AllReferences
            .Where(r => r.FilePath != null)
            .GroupBy(r => r.FilePath!)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<GDFoundReference>)g.OrderBy(r => r.Line).ThenBy(r => r.Column).ToList());
    }
}

/// <summary>
/// Service for finding all references to a symbol.
/// Uses GDSemanticModel for symbol resolution when available.
/// </summary>
public class GDFindReferencesService
{
    /// <summary>
    /// Determines the scope of a symbol at the given cursor position.
    /// </summary>
    public GDSymbolScope? DetermineSymbolScope(GDRefactoringContext context)
    {
        if (context?.ClassDeclaration == null)
            return null;

        // Use SemanticModel for symbol resolution
        var semanticModel = context.Script?.Analyzer?.SemanticModel;
        if (semanticModel != null)
        {
            var symbolInfo = semanticModel.GetSymbolAt(context.Cursor.Line, context.Cursor.Column);
            if (symbolInfo != null)
            {
                return ConvertToSymbolScope(symbolInfo, context);
            }
        }

        // SemanticModel not available or symbol not found
        return null;
    }

    /// <summary>
    /// Converts a GDSymbolInfo to GDSymbolScope for backwards compatibility.
    /// </summary>
    private GDSymbolScope ConvertToSymbolScope(GDSymbolInfo symbolInfo, GDRefactoringContext context)
    {
        var scopeType = symbolInfo.Kind switch
        {
            GDSymbolKind.Parameter => GDSymbolScopeType.MethodParameter,
            GDSymbolKind.Iterator => GDSymbolScopeType.ForLoopVariable,
            GDSymbolKind.MatchCaseBinding => GDSymbolScopeType.MatchCaseVariable,
            GDSymbolKind.Variable when symbolInfo.DeclaringTypeName == null => GDSymbolScopeType.LocalVariable,
            GDSymbolKind.Variable => GDSymbolScopeType.ClassMember,
            GDSymbolKind.Method => GDSymbolScopeType.ClassMember,
            GDSymbolKind.Signal => GDSymbolScopeType.ClassMember,
            GDSymbolKind.Property => GDSymbolScopeType.ClassMember,
            GDSymbolKind.Constant => GDSymbolScopeType.ClassMember,
            GDSymbolKind.Enum => GDSymbolScopeType.ClassMember,
            GDSymbolKind.EnumValue => GDSymbolScopeType.ClassMember,
            GDSymbolKind.Class => GDSymbolScopeType.ProjectWide,
            _ => GDSymbolScopeType.ProjectWide
        };

        // For inherited members, mark as external
        if (symbolInfo.IsInherited)
            scopeType = GDSymbolScopeType.ExternalMember;

        var containingMethod = symbolInfo.DeclarationNode != null
            ? GDPositionFinder.FindParent<GDMethodDeclaration>(symbolInfo.DeclarationNode)
            : null;

        var containingForLoop = symbolInfo.DeclarationNode as GDForStatement;

        // Check for match case variable
        GDMatchCaseDeclaration? containingMatchCase = null;
        if (symbolInfo.Kind == GDSymbolKind.MatchCaseBinding && symbolInfo.DeclarationNode != null)
        {
            containingMatchCase = GDPositionFinder.FindParent<GDMatchCaseDeclaration>(symbolInfo.DeclarationNode);
        }

        return new GDSymbolScope(
            scopeType,
            symbolInfo.Name,
            declarationNode: symbolInfo.DeclarationNode,
            containingMethod: containingMethod,
            containingForLoop: containingForLoop,
            containingMatchCase: containingMatchCase,
            containingClass: context.ClassDeclaration,
            containingScript: context.Script,
            callerTypeName: symbolInfo.DeclaringTypeName,
            declarationLine: symbolInfo.DeclarationNode?.StartLine ?? 0,
            isPublic: !symbolInfo.Name.StartsWith("_"));
    }

    /// <summary>
    /// Determines the scope of a specific identifier.
    /// </summary>
    public GDSymbolScope DetermineSymbolScope(GDIdentifier identifier, GDRefactoringContext context)
    {
        var symbolName = identifier.Sequence ?? "";
        var parent = identifier.Parent;

        // Local variable declaration
        if (parent is GDVariableDeclarationStatement)
        {
            var method = GDPositionFinder.FindParent<GDMethodDeclaration>(identifier);
            if (method != null)
            {
                return new GDSymbolScope(
                    GDSymbolScopeType.LocalVariable,
                    symbolName,
                    declarationNode: parent as GDNode,
                    containingMethod: method,
                    containingClass: context.ClassDeclaration,
                    containingScript: context.Script,
                    declarationLine: identifier.StartLine);
            }
        }

        // Method parameter
        if (parent is GDParameterDeclaration)
        {
            var method = GDPositionFinder.FindParent<GDMethodDeclaration>(identifier);
            if (method != null)
            {
                return new GDSymbolScope(
                    GDSymbolScopeType.MethodParameter,
                    symbolName,
                    declarationNode: parent as GDNode,
                    containingMethod: method,
                    containingClass: context.ClassDeclaration,
                    containingScript: context.Script);
            }
        }

        // For loop variable
        if (parent is GDForStatement forStmt)
        {
            return new GDSymbolScope(
                GDSymbolScopeType.ForLoopVariable,
                symbolName,
                declarationNode: forStmt,
                containingForLoop: forStmt,
                containingClass: context.ClassDeclaration,
                containingScript: context.Script);
        }

        // Match case variable binding (var x in match patterns)
        if (parent is GDMatchCaseVariableExpression matchCaseVar)
        {
            var matchCase = GDPositionFinder.FindParent<GDMatchCaseDeclaration>(identifier);
            if (matchCase != null)
            {
                return new GDSymbolScope(
                    GDSymbolScopeType.MatchCaseVariable,
                    symbolName,
                    declarationNode: matchCaseVar,
                    containingMatchCase: matchCase,
                    containingClass: context.ClassDeclaration,
                    containingScript: context.Script,
                    declarationLine: identifier.StartLine);
            }
        }

        // Class member (declaration)
        if (parent is GDMethodDeclaration || parent is GDVariableDeclaration ||
            parent is GDSignalDeclaration || parent is GDEnumDeclaration)
        {
            return new GDSymbolScope(
                GDSymbolScopeType.ClassMember,
                symbolName,
                declarationNode: parent as GDNode,
                containingClass: context.ClassDeclaration,
                containingScript: context.Script,
                isPublic: !symbolName.StartsWith("_"));
        }

        // Reference to identifier (not declaration)
        if (parent is GDIdentifierExpression)
        {
            // Check if it's a local variable reference
            var method = GDPositionFinder.FindParent<GDMethodDeclaration>(identifier);
            if (method != null)
            {
                var localDecl = FindLocalVariableDeclaration(symbolName, method, identifier.StartLine);
                if (localDecl != null)
                {
                    return new GDSymbolScope(
                        GDSymbolScopeType.LocalVariable,
                        symbolName,
                        declarationNode: localDecl,
                        containingMethod: method,
                        containingClass: context.ClassDeclaration,
                        containingScript: context.Script,
                        declarationLine: localDecl.StartLine);
                }

                // Check method parameters
                var param = method.Parameters?.OfType<GDParameterDeclaration>()
                    .FirstOrDefault(p => p.Identifier?.Sequence == symbolName);
                if (param != null)
                {
                    return new GDSymbolScope(
                        GDSymbolScopeType.MethodParameter,
                        symbolName,
                        declarationNode: param,
                        containingMethod: method,
                        containingClass: context.ClassDeclaration,
                        containingScript: context.Script);
                }

                // Check if it's a match case variable reference
                var containingMatchCase = GDPositionFinder.FindParent<GDMatchCaseDeclaration>(identifier);
                if (containingMatchCase != null)
                {
                    var matchCaseVarDecl = containingMatchCase.Conditions?.AllNodes
                        .OfType<GDMatchCaseVariableExpression>()
                        .FirstOrDefault(expr => expr.Identifier?.Sequence == symbolName);
                    if (matchCaseVarDecl != null)
                    {
                        return new GDSymbolScope(
                            GDSymbolScopeType.MatchCaseVariable,
                            symbolName,
                            declarationNode: matchCaseVarDecl,
                            containingMatchCase: containingMatchCase,
                            containingClass: context.ClassDeclaration,
                            containingScript: context.Script,
                            declarationLine: matchCaseVarDecl.Identifier?.StartLine ?? 0);
                    }
                }
            }

            // Check if it's a class member reference
            var classMember = FindClassMemberDeclaration(symbolName, context.ClassDeclaration);
            if (classMember != null)
            {
                return new GDSymbolScope(
                    GDSymbolScopeType.ClassMember,
                    symbolName,
                    declarationNode: classMember,
                    containingClass: context.ClassDeclaration,
                    containingScript: context.Script,
                    isPublic: !symbolName.StartsWith("_"));
            }
        }

        // Member access on another type
        if (parent is GDMemberOperatorExpression memberOp && memberOp.Identifier == identifier)
        {
            return new GDSymbolScope(
                GDSymbolScopeType.ExternalMember,
                symbolName,
                memberExpression: memberOp,
                containingClass: context.ClassDeclaration,
                containingScript: context.Script);
        }

        // Default: project-wide symbol
        return new GDSymbolScope(
            GDSymbolScopeType.ProjectWide,
            symbolName,
            containingClass: context.ClassDeclaration,
            containingScript: context.Script);
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
    /// </summary>
    public GDFindReferencesResult FindReferencesForScope(GDRefactoringContext context, GDSymbolScope scope)
    {
        var strictRefs = new List<GDFoundReference>();
        var potentialRefs = new List<GDFoundReference>();

        // Use SemanticModel for all scope types when available
        var semanticModel = context.Script?.Analyzer?.SemanticModel;
        if (semanticModel != null)
        {
            var collected = CollectReferencesViaSemanticModel(semanticModel, scope);
            if (collected != null)
            {
                strictRefs.AddRange(collected.Where(r => r.Confidence == GDReferenceConfidence.Strict));
                // Only include Potential, skip NameMatch (too weak for reference finding)
                potentialRefs.AddRange(collected.Where(r => r.Confidence == GDReferenceConfidence.Potential));
                return GDFindReferencesResult.Succeeded(scope, strictRefs, potentialRefs);
            }
        }

        // Fallback to manual collection only when SemanticModel is not available
        switch (scope.Type)
        {
            case GDSymbolScopeType.LocalVariable:
                CollectLocalVariableReferences(scope, strictRefs);
                break;

            case GDSymbolScopeType.MethodParameter:
                CollectMethodParameterReferences(scope, strictRefs);
                break;

            case GDSymbolScopeType.ForLoopVariable:
                CollectForLoopReferences(scope, strictRefs);
                break;

            case GDSymbolScopeType.MatchCaseVariable:
                CollectMatchCaseReferences(scope, strictRefs);
                break;

            case GDSymbolScopeType.ClassMember:
                CollectClassMemberReferences(scope, strictRefs, potentialRefs);
                break;

            case GDSymbolScopeType.ExternalMember:
                CollectExternalMemberReferences(scope, strictRefs, potentialRefs, context);
                break;

            case GDSymbolScopeType.ProjectWide:
            default:
                CollectProjectWideReferences(scope, strictRefs, potentialRefs);
                break;
        }

        return GDFindReferencesResult.Succeeded(scope, strictRefs, potentialRefs);
    }

    /// <summary>
    /// Collects references using the SemanticModel when available.
    /// Returns null if SemanticModel doesn't have the symbol.
    /// </summary>
    private List<GDFoundReference>? CollectReferencesViaSemanticModel(GDSemanticModel semanticModel, GDSymbolScope scope)
    {
        var symbolInfo = semanticModel.FindSymbol(scope.SymbolName);
        if (symbolInfo == null)
            return null;

        var references = semanticModel.GetReferencesTo(symbolInfo);
        if (references.Count == 0)
            return null;

        var filePath = scope.ContainingScript?.FullPath;
        var results = new List<GDFoundReference>();

        foreach (var gdRef in references)
        {
            var node = gdRef.ReferenceNode;
            if (node == null)
                continue;

            // Find the identifier within the node
            var identifier = FindIdentifierInNode(node, scope.SymbolName);
            if (identifier == null)
                continue;

            var (contextText, hlStart, hlEnd) = GetContextWithHighlight(identifier);
            var refKind = DetermineReferenceKind(identifier);
            var confidenceReason = semanticModel.GetConfidenceReason(identifier) ?? "Resolved via SemanticModel";

            results.Add(new GDFoundReference(
                scope.SymbolName,
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
        if (symbolInfo.DeclarationNode != null)
        {
            var declId = FindIdentifierInNode(symbolInfo.DeclarationNode, scope.SymbolName);
            if (declId != null && !results.Any(r => r.Line == declId.StartLine && r.Column == declId.StartColumn))
            {
                var (contextText, hlStart, hlEnd) = GetContextWithHighlight(declId);
                results.Add(new GDFoundReference(
                    scope.SymbolName,
                    filePath,
                    declId.StartLine,
                    declId.StartColumn,
                    declId.EndColumn,
                    GDReferenceKind.Declaration,
                    GDReferenceConfidence.Strict,
                    symbolInfo.DeclarationNode,
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
    /// </summary>
    private GDIdentifier? FindIdentifierInNode(GDNode node, string name)
    {
        // Check common patterns first for efficiency
        if (node is GDIdentifierExpression idExpr && idExpr.Identifier?.Sequence == name)
            return idExpr.Identifier;

        if (node is GDMemberOperatorExpression memberOp && memberOp.Identifier?.Sequence == name)
            return memberOp.Identifier;

        // Search in all tokens
        return node.AllTokens.OfType<GDIdentifier>().FirstOrDefault(i => i.Sequence == name);
    }

    #region Collection Methods

    private void CollectLocalVariableReferences(GDSymbolScope scope, List<GDFoundReference> references)
    {
        if (scope.ContainingMethod == null) return;

        var filePath = scope.ContainingScript?.FullPath;
        var method = scope.ContainingMethod;
        var symbolName = scope.SymbolName;

        // Find all usages within this method, after the declaration
        foreach (var idExpr in method.AllNodes.OfType<GDIdentifierExpression>()
            .Where(e => e.Identifier?.Sequence == symbolName))
        {
            if (idExpr.StartLine >= scope.DeclarationLine)
            {
                var id = idExpr.Identifier;
                if (id == null) continue;

                var (context, hlStart, hlEnd) = GetContextWithHighlight(id);
                references.Add(new GDFoundReference(
                    symbolName,
                    filePath,
                    id.StartLine,
                    id.StartColumn,
                    id.EndColumn,
                    DetermineReferenceKind(id),
                    GDReferenceConfidence.Strict,
                    idExpr,
                    context,
                    hlStart,
                    hlEnd,
                    "Local variable reference within method scope"));
            }
        }

        // Add the declaration itself
        foreach (var varDecl in method.AllNodes.OfType<GDVariableDeclarationStatement>()
            .Where(v => v.Identifier?.Sequence == symbolName && v.StartLine == scope.DeclarationLine))
        {
            var id = varDecl.Identifier;
            if (id == null) continue;

            references.Add(new GDFoundReference(
                symbolName,
                filePath,
                varDecl.StartLine,
                id.StartColumn,
                id.EndColumn,
                GDReferenceKind.Declaration,
                GDReferenceConfidence.Strict,
                varDecl,
                $"var {symbolName}",
                4,
                4 + symbolName.Length,
                "Local variable declaration"));
        }
    }

    private void CollectMethodParameterReferences(GDSymbolScope scope, List<GDFoundReference> references)
    {
        if (scope.ContainingMethod == null) return;

        var filePath = scope.ContainingScript?.FullPath;
        var method = scope.ContainingMethod;
        var symbolName = scope.SymbolName;

        // Add parameter declaration
        var param = method.Parameters?.OfType<GDParameterDeclaration>()
            .FirstOrDefault(p => p.Identifier?.Sequence == symbolName);
        if (param?.Identifier != null)
        {
            var id = param.Identifier;
            references.Add(new GDFoundReference(
                symbolName,
                filePath,
                param.StartLine,
                id.StartColumn,
                id.EndColumn,
                GDReferenceKind.Declaration,
                GDReferenceConfidence.Strict,
                param,
                $"param {symbolName}",
                6,
                6 + symbolName.Length,
                "Parameter declaration"));
        }

        // Find all usages within the method
        foreach (var idExpr in method.AllNodes.OfType<GDIdentifierExpression>()
            .Where(e => e.Identifier?.Sequence == symbolName))
        {
            var id = idExpr.Identifier;
            if (id == null) continue;

            var (context, hlStart, hlEnd) = GetContextWithHighlight(id);
            references.Add(new GDFoundReference(
                symbolName,
                filePath,
                id.StartLine,
                id.StartColumn,
                id.EndColumn,
                DetermineReferenceKind(id),
                GDReferenceConfidence.Strict,
                idExpr,
                context,
                hlStart,
                hlEnd,
                "Parameter reference within method scope"));
        }
    }

    private void CollectForLoopReferences(GDSymbolScope scope, List<GDFoundReference> references)
    {
        if (scope.ContainingForLoop == null) return;

        var filePath = scope.ContainingScript?.FullPath;
        var forStmt = scope.ContainingForLoop;
        var symbolName = scope.SymbolName;

        // Add for loop variable declaration
        var variable = forStmt.Variable;
        if (variable?.Sequence == symbolName)
        {
            references.Add(new GDFoundReference(
                symbolName,
                filePath,
                variable.StartLine,
                variable.StartColumn,
                variable.EndColumn,
                GDReferenceKind.Declaration,
                GDReferenceConfidence.Strict,
                forStmt,
                $"for {symbolName} in ...",
                4,
                4 + symbolName.Length,
                "For loop variable declaration"));
        }

        // Find usages within the for loop body
        if (forStmt.Statements != null)
        {
            foreach (var idExpr in forStmt.Statements.AllNodes.OfType<GDIdentifierExpression>()
                .Where(e => e.Identifier?.Sequence == symbolName))
            {
                var id = idExpr.Identifier;
                if (id == null) continue;

                var (context, hlStart, hlEnd) = GetContextWithHighlight(id);
                references.Add(new GDFoundReference(
                    symbolName,
                    filePath,
                    id.StartLine,
                    id.StartColumn,
                    id.EndColumn,
                    DetermineReferenceKind(id),
                    GDReferenceConfidence.Strict,
                    idExpr,
                    context,
                    hlStart,
                    hlEnd,
                    "For loop variable reference"));
            }
        }
    }

    private void CollectMatchCaseReferences(GDSymbolScope scope, List<GDFoundReference> references)
    {
        if (scope.ContainingMatchCase == null) return;

        var filePath = scope.ContainingScript?.FullPath;
        var matchCase = scope.ContainingMatchCase;
        var symbolName = scope.SymbolName;

        // 1. Find the declaration (var x) in match case conditions
        var variableBinding = matchCase.Conditions?.AllNodes
            .OfType<GDMatchCaseVariableExpression>()
            .FirstOrDefault(expr => expr.Identifier?.Sequence == symbolName);

        if (variableBinding?.Identifier != null)
        {
            var id = variableBinding.Identifier;
            var (context, hlStart, hlEnd) = GetContextWithHighlight(id);
            references.Add(new GDFoundReference(
                symbolName,
                filePath,
                id.StartLine,
                id.StartColumn,
                id.EndColumn,
                GDReferenceKind.Declaration,
                GDReferenceConfidence.Strict,
                variableBinding,
                context,
                hlStart,
                hlEnd,
                "Match case variable declaration"));
        }

        // 2. Find usages in the guard condition (when clause)
        if (matchCase.GuardCondition != null)
        {
            foreach (var idExpr in matchCase.GuardCondition.AllNodes
                .OfType<GDIdentifierExpression>()
                .Where(e => e.Identifier?.Sequence == symbolName))
            {
                var id = idExpr.Identifier;
                if (id == null) continue;

                var (context, hlStart, hlEnd) = GetContextWithHighlight(id);
                references.Add(new GDFoundReference(
                    symbolName,
                    filePath,
                    id.StartLine,
                    id.StartColumn,
                    id.EndColumn,
                    DetermineReferenceKind(id),
                    GDReferenceConfidence.Strict,
                    idExpr,
                    context,
                    hlStart,
                    hlEnd,
                    "Match case guard condition reference"));
            }
        }

        // 3. Find usages in the match case body (statements)
        if (matchCase.Statements != null)
        {
            foreach (var idExpr in matchCase.Statements.AllNodes
                .OfType<GDIdentifierExpression>()
                .Where(e => e.Identifier?.Sequence == symbolName))
            {
                var id = idExpr.Identifier;
                if (id == null) continue;

                var (context, hlStart, hlEnd) = GetContextWithHighlight(id);
                references.Add(new GDFoundReference(
                    symbolName,
                    filePath,
                    id.StartLine,
                    id.StartColumn,
                    id.EndColumn,
                    DetermineReferenceKind(id),
                    GDReferenceConfidence.Strict,
                    idExpr,
                    context,
                    hlStart,
                    hlEnd,
                    "Match case variable reference"));
            }
        }
    }

    private void CollectClassMemberReferences(
        GDSymbolScope scope,
        List<GDFoundReference> strictRefs,
        List<GDFoundReference> potentialRefs)
    {
        if (scope.ContainingClass == null) return;

        var filePath = scope.ContainingScript?.FullPath;
        var scriptClass = scope.ContainingClass;
        var symbolName = scope.SymbolName;

        // Collect all references within the same script
        foreach (var id in scriptClass.AllTokens.OfType<GDIdentifier>()
            .Where(i => i.Sequence == symbolName))
        {
            var (context, hlStart, hlEnd) = GetContextWithHighlight(id);
            var refNode = id.Parent as GDNode;
            if (refNode == null) continue;
            strictRefs.Add(new GDFoundReference(
                symbolName,
                filePath,
                id.StartLine,
                id.StartColumn,
                id.EndColumn,
                DetermineReferenceKind(id),
                GDReferenceConfidence.Strict,
                refNode,
                context,
                hlStart,
                hlEnd,
                "Class member reference within same file"));
        }

        // For public members, cross-file search would be done via GDCrossFileReferenceFinder
        // This is left for the Plugin/CLI to handle as it requires project context
    }

    private void CollectExternalMemberReferences(
        GDSymbolScope scope,
        List<GDFoundReference> strictRefs,
        List<GDFoundReference> potentialRefs,
        GDRefactoringContext context)
    {
        if (scope.MemberExpression == null) return;

        var symbolName = scope.SymbolName;
        var filePath = scope.ContainingScript?.FullPath;

        // Add the current member access as a reference
        var memberOp = scope.MemberExpression;
        var id = memberOp.Identifier;
        if (id != null)
        {
            var (ctxText, hlStart, hlEnd) = GetContextWithHighlight(id);

            // For external members, type is potentially unknown
            potentialRefs.Add(new GDFoundReference(
                symbolName,
                filePath,
                id.StartLine,
                id.StartColumn,
                id.EndColumn,
                DetermineReferenceKind(id),
                GDReferenceConfidence.Potential,
                memberOp,
                ctxText,
                hlStart,
                hlEnd,
                "External member access - type resolution required"));
        }

        // Full cross-file search requires project context - left for Plugin/CLI
    }

    private void CollectProjectWideReferences(
        GDSymbolScope scope,
        List<GDFoundReference> strictRefs,
        List<GDFoundReference> potentialRefs)
    {
        if (scope.ContainingClass == null) return;

        var filePath = scope.ContainingScript?.FullPath;
        var symbolName = scope.SymbolName;

        // Search in current file as fallback
        foreach (var id in scope.ContainingClass.AllTokens.OfType<GDIdentifier>()
            .Where(i => i.Sequence == symbolName))
        {
            var (context, hlStart, hlEnd) = GetContextWithHighlight(id);
            var refNode = id.Parent as GDNode;
            if (refNode == null) continue;
            potentialRefs.Add(new GDFoundReference(
                symbolName,
                filePath,
                id.StartLine,
                id.StartColumn,
                id.EndColumn,
                DetermineReferenceKind(id),
                GDReferenceConfidence.Potential,
                refNode,
                context,
                hlStart,
                hlEnd,
                "Name-based match - scope undetermined"));
        }
    }

    #endregion

    #region Helper Methods

    private GDVariableDeclarationStatement? FindLocalVariableDeclaration(
        string name, GDMethodDeclaration method, int beforeLine)
    {
        if (method?.Statements == null) return null;

        return method.AllNodes.OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == name && v.StartLine < beforeLine);
    }

    private GDIdentifiableClassMember? FindClassMemberDeclaration(string name, GDClassDeclaration? classDecl)
    {
        if (classDecl == null) return null;

        return classDecl.Members.OfType<GDIdentifiableClassMember>()
            .FirstOrDefault(m => m.Identifier?.Sequence == name);
    }

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
