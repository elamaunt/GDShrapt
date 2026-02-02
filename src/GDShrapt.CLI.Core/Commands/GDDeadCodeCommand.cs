using System;
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

    protected override Task<int> ExecuteOnProjectAsync(
        GDScriptProject project,
        string projectRoot,
        GDProjectConfig config,
        CancellationToken cancellationToken)
    {
        // Create semantic model for accurate dead code detection
        var projectModel = new GDProjectSemanticModel(project);
        var handler = Registry?.GetService<IGDDeadCodeHandler>() ?? new GDDeadCodeHandler(projectModel);

        var options = new GDDeadCodeOptions
        {
            MaxConfidence = GDReferenceConfidence.Strict, // Base: only Strict
            IncludeVariables = _options.IncludeVariables,
            IncludeFunctions = _options.IncludeFunctions,
            IncludeSignals = _options.IncludeSignals,
            IncludeParameters = _options.IncludeParameters,
            IncludePrivate = _options.IncludePrivate,
            IncludeUnreachable = _options.IncludeUnreachable
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

        // Filter and group results
        var items = report.Items.AsEnumerable();

        if (!string.IsNullOrEmpty(_options.Kind))
        {
            if (Enum.TryParse<GDDeadCodeKind>(_options.Kind, true, out var kind))
                items = items.Where(i => i.Kind == kind);
        }

        // Output results
        WriteDeadCodeOutput(items.ToList(), projectRoot);

        // Fail conditions
        var count = items.Count();
        if (_options.FailIfFound && count > 0)
        {
            _formatter.WriteError(_output, $"Found {count} dead code items");
            return Task.FromResult(GDExitCode.WarningsOrHints);
        }

        return Task.FromResult(GDExitCode.Success);
    }

    private void WriteDeadCodeOutput(System.Collections.Generic.List<GDDeadCodeItem> items, string projectRoot)
    {
        if (items.Count == 0)
        {
            _formatter.WriteMessage(_output, "No dead code found.");
            return;
        }

        // Group by file
        var byFile = items.GroupBy(i => i.FilePath);

        _formatter.WriteMessage(_output, $"Dead Code Analysis: {items.Count} items found");
        _formatter.WriteMessage(_output, "");

        foreach (var fileGroup in byFile.OrderBy(g => g.Key))
        {
            var relPath = GetRelativePath(fileGroup.Key, projectRoot);
            _formatter.WriteMessage(_output, $"{relPath}:");

            foreach (var item in fileGroup.OrderBy(i => i.Line))
            {
                var confidence = item.Confidence == GDReferenceConfidence.Strict ? "" : $" [{item.Confidence}]";
                _formatter.WriteMessage(_output, $"  {item.Line}:{item.Column} {item.Kind}: {item.Name}{confidence}");
                if (!string.IsNullOrEmpty(item.Reason))
                {
                    _formatter.WriteMessage(_output, $"    Reason: {item.Reason}");
                }
            }
            _formatter.WriteMessage(_output, "");
        }

        // Summary by kind
        _formatter.WriteMessage(_output, "Summary:");
        var byKind = items.GroupBy(i => i.Kind);
        foreach (var kindGroup in byKind.OrderBy(g => g.Key))
        {
            _formatter.WriteMessage(_output, $"  {kindGroup.Key}: {kindGroup.Count()}");
        }
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
}
