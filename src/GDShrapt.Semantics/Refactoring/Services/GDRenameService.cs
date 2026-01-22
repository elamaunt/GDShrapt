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
    private readonly GDScriptProject _project;

    public GDRenameService(GDScriptProject project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
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
        if (containingScript != null)
        {
            // Same-file edits are always Strict confidence
            var scriptEdits = CollectEditsFromScript(containingScript, symbol, oldName, newName);
            strictEdits.AddRange(scriptEdits);
            if (scriptEdits.Count > 0)
                filesModified.Add(containingScript.FullPath!);
        }

        // For class members, also search other scripts that might reference this symbol
        if (IsClassMemberSymbol(symbol) && containingScript != null)
        {
            var crossFileFinder = new GDCrossFileReferenceFinder(_project);
            var crossFileRefs = crossFileFinder.FindReferences(symbol, containingScript);

            // Process strict references
            foreach (var r in crossFileRefs.StrictReferences)
            {
                var filePath = r.FilePath;
                if (string.IsNullOrEmpty(filePath))
                    continue;

                strictEdits.Add(new GDTextEdit(
                    filePath,
                    r.Line,
                    r.Column,
                    oldName,
                    newName,
                    GDReferenceConfidence.Strict,
                    r.Reason));
                filesModified.Add(filePath);
            }

            // Process potential references
            foreach (var r in crossFileRefs.PotentialReferences)
            {
                var filePath = r.FilePath;
                if (string.IsNullOrEmpty(filePath))
                    continue;

                potentialEdits.Add(new GDTextEdit(
                    filePath,
                    r.Line,
                    r.Column,
                    oldName,
                    newName,
                    GDReferenceConfidence.Potential,
                    r.Reason));
                filesModified.Add(filePath);
            }
        }

        if (strictEdits.Count == 0 && potentialEdits.Count == 0)
            return GDRenameResult.NoOccurrences(oldName);

        // Sort edits by file, then by position (reverse order for applying)
        var sortedStrict = SortEditsReverse(strictEdits);
        var sortedPotential = SortEditsReverse(potentialEdits);

        return GDRenameResult.SuccessfulWithConfidence(sortedStrict, sortedPotential, filesModified.Count);
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

        var edits = new List<GDTextEdit>();
        var filesModified = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var script in _project.ScriptFiles)
        {
            // If file filter is specified, only process that file
            if (!string.IsNullOrEmpty(filterFilePath))
            {
                var fullPath = Path.GetFullPath(filterFilePath);
                if (!script.FullPath!.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            var fileEdits = CollectEditsFromScriptByName(script, oldName, newName);
            if (fileEdits.Count > 0)
            {
                edits.AddRange(fileEdits);
                filesModified.Add(script.FullPath!);
            }
        }

        if (edits.Count == 0)
            return GDRenameResult.NoOccurrences(oldName);

        // Sort edits by file, then by position (reverse order for applying)
        var sortedEdits = edits
            .OrderBy(e => e.FilePath)
            .ThenByDescending(e => e.Line)
            .ThenByDescending(e => e.Column)
            .ToList();

        return GDRenameResult.Successful(sortedEdits, filesModified.Count);
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

        foreach (var reference in refsResult.StrictReferences)
        {
            var filePath = reference.FilePath;
            if (string.IsNullOrEmpty(filePath))
                continue;

            strictEdits.Add(new GDTextEdit(
                filePath,
                reference.Line,
                reference.Column,
                oldName,
                newName,
                GDReferenceConfidence.Strict,
                reference.ConfidenceReason));
            filesModified.Add(filePath);
        }

        foreach (var reference in refsResult.PotentialReferences)
        {
            var filePath = reference.FilePath;
            if (string.IsNullOrEmpty(filePath))
                continue;

            potentialEdits.Add(new GDTextEdit(
                filePath,
                reference.Line,
                reference.Column,
                oldName,
                newName,
                GDReferenceConfidence.Potential,
                reference.ConfidenceReason));
            filesModified.Add(filePath);
        }

        // For class members and external members, also search other scripts
        if ((scope.Type == GDSymbolScopeType.ClassMember ||
             scope.Type == GDSymbolScopeType.ExternalMember ||
             scope.Type == GDSymbolScopeType.ProjectWide) &&
            scope.ContainingScript != null)
        {
            var crossFileFinder = new GDCrossFileReferenceFinder(_project);
            var containingScript = scope.ContainingScript;

            // Create a temporary symbol for cross-file search
            if (scope.DeclarationNode is GDIdentifiableClassMember identifiable)
            {
                var symbol = containingScript.SemanticModel?.FindSymbol(oldName);
                if (symbol != null)
                {
                    var crossFileRefs = crossFileFinder.FindReferences(symbol, containingScript);

                    foreach (var r in crossFileRefs.StrictReferences)
                    {
                        var filePath = r.FilePath;
                        if (string.IsNullOrEmpty(filePath) || filesModified.Contains(filePath))
                            continue;

                        strictEdits.Add(new GDTextEdit(
                            filePath,
                            r.Line,
                            r.Column,
                            oldName,
                            newName,
                            GDReferenceConfidence.Strict,
                            r.Reason));
                        filesModified.Add(filePath);
                    }

                    foreach (var r in crossFileRefs.PotentialReferences)
                    {
                        var filePath = r.FilePath;
                        if (string.IsNullOrEmpty(filePath) || filesModified.Contains(filePath))
                            continue;

                        potentialEdits.Add(new GDTextEdit(
                            filePath,
                            r.Line,
                            r.Column,
                            oldName,
                            newName,
                            GDReferenceConfidence.Potential,
                            r.Reason));
                        filesModified.Add(filePath);
                    }
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

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Applies edits directly to a file.
    /// </summary>
    /// <param name="filePath">The file to modify.</param>
    /// <param name="edits">The edits to apply.</param>
    public void ApplyEditsToFile(string filePath, IEnumerable<GDTextEdit> edits)
    {
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        var modified = ApplyEdits(content, edits);
        File.WriteAllText(filePath, modified, Encoding.UTF8);
    }

    #region Private helpers

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

    private List<GDTextEdit> CollectEditsFromScript(GDScriptFile script, GDSymbolInfo symbol, string oldName, string newName)
    {
        var edits = new List<GDTextEdit>();
        var semanticModel = script.SemanticModel;
        var filePath = script.FullPath;

        if (semanticModel == null || filePath == null)
            return edits;

        // Add declaration
        if (symbol.DeclarationNode != null)
        {
            edits.Add(new GDTextEdit(filePath, symbol.DeclarationNode.StartLine, symbol.DeclarationNode.StartColumn, oldName, newName));
        }

        // Add all references
        var refs = semanticModel.GetReferencesTo(symbol);
        foreach (var reference in refs)
        {
            var node = reference.ReferenceNode;
            if (node == null)
                continue;

            // Skip if it's the declaration (already added)
            if (node == symbol.DeclarationNode)
                continue;

            edits.Add(new GDTextEdit(filePath, node.StartLine, node.StartColumn, oldName, newName));
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

    #endregion
}
