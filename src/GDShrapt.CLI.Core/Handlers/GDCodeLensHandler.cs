using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for CodeLens operations.
/// Shows reference counts above class members, similar to Visual Studio Enterprise.
/// </summary>
public class GDCodeLensHandler : IGDCodeLensHandler
{
    protected readonly GDScriptProject _project;
    protected readonly GDProjectSemanticModel _projectModel;
    private Dictionary<string, GDSymbolReferences>? _referencesCache;
    private Dictionary<string, List<GDCodeLensReference>>? _sceneReferencesCache;
    private string? _cachedFilePath;

    public GDCodeLensHandler(GDScriptProject project, GDProjectSemanticModel projectModel)
    {
        _project = project;
        _projectModel = projectModel;
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDCodeLens> GetCodeLenses(string filePath)
    {
        _referencesCache = new Dictionary<string, GDSymbolReferences>(StringComparer.Ordinal);
        _cachedFilePath = filePath;

        if (IsTscnFile(filePath))
            return GetSceneCodeLenses(filePath);

        var script = _project.GetScript(filePath);
        var semanticModel = script?.SemanticModel;
        if (script?.Class == null || semanticModel == null)
            return [];

        var lenses = new List<GDCodeLens>();

        // CodeLens for class_name
        CollectClassNameLens(script, filePath, lenses);

        // CodeLens for all class-level members
        CollectMemberLenses(semanticModel, filePath, lenses);

        return lenses;
    }

    /// <summary>
    /// Creates a CodeLens for the class_name declaration showing cross-file reference count.
    /// </summary>
    private void CollectClassNameLens(GDScriptFile script, string filePath, List<GDCodeLens> lenses)
    {
        var classNameDecl = script.Class?.ClassName;
        if (classNameDecl == null)
            return;

        var identifier = classNameDecl.Identifier;
        if (identifier == null)
            return;

        var typeName = identifier.Sequence;
        if (string.IsNullOrEmpty(typeName))
            return;

        var (strict, union) = CountProjectReferences(typeName, filePath);

        var label = FormatReferenceLabel(strict, union);
        lenses.Add(new GDCodeLens
        {
            Line = identifier.StartLine + 1,
            StartColumn = identifier.StartColumn + 1,
            EndColumn = identifier.StartColumn + typeName.Length + 1,
            Label = label,
            CommandName = "gdshrapt.findReferences",
            CommandArgument = typeName
        });
    }

    /// <summary>
    /// Creates CodeLens items for all class-level members (methods, variables, signals, etc.).
    /// </summary>
    private void CollectMemberLenses(GDSemanticModel semanticModel, string filePath, List<GDCodeLens> lenses)
    {
        // Collect all class-level symbols (skip parameters, iterators, match bindings)
        var classLevelKinds = new HashSet<GDSymbolKind>
        {
            GDSymbolKind.Method,
            GDSymbolKind.Variable,
            GDSymbolKind.Property,
            GDSymbolKind.Signal,
            GDSymbolKind.Constant,
            GDSymbolKind.Enum,
            GDSymbolKind.Class
        };

        foreach (var symbol in semanticModel.Symbols)
        {
            if (!classLevelKinds.Contains(symbol.Kind))
                continue;

            // Skip inherited symbols
            if (symbol.IsInherited)
                continue;

            var posToken = symbol.PositionToken;
            if (posToken == null)
                continue;

            // Count references across the project (hierarchy-aware)
            var (strict, union) = CountProjectReferences(symbol.Name, filePath);

            var label = FormatReferenceLabel(strict, union);
            var nameLength = symbol.Name?.Length ?? 1;

            lenses.Add(new GDCodeLens
            {
                Line = posToken.StartLine + 1,
                StartColumn = posToken.StartColumn + 1,
                EndColumn = posToken.StartColumn + nameLength + 1,
                Label = label,
                CommandName = "gdshrapt.findReferences",
                CommandArgument = symbol.Name
            });
        }
    }

    /// <summary>
    /// Counts references to a symbol using hierarchy-aware GDSymbolReferenceCollector.
    /// Returns separate counts for strict and union references.
    /// Excludes only the original (non-override) declaration in the current file.
    /// </summary>
    private (int strict, int union) CountProjectReferences(string? symbolName, string? filePath)
    {
        if (string.IsNullOrEmpty(symbolName))
            return (0, 0);

        var collector = new GDSymbolReferenceCollector(_project, _projectModel);
        var result = collector.CollectReferences(symbolName, filePath);

        if (_referencesCache != null)
            _referencesCache[symbolName] = result;

        int strict = 0, union = 0;
        foreach (var r in result.References)
        {
            if (IsOwnDeclaration(r, filePath))
                continue;

            if (!IsRelevantConfidence(r))
                continue;

            if (r.Confidence == GDReferenceConfidence.Union)
                union++;
            else
                strict++;
        }
        return (strict, union);
    }

    /// <summary>
    /// Returns true for the original (non-override) declaration of the symbol
    /// in the file where CodeLens is displayed. Override declarations in other
    /// files are counted as references.
    /// </summary>
    private static bool IsOwnDeclaration(GDSymbolReference r, string? filePath)
    {
        if (r.Kind != GDSymbolReferenceKind.Declaration)
            return false;

        if (r.IsOverride)
            return false;

        if (filePath == null || r.FilePath == null)
            return false;

        return r.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRelevantConfidence(GDSymbolReference r)
    {
        return r.Confidence == GDReferenceConfidence.Strict ||
               r.Confidence == GDReferenceConfidence.Union;
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDCodeLensReference>? GetCachedReferences(string symbolName, string filePath)
    {
        if (_cachedFilePath == null)
            return null;

        if (!_cachedFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
            return null;

        // Check scene node references cache first
        if (_sceneReferencesCache != null &&
            _sceneReferencesCache.TryGetValue(symbolName, out var sceneRefs))
            return sceneRefs;

        if (_referencesCache == null)
            return null;

        if (!_referencesCache.TryGetValue(symbolName, out var refs))
            return null;

        var locations = new List<GDCodeLensReference>();
        GDSymbolReference? ownDecl = null;

        foreach (var r in refs.References)
        {
            if (r.FilePath == null)
                continue;

            if (IsOwnDeclaration(r, filePath))
            {
                ownDecl = r;
                continue;
            }

            if (!IsRelevantConfidence(r))
                continue;

            var identToken = r.IdentifierToken ?? ResolveIdentifierFromNode(r.Node);
            var identLine = identToken?.StartLine ?? r.Line;
            var identCol = identToken?.StartColumn ?? r.Column;
            var line1 = identLine + 1;
            var col1 = identCol + 1;
            var endCol1 = col1 + symbolName.Length;

            locations.Add(new GDCodeLensReference
            {
                FilePath = r.FilePath,
                Line = line1,
                Column = col1,
                EndColumn = endCol1,
                IsDeclaration = false
            });
        }

        // Prepend the own declaration so user can navigate to it
        if (ownDecl != null)
        {
            locations.Insert(0, new GDCodeLensReference
            {
                FilePath = ownDecl.FilePath!,
                Line = ownDecl.Line + 1,
                Column = ownDecl.Column + 1,
                EndColumn = ownDecl.Column + 1 + symbolName.Length,
                IsDeclaration = true
            });
        }

        return locations;
    }

    private static GDSyntaxToken? ResolveIdentifierFromNode(GDNode? node)
    {
        if (node is GDIdentifierExpression idExpr)
            return idExpr.Identifier;

        if (node is GDMemberOperatorExpression memberOp)
            return memberOp.Identifier;

        if (node is GDCallExpression callExpr)
        {
            if (callExpr.CallerExpression is GDMemberOperatorExpression callerMemberOp)
                return callerMemberOp.Identifier;
            if (callExpr.CallerExpression is GDIdentifierExpression callerIdExpr)
                return callerIdExpr.Identifier;
        }

        return null;
    }

    /// <summary>
    /// Formats the reference count label with optional union count.
    /// </summary>
    protected static string FormatReferenceLabel(int strict, int union)
    {
        var label = strict switch
        {
            0 => "0 references",
            1 => "1 reference",
            _ => $"{strict} references"
        };
        if (union > 0)
            label += $" (+{union} unions)";
        return label;
    }

    private static bool IsTscnFile(string filePath)
    {
        return filePath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase)
            || filePath.EndsWith(".tres", StringComparison.OrdinalIgnoreCase);
    }

    private IReadOnlyList<GDCodeLens> GetSceneCodeLenses(string filePath)
    {
        var sceneProvider = _project.SceneTypesProvider;
        if (sceneProvider == null)
            return [];

        var resPath = sceneProvider.ToResourcePath(filePath);
        if (string.IsNullOrEmpty(resPath))
            return [];

        var sceneInfo = sceneProvider.GetSceneInfo(resPath);
        if (sceneInfo == null)
            return [];

        var referenceFinder = new GDNodePathReferenceFinder(_project);
        var lenses = new List<GDCodeLens>();
        _sceneReferencesCache = new Dictionary<string, List<GDCodeLensReference>>(StringComparer.Ordinal);

        foreach (var node in sceneInfo.Nodes)
        {
            if (string.IsNullOrEmpty(node.Name) || node.LineNumber <= 0)
                continue;

            var refs = referenceFinder.FindGDScriptReferences(node.Name).ToList();
            if (refs.Count == 0)
                continue;

            // Cache the references for click resolution
            var cachedRefs = new List<GDCodeLensReference>();
            foreach (var r in refs)
            {
                if (r.FilePath == null)
                    continue;

                var col1 = r.PathSpecifier != null ? r.PathSpecifier.StartColumn + 1 : 1;
                cachedRefs.Add(new GDCodeLensReference
                {
                    FilePath = r.FilePath,
                    Line = r.LineNumber,
                    Column = col1,
                    EndColumn = col1 + node.Name.Length,
                    IsDeclaration = false
                });
            }
            _sceneReferencesCache[node.Name] = cachedRefs;

            var label = refs.Count == 1 ? "1 reference" : $"{refs.Count} references";
            lenses.Add(new GDCodeLens
            {
                Line = node.LineNumber,
                StartColumn = 1,
                EndColumn = node.Name.Length + 1,
                Label = label,
                CommandName = "gdshrapt.findReferences",
                CommandArgument = node.Name
            });
        }

        // Signal connection CodeLens — show references to callback method
        foreach (var conn in sceneInfo.SignalConnections)
        {
            if (string.IsNullOrEmpty(conn.Method) || conn.LineNumber <= 0)
                continue;

            var cacheKey = $"signal:{conn.Method}@{conn.LineNumber}";

            var collector = new GDSymbolReferenceCollector(_project, _projectModel);
            var result = collector.CollectReferences(conn.Method);

            int refCount = 0;
            var cachedRefs = new List<GDCodeLensReference>();

            foreach (var r in result.References)
            {
                if (r.FilePath == null)
                    continue;

                if (r.Kind == GDSymbolReferenceKind.SceneSignalConnection
                    && r.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)
                    && r.Line + 1 == conn.LineNumber)
                    continue;

                refCount++;
                cachedRefs.Add(new GDCodeLensReference
                {
                    FilePath = r.FilePath,
                    Line = r.Line + 1,
                    Column = r.Column + 1,
                    EndColumn = r.Column + 1 + conn.Method.Length,
                    IsDeclaration = r.Kind == GDSymbolReferenceKind.Declaration
                });
            }

            _sceneReferencesCache[cacheKey] = cachedRefs;

            var connLabel = refCount switch
            {
                0 => "0 references",
                1 => "1 reference",
                _ => $"{refCount} references"
            };

            lenses.Add(new GDCodeLens
            {
                Line = conn.LineNumber,
                StartColumn = conn.MethodColumn + 1,
                EndColumn = conn.MethodColumn + 1 + conn.Method.Length,
                Label = connLabel,
                CommandName = "gdshrapt.findReferences",
                CommandArgument = cacheKey
            });
        }

        return lenses;
    }
}
