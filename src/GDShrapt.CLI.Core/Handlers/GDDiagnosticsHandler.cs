using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;
using GDShrapt.Semantics;
using GDShrapt.Semantics.Validator;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for code diagnostics (validation + linting + semantic validation).
/// Wraps GDDiagnosticsService and GDSemanticValidator into a unified pipeline.
/// </summary>
public class GDDiagnosticsHandler : IGDDiagnosticsHandler
{
    protected readonly GDScriptProject _project;
    protected readonly GDDiagnosticsService _service;
    protected readonly GDProjectConfig? _config;

    public GDDiagnosticsHandler(GDScriptProject project, GDProjectConfig? config = null)
    {
        _project = project;
        _config = config;
        _service = config != null ? GDDiagnosticsService.FromConfig(config) : new GDDiagnosticsService();
    }

    /// <summary>
    /// Runs the full diagnostic pipeline on a script: syntax + validator + linter + semantic validator.
    /// This is the single entry point for all consumers (CLI, LSP, Plugin).
    /// </summary>
    public static GDDiagnosticsResult DiagnoseWithSemantics(
        GDScriptFile script,
        GDDiagnosticsService diagnosticsService,
        GDSemanticValidatorOptions? semanticOptions = null,
        GDProjectConfig? config = null)
    {
        var result = diagnosticsService.Diagnose(script);

        if (script.SemanticModel != null && script.Class != null)
        {
            var options = semanticOptions ?? GDSemanticValidatorOptions.Default;
            var validator = new GDSemanticValidator(script.SemanticModel, options);
            var semanticResult = validator.Validate(script.Class);

            foreach (var diag in semanticResult.Diagnostics)
            {
                var ruleId = diag.CodeString;

                if (config?.Linting.Rules.TryGetValue(ruleId, out var rc) == true && !rc.Enabled)
                    continue;

                var severity = GDSeverityMapper.FromValidator(diag.Severity);

                if (config?.Linting.Rules.TryGetValue(ruleId, out var rc2) == true && rc2.Severity.HasValue)
                    severity = GDSeverityMapper.FromValidator(rc2.Severity.Value);

                result.Add(new GDUnifiedDiagnostic
                {
                    Code = ruleId,
                    Message = diag.Message,
                    Severity = severity,
                    Source = GDDiagnosticSource.SemanticValidator,
                    StartLine = diag.StartLine,
                    StartColumn = diag.StartColumn,
                    EndLine = diag.EndLine,
                    EndColumn = diag.EndColumn
                });
            }
        }

        return result;
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDUnifiedDiagnostic> AnalyzeFile(string filePath)
    {
        var file = _project.GetScript(filePath);
        if (file == null)
            return [];

        return DiagnoseWithSemantics(file, _service, config: _config).Diagnostics.ToList();
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDUnifiedDiagnostic> AnalyzeProject()
    {
        var allDiagnostics = new List<GDUnifiedDiagnostic>();
        foreach (var file in _project.ScriptFiles)
        {
            allDiagnostics.AddRange(DiagnoseWithSemantics(file, _service, config: _config).Diagnostics);
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
