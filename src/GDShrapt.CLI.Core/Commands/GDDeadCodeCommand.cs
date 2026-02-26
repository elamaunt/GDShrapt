using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Command to find unused code (dead code analysis).
/// Base implementation uses only Strict confidence.
/// </summary>
public class GDDeadCodeCommand : GDProjectCommandBase
{
    private readonly GDDeadCodeCommandOptions _options;

    public override string Name => "dead-code";
    public override string Description => "Find unused code (variables, functions, signals)";

    public GDDeadCodeCommand(
        string projectPath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        GDProjectConfig? config = null,
        IGDLogger? logger = null,
        GDDeadCodeCommandOptions? options = null)
        : base(projectPath, formatter, output, config, logger)
    {
        _options = options ?? new GDDeadCodeCommandOptions();
    }

    protected override GDScriptProjectOptions? GetProjectOptions()
    {
        return new GDScriptProjectOptions
        {
            EnableCallSiteRegistry = true
        };
    }

    protected override Task<int> ExecuteOnProjectAsync(
        GDScriptProject project,
        string projectRoot,
        GDProjectConfig config,
        CancellationToken cancellationToken)
    {
        // Build call site registry for cross-file call tracking
        project.BuildCallSiteRegistry(cancellationToken);

        // Create semantic model for accurate dead code detection
        var projectModel = new GDProjectSemanticModel(project);
        var handler = Registry?.GetService<IGDDeadCodeHandler>() ?? new GDDeadCodeHandler(projectModel);

        // Merge CLI --exclude patterns with config exclude patterns
        var excludePatterns = config.Cli.Exclude.ToList();
        if (_options.ExcludePatterns.Count > 0)
        {
            foreach (var p in _options.ExcludePatterns)
            {
                if (!excludePatterns.Contains(p))
                    excludePatterns.Add(p);
            }
        }

        var options = new GDDeadCodeOptions
        {
            MaxConfidence = GDReferenceConfidence.Strict, // Base: only Strict
            IncludeVariables = _options.IncludeVariables,
            IncludeFunctions = _options.IncludeFunctions,
            IncludeSignals = _options.IncludeSignals,
            IncludeParameters = _options.IncludeParameters,
            IncludePrivate = _options.IncludePrivate,
            IncludeUnreachable = _options.IncludeUnreachable,
            ExcludeTestFiles = _options.ExcludeTests,
            ExcludePatterns = excludePatterns,
            CollectEvidence = _options.Explain,
            CollectDroppedByReflection = true,
            TreatClassNameAsPublicAPI = false, // CLI has full project visibility
            RespectSuppressionAnnotations = !_options.NoSuppressAnnotations,
            CustomSuppressionAnnotations = new HashSet<string>(_options.SuppressAnnotations, StringComparer.OrdinalIgnoreCase)
        };

        // Analyze based on scope
        GDDeadCodeReport report;
        if (!string.IsNullOrEmpty(_options.FilePath))
        {
            var fullPath = Path.GetFullPath(Path.Combine(projectRoot, _options.FilePath));
            report = handler.AnalyzeFile(fullPath, options);
        }
        else
        {
            report = handler.AnalyzeProject(options);
        }

        // Filter by kind
        var items = report.Items.AsEnumerable();

        if (!string.IsNullOrEmpty(_options.Kind))
        {
            if (Enum.TryParse<GDDeadCodeKind>(_options.Kind, true, out var kind))
                items = items.Where(i => i.Kind == kind);
        }

        var filteredReport = new GDDeadCodeReport(items.ToList())
        {
            FilesAnalyzed = report.FilesAnalyzed,
            SceneSignalConnectionsConsidered = report.SceneSignalConnectionsConsidered,
            VirtualMethodsSkipped = report.VirtualMethodsSkipped,
            AutoloadsResolved = report.AutoloadsResolved,
            TotalCallSitesRegistered = report.TotalCallSitesRegistered,
            CSharpCodeDetected = report.CSharpCodeDetected,
            CSharpInteropExcluded = report.CSharpInteropExcluded,
            DroppedByReflection = report.DroppedByReflection,
            AnnotationSuppressedCount = report.AnnotationSuppressedCount
        };

        // Output results
        if (_options.Quiet)
        {
            WriteQuietOutput(filteredReport);
        }
        else
        {
            WriteDeadCodeOutput(filteredReport, projectRoot);
        }

        // Fail conditions
        var count = filteredReport.Items.Count;
        if (_options.FailIfFound && count > 0)
        {
            _formatter.WriteError(_output, $"Found {count} dead code items");
            return Task.FromResult(GDExitCode.WarningsOrHints);
        }

        return Task.FromResult(GDExitCode.Success);
    }

    private void WriteQuietOutput(GDDeadCodeReport report)
    {
        if (report.Items.Count == 0)
        {
            _formatter.WriteMessage(_output, "dead-code: 0 items");
            return;
        }

        _formatter.WriteMessage(_output, GDDeadCodeOutputHelper.FormatSummaryLine(report));
    }

    private void WriteDeadCodeOutput(GDDeadCodeReport report, string projectRoot)
    {
        if (report.Items.Count == 0)
        {
            _formatter.WriteMessage(_output, "No dead code found.");
            return;
        }

        // Header with suppressed counts
        var suppressedParts = new List<string>();
        if (report.DroppedByReflection.Count > 0)
            suppressedParts.Add($"{report.DroppedByReflection.Count} by reflection");
        if (report.AnnotationSuppressedCount > 0)
            suppressedParts.Add($"{report.AnnotationSuppressedCount} by annotations");

        var suppressedSuffix = suppressedParts.Count > 0
            ? GDAnsiColors.Dim($" (+{string.Join(", ", suppressedParts)} suppressed)")
            : "";
        _formatter.WriteMessage(_output, $"{GDAnsiColors.Bold("Dead Code Analysis:")} {GDAnsiColors.Cyan(report.Items.Count.ToString())} items found{suppressedSuffix}");
        _formatter.WriteMessage(_output, "");

        // Top files
        GDDeadCodeOutputHelper.WriteTopOffenders(_formatter, _output, report, projectRoot, _options.TopN ?? 5);
        _formatter.WriteMessage(_output, "");

        // By kind summary (non-zero only by default, all with --verbose/--debug)
        GDDeadCodeOutputHelper.WriteKindSummary(_formatter, _output, report, _options.Verbose);
        _formatter.WriteMessage(_output, "");

        // By confidence summary
        GDDeadCodeOutputHelper.WriteConfidenceSummary(_formatter, _output, report);
        _formatter.WriteMessage(_output, "");

        // Legend (only codes present in results)
        GDDeadCodeOutputHelper.WriteLegend(_formatter, _output, report.Items.Select(i => i.ReasonCode));
        _formatter.WriteMessage(_output, "");

        // Analysis scope (--explain mode only)
        if (_options.Explain)
        {
            GDDeadCodeOutputHelper.WriteAnalysisScope(_formatter, _output, report);
            _formatter.WriteMessage(_output, "");
        }

        // Group by file, optionally limit to top N files
        var byFile = report.Items.GroupBy(i => i.FilePath)
            .OrderByDescending(g => g.Count())
            .AsEnumerable();

        if (_options.TopN.HasValue)
            byFile = byFile.Take(_options.TopN.Value);

        foreach (var fileGroup in byFile)
        {
            var relPath = GetRelativePath(fileGroup.Key, projectRoot);
            _formatter.WriteMessage(_output, GDAnsiColors.Bold($"{relPath}:"));

            var maxWidth = fileGroup.Max(i => GDDeadCodeOutputHelper.GetItemTextWidth(i));
            foreach (var item in fileGroup.OrderBy(i => i.Line))
            {
                GDDeadCodeOutputHelper.WriteItem(_formatter, _output, item, _options.Explain, maxWidth);
            }
            _formatter.WriteMessage(_output, "");
        }

        // Suppressed by reflection section
        if (report.DroppedByReflection.Count > 0)
        {
            _formatter.WriteMessage(_output, "");
            GDDeadCodeOutputHelper.WriteDroppedByReflection(_formatter, _output, report, projectRoot,
                limit: _options.ShowDroppedByReflection ? 0 : 5);
        }

        // Annotation hint (once, when annotations are enabled)
        if (!_options.NoSuppressAnnotations)
            GDDeadCodeOutputHelper.WriteAnnotationHint(_formatter, _output);

        // Tip
        GDDeadCodeOutputHelper.WriteTip(_formatter, _output, _options);
    }
}

/// <summary>
/// Options for dead-code command.
/// </summary>
public class GDDeadCodeCommandOptions
{
    /// <summary>
    /// Optional specific file to analyze.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Include unused variables.
    /// </summary>
    public bool IncludeVariables { get; set; } = true;

    /// <summary>
    /// Include unused functions.
    /// </summary>
    public bool IncludeFunctions { get; set; } = true;

    /// <summary>
    /// Include unused signals.
    /// </summary>
    public bool IncludeSignals { get; set; } = true;

    /// <summary>
    /// Include unused parameters.
    /// </summary>
    public bool IncludeParameters { get; set; }

    /// <summary>
    /// Include private members (starting with _).
    /// </summary>
    public bool IncludePrivate { get; set; }

    /// <summary>
    /// Include unreachable code.
    /// </summary>
    public bool IncludeUnreachable { get; set; } = true;

    /// <summary>
    /// Filter by kind (Variable, Function, Signal, Parameter, Unreachable).
    /// </summary>
    public string? Kind { get; set; }

    /// <summary>
    /// Fail if any dead code is found (for CI).
    /// </summary>
    public bool FailIfFound { get; set; }

    /// <summary>
    /// Show only the top N files by dead code count.
    /// </summary>
    public int? TopN { get; set; }

    /// <summary>
    /// Exclude test files from analysis.
    /// </summary>
    public bool ExcludeTests { get; set; }

    /// <summary>
    /// Show detailed evidence for each item.
    /// </summary>
    public bool Explain { get; set; }

    /// <summary>
    /// Output a single summary line (for CI).
    /// </summary>
    public bool Quiet { get; set; }

    /// <summary>
    /// Show items excluded from report because they are reachable via reflection patterns.
    /// </summary>
    public bool ShowDroppedByReflection { get; set; }

    /// <summary>
    /// Verbose output (show all kind breakdowns including zero-count).
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// Disable annotation-based suppression (@public_api, @dynamic_use).
    /// </summary>
    public bool NoSuppressAnnotations { get; set; }

    /// <summary>
    /// Custom annotation names that suppress dead code warnings.
    /// </summary>
    public List<string> SuppressAnnotations { get; set; } = new();

    /// <summary>
    /// Glob patterns to exclude files from analysis (from --exclude CLI option).
    /// </summary>
    public List<string> ExcludePatterns { get; set; } = new();
}
