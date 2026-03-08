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

        // TODO: SharedLocations not yet implemented in GDFindRefsResult
        return Task.FromResult<GDLspLocation[]?>(null);
    }
}
