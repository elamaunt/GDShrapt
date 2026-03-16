using System;
using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<string, Dictionary<string, GDSymbolReferences>> _referencesCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Dictionary<string, List<GDCodeLensReference>>> _sceneReferencesCache = new(StringComparer.OrdinalIgnoreCase);

    public GDCodeLensHandler(GDScriptProject project, GDProjectSemanticModel projectModel)
    {
        _project = project;
        _projectModel = projectModel;
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDCodeLens> GetCodeLenses(string filePath)
    {
        var fileRefsCache = new Dictionary<string, GDSymbolReferences>(StringComparer.Ordinal);
        _referencesCache[filePath] = fileRefsCache;

        if (IsTscnFile(filePath))
            return GetSceneCodeLenses(filePath);

        var script = _project.GetScript(filePath);
        var semanticModel = script?.SemanticModel;
        if (script?.Class == null || semanticModel == null)
            return [];

        var lenses = new List<GDCodeLens>();

        // CodeLens for class_name
        CollectClassNameLens(script, filePath, fileRefsCache, lenses);

        // CodeLens for all class-level members
        CollectMemberLenses(semanticModel, filePath, fileRefsCache, lenses);

        return lenses;
    }

    /// <summary>
    /// Creates a CodeLens for the class_name declaration showing cross-file reference count.
    /// </summary>
    private void CollectClassNameLens(GDScriptFile script, string filePath, Dictionary<string, GDSymbolReferences> fileRefsCache, List<GDCodeLens> lenses)
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

        var (strict, union, bridgeExtra) = CountProjectReferences(typeName, filePath, fileRefsCache);

        var label = FormatReferenceLabel(strict, union, bridgeExtra);
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
    private void CollectMemberLenses(GDSemanticModel semanticModel, string filePath, Dictionary<string, GDSymbolReferences> fileRefsCache, List<GDCodeLens> lenses)
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
            var (strict, union, bridgeExtra) = CountProjectReferences(symbol.Name, filePath, fileRefsCache);

            var label = FormatReferenceLabel(strict, union, bridgeExtra);
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
    private (int strict, int union, int bridgeExtra) CountProjectReferences(string? symbolName, string? filePath, Dictionary<string, GDSymbolReferences> fileRefsCache)
    {
        if (string.IsNullOrEmpty(symbolName))
            return (0, 0, 0);

        var collector = new GDSymbolReferenceCollector(_project, _projectModel);
        var allRefs = collector.CollectAllReferences(symbolName, filePath);
        var result = allRefs.Primary;

        fileRefsCache[symbolName] = result;

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

        int bridgeExtra = 0;
        if (allRefs.IsBridgeConnected && allRefs.PrimaryHierarchyRefCount > 0)
        {
            int totalRelevant = strict + union;
            int primaryRelevant = allRefs.PrimaryHierarchyRefCount;
            bridgeExtra = Math.Max(0, totalRelevant - primaryRelevant);
            strict = Math.Max(0, strict - bridgeExtra);
        }

        return (strict, union, bridgeExtra);
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
        // Check scene node references cache first (keyed by file path, then by node path/signal key)
        if (_sceneReferencesCache.TryGetValue(filePath, out var sceneCache) &&
            sceneCache.TryGetValue(symbolName, out var sceneRefs))
            return sceneRefs;

        if (!_referencesCache.TryGetValue(filePath, out var fileRefs))
            return null;

        if (!fileRefs.TryGetValue(symbolName, out var refs))
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

            // Include Strict, Union, and Potential refs in clickable reference list
            // (Potential refs from bridge files are confirmed duck-typed connections)
            if (!IsRelevantConfidence(r) && r.Confidence != GDReferenceConfidence.Potential)
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
    internal static string FormatReferenceLabel(int strict, int union, int bridgeExtra = 0)
    {
        string label;
        if (bridgeExtra > 0)
        {
            label = $"{strict}+{bridgeExtra} references";
        }
        else
        {
            label = strict switch
            {
                0 => "0 references",
                1 => "1 reference",
                _ => $"{strict} references"
            };
        }
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

        var lenses = new List<GDCodeLens>();
        var sceneCache = new Dictionary<string, List<GDCodeLensReference>>(StringComparer.Ordinal);
        _sceneReferencesCache[filePath] = sceneCache;

        // Collect all unique node names first
        var nodeNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in sceneInfo.Nodes)
        {
            if (!string.IsNullOrEmpty(node.Name) && node.LineNumber > 0)
                nodeNames.Add(node.Name);
        }

        // Collect scripts allowed for this scene (attached + instantiating + sub-scene scripts)
        var allowedScriptPaths = CollectAllowedScriptPaths(resPath, sceneInfo);

        // Build group → node names mapping for group query detection
        var groupToNodes = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var node in sceneInfo.Nodes)
        {
            if (node.Groups == null || string.IsNullOrEmpty(node.Name))
                continue;
            foreach (var group in node.Groups)
            {
                if (!groupToNodes.TryGetValue(group, out var groupList))
                {
                    groupList = new List<string>();
                    groupToNodes[group] = groupList;
                }
                groupList.Add(node.Name);
            }
        }

        // Build script → node path mapping (for parent-path scoping)
        var scriptToNodePath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in sceneInfo.Nodes)
        {
            if (!string.IsNullOrEmpty(node.ScriptPath) && !string.IsNullOrEmpty(node.Path))
                scriptToNodePath.TryAdd(node.ScriptPath, node.Path);
        }

        // Build name → [nodes] for resolving which node a $Path reference targets
        var nodesByName = new Dictionary<string, List<GDNodeTypeInfo>>(StringComparer.Ordinal);
        foreach (var node in sceneInfo.Nodes)
        {
            if (string.IsNullOrEmpty(node.Name) || node.LineNumber <= 0)
                continue;
            if (!nodesByName.TryGetValue(node.Name, out var nameList))
            {
                nameList = new List<GDNodeTypeInfo>();
                nodesByName[node.Name] = nameList;
            }
            nameList.Add(node);
        }

        // Single pass over allowed scripts to find ALL node references at once
        // Result keyed by node PATH (unique) instead of name (may have duplicates)
        var nodeRefsMap = FindAllNodeReferencesInOnePass(
            nodeNames, allowedScriptPaths, groupToNodes, scriptToNodePath, nodesByName);

        foreach (var node in sceneInfo.Nodes)
        {
            if (string.IsNullOrEmpty(node.Name) || node.LineNumber <= 0)
                continue;

            if (!nodeRefsMap.TryGetValue(node.Path, out var refs) || refs.Count == 0)
                continue;

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
            sceneCache[node.Path] = cachedRefs;

            var label = refs.Count == 1 ? "1 reference" : $"{refs.Count} references";
            lenses.Add(new GDCodeLens
            {
                Line = node.LineNumber,
                StartColumn = 1,
                EndColumn = node.Name.Length + 1,
                Label = label,
                CommandName = "gdshrapt.findReferences",
                CommandArgument = node.Path
            });
        }

        // Signal connection CodeLens — lightweight: find method declarations directly
        // instead of full CollectReferences (which does 7 full-project scans per symbol)
        var allowedScriptsCache = new Dictionary<string, List<GDScriptFile>>(StringComparer.OrdinalIgnoreCase);

        foreach (var conn in sceneInfo.SignalConnections)
        {
            if (string.IsNullOrEmpty(conn.Method) || conn.LineNumber <= 0)
                continue;

            var cacheKey = $"signal:{conn.Method}@{conn.LineNumber}";

            // Resolve target script
            GDScriptFile? targetScriptFile = null;
            if (!string.IsNullOrEmpty(conn.ToNode))
            {
                var targetNode = sceneInfo.Nodes.FirstOrDefault(n =>
                    n.Path != null && n.Path.Equals(conn.ToNode, StringComparison.Ordinal));
                if (targetNode?.ScriptPath != null)
                    targetScriptFile = _project.GetScriptByResourcePath(targetNode.ScriptPath);
            }

            // Find method declarations directly in target script + its inheritors
            var cachedRefs = new List<GDCodeLensReference>();
            int count = 0;

            if (targetScriptFile != null)
            {
                var scripts = GetOrBuildAllowedScripts(allowedScriptsCache, targetScriptFile);
                foreach (var script in scripts)
                {
                    var model = script.SemanticModel ?? _projectModel?.GetSemanticModel(script);
                    if (model == null)
                        continue;

                    var symbol = model.FindSymbol(conn.Method);
                    if (symbol?.PositionToken == null || symbol.Kind != GDSymbolKind.Method)
                        continue;

                    count++;
                    cachedRefs.Add(new GDCodeLensReference
                    {
                        FilePath = script.FullPath!,
                        Line = symbol.PositionToken.StartLine + 1,
                        Column = symbol.PositionToken.StartColumn + 1,
                        EndColumn = symbol.PositionToken.StartColumn + 1 + conn.Method.Length,
                        IsDeclaration = true
                    });
                }
            }

            sceneCache[cacheKey] = cachedRefs;

            var connLabel = count switch
            {
                0 => "0 references",
                1 => "1 reference",
                _ => $"{count} references"
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

    private List<GDScriptFile> GetOrBuildAllowedScripts(
        Dictionary<string, List<GDScriptFile>> cache,
        GDScriptFile targetScriptFile)
    {
        if (targetScriptFile.FullPath == null)
            return [targetScriptFile];

        if (cache.TryGetValue(targetScriptFile.FullPath, out var existing))
            return existing;

        var scripts = new List<GDScriptFile> { targetScriptFile };

        if (targetScriptFile.TypeName != null && _projectModel?.TypeSystem != null)
        {
            foreach (var script in _project.ScriptFiles)
            {
                if (script.FullPath != null && script.TypeName != null &&
                    script != targetScriptFile &&
                    _projectModel.TypeSystem.IsAssignableTo(script.TypeName, targetScriptFile.TypeName))
                {
                    scripts.Add(script);
                }
            }
        }

        cache[targetScriptFile.FullPath] = scripts;
        return scripts;
    }

    private HashSet<string> CollectAllowedScriptPaths(string resPath, GDSceneInfo sceneInfo)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in sceneInfo.Nodes)
        {
            if (!string.IsNullOrEmpty(node.ScriptPath))
                allowed.Add(node.ScriptPath);
        }

        if (_projectModel?.SceneFlow == null || _project.SceneTypesProvider == null)
            return allowed;

        foreach (var edge in _projectModel.SceneFlow.GetScenesThatInstantiate(resPath))
        {
            if (!string.IsNullOrEmpty(edge.SourceFile))
                allowed.Add(edge.SourceFile);
        }

        // BFS through sub-scenes to collect their attached scripts
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { resPath };
        var queue = new Queue<string>();
        foreach (var edge in _projectModel.SceneFlow.GetInstantiatedScenes(resPath))
            if (!string.IsNullOrEmpty(edge.TargetScene) && visited.Add(edge.TargetScene))
                queue.Enqueue(edge.TargetScene);

        while (queue.Count > 0)
        {
            var subScenePath = queue.Dequeue();
            var subSceneInfo = _project.SceneTypesProvider.GetSceneInfo(subScenePath);
            if (subSceneInfo == null)
                continue;

            foreach (var node in subSceneInfo.Nodes)
            {
                if (!string.IsNullOrEmpty(node.ScriptPath))
                    allowed.Add(node.ScriptPath);
            }

            foreach (var edge in _projectModel.SceneFlow.GetInstantiatedScenes(subScenePath))
                if (!string.IsNullOrEmpty(edge.TargetScene) && visited.Add(edge.TargetScene))
                    queue.Enqueue(edge.TargetScene);
        }

        return allowed;
    }

    private Dictionary<string, List<GDNodePathReference>> FindAllNodeReferencesInOnePass(
        HashSet<string> nodeNames,
        HashSet<string> allowedScriptPaths,
        Dictionary<string, List<string>> groupToNodes,
        Dictionary<string, string> scriptToNodePath,
        Dictionary<string, List<GDNodeTypeInfo>> nodesByName)
    {
        // Result keyed by node PATH (unique in scene), not by name
        var result = new Dictionary<string, List<GDNodePathReference>>(StringComparer.Ordinal);

        if (nodeNames.Count == 0 || allowedScriptPaths.Count == 0)
            return result;

        foreach (var script in _project.ScriptFiles)
        {
            if (script.Class == null)
                continue;

            var scriptResPath = script.Reference?.ResourcePath;
            if (scriptResPath == null || !allowedScriptPaths.Contains(scriptResPath))
                continue;

            scriptToNodePath.TryGetValue(scriptResPath, out var scriptNodePath);

            foreach (var astNode in script.Class.AllNodes)
            {
                if (astNode is GDGetNodeExpression pathExpr)
                {
                    var pathList = pathExpr.Path;
                    if (pathList == null)
                        continue;

                    int segmentIndex = 0;
                    foreach (var layer in pathList.OfType<GDLayersList>())
                    {
                        foreach (var specifier in layer.OfType<GDPathSpecifier>())
                        {
                            if (specifier.Type == GDPathSpecifierType.Identifier &&
                                specifier.IdentifierValue != null &&
                                nodeNames.Contains(specifier.IdentifierValue))
                            {
                                var name = specifier.IdentifierValue;
                                var nodeRef = new GDNodePathReference
                                {
                                    Type = GDNodePathReference.RefType.GDScript,
                                    FilePath = script.Reference.FullPath,
                                    ResourcePath = script.Reference.ResourcePath,
                                    LineNumber = specifier.StartLine + 1,
                                    NodePath = name,
                                    SegmentIndex = segmentIndex,
                                    PathSpecifier = specifier,
                                    ScriptReference = script.Reference
                                };
                                AddRefToMatchingNodes(result, name, nodeRef, scriptNodePath, nodesByName);
                            }
                            segmentIndex++;
                        }
                    }
                }
                else if (astNode is GDCallExpression call)
                {
                    string? callerName = null;
                    if (call.CallerExpression is GDIdentifierExpression ide)
                        callerName = ide.Identifier?.Sequence;
                    else if (call.CallerExpression is GDMemberOperatorExpression mem)
                        callerName = mem.Identifier?.Sequence;

                    if (callerName == null)
                        continue;

                    var args = call.Parameters?.ToList();
                    if (args == null || args.Count == 0)
                        continue;

                    if (args[0] is not GDStringExpression strExpr)
                        continue;

                    var pathStr = strExpr.String?.Sequence;
                    if (string.IsNullOrEmpty(pathStr))
                        continue;

                    if (callerName == "get_node" || callerName == "get_node_or_null" || callerName == "find_node")
                    {
                        var isFindNode = callerName == "find_node";
                        var parts = pathStr.Split('/');
                        for (int i = 0; i < parts.Length; i++)
                        {
                            if (nodeNames.Contains(parts[i]))
                            {
                                var name = parts[i];
                                var nodeRef = new GDNodePathReference
                                {
                                    Type = GDNodePathReference.RefType.GDScript,
                                    FilePath = script.Reference.FullPath,
                                    ResourcePath = script.Reference.ResourcePath,
                                    LineNumber = strExpr.StartLine + 1,
                                    NodePath = name,
                                    SegmentIndex = i,
                                    ScriptReference = script.Reference
                                };
                                // find_node() searches subtree recursively, no parent scoping
                                AddRefToMatchingNodes(result, name, nodeRef,
                                    isFindNode ? null : scriptNodePath, nodesByName,
                                    matchAll: isFindNode);
                            }
                        }
                    }
                    else if ((callerName == "get_nodes_in_group" || callerName == "get_first_node_in_group")
                             && groupToNodes.Count > 0)
                    {
                        if (groupToNodes.TryGetValue(pathStr, out var groupNodeNames))
                        {
                            foreach (var nodeName in groupNodeNames)
                            {
                                var nodeRef = new GDNodePathReference
                                {
                                    Type = GDNodePathReference.RefType.GDScript,
                                    FilePath = script.Reference.FullPath,
                                    ResourcePath = script.Reference.ResourcePath,
                                    LineNumber = strExpr.StartLine + 1,
                                    NodePath = nodeName,
                                    SegmentIndex = 0,
                                    ScriptReference = script.Reference
                                };
                                // Group queries reference all nodes in the group (no parent scoping)
                                AddRefToMatchingNodes(result, nodeName, nodeRef, null, nodesByName,
                                    matchAll: true);
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    private static void AddRefToMatchingNodes(
        Dictionary<string, List<GDNodePathReference>> result,
        string nodeName,
        GDNodePathReference nodeRef,
        string? scriptNodePath,
        Dictionary<string, List<GDNodeTypeInfo>> nodesByName,
        bool matchAll = false)
    {
        if (!nodesByName.TryGetValue(nodeName, out var candidates))
            return;

        if (scriptNodePath != null)
        {
            // Script is directly attached in this scene — match any node with this name.
            // Parent-path validation for same-name disambiguation:
            if (candidates.Count > 1)
            {
                // Multiple nodes with the same name — find the one under this script's node
                foreach (var candidate in candidates)
                {
                    if (string.Equals(candidate.ParentPath, scriptNodePath, StringComparison.Ordinal))
                    {
                        AddToResult(result, candidate.Path, nodeRef);
                        return;
                    }
                }

                // No exact parent match — try subtree match
                foreach (var candidate in candidates)
                {
                    if (candidate.ParentPath != null &&
                        candidate.ParentPath.StartsWith(scriptNodePath + "/", StringComparison.Ordinal))
                    {
                        AddToResult(result, candidate.Path, nodeRef);
                    }
                }
            }
            else
            {
                // Only one node with this name in the scene — no ambiguity
                AddToResult(result, candidates[0].Path, nodeRef);
            }
        }
        else if (matchAll)
        {
            // find_node() and group queries: match all nodes with this name
            foreach (var candidate in candidates)
            {
                AddToResult(result, candidate.Path, nodeRef);
            }
        }
        // else: script is from a sub-scene or instantiator with no attachment in this scene.
        // $Name and get_node("Name") are relative to the script's own node (in a different scene),
        // so don't match any nodes here.
    }

    private static void AddToResult(
        Dictionary<string, List<GDNodePathReference>> result,
        string nodePath,
        GDNodePathReference nodeRef)
    {
        if (!result.TryGetValue(nodePath, out var list))
        {
            list = new List<GDNodePathReference>();
            result[nodePath] = list;
        }
        list.Add(nodeRef);
    }
}
