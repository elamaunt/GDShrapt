using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for planning and executing rename operations across a GDScript project.
/// </summary>
public class GDRenameService
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    private readonly GDScriptProject _project;
    private readonly GDProjectSemanticModel? _projectModel;
    private readonly IGDRuntimeProvider? _runtimeProvider;

    public GDRenameService(GDScriptProject project, GDProjectSemanticModel? projectModel = null)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _projectModel = projectModel;
        _runtimeProvider = project.CreateRuntimeProvider();
    }

    /// <summary>
    /// Plans a rename operation for a symbol.
    /// </summary>
    /// <param name="symbol">The symbol to rename.</param>
    /// <param name="newName">The new name for the symbol.</param>
    /// <returns>The rename result with all required edits.</returns>
    public GDRenameResult PlanRename(GDSymbolInfo symbol, string newName)
    {
        if (symbol == null)
            return GDRenameResult.Failed("Symbol is null");

        // Validate the new name
        if (!ValidateIdentifier(newName, out var validationError))
            return GDRenameResult.Failed(validationError!);

        // Check for conflicts
        var conflicts = CheckConflicts(symbol, newName);
        if (conflicts.Count > 0)
            return GDRenameResult.WithConflicts(conflicts);

        var oldName = symbol.Name;
        var strictEdits = new List<GDTextEdit>();
        var potentialEdits = new List<GDTextEdit>();
        var filesModified = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Use unified collector for all GDScript references
        var collector = new GDSymbolReferenceCollector(_project, _projectModel);
        var containingScript = FindScriptContainingSymbol(symbol);
        var collectedRefs = collector.CollectReferences(symbol, containingScript);

        // Convert unified references to text edits
        var declaringTypeName = containingScript?.TypeName;
        var typesWithMethod = _runtimeProvider?.FindTypesWithMethod(oldName);

        ConvertRefsToEdits(collectedRefs, oldName, newName, declaringTypeName, typesWithMethod,
            strictEdits, potentialEdits, filesModified);

        // .tscn signal connections: [connection method="oldName"] — rename-specific (edits .tscn files)
        if (!string.IsNullOrEmpty(declaringTypeName))
            CollectTscnEdits(oldName, newName, declaringTypeName!, strictEdits, potentialEdits, filesModified);
        else
            CollectTscnEdits(oldName, newName, strictEdits, filesModified);

        if (strictEdits.Count == 0 && potentialEdits.Count == 0)
            return GDRenameResult.NoOccurrences(oldName);

        // Deduplicate edits
        strictEdits = DeduplicateEdits(strictEdits);
        potentialEdits = DeduplicateEdits(potentialEdits);

        // Sort edits by file, then by position (reverse order for applying)
        var sortedStrict = SortEditsReverse(strictEdits);
        var sortedPotential = SortEditsReverse(potentialEdits);

        return GDRenameResult.SuccessfulWithConfidence(
            sortedStrict, sortedPotential, filesModified.Count, collectedRefs.StringWarnings);
    }

    /// <summary>
    /// Adds GDReferenceLocation references to the edit lists with the specified confidence level.
    /// </summary>
    private static void AddReferencesToEdits(
        IEnumerable<GDReferenceLocation> references,
        List<GDTextEdit> edits,
        HashSet<string> filesModified,
        string oldName,
        string newName,
        GDReferenceConfidence confidence,
        bool skipExistingFiles = false)
    {
        foreach (var r in references)
        {
            var filePath = r.FilePath;
            if (string.IsNullOrEmpty(filePath))
                continue;

            if (skipExistingFiles && filesModified.Contains(filePath))
                continue;

            edits.Add(new GDTextEdit(
                filePath,
                r.Line + 1,
                r.Column + 1,
                oldName,
                newName,
                confidence,
                r.ConfidenceReason));
            filesModified.Add(filePath);
        }
    }

    /// <summary>
    /// Adds GDCrossFileReference references to the edit lists with the specified confidence level.
    /// </summary>
    private static void AddCrossFileRefsToEdits(
        IEnumerable<GDCrossFileReference> references,
        List<GDTextEdit> edits,
        HashSet<string> filesModified,
        string oldName,
        string newName,
        GDReferenceConfidence confidence,
        bool skipExistingFiles = false)
    {
        foreach (var r in references)
        {
            var filePath = r.FilePath;
            if (string.IsNullOrEmpty(filePath))
                continue;

            if (skipExistingFiles && filesModified.Contains(filePath))
                continue;

            edits.Add(new GDTextEdit(
                filePath,
                r.Line + 1,
                r.Column + 1,
                oldName,
                newName,
                confidence,
                r.Reason));
            filesModified.Add(filePath);
        }
    }

    /// <summary>
    /// Adds cross-file references to the edit lists.
    /// </summary>
    private static void AddCrossFileReferencesToEdits(
        GDCrossFileReferenceResult crossFileRefs,
        List<GDTextEdit> strictEdits,
        List<GDTextEdit> potentialEdits,
        HashSet<string> filesModified,
        string oldName,
        string newName,
        bool skipExistingFiles = false)
    {
        AddCrossFileRefsToEdits(crossFileRefs.StrictReferences, strictEdits, filesModified, oldName, newName, GDReferenceConfidence.Strict, skipExistingFiles);
        AddCrossFileRefsToEdits(crossFileRefs.PotentialReferences, potentialEdits, filesModified, oldName, newName, GDReferenceConfidence.Potential, skipExistingFiles);
    }

    /// <summary>
    /// Sorts edits in reverse order (by file, then by line desc, then by column desc).
    /// This ensures edits can be applied safely without shifting positions.
    /// </summary>
    private static List<GDTextEdit> SortEditsReverse(List<GDTextEdit> edits)
    {
        return edits
            .OrderBy(e => e.FilePath)
            .ThenByDescending(e => e.Line)
            .ThenByDescending(e => e.Column)
            .ToList();
    }

    /// <summary>
    /// Plans a rename operation by symbol name.
    /// </summary>
    /// <param name="oldName">Current symbol name.</param>
    /// <param name="newName">New symbol name.</param>
    /// <param name="filterFilePath">Optional file path to limit the search.</param>
    /// <returns>The rename result with all required edits.</returns>
    public GDRenameResult PlanRename(string oldName, string newName, string? filterFilePath = null)
    {
        if (string.IsNullOrEmpty(oldName))
            return GDRenameResult.Failed("Old name is empty");

        // Validate the new name
        if (!ValidateIdentifier(newName, out var validationError))
            return GDRenameResult.Failed(validationError!);

        // If filterFilePath is specified, try to find the symbol and use the full PlanRename(symbol, newName) path
        if (!string.IsNullOrEmpty(filterFilePath))
        {
            var fullPath = Path.GetFullPath(filterFilePath).Replace('\\', '/');
            var targetScript = _project.ScriptFiles
                .FirstOrDefault(f => f.FullPath != null &&
                    f.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase));

            if (targetScript != null)
            {
                var model = _projectModel?.GetSemanticModel(targetScript) ?? targetScript.SemanticModel;
                var symbol = model?.FindSymbol(oldName);

                if (symbol != null)
                    return PlanRename(symbol, newName);

                // Check if this is a class_name
                if (targetScript.TypeName == oldName)
                    return PlanClassNameRename(targetScript, oldName, newName);
            }
        }

        // No filter — find all definitions of oldName, group by type hierarchy,
        // and delegate to PlanRename(GDSymbolInfo) for each independent hierarchy.

        // 1. Check for class_name match first
        foreach (var script in _project.ScriptFiles)
        {
            if (script.FullPath != null && script.TypeName == oldName)
                return PlanClassNameRename(script, oldName, newName);
        }

        // 2. Collect all scripts where oldName is defined as a class member
        var definitions = new List<(GDScriptFile Script, GDSymbolInfo Symbol)>();
        GDScriptFile? localOnlyScript = null;
        GDSymbolInfo? localOnlySymbol = null;

        foreach (var script in _project.ScriptFiles)
        {
            if (script.FullPath == null)
                continue;

            var model = _projectModel?.GetSemanticModel(script) ?? script.SemanticModel;
            if (model == null)
                continue;

            var symbol = model.FindSymbol(oldName);
            if (symbol == null)
                continue;

            if (IsClassMemberSymbol(symbol))
                definitions.Add((script, symbol));
            else if (localOnlyScript == null)
            {
                localOnlyScript = script;
                localOnlySymbol = symbol;
            }
        }

        // 3. If class member definitions found, group by type hierarchy.
        //    Process each hierarchy via PlanRename(GDSymbolInfo) independently.
        //    Return only the hierarchy with the most strict edits (the primary one).
        //    Same-named members on unrelated types are excluded from strict edits.
        //    Then augment with duck-typed/has_method member access references.
        if (definitions.Count > 0)
        {
            var hierarchyRoots = FindHierarchyRoots(definitions);

            // Pick the hierarchy with the most strict edits
            GDRenameResult? bestResult = null;
            foreach (var root in hierarchyRoots)
            {
                var result = PlanRename(root.Symbol, newName);
                if (result.Success && (bestResult == null || result.StrictEdits.Count > bestResult.StrictEdits.Count))
                    bestResult = result;
            }

            if (bestResult == null)
                return GDRenameResult.NoOccurrences(oldName);

            // Augment with duck-typed and has_method() references from member access index
            if (_projectModel != null)
            {
                var strictEdits = new List<GDTextEdit>(bestResult.StrictEdits);
                var potentialEdits = new List<GDTextEdit>(bestResult.PotentialEdits);
                var filesModified = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var edit in strictEdits.Concat(potentialEdits))
                {
                    if (edit.FilePath != null)
                        filesModified.Add(edit.FilePath);
                }

                CollectAllMemberAccessEdits(oldName, newName, strictEdits, potentialEdits, filesModified);

                strictEdits = DeduplicateEdits(strictEdits);
                potentialEdits = DeduplicateEdits(potentialEdits);
                var warnings = CollectStringReferenceWarnings(oldName);

                return GDRenameResult.SuccessfulWithConfidence(
                    SortEditsReverse(strictEdits), SortEditsReverse(potentialEdits), filesModified.Count, warnings);
            }

            return bestResult;
        }

        // 4. Local variable only — single-file edits
        if (localOnlyScript != null && localOnlySymbol != null)
            return PlanRename(localOnlySymbol, newName);

        return GDRenameResult.NoOccurrences(oldName);
    }

    /// <summary>
    /// Plans a rename for a class_name type across the project.
    /// </summary>
    private GDRenameResult PlanClassNameRename(GDScriptFile containingScript, string oldName, string newName)
    {
        var strictEdits = new List<GDTextEdit>();
        var filesModified = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        CollectClassNameEdits(containingScript, oldName, newName, strictEdits, filesModified);

        if (strictEdits.Count == 0)
            return GDRenameResult.NoOccurrences(oldName);

        strictEdits = DeduplicateEdits(strictEdits);
        var sortedStrict = SortEditsReverse(strictEdits);

        return GDRenameResult.SuccessfulWithConfidence(sortedStrict, new List<GDTextEdit>(), filesModified.Count);
    }

    /// <summary>
    /// Plans a rename operation at cursor position using GDRefactoringContext.
    /// This method uses GDFindReferencesService to determine scope and find references.
    /// </summary>
    /// <param name="context">The refactoring context with cursor position.</param>
    /// <param name="newName">The new name for the symbol.</param>
    /// <returns>The rename result with all required edits.</returns>
    public GDRenameResult PlanRenameAtCursor(GDRefactoringContext context, string newName)
    {
        if (context == null)
            return GDRenameResult.Failed("Context is null");

        var findRefsService = new GDFindReferencesService(_project, _projectModel);
        var scope = findRefsService.DetermineSymbolScope(context);

        if (scope == null)
            return GDRenameResult.Failed("No symbol at cursor position");

        return PlanRenameInScope(context, scope, newName);
    }

    /// <summary>
    /// Plans a rename operation for a known symbol scope.
    /// </summary>
    /// <param name="context">The refactoring context.</param>
    /// <param name="scope">The symbol scope determined by GDFindReferencesService.</param>
    /// <param name="newName">The new name for the symbol.</param>
    /// <returns>The rename result with all required edits.</returns>
    public GDRenameResult PlanRenameInScope(GDRefactoringContext context, GDSymbolScope scope, string newName)
    {
        if (context == null)
            return GDRenameResult.Failed("Context is null");

        if (scope == null)
            return GDRenameResult.Failed("Scope is null");

        // Validate the new name
        if (!ValidateIdentifier(newName, out var validationError))
            return GDRenameResult.Failed(validationError!);

        var oldName = scope.SymbolName;

        // Check for reserved keywords
        if (GDNamingUtilities.IsReservedKeyword(newName))
            return GDRenameResult.WithConflicts(new List<GDRenameConflict> {
                new GDRenameConflict(newName, $"'{newName}' is a reserved GDScript keyword", GDRenameConflictType.ReservedKeyword)
            });

        // Find references using the service (delegates to unified collector for cross-file scopes)
        var findRefsService = new GDFindReferencesService(_project, _projectModel);
        var refsResult = findRefsService.FindReferencesForScope(context, scope);

        if (!refsResult.Success)
            return GDRenameResult.Failed(refsResult.ErrorMessage ?? "Failed to find references");

        // Convert references to text edits
        var strictEdits = new List<GDTextEdit>();
        var potentialEdits = new List<GDTextEdit>();
        var filesModified = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddReferencesToEdits(refsResult.StrictReferences, strictEdits, filesModified, oldName, newName, GDReferenceConfidence.Strict);
        AddReferencesToEdits(refsResult.PotentialReferences, potentialEdits, filesModified, oldName, newName, GDReferenceConfidence.Potential);

        if (strictEdits.Count == 0 && potentialEdits.Count == 0)
            return GDRenameResult.NoOccurrences(oldName);

        // Sort edits in reverse order for safe application
        var sortedStrict = SortEditsReverse(strictEdits);
        var sortedPotential = SortEditsReverse(potentialEdits);

        return GDRenameResult.SuccessfulWithConfidence(sortedStrict, sortedPotential, filesModified.Count);
    }

    /// <summary>
    /// Checks for naming conflicts before rename.
    /// </summary>
    /// <param name="symbol">The symbol being renamed.</param>
    /// <param name="newName">The proposed new name.</param>
    /// <returns>List of conflicts, empty if none.</returns>
    public IReadOnlyList<GDRenameConflict> CheckConflicts(GDSymbolInfo symbol, string newName)
    {
        var conflicts = new List<GDRenameConflict>();

        // Check reserved keywords
        if (GDNamingUtilities.IsReservedKeyword(newName))
        {
            conflicts.Add(new GDRenameConflict(
                newName,
                $"'{newName}' is a reserved GDScript keyword",
                GDRenameConflictType.ReservedKeyword));
        }

        // Check built-in types
        if (GDNamingUtilities.IsBuiltInType(newName))
        {
            conflicts.Add(new GDRenameConflict(
                newName,
                $"'{newName}' is a built-in type name",
                GDRenameConflictType.BuiltInType));
        }

        // Find the script containing this symbol
        var containingScript = FindScriptContainingSymbol(symbol);
        if (containingScript?.SemanticModel == null)
            return conflicts;

        // Check if new name already exists in the same scope
        var existingSymbol = containingScript.SemanticModel.FindSymbol(newName);
        if (existingSymbol != null && existingSymbol != symbol)
        {
            conflicts.Add(new GDRenameConflict(
                newName,
                $"A symbol named '{newName}' already exists",
                GDRenameConflictType.NameAlreadyExists,
                existingSymbol));
        }

        return conflicts;
    }

    /// <summary>
    /// Validates that a name is a valid GDScript identifier.
    /// Delegates to GDNamingUtilities for consistent validation.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <param name="errorMessage">Error message if invalid.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public bool ValidateIdentifier(string name, out string? errorMessage)
    {
        return GDNamingUtilities.ValidateIdentifier(name, out errorMessage);
    }

    /// <summary>
    /// Applies edits to a file content string.
    /// </summary>
    /// <param name="content">The original file content.</param>
    /// <param name="edits">The edits to apply (must be sorted in reverse order).</param>
    /// <returns>The modified content.</returns>
    public string ApplyEdits(string content, IEnumerable<GDTextEdit> edits)
    {
        // Detect original line ending style
        var lineEnding = content.Contains("\r\n") ? "\r\n" : "\n";

        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();

        foreach (var edit in edits)
        {
            if (edit.Line < 1 || edit.Line > lines.Count)
                continue;

            var lineIndex = edit.Line - 1;
            var line = lines[lineIndex];
            var column = edit.Column - 1;

            if (column < 0 || column >= line.Length)
                continue;

            // Find the identifier at this position
            var endColumn = column + edit.OldText.Length;
            if (endColumn > line.Length)
                continue;

            var found = line.Substring(column, edit.OldText.Length);
            if (found != edit.OldText)
                continue;

            // Replace
            lines[lineIndex] = line.Substring(0, column) + edit.NewText + line.Substring(endColumn);
        }

        return string.Join(lineEnding, lines);
    }

    /// <summary>
    /// Applies edits directly to a file.
    /// </summary>
    /// <param name="filePath">The file to modify.</param>
    /// <param name="edits">The edits to apply.</param>
    public void ApplyEditsToFile(string filePath, IEnumerable<GDTextEdit> edits)
    {
        var content = File.ReadAllText(filePath, Utf8NoBom);
        var modified = ApplyEdits(content, edits);
        File.WriteAllText(filePath, modified, Utf8NoBom);
    }

    #region Private helpers

    /// <summary>
    /// Converts unified GDSymbolReferences into GDTextEdit lists for rename.
    /// Handles confidence classification and provenance enrichment for duck-typed references.
    /// </summary>
    private void ConvertRefsToEdits(
        GDSymbolReferences collectedRefs,
        string oldName,
        string newName,
        string? declaringTypeName,
        IReadOnlyList<string>? typesWithMethod,
        List<GDTextEdit> strictEdits,
        List<GDTextEdit> potentialEdits,
        HashSet<string> filesModified)
    {
        // Build a set of file paths that are in a different type hierarchy than the declaring script.
        // References from these files should be excluded from strict edits.
        var unrelatedFiles = BuildUnrelatedFilesSet(collectedRefs, declaringTypeName);

        foreach (var sref in collectedRefs.References)
        {
            if (sref.FilePath == null) continue;

            // Signal connections in GDScript are informational for find-refs; rename edits .tscn directly
            if (sref.IsSignalConnection)
                continue;

            // References from unrelated hierarchies: skip strict, keep potential
            var isFromUnrelatedHierarchy = unrelatedFiles.Contains(sref.FilePath);

            // Determine the identifier token for precise column placement
            var identToken = sref.IdentifierToken;
            int line, col;

            if (identToken != null)
            {
                line = identToken.StartLine + 1;
                col = identToken is GDStringNode ? identToken.StartColumn + 2 : identToken.StartColumn + 1;
            }
            else
            {
                // sref.Line/Column already point to the identifier position (set by collector)
                line = sref.Line + 1;
                col = sref.Column + 1;
            }

            var isContractString = sref.IsContractString;

            if (isContractString)
            {
                // Contract string references need type-filtered enrichment
                var callerType = sref.CallerTypeName;
                var isUnknown = string.IsNullOrEmpty(callerType)
                    || callerType == GDWellKnownTypes.Variant
                    || callerType == GDWellKnownTypes.Object;

                if (!isUnknown && !string.IsNullOrEmpty(declaringTypeName))
                {
                    if (!IsTypeCompatible(callerType!, declaringTypeName))
                        continue;

                    // From unrelated hierarchy: skip strict contract strings
                    if (isFromUnrelatedHierarchy && sref.Confidence == GDReferenceConfidence.Strict)
                        continue;

                    var targetEdits = sref.Confidence == GDReferenceConfidence.Strict
                        ? strictEdits : potentialEdits;
                    targetEdits.Add(new GDTextEdit(
                        sref.FilePath, line, col, oldName, newName,
                        sref.Confidence, sref.ConfidenceReason)
                    {
                        IsContractString = true
                    });
                }
                else
                {
                    // Duck-typed contract string — enriched reason + provenance
                    var reason = sref.ConfidenceReason ?? "Duck-typed access";
                    IReadOnlyList<GDTypeProvenanceEntry>? provenance = null;
                    string? provenanceVarName = null;

                    // Build provenance if we have the original reference data
                    if (identToken != null)
                    {
                        var file = sref.Script;
                        var refNode = sref.Node;
                        if (refNode != null && file != null)
                        {
                            var gdRef = new GDReference
                            {
                                ReferenceNode = refNode,
                                IdentifierToken = identToken,
                                Confidence = sref.Confidence,
                                ConfidenceReason = sref.ConfidenceReason,
                                CallerTypeName = sref.CallerTypeName
                            };
                            provenance = BuildDuckTypeProvenance(file, gdRef, oldName,
                                declaringTypeName ?? "", typesWithMethod);
                            provenanceVarName = ExtractVariableName(gdRef);
                        }
                    }

                    potentialEdits.Add(new GDTextEdit(
                        sref.FilePath, line, col, oldName, newName,
                        GDReferenceConfidence.Potential, reason)
                    {
                        DetailedProvenance = provenance,
                        ProvenanceVariableName = provenanceVarName,
                        IsContractString = true
                    });
                }
                filesModified.Add(sref.FilePath);
            }
            else
            {
                // From unrelated hierarchy: skip strict references, keep potential (duck-typed)
                if (isFromUnrelatedHierarchy && sref.Confidence == GDReferenceConfidence.Strict)
                    continue;

                // Regular references: declaration, read, write, super call, type usage, override
                var targetEdits = sref.Confidence == GDReferenceConfidence.Strict
                    ? strictEdits : potentialEdits;

                // Duck-typed cross-file references get provenance
                IReadOnlyList<GDTypeProvenanceEntry>? editProvenance = null;
                string? editProvenanceVar = null;
                if (sref.Confidence != GDReferenceConfidence.Strict && sref.Node != null)
                {
                    var gdRef = new GDReference
                    {
                        ReferenceNode = sref.Node,
                        IdentifierToken = identToken,
                        Confidence = sref.Confidence,
                        ConfidenceReason = sref.ConfidenceReason,
                        CallerTypeName = sref.CallerTypeName
                    };
                    editProvenance = BuildDuckTypeProvenance(
                        sref.Script, gdRef, oldName, declaringTypeName ?? "", typesWithMethod);
                    editProvenanceVar = ExtractVariableName(gdRef);
                }

                targetEdits.Add(new GDTextEdit(
                    sref.FilePath, line, col, oldName, newName,
                    sref.Confidence, sref.ConfidenceReason)
                {
                    DetailedProvenance = editProvenance,
                    ProvenanceVariableName = editProvenanceVar
                });
                filesModified.Add(sref.FilePath);
            }
        }
    }

    /// <summary>
    /// Collects edits for class_name rename across the project using type usages.
    /// </summary>
    private HashSet<string> BuildUnrelatedFilesSet(GDSymbolReferences collectedRefs, string? declaringTypeName)
    {
        var unrelated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var declaringScript = collectedRefs.DeclaringScript;
        if (declaringScript?.FullPath == null || string.IsNullOrEmpty(declaringTypeName))
            return unrelated;

        // Collect scripts with independent declarations (not the declaring script itself)
        var otherDeclarationScripts = collectedRefs.References
            .Where(r => r.FilePath != null
                && r.Kind == GDSymbolReferenceKind.Declaration
                && !string.Equals(r.FilePath, declaringScript.FullPath, StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Script)
            .Distinct()
            .ToList();

        var typeSystem = _projectModel?.TypeSystem;

        foreach (var otherScript in otherDeclarationScripts)
        {
            if (otherScript.FullPath == null) continue;
            var otherType = otherScript.TypeName;

            // If no type info, or not in same hierarchy, mark as unrelated
            if (string.IsNullOrEmpty(otherType) || typeSystem == null)
            {
                unrelated.Add(otherScript.FullPath);
                continue;
            }

            var inSameHierarchy =
                typeSystem.IsAssignableTo(otherType, declaringTypeName!) ||
                typeSystem.IsAssignableTo(declaringTypeName!, otherType);

            if (!inSameHierarchy)
                unrelated.Add(otherScript.FullPath);
        }

        // Also mark files that only have references (no declaration) but belong to unrelated hierarchies.
        // Files with references but no declaration: their script type should be in the declaring type's hierarchy.
        var filesWithDecl = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        filesWithDecl.Add(declaringScript.FullPath);
        foreach (var sref in collectedRefs.References)
        {
            if (sref.FilePath != null && sref.Kind == GDSymbolReferenceKind.Declaration)
                filesWithDecl.Add(sref.FilePath);
        }

        // For files that have references but no declaration, check if they're related
        var refOnlyFiles = collectedRefs.References
            .Where(r => r.FilePath != null && !filesWithDecl.Contains(r.FilePath))
            .Select(r => r.Script)
            .Distinct()
            .ToList();

        foreach (var refScript in refOnlyFiles)
        {
            if (refScript.FullPath == null) continue;
            var refType = refScript.TypeName;
            if (string.IsNullOrEmpty(refType) || typeSystem == null) continue;

            var inSameHierarchy =
                typeSystem.IsAssignableTo(refType, declaringTypeName!) ||
                typeSystem.IsAssignableTo(declaringTypeName!, refType);

            if (!inSameHierarchy)
                unrelated.Add(refScript.FullPath);
        }

        return unrelated;
    }

    private void CollectClassNameEdits(
        GDScriptFile containingScript,
        string oldName,
        string newName,
        List<GDTextEdit> strictEdits,
        HashSet<string> filesModified)
    {
        if (_projectModel == null)
            return;

        // class_name declaration itself
        var classNameIdent = containingScript.Class?.ClassName?.Identifier;
        if (classNameIdent != null && containingScript.FullPath != null)
        {
            strictEdits.Add(new GDTextEdit(
                containingScript.FullPath,
                classNameIdent.StartLine + 1,
                classNameIdent.StartColumn + 1,
                oldName,
                newName,
                GDReferenceConfidence.Strict,
                "class_name declaration"));
            filesModified.Add(containingScript.FullPath);
        }

        // Find type usages across all project files
        foreach (var script in _project.ScriptFiles)
        {
            if (script.FullPath == null)
                continue;

            var model = _projectModel.GetSemanticModel(script);
            if (model == null)
                continue;

            var usages = model.GetTypeUsages(oldName);
            foreach (var usage in usages)
            {
                strictEdits.Add(new GDTextEdit(
                    script.FullPath,
                    usage.Line + 1,
                    usage.Column + 1,
                    oldName,
                    newName,
                    GDReferenceConfidence.Strict,
                    $"Type usage ({usage.Kind}) in {System.IO.Path.GetFileName(script.FullPath)}"));
                filesModified.Add(script.FullPath);
            }
        }
    }

    /// <summary>
    /// Removes duplicate edits at the same file/line/column position.
    /// </summary>
    private static List<GDTextEdit> DeduplicateEdits(List<GDTextEdit> edits)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<GDTextEdit>(edits.Count);

        foreach (var edit in edits)
        {
            var key = $"{edit.FilePath}|{edit.Line}:{edit.Column}";
            if (seen.Add(key))
                result.Add(edit);
        }

        return result;
    }

    private GDScriptFile? FindScriptContainingSymbol(GDSymbolInfo symbol)
    {
        foreach (var script in _project.ScriptFiles)
        {
            if (script.SemanticModel == null)
                continue;

            if (script.SemanticModel.Symbols.Contains(symbol))
                return script;
        }
        return null;
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

            // Find the root of the hierarchy containing this definition
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
                    // otherType is a base of rootType → other is more-base
                    assigned.Add(i);
                    root = other;
                    rootType = otherType;
                }
                else if (_projectModel.TypeSystem.IsAssignableTo(otherType, rootType))
                {
                    // rootType is a base of otherType → mark other as covered
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

    private List<GDTextEdit> CollectEditsFromScript(GDScriptFile script, GDSymbolInfo symbol, string oldName, string newName)
    {
        var edits = new List<GDTextEdit>();
        var semanticModel = script.SemanticModel;
        var filePath = script.FullPath;

        if (semanticModel == null || filePath == null)
            return edits;

        // Add declaration
        if (symbol.DeclarationIdentifier != null)
        {
            edits.Add(new GDTextEdit(filePath,
                symbol.DeclarationIdentifier.StartLine + 1,
                symbol.DeclarationIdentifier.StartColumn + 1,
                oldName, newName));
        }

        // Add all references
        var refs = semanticModel.GetReferencesTo(symbol);
        foreach (var reference in refs)
        {
            if (reference.ReferenceNode == null)
                continue;

            // Skip if it's the declaration (already added)
            if (reference.ReferenceNode == symbol.DeclarationNode)
                continue;

            var identToken = reference.IdentifierToken;
            if (identToken == null)
                continue;

            edits.Add(new GDTextEdit(filePath,
                identToken.StartLine + 1,
                identToken.StartColumn + 1,
                oldName, newName));
        }

        return edits;
    }

    private List<GDTextEdit> CollectEditsFromScriptByName(GDScriptFile script, string oldName, string newName)
    {
        var edits = new List<GDTextEdit>();
        var semanticModel = script.SemanticModel;
        var filePath = script.FullPath;

        if (semanticModel == null || filePath == null)
            return edits;

        var symbol = semanticModel.FindSymbol(oldName);
        if (symbol == null)
            return edits;

        return CollectEditsFromScript(script, symbol, oldName, newName);
    }

    /// <summary>
    /// Collects edits from ALL member access patterns for a given member name across the project,
    /// regardless of caller type. Ensures super.method() calls are found even when
    /// the caller type differs from script.TypeName.
    /// </summary>
    private void CollectAllMemberAccessEdits(
        string oldName,
        string newName,
        List<GDTextEdit> strictEdits,
        List<GDTextEdit> potentialEdits,
        HashSet<string> filesModified)
    {
        foreach (var (file, reference) in _projectModel!.GetAllMemberAccessesForMemberInProject(oldName))
        {
            if (file.FullPath == null)
                continue;

            var identToken = reference.IdentifierToken;
            if (identToken == null)
                continue;

            // String literal tokens (e.g., has_method("name")) need +1 column offset for the opening quote
            var columnOffset = identToken is GDStringNode ? 2 : 1;

            var isContractString = identToken is GDStringNode;

            if (reference.Confidence == GDReferenceConfidence.Strict)
            {
                strictEdits.Add(new GDTextEdit(
                    file.FullPath,
                    identToken.StartLine + 1,
                    identToken.StartColumn + columnOffset,
                    oldName,
                    newName,
                    reference.Confidence,
                    reference.ConfidenceReason)
                {
                    IsContractString = isContractString
                });
            }
            else
            {
                var provenance = BuildDuckTypeProvenance(file, reference, oldName, "", null);
                potentialEdits.Add(new GDTextEdit(
                    file.FullPath,
                    identToken.StartLine + 1,
                    identToken.StartColumn + columnOffset,
                    oldName,
                    newName,
                    GDReferenceConfidence.Potential,
                    reference.ConfidenceReason)
                {
                    DetailedProvenance = provenance,
                    ProvenanceVariableName = ExtractVariableName(reference),
                    IsContractString = isContractString
                });
            }
            filesModified.Add(file.FullPath);
        }
    }

    /// <summary>
    /// Collects string reference warnings (e.g. concatenated strings matching oldName) from all semantic models.
    /// </summary>
    private List<GDRenameWarning> CollectStringReferenceWarnings(string oldName)
    {
        var warnings = new List<GDRenameWarning>();
        if (_projectModel == null)
            return warnings;

        foreach (var script in _project.ScriptFiles)
        {
            var model = _projectModel.GetSemanticModel(script);
            if (model == null)
                continue;

            foreach (var w in model.GetStringReferenceWarnings(oldName))
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

    private void CollectTscnEdits(
        string oldName,
        string newName,
        List<GDTextEdit> strictEdits,
        HashSet<string> filesModified)
    {
        var sceneProvider = _project.SceneTypesProvider;
        if (sceneProvider == null)
            return;

        foreach (var scene in sceneProvider.AllScenes)
        {
            if (string.IsNullOrEmpty(scene.FullPath))
                continue;

            foreach (var conn in scene.SignalConnections)
            {
                if (conn.Method != oldName)
                    continue;

                // Find the column of the method name within the connection line
                // Format: [connection ... method="take_damage" ...]
                var column = FindMethodColumnInTscn(scene.FullPath, conn.LineNumber, oldName);

                strictEdits.Add(new GDTextEdit(
                    scene.FullPath,
                    conn.LineNumber,
                    column,
                    oldName,
                    newName,
                    GDReferenceConfidence.Strict,
                    $".tscn signal connection method=\"{oldName}\""));
                filesModified.Add(scene.FullPath);
            }
        }
    }

    private int FindMethodColumnInTscn(string filePath, int lineNumber, string methodName)
    {
        try
        {
            var lines = File.ReadAllLines(filePath);
            if (lineNumber > 0 && lineNumber <= lines.Length)
            {
                var line = lines[lineNumber - 1];
                var marker = $"method=\"{methodName}\"";
                var idx = line.IndexOf(marker, StringComparison.Ordinal);
                if (idx >= 0)
                    return idx + "method=\"".Length + 1; // 1-based column, inside the quotes
            }
        }
        catch
        {
            // Fall back to column 1 if file can't be read
        }

        return 1;
    }

    private bool IsTypeCompatible(string sourceType, string targetType)
    {
        if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
            return false;

        if (string.Equals(sourceType, targetType, StringComparison.Ordinal))
            return true;

        return _runtimeProvider?.IsAssignableTo(sourceType, targetType) ?? false;
    }

    /// <summary>
    /// Type-filtered version: collects member access edits only for references
    /// where the caller type is compatible with the declaring type.
    /// Duck-typed references (Variant/Object/unknown) go to potential with enriched reasons.
    /// </summary>
    private void CollectAllMemberAccessEdits(
        string oldName,
        string newName,
        string declaringTypeName,
        List<GDTextEdit> strictEdits,
        List<GDTextEdit> potentialEdits,
        HashSet<string> filesModified)
    {
        var typesWithMethod = _runtimeProvider?.FindTypesWithMethod(oldName);

        foreach (var (file, reference) in _projectModel!.GetAllMemberAccessesForMemberInProject(oldName))
        {
            if (file.FullPath == null)
                continue;

            var identToken = reference.IdentifierToken;
            if (identToken == null)
                continue;

            var callerType = reference.CallerTypeName;
            var isUnknown = string.IsNullOrEmpty(callerType)
                || callerType == GDWellKnownTypes.Variant
                || callerType == GDWellKnownTypes.Object;

            // String literal tokens need +1 column offset for the opening quote
            var columnOffset = identToken is GDStringNode ? 2 : 1;
            var isContractString = identToken is GDStringNode;

            if (!isUnknown)
            {
                // Known type — check compatibility
                if (!IsTypeCompatible(callerType!, declaringTypeName))
                    continue;

                var targetEdits = reference.Confidence == GDReferenceConfidence.Strict
                    ? strictEdits : potentialEdits;

                targetEdits.Add(new GDTextEdit(
                    file.FullPath,
                    identToken.StartLine + 1,
                    identToken.StartColumn + columnOffset,
                    oldName,
                    newName,
                    reference.Confidence,
                    reference.ConfidenceReason)
                {
                    IsContractString = isContractString
                });
                filesModified.Add(file.FullPath);
            }
            else
            {
                // Duck-typed — enrich reason with possible types
                var reason = EnrichDuckTypeReason(reference.ConfidenceReason, oldName, typesWithMethod);
                var provenance = BuildDuckTypeProvenance(file, reference, oldName, declaringTypeName, typesWithMethod);

                potentialEdits.Add(new GDTextEdit(
                    file.FullPath,
                    identToken.StartLine + 1,
                    identToken.StartColumn + columnOffset,
                    oldName,
                    newName,
                    GDReferenceConfidence.Potential,
                    reason)
                {
                    DetailedProvenance = provenance,
                    ProvenanceVariableName = ExtractVariableName(reference),
                    IsContractString = isContractString
                });
                filesModified.Add(file.FullPath);
            }
        }
    }

    private string EnrichDuckTypeReason(string? baseReason, string memberName, IReadOnlyList<string>? typesWithMethod)
    {
        return baseReason ?? "Duck-typed access";
    }

    private IReadOnlyList<GDTypeProvenanceEntry>? BuildDuckTypeProvenance(
        GDScriptFile file,
        GDReference reference,
        string memberName,
        string declaringTypeName,
        IReadOnlyList<string>? typesWithMethod)
    {
        var result = new List<GDTypeProvenanceEntry>();

        // Step 1: Extract variable name and find enclosing method
        var varName = ExtractVariableName(reference);
        var method = FindEnclosingMethod(reference.ReferenceNode);

        if (varName == null || method == null)
            return null;

        var enclosingTypeName = file.TypeName;
        var methodName = method.Identifier?.Sequence;

        if (string.IsNullOrEmpty(enclosingTypeName) || string.IsNullOrEmpty(methodName))
            return null;

        // Step 2: Determine if variable is a parameter
        var paramIndex = FindParameterIndex(method, varName);
        var isParameter = paramIndex >= 0;

        if (isParameter)
        {
            // Level 1: Direct call site evidence
            try
            {
                var collector = new GDCallSiteCollector(_project);
                var callSites = collector.CollectCallSites(enclosingTypeName, methodName);

                foreach (var cs in callSites)
                {
                    var arg = cs.GetArgument(paramIndex);
                    if (arg == null) continue;
                    var argType = arg.InferredType?.DisplayName;
                    if (string.IsNullOrEmpty(argType) || argType == "Variant") continue;

                    string evidenceType = argType;
                    string reason = $"arg at {enclosingTypeName}.{methodName}()";

                    // For containers: extract element type
                    if (argType.Contains('['))
                    {
                        var el = GDFlowNarrowingHelper.ExtractElementTypeFromTypeName(argType);
                        if (!string.IsNullOrEmpty(el))
                        {
                            evidenceType = el;
                            reason = $"element of {argType}";
                        }
                    }

                    var innerChain = TraceArgumentOrigin(cs.SourceScript, arg.ExpressionText, arg.Expression);

                    // Try to narrow evidenceType from inner chain (e.g. parameter Node2D → call site passes Area2D)
                    var narrowed = TryNarrowTypeFromChain(innerChain, evidenceType);
                    if (narrowed != null)
                        evidenceType = narrowed;

                    var callSiteEntries = new List<GDCallSiteProvenanceEntry>
                    {
                        new GDCallSiteProvenanceEntry(cs.FilePath, cs.Line + 1, arg.ExpressionText, innerChain)
                    };

                    result.Add(new GDTypeProvenanceEntry(
                        evidenceType, reason, cs.Line,
                        callSites: callSiteEntries,
                        sourceFilePath: cs.FilePath));
                }
            }
            catch
            {
                // Call site collection may fail
            }

            // Level 2: Signal callback parameter evidence
            if (_projectModel != null)
            {
                try
                {
                    var connections = _projectModel.SignalConnectionRegistry
                        .GetSignalsCallingMethod(enclosingTypeName, methodName);

                    foreach (var conn in connections)
                    {
                        if (string.IsNullOrEmpty(conn.EmitterType)) continue;

                        var signalParams = GetSignalParameterTypes(conn.EmitterType, conn.SignalName);
                        if (signalParams != null && paramIndex < signalParams.Count)
                        {
                            var paramType = signalParams[paramIndex];
                            if (!string.IsNullOrEmpty(paramType) && paramType != "Variant")
                            {
                                var signalCallSite = new GDCallSiteProvenanceEntry(
                                    conn.SourceFilePath ?? file.FullPath ?? "",
                                    conn.Line,
                                    $"{conn.EmitterType}.{conn.SignalName} signal -> {methodName}({varName}: {paramType})");

                                result.Add(new GDTypeProvenanceEntry(
                                    paramType,
                                    $"from {conn.EmitterType}.{conn.SignalName} signal",
                                    conn.Line,
                                    callSites: new[] { signalCallSite },
                                    sourceFilePath: conn.SourceFilePath));
                            }
                        }
                    }
                }
                catch
                {
                    // Signal tracing may fail
                }
            }
        }
        else
        {
            // Not a parameter — check flow-sensitive type (local variable or class member)
            try
            {
                var model = _projectModel?.GetSemanticModel(file) ?? file.SemanticModel;
                if (model != null)
                {
                    var flowType = model.GetFlowVariableType(varName, reference.ReferenceNode);
                    if (flowType?.DeclaredType != null)
                    {
                        var typeName = flowType.DeclaredType.DisplayName;
                        if (!string.IsNullOrEmpty(typeName) && typeName != "Variant")
                            result.Add(new GDTypeProvenanceEntry(typeName, "type annotation"));
                    }
                    else if (flowType?.CurrentType != null)
                    {
                        var effectiveType = flowType.CurrentType.EffectiveType?.DisplayName;
                        if (!string.IsNullOrEmpty(effectiveType) && effectiveType != "Variant")
                            result.Add(new GDTypeProvenanceEntry(effectiveType, "flow-inferred type"));
                    }
                }

                // Level 3: Container element type for iteration variables
                if (_projectModel != null && result.Count == 0)
                {
                    // First try direct container profile for varName
                    var containerProfile = _projectModel.GetMergedContainerProfile(enclosingTypeName, varName);
                    string? containerVarName = varName;

                    // If not found, check if varName is a for-loop iteration variable
                    // and trace back to the source container
                    if (containerProfile == null)
                    {
                        var forStmt = FindEnclosingForStatement(reference.ReferenceNode, varName);
                        if (forStmt?.Collection is GDIdentifierExpression collectionIdent)
                        {
                            containerVarName = collectionIdent.Identifier?.Sequence;
                            if (!string.IsNullOrEmpty(containerVarName))
                            {
                                containerProfile = _projectModel.GetMergedContainerProfile(
                                    enclosingTypeName, containerVarName);
                            }
                        }
                    }

                    if (containerProfile != null)
                    {
                        var elementType = containerProfile.ComputeInferredType();
                        if (elementType?.HasElementTypes == true)
                        {
                            var elType = elementType.EffectiveElementType?.DisplayName;
                            if (!string.IsNullOrEmpty(elType) && elType != "Variant")
                                result.Add(new GDTypeProvenanceEntry(elType, $"element of {containerVarName}"));
                        }

                        // Level 4: If container element type is Variant, trace append sites
                        // to find signal callback parameters that populate the container
                        if (result.Count == 0)
                        {
                            TraceContainerAppendSources(file, containerProfile, enclosingTypeName!, containerVarName!, result);
                        }
                    }
                }
            }
            catch
            {
                // Flow analysis may fail
            }
        }

        // No fallback — if no evidence found, return null (honest)
        return result.Count > 0 ? result : null;
    }

    private static string? ExtractVariableName(GDReference reference)
    {
        // Handle has_method string literal: obj.has_method("take_damage") → "obj"
        if (reference.ReferenceNode is GDStringNode or GDStringExpression)
        {
            var parent = reference.ReferenceNode.Parent;
            while (parent != null && parent is not GDCallExpression)
                parent = parent.Parent;
            if (parent is GDCallExpression call
                && call.CallerExpression is GDMemberOperatorExpression hasMethodMemberOp)
            {
                var callerExpr = hasMethodMemberOp.CallerExpression;
                while (callerExpr is GDMemberOperatorExpression nested)
                    callerExpr = nested.CallerExpression;
                return (callerExpr as GDIdentifierExpression)?.Identifier?.Sequence;
            }
        }

        // Extract from ConfidenceReason: "Duck-typed access on 'varName'"
        var reason = reference.ConfidenceReason;
        if (reason != null)
        {
            var startIdx = reason.IndexOf('\'');
            if (startIdx >= 0)
            {
                var endIdx = reason.IndexOf('\'', startIdx + 1);
                if (endIdx > startIdx)
                    return reason.Substring(startIdx + 1, endIdx - startIdx - 1);
            }
        }

        // Fallback: walk the AST from ReferenceNode to find the caller identifier
        if (reference.ReferenceNode is GDMemberOperatorExpression memberOp)
        {
            var caller = memberOp.CallerExpression;
            while (caller is GDMemberOperatorExpression nested)
                caller = nested.CallerExpression;
            return (caller as GDIdentifierExpression)?.Identifier?.Sequence;
        }

        return null;
    }

    private static GDMethodDeclaration? FindEnclosingMethod(GDNode? node)
    {
        var current = node;
        while (current != null)
        {
            if (current is GDMethodDeclaration method)
                return method;
            current = current.Parent;
        }
        return null;
    }

    private static GDForStatement? FindEnclosingForStatement(GDNode? node, string varName)
    {
        var current = node;
        while (current != null)
        {
            if (current is GDForStatement forStmt && forStmt.Variable?.Sequence == varName)
                return forStmt;
            if (current is GDMethodDeclaration)
                break;
            current = current.Parent;
        }
        return null;
    }

    private void TraceContainerAppendSources(
        GDScriptFile file,
        GDContainerUsageProfile containerProfile,
        string enclosingTypeName,
        string containerVarName,
        List<GDTypeProvenanceEntry> result)
    {
        if (_projectModel == null)
            return;

        // Use container profile's ValueUsages to find append sites,
        // then trace each appended value back to its source
        foreach (var usage in containerProfile.ValueUsages)
        {
            if (usage.Node == null)
                continue;

            // Only handle append-like operations
            if (usage.Kind != GDContainerUsageKind.Append
                && usage.Kind != GDContainerUsageKind.PushBack
                && usage.Kind != GDContainerUsageKind.PushFront)
                continue;

            // If the type is already concrete, skip (Level 3 should have caught it)
            if (usage.InferredType != null && !usage.InferredType.IsVariant)
                continue;

            // Find the call expression and extract the appended argument
            var callNode = usage.Node is GDCallExpression callExpr
                ? callExpr
                : FindParentOfType<GDCallExpression>(usage.Node);
            if (callNode == null)
                continue;

            var args = callNode.Parameters?.ToList();
            if (args == null || args.Count == 0)
                continue;

            var appendedExpr = args[0];
            if (appendedExpr is not GDIdentifierExpression appendedIdent)
                continue;

            var appendedVarName = appendedIdent.Identifier?.Sequence;
            if (string.IsNullOrEmpty(appendedVarName))
                continue;

            // Find the enclosing method of the append call
            var method = FindEnclosingMethod(usage.Node);
            if (method == null)
                continue;

            // Check if the appended variable is a parameter of this method
            var paramIdx = FindParameterIndex(method, appendedVarName);
            if (paramIdx < 0)
                continue;

            var appendMethodName = method.Identifier?.Sequence;
            if (string.IsNullOrEmpty(appendMethodName))
                continue;

            // Level 4a: Check signal callback parameters
            try
            {
                var connections = _projectModel.SignalConnectionRegistry
                    .GetSignalsCallingMethod(enclosingTypeName, appendMethodName);

                foreach (var conn in connections)
                {
                    if (string.IsNullOrEmpty(conn.EmitterType))
                        continue;

                    var signalParams = GetSignalParameterTypes(conn.EmitterType, conn.SignalName);
                    if (signalParams != null && paramIdx < signalParams.Count)
                    {
                        var paramType = signalParams[paramIdx];
                        if (!string.IsNullOrEmpty(paramType) && paramType != "Variant")
                        {
                            var usageLine = (usage.Node.AllTokens.FirstOrDefault()?.StartLine ?? 0) + 1;
                            var appendCallSite = new GDCallSiteProvenanceEntry(
                                file.FullPath ?? "", usageLine,
                                $"{containerVarName}.append({appendedVarName}) " +
                                $"<- {appendMethodName}({appendedVarName}: {paramType}) " +
                                $"<- {conn.EmitterType}.{conn.SignalName} signal");

                            result.Add(new GDTypeProvenanceEntry(
                                paramType,
                                $"via {conn.EmitterType}.{conn.SignalName} -> {appendMethodName}() -> {containerVarName}",
                                conn.Line,
                                callSites: new[] { appendCallSite },
                                sourceFilePath: conn.SourceFilePath));
                        }
                    }
                }
            }
            catch
            {
                // Signal tracing may fail
            }

            // Level 4b: Check direct call sites
            if (result.Count == 0)
            {
                try
                {
                    var collector = new GDCallSiteCollector(_project);
                    var callSites = collector.CollectCallSites(enclosingTypeName, appendMethodName);

                    foreach (var cs in callSites)
                    {
                        var arg = cs.GetArgument(paramIdx);
                        if (arg == null) continue;
                        var argType = arg.InferredType?.DisplayName;
                        if (string.IsNullOrEmpty(argType) || argType == "Variant") continue;

                        var innerChain = TraceArgumentOrigin(cs.SourceScript, arg.ExpressionText, arg.Expression);
                        var callSiteEntry = new GDCallSiteProvenanceEntry(
                            cs.FilePath, cs.Line + 1,
                            $"{appendMethodName}({arg.ExpressionText}) -> {containerVarName}", innerChain);

                        result.Add(new GDTypeProvenanceEntry(
                            argType,
                            $"via {appendMethodName}() -> {containerVarName}",
                            cs.Line,
                            callSites: new[] { callSiteEntry },
                            sourceFilePath: cs.FilePath));
                    }
                }
                catch
                {
                    // Call site collection may fail
                }
            }
        }
    }

    private List<GDCallSiteProvenanceEntry> TraceArgumentOrigin(
        GDScriptFile callSiteFile,
        string argVarName,
        GDExpression? argExpr,
        int maxDepth = 3)
    {
        if (maxDepth <= 0 || _projectModel == null)
            return new List<GDCallSiteProvenanceEntry>();

        var chain = new List<GDCallSiteProvenanceEntry>();
        var enclosingType = callSiteFile.TypeName;
        if (string.IsNullOrEmpty(enclosingType))
            return chain;

        // 1. For-loop variable -> trace container
        if (argExpr != null)
        {
            var forStmt = FindEnclosingForStatement(argExpr, argVarName);
            if (forStmt != null)
            {
                var collectionName = (forStmt.Collection as GDIdentifierExpression)
                    ?.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(collectionName))
                {
                    var forLine = (forStmt.AllTokens.FirstOrDefault()?.StartLine ?? 0) + 1;
                    var innerChain = TraceContainerOrigin(
                        callSiteFile, enclosingType, collectionName, maxDepth - 1);
                    chain.Add(new GDCallSiteProvenanceEntry(
                        callSiteFile.FullPath ?? "", forLine,
                        $"for {argVarName} in {collectionName}", innerChain));
                    return chain;
                }
            }
        }

        // 2. Parameter -> trace callers (signals + call sites)
        var method = argExpr != null ? FindEnclosingMethod(argExpr) : null;
        if (method != null)
        {
            var paramIdx = FindParameterIndex(method, argVarName);
            if (paramIdx >= 0)
            {
                var methodName = method.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(methodName))
                {
                    // 2a: Signal connections -> signal parameter types
                    try
                    {
                        var connections = _projectModel.SignalConnectionRegistry
                            .GetSignalsCallingMethod(enclosingType, methodName);
                        foreach (var conn in connections)
                        {
                            if (string.IsNullOrEmpty(conn.EmitterType)) continue;
                            var signalParams = GetSignalParameterTypes(
                                conn.EmitterType, conn.SignalName);
                            if (signalParams != null && paramIdx < signalParams.Count)
                            {
                                chain.Add(new GDCallSiteProvenanceEntry(
                                    conn.SourceFilePath ?? callSiteFile.FullPath ?? "",
                                    conn.Line,
                                    $"{conn.EmitterType}.{conn.SignalName} signal -> " +
                                    $"{methodName}({argVarName}: {signalParams[paramIdx]})"));
                            }
                        }
                    }
                    catch { }

                    // 2b: Direct call sites -> recurse into argument
                    if (chain.Count == 0)
                    {
                        try
                        {
                            var collector = new GDCallSiteCollector(_project);
                            var callSites = collector.CollectCallSites(
                                enclosingType, methodName);
                            foreach (var cs in callSites)
                            {
                                var arg = cs.GetArgument(paramIdx);
                                if (arg?.Expression == null) continue;
                                var innerChain = TraceArgumentOrigin(
                                    cs.SourceScript, arg.ExpressionText,
                                    arg.Expression, maxDepth - 1);
                                chain.Add(new GDCallSiteProvenanceEntry(
                                    cs.FilePath, cs.Line + 1,
                                    arg.ExpressionText, innerChain));
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        return chain;
    }

    /// <summary>
    /// Tries to extract a more specific type from the inner chain.
    /// For example, if declared type is Node2D but the chain shows the argument
    /// originates from a signal with Area2D parameter, returns Area2D.
    /// </summary>
    private string? TryNarrowTypeFromChain(
        IReadOnlyList<GDCallSiteProvenanceEntry> chain, string currentType)
    {
        if (_runtimeProvider == null || chain.Count == 0)
            return null;

        string? narrowest = null;

        foreach (var entry in chain)
        {
            // Signal entries contain type in expression like "EmitterType.signal_name signal -> method(param: Area2D)"
            var signalType = ExtractSignalParamType(entry.Expression);
            if (!string.IsNullOrEmpty(signalType)
                && signalType != "Variant"
                && signalType != currentType
                && _runtimeProvider.IsAssignableTo(signalType, currentType) == true)
            {
                if (narrowest == null
                    || _runtimeProvider.IsAssignableTo(signalType, narrowest) == true)
                    narrowest = signalType;
            }

            // Recurse into inner chain
            var innerNarrowed = TryNarrowTypeFromChain(entry.InnerChain.ToList(), currentType);
            if (innerNarrowed != null)
            {
                if (narrowest == null
                    || _runtimeProvider.IsAssignableTo(innerNarrowed, narrowest) == true)
                    narrowest = innerNarrowed;
            }
        }

        return narrowest;
    }

    private static string? ExtractSignalParamType(string expression)
    {
        // Pattern: "EmitterType.signal_name signal -> method(param: TypeName)"
        var arrowIdx = expression.IndexOf("-> ");
        if (arrowIdx < 0) return null;

        var colonIdx = expression.LastIndexOf(": ");
        if (colonIdx < arrowIdx) return null;

        var closeParen = expression.IndexOf(')', colonIdx);
        if (closeParen < 0) return null;

        return expression.Substring(colonIdx + 2, closeParen - colonIdx - 2).Trim();
    }

    private List<GDCallSiteProvenanceEntry> TraceContainerOrigin(
        GDScriptFile file, string enclosingType,
        string containerVarName, int maxDepth)
    {
        var chain = new List<GDCallSiteProvenanceEntry>();
        if (maxDepth <= 0 || _projectModel == null) return chain;

        var profile = _projectModel.GetMergedContainerProfile(
            enclosingType, containerVarName);

        if (profile == null)
        {
            // Try base class profile
            if (_runtimeProvider != null)
            {
                var baseType = _runtimeProvider.GetBaseType(enclosingType);
                while (!string.IsNullOrEmpty(baseType) && profile == null)
                {
                    profile = _projectModel.GetMergedContainerProfile(baseType, containerVarName);
                    if (profile != null)
                    {
                        var baseScript = _project.ScriptFiles.FirstOrDefault(s => s.TypeName == baseType);
                        if (baseScript != null) file = baseScript;
                        break;
                    }
                    baseType = _runtimeProvider.GetBaseType(baseType);
                }
            }

            // Still null? Check flow analysis for explicitly typed containers
            if (profile == null)
            {
                var script = _project.ScriptFiles.FirstOrDefault(s => s.TypeName == enclosingType)
                    ?? file;
                var model = _projectModel.GetSemanticModel(script) ?? script.SemanticModel;
                var flowType = model?.GetFlowVariableType(containerVarName, null);
                if (flowType?.DeclaredType != null && flowType.DeclaredType.DisplayName?.Contains("[") == true)
                {
                    var declLine = FindVariableDeclarationLine(script, containerVarName);
                    chain.Add(new GDCallSiteProvenanceEntry(
                        script.FullPath ?? file.FullPath ?? "",
                        declLine,
                        $"var {containerVarName}: {flowType.DeclaredType.DisplayName}")
                    { IsExplicitType = true });
                    return chain;
                }
                return chain;
            }
        }

        // Typed container (e.g. Array[EnemyBase]) -> declaration location
        var elementType = profile.ComputeInferredType();
        if (elementType?.HasElementTypes == true)
        {
            var elType = elementType.EffectiveElementType?.DisplayName;
            if (!string.IsNullOrEmpty(elType) && elType != "Variant")
            {
                chain.Add(new GDCallSiteProvenanceEntry(
                    file.FullPath ?? "",
                    profile.DeclarationLine + 1,
                    $"var {containerVarName} ~: Array[{elType}]")
                { IsExplicitType = false });
                return chain;
            }
        }

        // Untyped container -> trace append sites
        foreach (var usage in profile.ValueUsages)
        {
            if (usage.Node == null) continue;
            if (usage.Kind != GDContainerUsageKind.Append
                && usage.Kind != GDContainerUsageKind.PushBack
                && usage.Kind != GDContainerUsageKind.PushFront) continue;

            var callNode = usage.Node is GDCallExpression ce
                ? ce : FindParentOfType<GDCallExpression>(usage.Node);
            if (callNode == null) continue;

            var args = callNode.Parameters?.ToList();
            if (args == null || args.Count == 0) continue;
            if (args[0] is not GDIdentifierExpression appendedIdent) continue;

            var appendedVarName = appendedIdent.Identifier?.Sequence;
            if (string.IsNullOrEmpty(appendedVarName)) continue;

            var method = FindEnclosingMethod(usage.Node);
            if (method == null) continue;

            var paramIdx = FindParameterIndex(method, appendedVarName);
            if (paramIdx < 0) continue;

            var methodName = method.Identifier?.Sequence;
            if (string.IsNullOrEmpty(methodName)) continue;

            // Signal -> append -> container
            try
            {
                var connections = _projectModel.SignalConnectionRegistry
                    .GetSignalsCallingMethod(enclosingType, methodName);
                foreach (var conn in connections)
                {
                    if (string.IsNullOrEmpty(conn.EmitterType)) continue;
                    var signalParams = GetSignalParameterTypes(
                        conn.EmitterType, conn.SignalName);
                    if (signalParams != null && paramIdx < signalParams.Count)
                    {
                        var paramType = signalParams[paramIdx];
                        if (!string.IsNullOrEmpty(paramType) && paramType != "Variant")
                        {
                            var usageLine = (usage.Node.AllTokens.FirstOrDefault()?.StartLine ?? 0) + 1;
                            chain.Add(new GDCallSiteProvenanceEntry(
                                file.FullPath ?? "", usageLine,
                                $"{containerVarName}.append({appendedVarName}) " +
                                $"<- {methodName}({appendedVarName}: {paramType}) " +
                                $"<- {conn.EmitterType}.{conn.SignalName} signal"));
                        }
                    }
                }
            }
            catch { }
        }

        return chain;
    }

    private static T? FindParentOfType<T>(GDSyntaxToken? node) where T : GDNode
    {
        var current = node?.Parent;
        while (current != null)
        {
            if (current is T target)
                return target;
            current = current.Parent;
        }
        return null;
    }

    private static int FindVariableDeclarationLine(GDScriptFile script, string varName)
    {
        if (script.Class == null) return 0;
        foreach (var member in script.Class.Members)
        {
            if (member is GDVariableDeclaration varDecl
                && varDecl.Identifier?.Sequence == varName)
                return varDecl.Identifier.StartLine + 1;
        }
        return 0;
    }

    private static int FindParameterIndex(GDMethodDeclaration method, string paramName)
    {
        if (method.Parameters == null)
            return -1;

        int index = 0;
        foreach (var param in method.Parameters)
        {
            if (param is GDParameterDeclaration pd && pd.Identifier?.Sequence == paramName)
                return index;
            index++;
        }
        return -1;
    }

    private string? FindRootDeclaringType(string typeName, string memberName)
    {
        if (_runtimeProvider == null)
            return null;

        var current = typeName;
        string? root = null;
        var visited = new HashSet<string>();

        while (!string.IsNullOrEmpty(current) && visited.Add(current))
        {
            if (_runtimeProvider.GetMember(current, memberName) != null)
                root = current;
            current = _runtimeProvider.GetBaseType(current);
        }

        return root;
    }

    private IReadOnlyList<string>? GetSignalParameterTypes(string emitterType, string signalName)
    {
        if (_runtimeProvider == null)
            return null;

        // Walk inheritance chain to find signal (handles project class → built-in type)
        var visited = new HashSet<string>();
        var current = emitterType;
        while (!string.IsNullOrEmpty(current) && visited.Add(current))
        {
            var member = _runtimeProvider.GetMember(current, signalName);
            if (member?.Kind == GDRuntimeMemberKind.Signal && member.Parameters != null && member.Parameters.Count > 0)
            {
                return member.Parameters.Select(p => p.Type ?? "Variant").ToList();
            }

            // Check project signal declarations at this level
            var script = _project.GetScriptByTypeName(current);
            if (script?.Class != null)
            {
                var signalDecl = script.Class.Members
                    .OfType<GDSignalDeclaration>()
                    .FirstOrDefault(s => s.Identifier?.Sequence == signalName);
                if (signalDecl?.Parameters != null)
                {
                    return signalDecl.Parameters
                        .Select(p => p.Type?.BuildName() ?? "Variant")
                        .ToList();
                }
            }

            current = _runtimeProvider.GetBaseType(current);
        }

        return null;
    }

    /// <summary>
    /// Type-filtered version: collects .tscn signal connection edits only where
    /// the target node type is compatible with the declaring type.
    /// </summary>
    private void CollectTscnEdits(
        string oldName,
        string newName,
        string declaringTypeName,
        List<GDTextEdit> strictEdits,
        List<GDTextEdit> potentialEdits,
        HashSet<string> filesModified)
    {
        var sceneProvider = _project.SceneTypesProvider;
        if (sceneProvider == null)
            return;

        foreach (var scene in sceneProvider.AllScenes)
        {
            if (string.IsNullOrEmpty(scene.FullPath))
                continue;

            foreach (var conn in scene.SignalConnections)
            {
                if (conn.Method != oldName)
                    continue;

                // Resolve target node type
                var targetNode = scene.Nodes.FirstOrDefault(n =>
                    n.Path == conn.ToNode || (conn.ToNode == "." && n.Path == "."));
                var targetType = targetNode?.ScriptTypeName ?? targetNode?.NodeType;

                var column = FindMethodColumnInTscn(scene.FullPath, conn.LineNumber, oldName);

                if (!string.IsNullOrEmpty(targetType) && IsTypeCompatible(targetType!, declaringTypeName))
                {
                    // Compatible type → Strict
                    strictEdits.Add(new GDTextEdit(
                        scene.FullPath,
                        conn.LineNumber,
                        column,
                        oldName,
                        newName,
                        GDReferenceConfidence.Strict,
                        $".tscn signal connection method=\"{oldName}\" (target: {targetType})"));
                    filesModified.Add(scene.FullPath);
                }
                else if (string.IsNullOrEmpty(targetType))
                {
                    // Unknown type → Potential
                    potentialEdits.Add(new GDTextEdit(
                        scene.FullPath,
                        conn.LineNumber,
                        column,
                        oldName,
                        newName,
                        GDReferenceConfidence.Potential,
                        $".tscn signal connection method=\"{oldName}\" (target type unknown)"));
                    filesModified.Add(scene.FullPath);
                }
                // Incompatible type → skip
            }
        }
    }

    #endregion
}
