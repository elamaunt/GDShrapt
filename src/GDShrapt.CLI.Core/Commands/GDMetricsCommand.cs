using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Command to analyze and display code complexity metrics.
/// </summary>
public class GDMetricsCommand : GDProjectCommandBase
{
    private readonly GDMetricsOptions _options;

    public override string Name => "metrics";
    public override string Description => "Calculate code complexity metrics";

    public GDMetricsCommand(
        string projectPath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        GDProjectConfig? config = null,
        IGDLogger? logger = null,
        GDMetricsOptions? options = null)
        : base(projectPath, formatter, output, config, logger)
    {
        _options = options ?? new GDMetricsOptions();
    }

    protected override Task<int> ExecuteOnProjectAsync(
        GDScriptProject project,
        string projectRoot,
        GDProjectConfig config,
        CancellationToken cancellationToken)
    {
        var projectModel = new GDProjectSemanticModel(project);
        var handler = Registry?.GetService<IGDMetricsHandler>() ?? new GDMetricsHandler(projectModel);

        // Analyze based on scope
        GDProjectMetrics metrics;
        if (!string.IsNullOrEmpty(_options.FilePath))
        {
            var fullPath = Path.GetFullPath(Path.Combine(projectRoot, _options.FilePath));
            var fileMetrics = handler.AnalyzeFile(fullPath);
            metrics = new GDProjectMetrics
            {
                FileCount = 1,
                TotalLines = fileMetrics.TotalLines,
                CodeLines = fileMetrics.CodeLines,
                CommentLines = fileMetrics.CommentLines,
                ClassCount = fileMetrics.ClassCount,
                MethodCount = fileMetrics.MethodCount,
                SignalCount = fileMetrics.SignalCount,
                AverageComplexity = fileMetrics.AverageComplexity,
                AverageMaintainability = fileMetrics.MaintainabilityIndex,
                Files = new[] { fileMetrics }.ToList()
            };
        }
        else
        {
            metrics = handler.AnalyzeProject();
        }

        // Apply sorting and top-N
        var files = metrics.Files.AsEnumerable();

        files = _options.SortBy?.ToLowerInvariant() switch
        {
            "complexity" => files.OrderByDescending(f => f.MaxComplexity),
            "lines" => files.OrderByDescending(f => f.TotalLines),
            "methods" => files.OrderByDescending(f => f.MethodCount),
            "maintainability" => files.OrderBy(f => f.MaintainabilityIndex),
            _ => files
        };

        if (_options.Top > 0)
            files = files.Take(_options.Top);

        // Output based on format
        WriteMetricsOutput(metrics, files.ToList(), projectRoot, projectModel);

        // Fail conditions
        if (_options.FailAboveComplexity > 0 && metrics.Files.Any(f => f.MaxComplexity > _options.FailAboveComplexity))
        {
            _formatter.WriteError(_output, $"Files with complexity > {_options.FailAboveComplexity} found");
            return Task.FromResult(GDExitCode.WarningsOrHints);
        }

        return Task.FromResult(GDExitCode.Success);
    }

    private void WriteMetricsOutput(GDProjectMetrics metrics, System.Collections.Generic.List<GDFileMetrics> files, string projectRoot, GDProjectSemanticModel projectModel)
    {
        // Summary
        _formatter.WriteMessage(_output, GDAnsiColors.Bold("Project Metrics Summary:"));
        _formatter.WriteMessage(_output, $"  Files:              {GDAnsiColors.Cyan($"{metrics.FileCount}")}");
        _formatter.WriteMessage(_output, $"  Total Lines:        {GDAnsiColors.Cyan($"{metrics.TotalLines:N0}")}");
        _formatter.WriteMessage(_output, $"  Code Lines:         {GDAnsiColors.Cyan($"{metrics.CodeLines:N0}")}");
        _formatter.WriteMessage(_output, $"  Comment Lines:      {GDAnsiColors.Cyan($"{metrics.CommentLines:N0}")}");
        _formatter.WriteMessage(_output, $"  Methods:            {GDAnsiColors.Cyan($"{metrics.MethodCount}")}");
        _formatter.WriteMessage(_output, $"  Signals:            {GDAnsiColors.Cyan($"{metrics.SignalCount}")}");
        _formatter.WriteMessage(_output, $"  Avg Complexity:     {ColorCC(metrics.AverageComplexity)}");
        _formatter.WriteMessage(_output, $"  Avg Maintainability:{ColorMI(metrics.AverageMaintainability)}");
        _formatter.WriteMessage(_output, "");

        // Scene Metrics
        var sceneReport = projectModel.SceneFlow.AnalyzeProject();
        if (sceneReport.TotalScenes > 0)
        {
            _formatter.WriteMessage(_output, GDAnsiColors.Bold("Scene Metrics:"));
            _formatter.WriteMessage(_output, $"  Total Scenes:       {GDAnsiColors.Cyan($"{sceneReport.TotalScenes}")}");

            var totalNodes = 0;
            var sceneNodeCounts = new System.Collections.Generic.List<(string path, int nodeCount, int subSceneCount)>();
            foreach (var kvp in sceneReport.Scenes)
            {
                var nodeCount = kvp.Value.SceneInfo?.Nodes.Count ?? 0;
                var subCount = kvp.Value.SubScenes.Count;
                totalNodes += nodeCount;
                sceneNodeCounts.Add((kvp.Key, nodeCount, subCount));
            }

            _formatter.WriteMessage(_output, $"  Total Scene Nodes:  {GDAnsiColors.Cyan($"{totalNodes}")}");
            if (sceneReport.TotalScenes > 0)
            {
                _formatter.WriteMessage(_output, $"  Avg Nodes/Scene:    {GDAnsiColors.Cyan($"{(double)totalNodes / sceneReport.TotalScenes:F1}")}");
            }
            _formatter.WriteMessage(_output, "");

            var largestScenes = sceneNodeCounts.OrderByDescending(s => s.nodeCount).Take(5).ToList();
            if (largestScenes.Count > 0)
            {
                _formatter.WriteMessage(_output, GDAnsiColors.Bold("  Largest Scenes:"));
                foreach (var scene in largestScenes)
                {
                    var relPath = GetRelativePath(scene.path, projectRoot);
                    _formatter.WriteMessage(_output, $"    {GDAnsiColors.Bold(relPath)}: {GDAnsiColors.Cyan($"{scene.nodeCount}")} nodes, {GDAnsiColors.Cyan($"{scene.subSceneCount}")} sub-scenes");
                }
            }
            _formatter.WriteMessage(_output, "");
        }

        if (_options.ShowFiles)
        {
            _formatter.WriteMessage(_output, GDAnsiColors.Bold("File Details:"));
            foreach (var file in files)
            {
                var relPath = GetRelativePath(file.FilePath, projectRoot);
                _formatter.WriteMessage(_output, $"  {GDAnsiColors.Bold(relPath)}:");
                _formatter.WriteMessage(_output, $"    Lines: {GDAnsiColors.Cyan($"{file.TotalLines}")}, Methods: {GDAnsiColors.Cyan($"{file.MethodCount}")}, Max CC: {ColorCC(file.MaxComplexity)}, MI: {ColorMI(file.MaintainabilityIndex, "F1")}");

                if (_options.ShowMethods && file.Methods.Count > 0)
                {
                    foreach (var method in file.Methods.OrderByDescending(m => m.CyclomaticComplexity).Take(5))
                    {
                        _formatter.WriteMessage(_output, $"      {GDAnsiColors.Bold(method.Name)}: CC={ColorCC(method.CyclomaticComplexity)}, Cog={GDAnsiColors.Cyan($"{method.CognitiveComplexity}")}, Nesting={GDAnsiColors.Cyan($"{method.NestingDepth}")}");
                    }
                }
            }
        }
    }

    private static string ColorCC(double cc, string format = "F2") => cc switch
    {
        <= 10 => GDAnsiColors.Green(cc.ToString(format)),
        <= 20 => GDAnsiColors.Yellow(cc.ToString(format)),
        _ => GDAnsiColors.Red(cc.ToString(format))
    };

    private static string ColorMI(double mi, string format = "F2") => mi switch
    {
        >= 65 => GDAnsiColors.Green(mi.ToString(format)),
        >= 40 => GDAnsiColors.Yellow(mi.ToString(format)),
        _ => GDAnsiColors.Red(mi.ToString(format))
    };
}

/// <summary>
/// Options for metrics command.
/// </summary>
public class GDMetricsOptions
{
    /// <summary>
    /// Optional specific file to analyze.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Sort by: complexity, lines, methods, maintainability.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Show only top N files.
    /// </summary>
    public int Top { get; set; }

    /// <summary>
    /// Show file-level details.
    /// </summary>
    public bool ShowFiles { get; set; } = true;

    /// <summary>
    /// Show method-level details.
    /// </summary>
    public bool ShowMethods { get; set; }

    /// <summary>
    /// Fail if any file has complexity above this threshold.
    /// Pro feature for CI gates.
    /// </summary>
    public int FailAboveComplexity { get; set; }
}
