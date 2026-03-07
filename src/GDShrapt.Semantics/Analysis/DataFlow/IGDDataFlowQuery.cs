using System.Collections.Generic;
using GDShrapt.Abstractions;

namespace GDShrapt.Semantics;

/// <summary>
/// Query API for data flow information at specific program points.
/// All queries are by GDSymbolInfo + position (line/column).
/// </summary>
public interface IGDDataFlowQuery
{
    /// <summary>
    /// Gets complete data flow information for a symbol at a specific program point.
    /// Returns null if the symbol has no flow data (e.g., built-in type, class-level constant).
    /// </summary>
    GDDataFlowInfo? GetDataFlowAt(GDSymbolInfo symbol, int line, int column);

    /// <summary>
    /// Detects type conflicts (widening, incompatible assignment, etc.) for a symbol at a point.
    /// </summary>
    IReadOnlyList<GDTypeConflict> GetTypeConflicts(GDSymbolInfo symbol, int line, int column);

    /// <summary>
    /// Gets all origins that contribute Variant/unknown types for a symbol.
    /// </summary>
    IReadOnlyList<GDTypeOrigin> GetUnknownSources(GDSymbolInfo symbol);

    /// <summary>
    /// Gets all points where the symbol's data escaped analysis scope.
    /// </summary>
    IReadOnlyList<GDEscapePoint> GetEscapePoints(GDSymbolInfo symbol);

    /// <summary>
    /// Gets the object state (scene hierarchy, collision layers, properties) for a symbol at a point.
    /// Returns null if the symbol has no tracked object state.
    /// </summary>
    GDObjectState? GetObjectState(GDSymbolInfo symbol, int line, int column);
}
