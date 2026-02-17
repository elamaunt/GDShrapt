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

        // If filtering by file, use standard single-hierarchy collection
        if (!string.IsNullOrEmpty(filterFilePath))
        {
            var refs = CollectReferences(symbolName, filterFilePath);
            return new GDAllSymbolReferences(refs, Array.Empty<GDSymbolReferences>());
        }

        // Collect all scripts where symbolName is defined as a class member
        var definitions = new List<(GDScriptFile Script, GDSymbolInfo Symbol)>();
        GDScriptFile? localOnlyScript = null;
        GDSymbolInfo? localOnlySymbol = null;

        foreach (var script in _project.ScriptFiles)
        {
            if (script.FullPath == null) continue;

            var model = _projectModel?.GetSemanticModel(script) ?? script.SemanticModel;
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
            var refs = localOnlySymbol != null
                ? CollectReferences(localOnlySymbol, localOnlyScript)
                : Empty(symbolName);
            return new GDAllSymbolReferences(refs, Array.Empty<GDSymbolReferences>());
        }

        // Group by inheritance hierarchy
        var hierarchyRoots = FindHierarchyRoots(definitions);

        if (hierarchyRoots.Count == 1)
        {
            var singleRefs = CollectReferences(hierarchyRoots[0].Symbol, hierarchyRoots[0].Script);
            return new GDAllSymbolReferences(singleRefs, Array.Empty<GDSymbolReferences>());
        }

        // Build per-hierarchy file sets to exclude cross-contamination
        var hierarchyFiles = new List<HashSet<string>>();
        for (int i = 0; i < hierarchyRoots.Count; i++)
        {
            var rootScript = hierarchyRoots[i].Script;
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (rootScript.FullPath != null)
                files.Add(rootScript.FullPath);
            // Also include scripts that inherit from this root
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

        // Pick primary hierarchy: the one with the most files (most derived classes)
        int primaryIndex = 0;
        int maxFiles = hierarchyFiles[0].Count;
        for (int i = 1; i < hierarchyRoots.Count; i++)
        {
            if (hierarchyFiles[i].Count > maxFiles)
            {
                maxFiles = hierarchyFiles[i].Count;
                primaryIndex = i;
            }
        }

        var primary = hierarchyRoots[primaryIndex];
        var primaryAllRefs = CollectReferences(primary.Symbol, primary.Script);

        // Filter primary: exclude references from unrelated hierarchy files
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

        // Collect unrelated hierarchies with filtering
        var unrelatedList = new List<GDSymbolReferences>();
        for (int i = 0; i < hierarchyRoots.Count; i++)
        {
            if (i == primaryIndex) continue;
            var root = hierarchyRoots[i];
            var allRefs = CollectReferences(root.Symbol, root.Script);
            var myFiles = hierarchyFiles[i];
            var filtered = allRefs.References
                .Where(r => r.FilePath == null || myFiles.Contains(r.FilePath))
                .ToList();
            unrelatedList.Add(new GDSymbolReferences(allRefs.Symbol, allRefs.DeclaringScript, filtered, allRefs.StringWarnings));
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
        if (_projectModel != null)
        {
            CollectSignalConnectionReferences(symbol.Name, refs, seen);
        }

        // Step 4: Contract string references (has_method, emit_signal, call, etc.)
        if (_projectModel != null)
        {
            CollectContractStringReferences(symbol.Name, refs, seen);
        }

        // Step 5: Class_name type usages
        if (_projectModel != null && declaringScript != null && declaringScript.TypeName == symbol.Name)
        {
            CollectTypeUsageReferences(symbol.Name, refs, seen);
        }

        // Step 6: String reference warnings
        var warnings = CollectStringWarnings(symbol.Name);

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
            var model = _projectModel?.GetSemanticModel(script) ?? script.SemanticModel;
            if (model == null) continue;

            var localSymbol = model.FindSymbol(symbol.Name);
            if (localSymbol == null) continue;

            var hasLocalDeclaration = localSymbol.DeclarationNode != null;
            var isOverride = hasLocalDeclaration && IsOverrideDeclaration(script, localSymbol);
            var isInherited = !hasLocalDeclaration && HasInheritedSymbol(script, symbol.Name);

            var localRefs = model.GetReferencesTo(localSymbol);
            bool foundDeclaration = false;

            foreach (var reference in localRefs)
            {
                var node = reference.ReferenceNode;
                if (node == null) continue;

                // Skip contract strings — collected separately in step 5
                if (node is GDStringExpression or GDStringNameExpression)
                    continue;

                var line = node.StartLine;
                var col = node.StartColumn;
                if (!seen.Add((script.FullPath, line, col)))
                    continue;

                var isDecl = node == localSymbol.DeclarationNode
                          || node == localSymbol.DeclarationIdentifier;
                // Also detect declaration by position match (in case of different AST node instances)
                if (!isDecl && hasLocalDeclaration && localSymbol.DeclarationIdentifier != null)
                {
                    isDecl = line == localSymbol.DeclarationIdentifier.StartLine
                          && col == localSymbol.DeclarationIdentifier.StartColumn;
                }
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
                // Use declaration node position (e.g., "func" keyword) for line/column
                var declLine = localSymbol.DeclarationNode.StartLine;
                var declCol = localSymbol.DeclarationNode.StartColumn;
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

        foreach (var conn in connections)
        {
            // Scene connections use 1-based line numbers; code connections use 0-based
            var connLine = conn.IsSceneConnection ? conn.Line - 1 : conn.Line;
            var connCol = conn.Column;

            if (!seen.Add((conn.SourceFilePath, connLine, connCol)))
                continue;

            // Try to find the corresponding script file.
            // For .tscn scene files, GetScript returns null since they're not GDScript files.
            // Create a lightweight GDScriptFile wrapper so the reference has a valid FilePath.
            var script = _project.GetScript(conn.SourceFilePath)
                ?? (conn.IsSceneConnection ? new GDScriptFile(new GDScriptReference(conn.SourceFilePath)) : null);
            if (script == null) continue;

            // Remove plain read reference on the same line — signal connection replaces it
            refs.RemoveAll(r =>
                r.Script == script &&
                r.Line == connLine &&
                r.Kind != GDSymbolReferenceKind.Declaration &&
                r.Kind != GDSymbolReferenceKind.Override &&
                r.Kind != GDSymbolReferenceKind.SuperCall &&
                r.Kind != GDSymbolReferenceKind.Write);

            refs.Add(new GDSymbolReference(
                script, null, null,
                connLine, connCol,
                conn.Confidence,
                null,
                conn.IsSceneConnection
                    ? GDSymbolReferenceKind.SceneSignalConnection
                    : GDSymbolReferenceKind.SignalConnection,
                callerTypeName: conn.CallbackClassName,
                signalName: conn.SignalName,
                isSceneSignal: conn.IsSceneConnection));
        }
    }

    // ========================================
    // Step 4: Contract string + duck-typed member access references
    // ========================================

    private void CollectContractStringReferences(
        string symbolName,
        List<GDSymbolReference> refs,
        HashSet<(string?, int, int)> seen)
    {
        foreach (var (file, reference) in _projectModel!.GetAllMemberAccessesForMemberInProject(symbolName))
        {
            if (file.FullPath == null) continue;

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

    private IReadOnlyList<GDRenameWarning> CollectStringWarnings(string symbolName)
    {
        var warnings = new List<GDRenameWarning>();
        if (_projectModel == null)
            return warnings;

        foreach (var script in _project.ScriptFiles)
        {
            var model = _projectModel.GetSemanticModel(script);
            if (model == null) continue;

            foreach (var w in model.GetStringReferenceWarnings(symbolName))
            {
                warnings.Add(new GDRenameWarning(
                    script.FullPath ?? "",
                    w.Node.StartLine + 1,
                    w.Node.StartColumn + 1,
                    w.Reason));
            }
        }

        return warnings;
    }

    // ========================================
    // Helpers
    // ========================================

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
        var model = _projectModel?.GetSemanticModel(script) ?? script.SemanticModel;
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

    private static GDSymbolReferences Empty(string? symbolName) =>
        new(
            new GDSymbolInfo(symbolName ?? "", GDSymbolKind.Variable, null, ""),
            null,
            Array.Empty<GDSymbolReference>(),
            Array.Empty<GDRenameWarning>());
}
