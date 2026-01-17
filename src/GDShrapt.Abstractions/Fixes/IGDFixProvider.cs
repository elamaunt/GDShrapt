using System.Collections.Generic;
using GDShrapt.Reader;

namespace GDShrapt.Abstractions;

/// <summary>
/// Interface for generating code fix descriptors for diagnostics.
/// </summary>
public interface IGDFixProvider
{
    /// <summary>
    /// Gets available fixes for a diagnostic.
    /// </summary>
    /// <param name="diagnosticCode">The diagnostic code (e.g., "GD7002").</param>
    /// <param name="node">The AST node associated with the diagnostic.</param>
    /// <param name="analyzer">Optional member access analyzer for type inference.</param>
    /// <param name="runtimeProvider">Optional runtime provider for type information.</param>
    /// <returns>Collection of available fix descriptors.</returns>
    IEnumerable<GDFixDescriptor> GetFixes(
        string diagnosticCode,
        GDNode? node,
        IGDMemberAccessAnalyzer? analyzer,
        IGDRuntimeProvider? runtimeProvider);
}
