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

    public GDRenameService(GDScriptProject project, GDProjectSemanticModel? projectModel = null)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _projectModel = projectModel;
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

        // Find the script containing this symbol
        var containingScript = FindScriptContainingSymbol(symbol);
        if (containingScript?.FullPath != null)
        {
            // Same-file edits are always Strict confidence
            var scriptEdits = CollectEditsFromScript(containingScript, symbol, oldName, newName);
            strictEdits.AddRange(scriptEdits);
            if (scriptEdits.Count > 0)
                filesModified.Add(containingScript.FullPath);
        }

        // For class members, also search other scripts that might reference this symbol
        if (IsClassMemberSymbol(symbol) && containingScript != null)
        {
            var crossFileFinder = new GDCrossFileReferenceFinder(_project, _projectModel);
            var crossFileRefs = crossFileFinder.FindReferences(symbol, containingScript);

            AddCrossFileReferencesToEdits(crossFileRefs, strictEdits, potentialEdits, filesModified, oldName, newName);
        }

        // class_name rename: find type usages across the project
        if (containingScript != null && oldName == containingScript.TypeName && _projectModel != null)
        {
            CollectClassNameEdits(containingScript, oldName, newName, strictEdits, filesModified);
        }

        // .tscn signal connections: [connection method="oldName"]
        CollectTscnEdits(oldName, newName, strictEdits, filesModified);

        if (strictEdits.Count == 0 && potentialEdits.Count == 0)
            return GDRenameResult.NoOccurrences(oldName);

        // Deduplicate edits
        strictEdits = DeduplicateEdits(strictEdits);
        potentialEdits = DeduplicateEdits(potentialEdits);

        // Sort edits by file, then by position (reverse order for applying)
        var sortedStrict = SortEditsReverse(strictEdits);
        var sortedPotential = SortEditsReverse(potentialEdits);

        return GDRenameResult.SuccessfulWithConfidence(sortedStrict, sortedPotential, filesModified.Count);
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
            var fullPath = Path.GetFullPath(filterFilePath);
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

                return GDRenameResult.SuccessfulWithConfidence(
                    SortEditsReverse(strictEdits), SortEditsReverse(potentialEdits), filesModified.Count);
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

        var findRefsService = new GDFindReferencesService();
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

        // Find references using the service
        var findRefsService = new GDFindReferencesService();
        var refsResult = findRefsService.FindReferencesForScope(context, scope);

        if (!refsResult.Success)
            return GDRenameResult.Failed(refsResult.ErrorMessage ?? "Failed to find references");

        // Convert references to text edits
        var strictEdits = new List<GDTextEdit>();
        var potentialEdits = new List<GDTextEdit>();
        var filesModified = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddReferencesToEdits(refsResult.StrictReferences, strictEdits, filesModified, oldName, newName, GDReferenceConfidence.Strict);
        AddReferencesToEdits(refsResult.PotentialReferences, potentialEdits, filesModified, oldName, newName, GDReferenceConfidence.Potential);

        // For class members and external members, also search other scripts
        if ((scope.Type == GDSymbolScopeType.ClassMember ||
             scope.Type == GDSymbolScopeType.ExternalMember ||
             scope.Type == GDSymbolScopeType.ProjectWide) &&
            scope.ContainingScript != null)
        {
            var crossFileFinder = new GDCrossFileReferenceFinder(_project, _projectModel);
            var containingScript = scope.ContainingScript;

            // Create a temporary symbol for cross-file search
            if (scope.DeclarationNode is GDIdentifiableClassMember identifiable)
            {
                var symbol = containingScript.SemanticModel?.FindSymbol(oldName);
                if (symbol != null)
                {
                    var crossFileRefs = crossFileFinder.FindReferences(symbol, containingScript);
                    AddCrossFileReferencesToEdits(crossFileRefs, strictEdits, potentialEdits, filesModified, oldName, newName, skipExistingFiles: true);
                }
            }
        }

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
    /// Collects edits for class_name rename across the project using type usages.
    /// </summary>
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

            var targetEdits = reference.Confidence == GDReferenceConfidence.Strict
                ? strictEdits : potentialEdits;

            // String literal tokens (e.g., has_method("name")) need +1 column offset for the opening quote
            var columnOffset = identToken is GDStringNode ? 2 : 1;

            targetEdits.Add(new GDTextEdit(
                file.FullPath,
                identToken.StartLine + 1,
                identToken.StartColumn + columnOffset,
                oldName,
                newName,
                reference.Confidence,
                reference.ConfidenceReason));
            filesModified.Add(file.FullPath);
        }
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

    #endregion
}
