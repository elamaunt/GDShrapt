using System.Collections.Generic;
using System.Linq;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for finding symbol references.
/// Uses GDScriptAnalyzer for CLI-friendly symbol lookup.
/// </summary>
public class GDFindRefsHandler : IGDFindRefsHandler
{
    protected readonly GDScriptProject _project;

    public GDFindRefsHandler(GDScriptProject project)
    {
        _project = project;
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDReferenceLocation> FindReferences(string symbolName, string? filePath = null)
    {
        var results = new List<GDReferenceLocation>();

        if (!string.IsNullOrEmpty(filePath))
        {
            // Search in specific file
            var script = _project.GetScript(filePath);
            if (script != null)
            {
                CollectReferencesFromScript(script, symbolName, results);
            }
        }
        else
        {
            // Search across all files
            foreach (var script in _project.ScriptFiles)
            {
                CollectReferencesFromScript(script, symbolName, results);
            }
        }

        return results;
    }

    private static void CollectReferencesFromScript(
        GDScriptFile script,
        string symbolName,
        List<GDReferenceLocation> results)
    {
        var analyzer = script.Analyzer;
        if (analyzer == null)
            return;

        // Find the symbol first
        var symbol = analyzer.FindSymbol(symbolName);
        if (symbol == null)
            return;

        // Get all references to this symbol
        var refs = analyzer.GetReferencesTo(symbol);

        foreach (var reference in refs)
        {
            var node = reference.ReferenceNode;
            if (node == null)
                continue;

            results.Add(new GDReferenceLocation
            {
                FilePath = script.Reference.FullPath,
                Line = node.StartLine,
                Column = node.StartColumn,
                IsDeclaration = node == symbol.DeclarationNode,
                IsWrite = false
            });
        }

        // Add declaration location if not already included
        if (symbol.DeclarationNode != null)
        {
            var declarationIncluded = results.Any(r =>
                r.Line == symbol.DeclarationNode.StartLine &&
                r.Column == symbol.DeclarationNode.StartColumn &&
                r.FilePath == script.Reference.FullPath);

            if (!declarationIncluded)
            {
                results.Insert(0, new GDReferenceLocation
                {
                    FilePath = script.Reference.FullPath,
                    Line = symbol.DeclarationNode.StartLine,
                    Column = symbol.DeclarationNode.StartColumn,
                    IsDeclaration = true,
                    IsWrite = false
                });
            }
        }
    }
}
