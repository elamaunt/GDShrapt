using System.Collections.Generic;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for code diagnostics (validation + linting).
/// </summary>
public interface IGDDiagnosticsHandler
{
    /// <summary>
    /// Analyzes a file and returns diagnostics.
    /// </summary>
    /// <param name="filePath">Path to the file to analyze.</param>
    /// <returns>List of diagnostics found.</returns>
    IReadOnlyList<GDUnifiedDiagnostic> AnalyzeFile(string filePath);

    /// <summary>
    /// Analyzes the entire project and returns diagnostics.
    /// </summary>
    /// <returns>List of diagnostics found across all files.</returns>
    IReadOnlyList<GDUnifiedDiagnostic> AnalyzeProject();

    /// <summary>
    /// Gets cached diagnostics for a file if available.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>Cached diagnostics or null.</returns>
    IReadOnlyList<GDUnifiedDiagnostic>? GetCachedDiagnostics(string filePath);
}
