using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Command to analyze type annotation coverage.
/// Shows how many variables, parameters, and return types have explicit type annotations.
/// </summary>
public class GDTypeCoverageCommand : GDProjectCommandBase
{
    private readonly GDTypeCoverageOptions _options;

    public override string Name => "type-coverage";
    public override string Description => "Analyze type annotation coverage";

    public GDTypeCoverageCommand(
        string projectPath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        GDProjectConfig? config = null,
        IGDLogger? logger = null,
        GDTypeCoverageOptions? options = null)
        : base(projectPath, formatter, output, config, logger)
    {
        _options = options ?? new GDTypeCoverageOptions();
    }

    protected override Task<int> ExecuteOnProjectAsync(
        GDScriptProject project,
        string projectRoot,
        GDProjectConfig config,
        CancellationToken cancellationToken)
    {
        var handler = Registry?.GetService<IGDTypeCoverageHandler>() ?? new GDTypeCoverageHandler(new GDProjectSemanticModel(project));

        // Analyze based on scope
        GDTypeCoverageReport report;
        if (!string.IsNullOrEmpty(_options.FilePath))
        {
            var fullPath = Path.GetFullPath(Path.Combine(projectRoot, _options.FilePath));
            report = handler.AnalyzeFile(fullPath);
        }
        else
        {
            report = handler.AnalyzeProject();
        }

        // Output results
        WriteTypeCoverageOutput(report);

        // Fail conditions
        if (_options.FailBelowCoverage > 0 && report.AnnotationCoverage < _options.FailBelowCoverage)
        {
            _formatter.WriteError(_output, $"Type annotation coverage ({report.AnnotationCoverage:F1}%) is below threshold ({_options.FailBelowCoverage}%)");
            return Task.FromResult(GDExitCode.WarningsOrHints);
        }

        if (_options.FailBelowEffective > 0 && report.EffectiveCoverage < _options.FailBelowEffective)
        {
            _formatter.WriteError(_output, $"Effective type coverage ({report.EffectiveCoverage:F1}%) is below threshold ({_options.FailBelowEffective}%)");
            return Task.FromResult(GDExitCode.WarningsOrHints);
        }

        return Task.FromResult(GDExitCode.Success);
    }

    private void WriteTypeCoverageOutput(GDTypeCoverageReport report)
    {
        _formatter.WriteMessage(_output, "Type Coverage Report");
        _formatter.WriteMessage(_output, "====================");
        _formatter.WriteMessage(_output, "");

        // Variables
        _formatter.WriteMessage(_output, "Variables:");
        _formatter.WriteMessage(_output, $"  Total: {report.TotalVariables}");
        _formatter.WriteMessage(_output, $"  Annotated: {report.AnnotatedVariables} ({report.AnnotationCoverage:F1}%)");
        _formatter.WriteMessage(_output, $"  Inferred: {report.InferredVariables}");
        _formatter.WriteMessage(_output, $"  Variant: {report.VariantVariables} ({report.VariantPercentage:F1}%)");
        _formatter.WriteMessage(_output, "");

        // Parameters
        _formatter.WriteMessage(_output, "Parameters:");
        _formatter.WriteMessage(_output, $"  Total: {report.TotalParameters}");
        _formatter.WriteMessage(_output, $"  Annotated: {report.AnnotatedParameters} ({report.ParameterCoverage:F1}%)");
        _formatter.WriteMessage(_output, $"  Inferred: {report.InferredParameters}");
        _formatter.WriteMessage(_output, "");

        // Return Types
        _formatter.WriteMessage(_output, "Return Types:");
        _formatter.WriteMessage(_output, $"  Total: {report.TotalReturnTypes}");
        _formatter.WriteMessage(_output, $"  Annotated: {report.AnnotatedReturnTypes} ({report.ReturnTypeCoverage:F1}%)");
        _formatter.WriteMessage(_output, $"  Inferred: {report.InferredReturnTypes}");
        _formatter.WriteMessage(_output, "");

        // Summary scores
        _formatter.WriteMessage(_output, "Summary:");
        _formatter.WriteMessage(_output, $"  Annotation Coverage: {report.AnnotationCoverage:F1}%");
        _formatter.WriteMessage(_output, $"  Effective Coverage: {report.EffectiveCoverage:F1}% (annotated + inferred)");
        _formatter.WriteMessage(_output, $"  Type Safety Score: {report.TypeSafetyScore:F1}%");
        _formatter.WriteMessage(_output, "");

        // Progress bar visualization
        WriteProgressBar("Annotation", report.AnnotationCoverage);
        WriteProgressBar("Effective ", report.EffectiveCoverage);
        WriteProgressBar("Type Safe ", report.TypeSafetyScore);
    }

    private void WriteProgressBar(string label, double percentage)
    {
        const int barWidth = 40;
        int filled = (int)(percentage / 100 * barWidth);
        filled = Math.Max(0, Math.Min(barWidth, filled));

        var bar = new string('█', filled) + new string('░', barWidth - filled);
        _formatter.WriteMessage(_output, $"  {label}: [{bar}] {percentage:F1}%");
    }
}

/// <summary>
/// Options for type-coverage command.
/// </summary>
public class GDTypeCoverageOptions
{
    /// <summary>
    /// Optional specific file to analyze.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Fail if annotation coverage is below this percentage (for CI).
    /// Pro feature.
    /// </summary>
    public double FailBelowCoverage { get; set; }

    /// <summary>
    /// Fail if effective coverage (annotated + inferred) is below this percentage (for CI).
    /// Pro feature.
    /// </summary>
    public double FailBelowEffective { get; set; }
}
