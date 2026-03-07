using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

/// <summary>
/// Handles gdshrapt/unionReferences custom request.
/// Returns shared references from neutral files — duck-typed calls
/// that could refer to multiple hierarchies' declarations.
/// Excludes signal connections and scene signals (those belong in primary refs).
/// </summary>
public class GDUnionReferencesHandler
{
    private readonly IGDFindRefsHandler _findRefsHandler;

    public GDUnionReferencesHandler(IGDFindRefsHandler findRefsHandler)
    {
        _findRefsHandler = findRefsHandler;
    }

    public Task<GDLspLocation[]?> HandleAsync(GDUnionReferencesParams @params, CancellationToken cancellationToken)
    {
        var symbolName = @params.SymbolName;

        if (string.IsNullOrEmpty(symbolName))
            return Task.FromResult<GDLspLocation[]?>(null);

        var result = _findRefsHandler.FindAllReferences(symbolName);

        // Filter: only duck-typed code references, not signal connections or scene signals
        var allLocations = new List<GDCliReferenceLocation>();

        foreach (var group in result.PrimaryGroups)
        {
            if (group.IsCrossFile)
                CollectLocations(group, allLocations);
        }
        foreach (var group in result.UnrelatedGroups)
        {
            CollectLocations(group, allLocations);
        }

        var codeUnions = allLocations
            .Where(r => !r.IsSignalConnection && !r.IsSceneSignal && !r.IsContractString);

        var locations = new List<GDLspLocation>();
        foreach (var reference in codeUnions)
        {
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

        return Task.FromResult<GDLspLocation[]?>(locations.Count > 0 ? locations.ToArray() : null);
    }

    private static void CollectLocations(GDReferenceGroup group, List<GDCliReferenceLocation> locations)
    {
        locations.AddRange(group.Locations);
        foreach (var child in group.Overrides)
            CollectLocations(child, locations);
    }
}
