using System.Collections.Generic;
using System.Linq;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for code diagnostics (validation + linting).
/// Wraps GDDiagnosticsService.
/// </summary>
public class GDDiagnosticsHandler : IGDDiagnosticsHandler
{
    protected readonly GDScriptProject _project;
    protected readonly GDDiagnosticsService _service;

    public GDDiagnosticsHandler(GDScriptProject project)
    {
        _project = project;
        _service = new GDDiagnosticsService();
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDUnifiedDiagnostic> AnalyzeFile(string filePath)
    {
        var file = _project.GetScript(filePath);
        if (file == null)
            return [];

        var result = _service.Diagnose(file);
        return result.Diagnostics.ToList();
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDUnifiedDiagnostic> AnalyzeProject()
    {
        var allDiagnostics = new List<GDUnifiedDiagnostic>();
        foreach (var file in _project.ScriptFiles)
        {
            var result = _service.Diagnose(file);
            allDiagnostics.AddRange(result.Diagnostics);
        }
        return allDiagnostics;
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDUnifiedDiagnostic>? GetCachedDiagnostics(string filePath)
    {
        // Base implementation doesn't cache
        return null;
    }
}
