using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Unified service for collecting all references to a symbol across a project.
/// Used by both find-refs and rename commands to ensure any improvement
/// to reference gathering automatically benefits all consumers.
/// </summary>
public class GDSymbolReferenceCollector
{
    private readonly GDScriptProject _project;
    private readonly GDProjectSemanticModel? _projectModel;

    public GDSymbolReferenceCollector(GDScriptProject project, GDProjectSemanticModel? projectModel = null)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _projectModel = projectModel;
    }

    /// <summary>
    /// Collects all references to a symbol by name across the project.
    /// </summary>
    public GDSymbolReferences CollectReferences(string symbolName, string? filterFilePath = null)
    {
        if (string.IsNullOrEmpty(symbolName))
            return Empty(symbolName);

        GDSymbolInfo? symbol = null;
        GDScriptFile? declaringScript = null;

        if (!string.IsNullOrEmpty(filterFilePath))
        {
            var script = _project.GetScript(filterFilePath);
            if (script?.SemanticModel != null)
            {
                symbol = script.SemanticModel.FindSymbol(symbolName);
                if (symbol?.DeclarationNode != null)
                    declaringScript = script;
            }
        }
        else
        {
            foreach (var script in _project.ScriptFiles)
            {
                if (script.SemanticModel == null) continue;
                var sym = script.SemanticModel.FindSymbol(symbolName);
                if (sym?.DeclarationNode != null)
                {
                    symbol = sym;
                    declaringScript = script;
                    break;
                }
            }
        }

        if (symbol == null)
            return Empty(symbolName);

        return CollectReferences(symbol, declaringScript);
    }

    /// <summary>
    /// Collects all references to a symbol, including unrelated same-name symbols
    /// on different inheritance hierarchies.
    /// </summary>
    public GDAllSymbolReferences CollectAllReferences(string symbolName, string? filterFilePath = null)
    {
        if (string.IsNullOrEmpty(symbolName))
            return new GDAllSymbolReferences(Empty(symbolName), Array.Empty<GDSymbolReferences>());

        if (filterFilePath != null)
            filterFilePath = filterFilePath.Replace('\\', '/');

        // Collect all scripts where symbolName is defined as a class member
        var definitions = new List<(GDScriptFile Script, GDSymbolInfo Symbol)>();
        GDScriptFile? localOnlyScript = null;
        GDSymbolInfo? localOnlySymbol = null;

        foreach (var script in _project.ScriptFiles)
        {
            if (script.FullPath == null) continue;

            var model = _projectModel.ResolveModel(script);
            if (model == null) continue;

            var symbol = model.FindSymbol(symbolName);
            if (symbol?.DeclarationNode == null) continue;

            if (IsClassMemberSymbol(symbol))
                definitions.Add((script, symbol));
            else if (localOnlyScript == null)
            {
                localOnlyScript = script;
                localOnlySymbol = symbol;
            }
        }

        // No class member definitions — use local or single collection
        if (definitions.Count == 0)
        {
            var refs = !string.IsNullOrEmpty(filterFilePath)
                ? CollectReferences(symbolName, filterFilePath)
                : (localOnlySymbol != null
                    ? CollectReferences(localOnlySymbol, localOnlyScript)
                    : Empty(symbolName));
            return new GDAllSymbolReferences(refs, Array.Empty<GDSymbolReferences>());
        }

        // Group by inheritance hierarchy
        var hierarchyRoots = FindHierarchyRoots(definitions);

        if (hierarchyRoots.Count == 1)
        {
            var singleRefs = !string.IsNullOrEmpty(filterFilePath)
                ? CollectReferences(symbolName, filterFilePath)
                : CollectReferences(hierarchyRoots[0].Symbol, hierarchyRoots[0].Script);
            return new GDAllSymbolReferences(singleRefs, Array.Empty<GDSymbolReferences>());
        }

        // Multiple hierarchies — build per-hierarchy file sets to exclude cross-contamination
        var hierarchyFiles = new List<HashSet<string>>();
        for (int i = 0; i < hierarchyRoots.Count; i++)
        {
            var rootScript = hierarchyRoots[i].Script;
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (rootScript.FullPath != null)
                files.Add(rootScript.FullPath);
            if (_projectModel?.TypeSystem != null && rootScript.TypeName != null)
            {
                foreach (var script in _project.ScriptFiles)
                {
                    if (script.FullPath != null && script.TypeName != null &&
                        _projectModel.TypeSystem.IsAssignableTo(script.TypeName, rootScript.TypeName))
                        files.Add(script.FullPath);
                }
            }
            hierarchyFiles.Add(files);
        }

        // Pick primary hierarchy: by filterFilePath if provided, else by most files
        int primaryIndex = 0;
        if (!string.IsNullOrEmpty(filterFilePath))
        {
            for (int i = 0; i < hierarchyRoots.Count; i++)
            {
                if (hierarchyFiles[i].Contains(filterFilePath))
                {
                    primaryIndex = i;
                    break;
                }
            }
        }
        else
        {
            int maxFiles = hierarchyFiles[0].Count;
            for (int i = 1; i < hierarchyRoots.Count; i++)
            {
                if (hierarchyFiles[i].Count > maxFiles)
                {
                    maxFiles = hierarchyFiles[i].Count;
                    primaryIndex = i;
                }
            }
        }

        // Collect refs for all hierarchies
        var allHierarchyFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var hf in hierarchyFiles)
            foreach (var f in hf)
                allHierarchyFiles.Add(f);

        var perHierarchyRefs = new List<GDSymbolReferences>();
        var perHierarchyExternalFiles = new List<HashSet<string>>();
        for (int i = 0; i < hierarchyRoots.Count; i++)
        {
            var root = hierarchyRoots[i];
            var refs = CollectReferences(root.Symbol, root.Script);
            perHierarchyRefs.Add(refs);

            var externalFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in refs.References)
            {
                if (r.FilePath != null && !allHierarchyFiles.Contains(r.FilePath)
                    && r.Confidence != GDReferenceConfidence.NameMatch)
                    externalFiles.Add(r.FilePath);
            }
            perHierarchyExternalFiles.Add(externalFiles);
        }

        // Bridge detection: a file outside all hierarchies that appears in refs
        // for 2+ different hierarchies is a bridge (duck-typed call on untyped variable)
        bool hasBridge = false;
        for (int i = 0; i < perHierarchyExternalFiles.Count && !hasBridge; i++)
        {
            foreach (var file in perHierarchyExternalFiles[i])
            {
                for (int j = i + 1; j < perHierarchyExternalFiles.Count; j++)
                {
                    if (perHierarchyExternalFiles[j].Contains(file))
                    {
                        hasBridge = true;
                        break;
                    }
                }
                if (hasBridge) break;
            }
        }

        // Validate bridge via call-site analysis: bridge methods must have call sites
        // with arguments from 2+ hierarchies
        if (hasBridge)
        {
            // Skip expensive call-site validation for common virtual methods
            // (many unrelated classes override _process/_ready — not a real bridge)
            if (hierarchyRoots.Count > 3 || IsBuiltinVirtualMethod(symbolName))
            {
                hasBridge = false;
            }
            else
            {
                var bridgeFileSet = CollectBridgeFiles(perHierarchyExternalFiles);
                hasBridge = ValidateBridgeCallSites(
                    bridgeFileSet, symbolName, hierarchyRoots, perHierarchyRefs);
            }
        }

        if (hasBridge)
        {
            // Bridge detected: merge all hierarchy refs into single Primary
            var seen = new HashSet<(string?, int, int)>();
            var mergedRefs = new List<GDSymbolReference>();
            var mergedWarnings = new List<GDRenameWarning>();
            foreach (var refs in perHierarchyRefs)
            {
                foreach (var r in refs.References)
                    if (seen.Add((r.FilePath, r.Line, r.Column)))
                        mergedRefs.Add(r);
                mergedWarnings.AddRange(refs.StringWarnings);
            }

            // Count only relevant refs from primary hierarchy (Strict+Union, excluding own declaration)
            var primaryDecl = perHierarchyRefs[primaryIndex].DeclaringScript;
            int primaryRefCount = 0;
            foreach (var r in perHierarchyRefs[primaryIndex].References)
            {
                if (r.Kind == GDSymbolReferenceKind.Declaration && !r.IsOverride
                    && r.FilePath != null && primaryDecl?.FullPath != null
                    && r.FilePath.Equals(primaryDecl.FullPath, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (r.Confidence == GDReferenceConfidence.Strict || r.Confidence == GDReferenceConfidence.Union)
                    primaryRefCount++;
            }

            var merged = new GDSymbolReferences(
                perHierarchyRefs[primaryIndex].Symbol,
                perHierarchyRefs[primaryIndex].DeclaringScript,
                mergedRefs, mergedWarnings);
            return new GDAllSymbolReferences(merged, Array.Empty<GDSymbolReferences>(),
                isBridgeConnected: true, primaryHierarchyRefCount: primaryRefCount);
        }

        // No bridge: filter per hierarchy (existing logic)
        var primary = hierarchyRoots[primaryIndex];
        var primaryAllRefs = perHierarchyRefs[primaryIndex];

        var unrelatedFileSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < hierarchyRoots.Count; i++)
        {
            if (i == primaryIndex) continue;
            foreach (var f in hierarchyFiles[i])
                unrelatedFileSet.Add(f);
        }

        var filteredPrimaryRefs = primaryAllRefs.References
            .Where(r => r.FilePath == null || !unrelatedFileSet.Contains(r.FilePath))
            .ToList();
        var primaryRefs = new GDSymbolReferences(primaryAllRefs.Symbol, primaryAllRefs.DeclaringScript, filteredPrimaryRefs, primaryAllRefs.StringWarnings);

        var unrelatedList = new List<GDSymbolReferences>();
        for (int i = 0; i < hierarchyRoots.Count; i++)
        {
            if (i == primaryIndex) continue;
            var myRefs = perHierarchyRefs[i];
            var myFiles = hierarchyFiles[i];
            var filtered = myRefs.References
                .Where(r => r.FilePath == null || myFiles.Contains(r.FilePath))
                .ToList();
            unrelatedList.Add(new GDSymbolReferences(myRefs.Symbol, myRefs.DeclaringScript, filtered, myRefs.StringWarnings));
        }

        return new GDAllSymbolReferences(primaryRefs, unrelatedList);
    }

    /// <summary>
    /// Groups same-named class member definitions by type hierarchy and returns
    /// the root (most-base) definition for each independent hierarchy.
    /// </summary>
    private List<(GDScriptFile Script, GDSymbolInfo Symbol)> FindHierarchyRoots(
        List<(GDScriptFile Script, GDSymbolInfo Symbol)> definitions)
    {
        if (definitions.Count == 1 || _projectModel?.TypeSystem == null)
            return definitions;

        var roots = new List<(GDScriptFile Script, GDSymbolInfo Symbol)>();
        var assigned = new HashSet<int>();

        for (int i = 0; i < definitions.Count; i++)
        {
            if (assigned.Contains(i))
                continue;

            var root = definitions[i];
            var rootType = root.Script.TypeName;

            for (int j = 0; j < definitions.Count; j++)
            {
                if (i == j || assigned.Contains(j))
                    continue;

                var other = definitions[j];
                var otherType = other.Script.TypeName;

                if (rootType == null || otherType == null)
                    continue;

                if (_projectModel.TypeSystem.IsAssignableTo(rootType, otherType))
                {
                    assigned.Add(i);
                    root = other;
                    rootType = otherType;
                }
                else if (_projectModel.TypeSystem.IsAssignableTo(otherType, rootType))
                {
                    assigned.Add(j);
                }
            }

            if (!assigned.Contains(i))
            {
                roots.Add(root);
                assigned.Add(i);
            }
        }

        return roots;
    }

    /// <summary>
    /// Collects all references to a symbol with a known declaring script.
    /// </summary>
    public GDSymbolReferences CollectReferences(GDSymbolInfo symbol, GDScriptFile? declaringScript)
    {
        if (symbol == null)
            return Empty("");

        var refs = new List<GDSymbolReference>();
        var seen = new HashSet<(string? file, int line, int col)>();

        var isClassMember = IsClassMemberSymbol(symbol);

        // Step 1: Per-file references. For class members, search all scripts.
        // For local variables/parameters, only search the declaring script.
        CollectPerFileReferences(symbol, declaringScript, isClassMember, refs, seen);

        // Step 2: Cross-file references (duck-typed, inherited)
        if (_projectModel != null && declaringScript != null && isClassMember)
        {
            CollectCrossFileReferences(symbol, declaringScript, refs, seen);
        }

        // Step 3: Signal connections from SignalConnectionRegistry
        if (_projectModel != null && isClassMember)
        {
            CollectSignalConnectionReferences(symbol.Name, declaringScript, refs, seen);
        }

        // Step 4: Contract string references (has_method, emit_signal, call, etc.)
        if (_projectModel != null && isClassMember)
        {
            CollectContractStringReferences(symbol.Name, declaringScript, refs, seen);
        }

        // Step 5: Reflection pattern references (get_method_list() + call(method.name))
        if (_projectModel != null && isClassMember)
        {
            CollectReflectionSiteReferences(symbol, refs, seen);
        }

        // Step 6: Class_name type usages
        if (_projectModel != null && declaringScript != null && declaringScript.TypeName == symbol.Name)
        {
            CollectTypeUsageReferences(symbol.Name, refs, seen);
        }

        // Step 7: String reference warnings
        var warnings = CollectStringWarnings(symbol);

        return new GDSymbolReferences(symbol, declaringScript, refs, warnings);
    }

    // ========================================
    // Step 1: Per-file references
    // ========================================

    private void CollectPerFileReferences(
        GDSymbolInfo symbol,
        GDScriptFile? declaringScript,
        bool isClassMember,
        List<GDSymbolReference> refs,
        HashSet<(string?, int, int)> seen)
    {
        IEnumerable<GDScriptFile> scripts;
        if (string.IsNullOrEmpty(symbol.Name))
            scripts = Enumerable.Empty<GDScriptFile>();
        else if (!isClassMember && declaringScript != null)
            scripts = new[] { declaringScript };
        else
            scripts = _project.ScriptFiles;

        foreach (var script in scripts)
        {
            var model = _projectModel.ResolveModel(script);
            if (model == null) continue;

            var localSymbol = model.FindSymbol(symbol.Name);
            if (localSymbol == null) continue;

            var hasLocalDeclaration = localSymbol.DeclarationNode != null;
            var isOverride = hasLocalDeclaration && IsOverrideDeclaration(script, localSymbol);
            var isInherited = !hasLocalDeclaration && HasInheritedSymbol(script, symbol.Name);

            // Skip scripts that have their own independent declaration of the same symbol.
            // If a script declares the same name but it's NOT an override and NOT the original
            // declaring script, it's an unrelated symbol (e.g., different class with same var name).
            if (isClassMember && hasLocalDeclaration && !isOverride && script != declaringScript)
                continue;

            var localRefs = model.GetReferencesTo(localSymbol);
            bool foundDeclaration = false;

            foreach (var reference in localRefs)
            {
                var node = reference.ReferenceNode;
                if (node == null) continue;

                // Skip contract strings — collected separately in step 5
                if (node is GDStringExpression or GDStringNameExpression)
                    continue;

                var isDecl = node == localSymbol.DeclarationNode
                          || node == localSymbol.DeclarationIdentifier;
                // Also detect declaration by position match (in case of different AST node instances)
                if (!isDecl && hasLocalDeclaration && localSymbol.DeclarationIdentifier != null)
                {
                    isDecl = node.StartLine == localSymbol.DeclarationIdentifier.StartLine
                          && node.StartColumn == localSymbol.DeclarationIdentifier.StartColumn;
                }

                // For declarations, use identifier position so highlighting covers the name, not the keyword
                var identToken = reference.IdentifierToken as GDSyntaxToken;
                var line = (isDecl && identToken != null) ? identToken.StartLine : node.StartLine;
                var col = (isDecl && identToken != null) ? identToken.StartColumn : node.StartColumn;
                if (!seen.Add((script.FullPath, line, col)))
                    continue;
                if (isDecl)
                {
                    foundDeclaration = true;
                    // Mark both identifier and declaration node positions as seen to prevent duplicates
                    if (localSymbol.DeclarationNode != null)
                        seen.Add((script.FullPath, localSymbol.DeclarationNode.StartLine, localSymbol.DeclarationNode.StartColumn));
                    if (localSymbol.DeclarationIdentifier != null)
                        seen.Add((script.FullPath, localSymbol.DeclarationIdentifier.StartLine, localSymbol.DeclarationIdentifier.StartColumn));
                }
                var kind = isDecl
                    ? GDSymbolReferenceKind.Declaration
                    : reference.IsWrite
                        ? GDSymbolReferenceKind.Write
                        : GDSymbolReferenceKind.Read;

                string? reason = null;
                if (isDecl && isOverride)
                    reason = "Method override in derived class";
                else if (!isDecl && isInherited)
                    reason = $"Inherited member '{symbol.Name}' used directly in derived class";

                refs.Add(new GDSymbolReference(
                    script, node, reference.IdentifierToken,
                    line, col,
                    GDReferenceConfidence.Strict,
                    reason,
                    kind,
                    isInherited: isInherited,
                    isOverride: isDecl && isOverride));
            }

            // Ensure declaration is included even if not already found in references
            if (!foundDeclaration && hasLocalDeclaration && localSymbol.DeclarationNode != null)
            {
                var declIdent = localSymbol.DeclarationIdentifier as GDSyntaxToken;
                // For local variable statements, extract identifier directly
                if (declIdent == null && localSymbol.DeclarationNode is GDVariableDeclarationStatement varDeclStmt)
                    declIdent = varDeclStmt.Identifier;
                // Use identifier position when available, otherwise fall back to declaration node
                var declLine = declIdent?.StartLine ?? localSymbol.DeclarationNode.StartLine;
                var declCol = declIdent?.StartColumn ?? localSymbol.DeclarationNode.StartColumn;
                if (seen.Add((script.FullPath, declLine, declCol)))
                {
                    // Also mark the identifier position to prevent cross-file duplicates
                    if (localSymbol.DeclarationIdentifier != null)
                        seen.Add((script.FullPath, localSymbol.DeclarationIdentifier.StartLine, localSymbol.DeclarationIdentifier.StartColumn));

                    refs.Add(new GDSymbolReference(
                        script, localSymbol.DeclarationNode,
                        declIdent,
                        declLine, declCol,
                        GDReferenceConfidence.Strict,
                        isOverride ? "Method override in derived class" : null,
                        GDSymbolReferenceKind.Declaration,
                        isOverride: isOverride));
                }
            }

            // Super calls (e.g., super.method())
            var extendsType = GetExtendsTypeName(script);
            if (!string.IsNullOrEmpty(extendsType))
            {
                var memberAccesses = model.GetMemberAccesses(extendsType, symbol.Name);
                foreach (var access in memberAccesses)
                {
                    var accessNode = access.ReferenceNode;
                    if (accessNode == null) continue;

                    // Only match actual super.method() calls, not inherited method calls
                    if (accessNode is not GDMemberOperatorExpression memberOp
                        || memberOp.Identifier == null
                        || memberOp.CallerExpression is not GDIdentifierExpression callerIdent
                        || callerIdent.Identifier?.Sequence != "super")
                        continue;

                    var accessLine = memberOp.Identifier.StartLine;
                    var accessCol = memberOp.Identifier.StartColumn;

                    // Remove plain read reference on the same line — super.X replaces it
                    refs.RemoveAll(r =>
                        r.Script == script &&
                        r.Line == accessLine &&
                        r.Kind != GDSymbolReferenceKind.Declaration &&
                        r.Kind != GDSymbolReferenceKind.Override &&
                        r.Kind != GDSymbolReferenceKind.SuperCall &&
                        r.Kind != GDSymbolReferenceKind.Write);

                    seen.Remove((script.FullPath, accessLine, accessCol));

                    if (seen.Add((script.FullPath, accessLine, accessCol)))
                    {
                        refs.Add(new GDSymbolReference(
                            script, accessNode, access.IdentifierToken,
                            accessLine, accessCol,
                            GDReferenceConfidence.Strict,
                            $"super.{symbol.Name}() call in derived class",
                            GDSymbolReferenceKind.SuperCall));
                    }
                }
            }
        }
    }

    // ========================================
    // Step 2: Cross-file references
    // ========================================

    private void CollectCrossFileReferences(
        GDSymbolInfo symbol,
        GDScriptFile declaringScript,
        List<GDSymbolReference> refs,
        HashSet<(string?, int, int)> seen)
    {
        var crossFileFinder = new GDCrossFileReferenceFinder(_project, _projectModel);
        var crossFileRefs = crossFileFinder.FindReferences(symbol, declaringScript);

        foreach (var crossRef in crossFileRefs.StrictReferences.Concat(crossFileRefs.PotentialReferences))
        {
            var node = crossRef.Node;
            if (node == null) continue;

            // Skip contract strings — collected separately
            if (node is GDStringExpression or GDStringNameExpression)
                continue;

            // Use crossRef.Line/Column — the cross-file finder already resolves to identifier position
            int line = crossRef.Line;
            int col = crossRef.Column;

            // Detect kind from the cross-file finder's reason
            var kind = GDSymbolReferenceKind.Read;
            bool isOverride = false;
            bool isInherited = false;
            if (crossRef.Reason != null)
            {
                if (crossRef.Reason.Contains("Method override"))
                {
                    kind = GDSymbolReferenceKind.Override;
                    isOverride = true;
                    isInherited = true;
                }
                else if (crossRef.Reason.Contains("super."))
                {
                    kind = GDSymbolReferenceKind.SuperCall;
                    isInherited = true;
                }
                else
                {
                    isInherited = true;
                }
            }

            var key = (crossRef.Script.FullPath, line, col);
            if (!seen.Add(key))
            {
                // Position already seen from per-file collection.
                // If cross-file finder has a better reason, replace the existing ref.
                if (!string.IsNullOrEmpty(crossRef.Reason))
                {
                    var idx = refs.FindIndex(r =>
                        r.Line == line && r.Column == col &&
                        string.Equals(r.FilePath, crossRef.Script.FullPath, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0 && string.IsNullOrEmpty(refs[idx].ConfidenceReason))
                    {
                        refs[idx] = new GDSymbolReference(
                            crossRef.Script, node, null,
                            line, col,
                            crossRef.Confidence,
                            crossRef.Reason,
                            refs[idx].Kind == GDSymbolReferenceKind.Declaration ? GDSymbolReferenceKind.Declaration : kind,
                            isInherited: isInherited,
                            isOverride: isOverride);
                    }
                }
                continue;
            }

            refs.Add(new GDSymbolReference(
                crossRef.Script, node, null,
                line, col,
                crossRef.Confidence,
                crossRef.Reason,
                kind,
                isInherited: isInherited,
                isOverride: isOverride));
        }
    }

    // ========================================
    // Step 3: Signal connections
    // ========================================

    private void CollectSignalConnectionReferences(
        string symbolName,
        GDScriptFile? declaringScript,
        List<GDSymbolReference> refs,
        HashSet<(string?, int, int)> seen)
    {
        var registry = _projectModel!.SignalConnectionRegistry;

        // Collect class names from existing declarations
        var classNames = refs
            .Where(r => r.Kind == GDSymbolReferenceKind.Declaration && r.Script.TypeName != null)
            .Select(r => r.Script.TypeName!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        // Also try null class name for self-connections
        classNames.Insert(0, null!);

        var connections = new List<GDSignalConnectionEntry>();
        var connSeen = new HashSet<string>();

        foreach (var className in classNames)
        {
            var matching = registry.GetSignalsCallingMethod(className, symbolName);
            foreach (var conn in matching)
            {
                var key = $"{conn.SourceFilePath}:{conn.Line}:{conn.Column}:{conn.SignalName}";
                if (connSeen.Add(key))
                    connections.Add(conn);
            }
        }

        // Build the set of types in the declaring symbol's hierarchy for filtering
        var hierarchyTypes = BuildHierarchyTypeSet(declaringScript);

        foreach (var conn in connections)
        {
            // For self-connections (CallbackClassName=null), verify the source script's type
            // is in the same hierarchy as the declaring type. Without this check,
            // connect(open) in Window subclass would match Door.open.
            if (conn.CallbackClassName == null && hierarchyTypes != null)
            {
                var sourceScript = _project.GetScript(conn.SourceFilePath);
                var sourceType = sourceScript?.TypeName;
                if (sourceType != null && !hierarchyTypes.Contains(sourceType))
                    continue;
            }

            // Scene connections use 1-based line numbers; code connections use 0-based
            var connLine = conn.IsSceneConnection ? conn.Line - 1 : conn.Line;
            var connCol = conn.Column;

            // Use callback identifier position when available (for accurate highlight)
            var refLine = conn.CallbackLine >= 0 ? conn.CallbackLine : connLine;
            var refCol = conn.CallbackColumn >= 0 ? conn.CallbackColumn : connCol;

            seen.Add((conn.SourceFilePath, refLine, refCol));

            // Try to find the corresponding script file.
            // For .tscn scene files, GetScript returns null since they're not GDScript files.
            // Create a lightweight GDScriptFile wrapper so the reference has a valid FilePath.
            var script = _project.GetScript(conn.SourceFilePath)
                ?? (conn.IsSceneConnection ? new GDScriptFile(new GDScriptReference(conn.SourceFilePath)) : null);
            if (script == null) continue;

            // Remove plain read reference on the same line — signal connection replaces it
            refs.RemoveAll(r =>
                r.Script == script &&
                r.Line == refLine &&
                r.Kind != GDSymbolReferenceKind.Declaration &&
                r.Kind != GDSymbolReferenceKind.Override &&
                r.Kind != GDSymbolReferenceKind.SuperCall &&
                r.Kind != GDSymbolReferenceKind.Write);

            refs.Add(new GDSymbolReference(
                script, null, null,
                refLine, refCol,
                conn.Confidence,
                null,
                conn.IsSceneConnection
                    ? GDSymbolReferenceKind.SceneSignalConnection
                    : GDSymbolReferenceKind.SignalConnection,
                callerTypeName: conn.CallbackClassName ?? script.TypeName,
                signalName: conn.SignalName,
                isSceneSignal: conn.IsSceneConnection));
        }
    }

    // ========================================
    // Step 4: Contract string + duck-typed member access references
    // ========================================

    private void CollectContractStringReferences(
        string symbolName,
        GDScriptFile? declaringScript,
        List<GDSymbolReference> refs,
        HashSet<(string?, int, int)> seen)
    {
        var declaringType = declaringScript?.TypeName;

        foreach (var (file, reference) in _projectModel!.GetAllMemberAccessesForMemberInProject(symbolName))
        {
            if (file.FullPath == null) continue;

            // Filter out member accesses on unrelated types.
            // E.g. when collecting refs for Door.open(), skip FileAccess.open().
            var callerType = reference.CallerTypeName;
            if (!string.IsNullOrEmpty(callerType) && !string.IsNullOrEmpty(declaringType))
            {
                if (!_projectModel.TypeSystem.IsAssignableTo(callerType, declaringType) &&
                    !_projectModel.TypeSystem.IsAssignableTo(declaringType, callerType))
                    continue;
            }

            var identToken = reference.IdentifierToken;
            if (identToken == null) continue;

            var isStringRef = identToken is GDStringNode;

            var line = identToken.StartLine;
            var col = identToken.StartColumn;

            var key = (file.FullPath, line, col);
            if (!seen.Add(key))
            {
                // Position already seen from per-file collection.
                // Upgrade with the member access reason when it provides richer context.
                if (!isStringRef && !string.IsNullOrEmpty(reference.ConfidenceReason))
                {
                    var idx = refs.FindIndex(r =>
                        r.Line == line && r.Column == col &&
                        string.Equals(r.FilePath, file.FullPath, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0)
                    {
                        var existing = refs[idx];
                        var existingReason = existing.ConfidenceReason;
                        // Only upgrade refs that have no reason set by the per-file collector.
                        // Preserve specific per-file reasons like "Inherited member 'X' used...",
                        // "super.X() call...", "Method override..." etc.
                        if (string.IsNullOrEmpty(existingReason))
                        {
                            refs[idx] = new GDSymbolReference(
                                file, reference.ReferenceNode, identToken,
                                line, col,
                                reference.Confidence,
                                reference.ConfidenceReason,
                                existing.Kind,
                                callerTypeName: reference.CallerTypeName);
                        }
                    }
                }
                continue;
            }

            refs.Add(new GDSymbolReference(
                file, reference.ReferenceNode, identToken,
                line, col,
                reference.Confidence,
                reference.ConfidenceReason,
                isStringRef ? GDSymbolReferenceKind.ContractString : GDSymbolReferenceKind.Read,
                callerTypeName: reference.CallerTypeName));
        }
    }

    // ========================================
    // Step 5: Reflection pattern references
    // ========================================

    private void CollectReflectionSiteReferences(
        GDSymbolInfo symbol,
        List<GDSymbolReference> refs,
        HashSet<(string?, int, int)> seen)
    {
        var reflectionKind = MapToReflectionKind(symbol.Kind);
        if (reflectionKind == null) return;

        foreach (var script in _project.ScriptFiles)
        {
            if (script.FullPath == null) continue;

            var model = _projectModel!.GetSemanticModel(script);
            if (model == null) continue;

            foreach (var site in model.GetReflectionCallSites())
            {
                if (site.Kind != reflectionKind) continue;
                if (!site.Matches(symbol.Name)) continue;

                var key = (script.FullPath, site.Line, site.Column);
                if (!seen.Add(key)) continue;

                refs.Add(new GDSymbolReference(
                    script, null, null,
                    site.Line, site.Column,
                    GDReferenceConfidence.Potential,
                    $"Reflection pattern: {FormatReflectionListMethod(site.Kind)} + {site.CallMethod}()",
                    GDSymbolReferenceKind.ContractString));
            }
        }
    }

    // ========================================
    // Step 6: Class_name type usages
    // ========================================

    private void CollectTypeUsageReferences(
        string typeName,
        List<GDSymbolReference> refs,
        HashSet<(string?, int, int)> seen)
    {
        foreach (var script in _project.ScriptFiles)
        {
            if (script.FullPath == null) continue;

            var model = _projectModel!.GetSemanticModel(script);
            if (model == null) continue;

            var usages = model.GetTypeUsages(typeName);
            foreach (var usage in usages)
            {
                if (!seen.Add((script.FullPath, usage.Line, usage.Column)))
                    continue;

                refs.Add(new GDSymbolReference(
                    script, usage.Node, null,
                    usage.Line, usage.Column,
                    GDReferenceConfidence.Strict,
                    $"Type usage ({usage.Kind})",
                    GDSymbolReferenceKind.TypeUsage));
            }
        }
    }

    // ========================================
    // Step 7: String warnings
    // ========================================

    private IReadOnlyList<GDRenameWarning> CollectStringWarnings(GDSymbolInfo symbol)
    {
        var warnings = new List<GDRenameWarning>();
        if (_projectModel == null)
            return warnings;

        foreach (var script in _project.ScriptFiles)
        {
            var model = _projectModel.GetSemanticModel(script);
            if (model == null) continue;

            foreach (var w in model.GetStringReferenceWarnings(symbol.Name))
            {
                warnings.Add(new GDRenameWarning(
                    script.FullPath ?? "",
                    w.Node.StartLine + 1,
                    w.Node.StartColumn + 1,
                    w.Reason));
            }
        }

        // Reflection pattern warnings
        var reflectionKind = MapToReflectionKind(symbol.Kind);
        if (reflectionKind != null)
        {
            foreach (var script in _project.ScriptFiles)
            {
                var model = _projectModel.GetSemanticModel(script);
                if (model == null) continue;

                foreach (var site in model.GetReflectionCallSites())
                {
                    if (site.Kind != reflectionKind) continue;
                    if (!site.Matches(symbol.Name)) continue;

                    warnings.Add(new GDRenameWarning(
                        script.FullPath ?? "",
                        site.Line + 1,
                        site.Column + 1,
                        $"Reflection pattern: {FormatReflectionListMethod(site.Kind)} + {site.CallMethod}() may reference '{symbol.Name}' dynamically"));
                }
            }
        }

        return warnings;
    }

    // ========================================
    // Helpers
    // ========================================

    /// <summary>
    /// Builds a set of type names in the declaring script's inheritance hierarchy
    /// (ancestors + descendants). Used to filter self-connections by type.
    /// </summary>
    private HashSet<string>? BuildHierarchyTypeSet(GDScriptFile? declaringScript)
    {
        if (declaringScript == null || _projectModel?.TypeSystem == null)
            return null;

        var declaringType = declaringScript.TypeName;
        if (string.IsNullOrEmpty(declaringType))
            return null;

        var set = new HashSet<string>(StringComparer.Ordinal) { declaringType };

        foreach (var script in _project.ScriptFiles)
        {
            if (script.TypeName == null) continue;
            if (_projectModel.TypeSystem.IsAssignableTo(script.TypeName, declaringType) ||
                _projectModel.TypeSystem.IsAssignableTo(declaringType, script.TypeName))
                set.Add(script.TypeName);
        }

        return set;
    }

    private static bool IsClassMemberSymbol(GDSymbolInfo symbol)
    {
        return symbol.Kind switch
        {
            GDSymbolKind.Method => true,
            GDSymbolKind.Signal => true,
            GDSymbolKind.Variable when symbol.DeclarationNode is GDVariableDeclaration => true,
            GDSymbolKind.Constant when symbol.DeclarationNode is GDVariableDeclaration => true,
            GDSymbolKind.Enum => true,
            GDSymbolKind.EnumValue => true,
            GDSymbolKind.Class => true,
            _ => false
        };
    }

    private bool IsOverrideDeclaration(GDScriptFile script, GDSymbolInfo symbol)
    {
        if (symbol.DeclarationNode == null)
            return false;

        if (symbol.Kind != GDSymbolKind.Method && symbol.Kind != GDSymbolKind.Variable)
            return false;

        var extendsType = GetExtendsTypeName(script);
        if (string.IsNullOrEmpty(extendsType))
            return false;

        return HasMemberInBaseType(extendsType, symbol.Name);
    }

    private bool HasInheritedSymbol(GDScriptFile script, string symbolName)
    {
        var extendsType = GetExtendsTypeName(script);
        if (string.IsNullOrEmpty(extendsType))
            return false;

        return HasMemberInBaseType(extendsType, symbolName);
    }

    private bool HasMemberInBaseType(string typeName, string memberName)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = typeName;

        while (!string.IsNullOrEmpty(current) && visited.Add(current))
        {
            var baseScript = _project.GetScriptByTypeName(current);
            if (baseScript?.SemanticModel != null)
            {
                var baseSymbol = baseScript.SemanticModel.FindSymbol(memberName);
                if (baseSymbol != null && baseSymbol.DeclarationNode != null)
                    return true;

                current = GetExtendsTypeName(baseScript);
                continue;
            }

            var runtimeProvider = GetRuntimeProvider();
            if (runtimeProvider != null)
            {
                var member = runtimeProvider.GetMember(current, memberName);
                if (member != null)
                    return true;

                current = runtimeProvider.GetBaseType(current);
                continue;
            }

            break;
        }

        return false;
    }

    private string? GetExtendsTypeName(GDScriptFile script)
    {
        var model = _projectModel.ResolveModel(script);
        return model?.BaseTypeName;
    }

    private IGDRuntimeProvider? GetRuntimeProvider()
    {
        if (_projectModel != null)
        {
            foreach (var script in _project.ScriptFiles)
            {
                var model = _projectModel.GetSemanticModel(script);
                if (model?.RuntimeProvider != null)
                    return model.RuntimeProvider;
            }
        }

        return _project.ScriptFiles
            .FirstOrDefault(s => s.SemanticModel != null)
            ?.SemanticModel?.RuntimeProvider;
    }

    private static GDReflectionKind? MapToReflectionKind(GDSymbolKind kind)
    {
        return kind switch
        {
            GDSymbolKind.Method => GDReflectionKind.Method,
            GDSymbolKind.Variable => GDReflectionKind.Property,
            GDSymbolKind.Constant => GDReflectionKind.Property,
            GDSymbolKind.Property => GDReflectionKind.Property,
            GDSymbolKind.Signal => GDReflectionKind.Signal,
            _ => null
        };
    }

    private static string FormatReflectionListMethod(GDReflectionKind kind)
    {
        return kind switch
        {
            GDReflectionKind.Method => "get_method_list()",
            GDReflectionKind.Property => "get_property_list()",
            GDReflectionKind.Signal => "get_signal_list()",
            _ => "reflection"
        };
    }

    private static GDSymbolReferences Empty(string? symbolName) =>
        new(
            new GDSymbolInfo(symbolName ?? "", GDSymbolKind.Variable, null, ""),
            null,
            Array.Empty<GDSymbolReference>(),
            Array.Empty<GDRenameWarning>());

    private bool IsBuiltinVirtualMethod(string symbolName)
    {
        if (_projectModel?.TypeSystem == null) return false;

        // Virtual methods inherited from built-in types (Node, Object, etc.)
        // are overridden by many unrelated classes — not a bridge pattern
        var provider = GetRuntimeProvider();
        if (provider == null) return false;

        var member = provider.GetMember("Node", symbolName)
                  ?? provider.GetMember("Object", symbolName)
                  ?? provider.GetMember("Resource", symbolName)
                  ?? provider.GetMember("RefCounted", symbolName);
        return member != null && member.Kind == GDRuntimeMemberKind.Method;
    }

    private static HashSet<string> CollectBridgeFiles(List<HashSet<string>> perHierarchyExternalFiles)
    {
        var bridgeFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < perHierarchyExternalFiles.Count; i++)
            for (int j = i + 1; j < perHierarchyExternalFiles.Count; j++)
                foreach (var f in perHierarchyExternalFiles[i])
                    if (perHierarchyExternalFiles[j].Contains(f))
                        bridgeFiles.Add(f);
        return bridgeFiles;
    }

    private bool ValidateBridgeCallSites(
        HashSet<string> bridgeFiles,
        string symbolName,
        List<(GDScriptFile Script, GDSymbolInfo Symbol)> hierarchyRoots,
        List<GDSymbolReferences> perHierarchyRefs)
    {
        if (_projectModel?.TypeSystem == null) return true;

        foreach (var bridgeFilePath in bridgeFiles)
        {
            var bridgeScript = _project.ScriptFiles
                .FirstOrDefault(s => s.FullPath != null &&
                    s.FullPath.Equals(bridgeFilePath, StringComparison.OrdinalIgnoreCase));
            if (bridgeScript == null) continue;

            var bridgeTypeName = bridgeScript.TypeName;
            if (string.IsNullOrEmpty(bridgeTypeName)) continue;

            var bridgeMethods = FindBridgeMethodsWithDuckRef(
                bridgeFilePath, symbolName, perHierarchyRefs);

            foreach (var methodName in bridgeMethods)
            {
                var coveredHierarchies = new HashSet<int>();

                // Try cached registry first (O(1)), fallback to AST scan
                var registry = _project.CallSiteRegistry;
                if (registry != null)
                {
                    var callers = registry.GetCallersOf(bridgeTypeName, methodName);
                    if (callers.Count == 0) continue;

                    foreach (var caller in callers)
                    {
                        var callExpr = caller.CallExpression;
                        if (callExpr?.Parameters == null) continue;

                        var sourceScript = _project.ScriptFiles
                            .FirstOrDefault(s => s.FullPath != null &&
                                s.FullPath.Equals(caller.SourceFilePath, StringComparison.OrdinalIgnoreCase));
                        var model = sourceScript != null ? _projectModel.ResolveModel(sourceScript) : null;

                        CheckCallArguments(callExpr.Parameters, model, hierarchyRoots, coveredHierarchies);
                        if (coveredHierarchies.Count >= 2) return true;
                    }
                }
                else
                {
                    // Fallback: scan project scripts for calls to bridgeTypeName.methodName
                    bool foundAnyCaller = false;
                    foreach (var script in _project.ScriptFiles)
                    {
                        if (script.Class == null || script.FullPath == null) continue;
                        if (bridgeFiles.Contains(script.FullPath)) continue;

                        var model = _projectModel.ResolveModel(script);
                        if (model == null) continue;

                        foreach (var callExpr in FindCallsToMethod(script.Class, bridgeTypeName, methodName, model))
                        {
                            foundAnyCaller = true;
                            CheckCallArguments(callExpr.Parameters, model, hierarchyRoots, coveredHierarchies);
                            if (coveredHierarchies.Count >= 2) return true;
                        }
                    }
                    if (!foundAnyCaller) continue;
                }
            }
        }

        return false;
    }

    private void CheckCallArguments(
        GDExpressionsList? parameters,
        GDSemanticModel? model,
        List<(GDScriptFile Script, GDSymbolInfo Symbol)> hierarchyRoots,
        HashSet<int> coveredHierarchies)
    {
        if (parameters == null) return;

        foreach (var argExpr in parameters)
        {
            string? argTypeName = null;
            if (model != null)
            {
                var argType = model.InferSemanticTypeForExpression(argExpr);
                if (argType != null && !argType.IsVariant)
                    argTypeName = argType.DisplayName;
            }
            if (string.IsNullOrEmpty(argTypeName)) continue;

            for (int h = 0; h < hierarchyRoots.Count; h++)
            {
                var rootTypeName = hierarchyRoots[h].Script.TypeName;
                if (rootTypeName != null &&
                    _projectModel!.TypeSystem.IsAssignableTo(argTypeName, rootTypeName))
                {
                    coveredHierarchies.Add(h);
                    break;
                }
            }
        }
    }

    private static List<GDCallExpression> FindCallsToMethod(
        GDClassDeclaration classDecl, string typeName, string methodName, GDSemanticModel model)
    {
        var results = new List<GDCallExpression>();
        var visitor = new BridgeCallFinder(typeName, methodName, model, results);
        classDecl.WalkIn(visitor);
        return results;
    }

    private sealed class BridgeCallFinder : GDVisitor
    {
        private readonly string _typeName;
        private readonly string _methodName;
        private readonly GDSemanticModel _model;
        private readonly List<GDCallExpression> _results;

        public BridgeCallFinder(string typeName, string methodName,
            GDSemanticModel model, List<GDCallExpression> results)
        {
            _typeName = typeName;
            _methodName = methodName;
            _model = model;
            _results = results;
        }

        public override void Left(GDCallExpression callExpr)
        {
            // Match pattern: expr.methodName(args) where expr type is bridgeTypeName
            if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp
                && memberOp.Identifier?.Sequence == _methodName
                && memberOp.CallerExpression != null)
            {
                var callerType = _model.InferSemanticTypeForExpression(memberOp.CallerExpression);
                if (callerType != null && !callerType.IsVariant
                    && callerType.DisplayName == _typeName)
                {
                    _results.Add(callExpr);
                }
            }
        }
    }

    private static HashSet<string> FindBridgeMethodsWithDuckRef(
        string bridgeFilePath, string symbolName,
        List<GDSymbolReferences> perHierarchyRefs)
    {
        var methods = new HashSet<string>(StringComparer.Ordinal);
        foreach (var hierarchyRefs in perHierarchyRefs)
        {
            foreach (var r in hierarchyRefs.References)
            {
                if (r.FilePath == null || !r.FilePath.Equals(bridgeFilePath, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (r.Confidence != GDReferenceConfidence.Potential && r.Confidence != GDReferenceConfidence.NameMatch)
                    continue;

                var node = r.Node;
                while (node != null)
                {
                    if (node is GDMethodDeclaration method)
                    {
                        if (method.Identifier?.Sequence != null)
                            methods.Add(method.Identifier.Sequence);
                        break;
                    }
                    node = node.Parent as GDNode;
                }
            }
        }
        return methods;
    }
}
