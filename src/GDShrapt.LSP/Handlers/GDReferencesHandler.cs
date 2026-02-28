using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/references requests.
/// Thin wrapper over IGDFindRefsHandler from CLI.Core.
/// Uses IGDGoToDefHandler to get symbol name at cursor position.
/// </summary>
public class GDReferencesHandler
{
    private readonly IGDFindRefsHandler _findRefsHandler;
    private readonly IGDGoToDefHandler _goToDefHandler;

    public GDReferencesHandler(IGDFindRefsHandler findRefsHandler, IGDGoToDefHandler goToDefHandler)
    {
        _findRefsHandler = findRefsHandler;
        _goToDefHandler = goToDefHandler;
    }

    public Task<GDLspLocation[]?> HandleAsync(GDReferencesParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);

        // Convert LSP 0-based to CLI.Core 1-based
        var line = @params.Position.Line + 1;
        var column = @params.Position.Character + 1;

        // First, get the symbol name at the cursor position
        var definition = _goToDefHandler.FindDefinition(filePath, line, column);
        if (definition == null || string.IsNullOrEmpty(definition.SymbolName))
            return Task.FromResult<GDLspLocation[]?>(null);

        var symbolName = definition.SymbolName;

        // Delegate to CLI.Core handler
        var groups = _findRefsHandler.FindReferences(symbolName, filePath);
        if (groups == null || groups.Count == 0)
            return Task.FromResult<GDLspLocation[]?>(null);

        // Flatten groups (including nested overrides) into a single list
        // Filter to Strict confidence only (null = declaration-based, Strict = type-resolved)
        var allRefs = FlattenLocations(groups)
            .Where(r => r.Confidence == null || r.Confidence == GDReferenceConfidence.Strict);

        // Filter results based on IncludeDeclaration
        if (!@params.Context.IncludeDeclaration)
            allRefs = allRefs.Where(r => !r.IsDeclaration);

        // Sort so declarations come first (VS Code highlights the first match as definition)
        allRefs = allRefs.OrderByDescending(r => r.IsDeclaration);

        // Convert CLI.Core results to LSP locations
        var locations = new List<GDLspLocation>();
        foreach (var reference in allRefs)
        {
            // GDCliReferenceLocation uses 1-based line and 1-based column
            // Convert column to 0-based for ToLspRange which expects 0-based columns
            var col0 = reference.Column - 1;
            locations.Add(new GDLspLocation
            {
                Uri = GDDocumentManager.PathToUri(reference.FilePath),
                Range = GDLocationAdapter.ToLspRange(
                    reference.Line,
                    col0,
                    reference.Line,
                    col0 + symbolName.Length)
            });
        }

        return Task.FromResult<GDLspLocation[]?>(locations.ToArray());
    }

    private static IEnumerable<CLI.Core.GDCliReferenceLocation> FlattenLocations(IEnumerable<GDReferenceGroup> groups)
    {
        foreach (var group in groups)
        {
            foreach (var loc in group.Locations)
                yield return loc;

            foreach (var loc in FlattenLocations(group.Overrides))
                yield return loc;
        }
    }
}
