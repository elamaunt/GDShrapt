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
/// Graph layer selection for deps command.
/// </summary>
public enum GDDepsGraphLayer
{
    All,
    Code,
    Scenes,
    Signals
}

/// <summary>
/// Options for deps command.
/// </summary>
public class GDDepsOptions
{
    public string? FilePath { get; set; }
    public bool FailOnCycles { get; set; }
    public GDDepsGraphLayer GraphLayer { get; set; } = GDDepsGraphLayer.All;
    public int TopN { get; set; } = 10;
    public bool Explain { get; set; }
    public string? GroupByDir { get; set; }
    public int GroupDepth { get; set; } = 2;
    public string? Dir { get; set; }
    public bool ExcludeAddons { get; set; }
    public bool ExcludeTests { get; set; }
}

/// <summary>
/// Analyze code/scene/signal dependencies, find cycles and coupling hotspots.
/// </summary>
public class GDDepsCommand : GDProjectCommandBase
{
    private readonly GDDepsOptions _options;

    public override string Name => "deps";
    public override string Description => "Analyze dependencies, find cycles and coupling hotspots";

    public GDDepsCommand(
        string projectPath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        GDProjectConfig? config = null,
        IGDLogger? logger = null,
        GDDepsOptions? options = null)
        : base(projectPath, formatter, output, config, logger)
    {
        _options = options ?? new GDDepsOptions();
    }

    protected override Task<int> ExecuteOnProjectAsync(
        GDScriptProject project,
        string projectRoot,
        GDProjectConfig config,
        CancellationToken cancellationToken)
    {
        var projectModel = new GDProjectSemanticModel(project);
        var handler = Registry?.GetService<IGDDependencyHandler>() ?? new GDDependencyHandler(projectModel);

        // Single file mode
        if (!string.IsNullOrEmpty(_options.FilePath))
        {
            var fullPath = Path.GetFullPath(Path.Combine(projectRoot, _options.FilePath));
            var info = handler.AnalyzeFile(fullPath);
            WriteSingleFileOutput(info, projectRoot, handler);
            return Task.FromResult(GDExitCode.Success);
        }

        // Project-wide analysis
        var report = handler.AnalyzeProject();
        var filteredFiles = FilterFiles(report.Files, projectRoot);

        // Directory grouping mode
        if (!string.IsNullOrEmpty(_options.GroupByDir))
        {
            WriteDirectoryGroupedOutput(filteredFiles, report, projectRoot);
            if (_options.FailOnCycles && report.HasCycles)
            {
                _formatter.WriteError(_output, $"Found {report.CycleCount} circular dependencies");
                return Task.FromResult(GDExitCode.WarningsOrHints);
            }
            return Task.FromResult(GDExitCode.Success);
        }

        // Standard output by graph layer
        WriteHeader(report, filteredFiles, projectRoot);

        var layer = _options.GraphLayer;

        if (layer == GDDepsGraphLayer.All || layer == GDDepsGraphLayer.Code)
        {
            WriteCycles(report, projectRoot);
            WriteCodeMetrics(filteredFiles, projectRoot, handler);
        }

        if (layer == GDDepsGraphLayer.All || layer == GDDepsGraphLayer.Scenes)
        {
            WriteSceneMetrics(projectModel, projectRoot);
        }

        if (layer == GDDepsGraphLayer.All || layer == GDDepsGraphLayer.Signals)
        {
            WriteSignalMetrics(projectModel, projectRoot);
        }

        if (_options.FailOnCycles && report.HasCycles)
        {
            _formatter.WriteError(_output, $"Found {report.CycleCount} circular dependencies");
            return Task.FromResult(GDExitCode.WarningsOrHints);
        }

        return Task.FromResult(GDExitCode.Success);
    }

    #region Filtering

    private List<GDFileDependencyInfo> FilterFiles(IReadOnlyList<GDFileDependencyInfo> files, string projectRoot)
    {
        var result = files.AsEnumerable();

        if (!string.IsNullOrEmpty(_options.Dir))
        {
            var dirPrefix = _options.Dir.Replace('\\', '/').TrimEnd('/') + "/";
            result = result.Where(f =>
            {
                var rel = GetRelativePath(f.FilePath, projectRoot).Replace('\\', '/');
                return rel.StartsWith(dirPrefix, StringComparison.OrdinalIgnoreCase);
            });
        }

        if (_options.ExcludeAddons)
        {
            result = result.Where(f =>
            {
                var rel = GetRelativePath(f.FilePath, projectRoot).Replace('\\', '/');
                return !rel.StartsWith("addons/", StringComparison.OrdinalIgnoreCase);
            });
        }

        if (_options.ExcludeTests)
        {
            result = result.Where(f =>
            {
                var rel = GetRelativePath(f.FilePath, projectRoot).Replace('\\', '/');
                return !rel.StartsWith("test", StringComparison.OrdinalIgnoreCase)
                    && !rel.Contains("/test", StringComparison.OrdinalIgnoreCase);
            });
        }

        return result.ToList();
    }

    #endregion

    #region Header

    private void WriteHeader(GDProjectDependencyReport report, List<GDFileDependencyInfo> filteredFiles, string projectRoot)
    {
        var scriptCount = filteredFiles.Count(f => f.FilePath.EndsWith(".gd", StringComparison.OrdinalIgnoreCase));
        var sceneCount = filteredFiles.Count(f => f.FilePath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase));

        _formatter.WriteMessage(_output, GDAnsiColors.Bold("Dependency Analysis:"));

        if (sceneCount > 0)
            _formatter.WriteMessage(_output, $"  Total Files:  {GDAnsiColors.Cyan($"{scriptCount}")} scripts, {GDAnsiColors.Cyan($"{sceneCount}")} scenes");
        else
            _formatter.WriteMessage(_output, $"  Total Files:  {GDAnsiColors.Cyan($"{filteredFiles.Count}")}");

        var cycleLabel = report.CycleCount > 0
            ? GDAnsiColors.Red($"{report.CycleCount}")
            : GDAnsiColors.Cyan($"{report.CycleCount}");
        _formatter.WriteMessage(_output, $"  Cycles:       {cycleLabel}");
        _formatter.WriteMessage(_output, "");
    }

    #endregion

    #region Cycles

    private void WriteCycles(GDProjectDependencyReport report, string projectRoot)
    {
        if (!report.HasCycles)
            return;

        _formatter.WriteMessage(_output, GDAnsiColors.Bold("Circular Dependencies:"));
        int cycleNum = 1;
        foreach (var cycle in report.Cycles)
        {
            _formatter.WriteMessage(_output, $"  Cycle {cycleNum++}:");
            foreach (var file in cycle)
            {
                var relPath = GetRelativePath(file, projectRoot);
                _formatter.WriteMessage(_output, $"    -> {GDAnsiColors.Red(relPath)}");
            }
        }
        _formatter.WriteMessage(_output, "");
    }

    #endregion

    #region Code Metrics

    private void WriteCodeMetrics(List<GDFileDependencyInfo> files, string projectRoot, IGDDependencyHandler handler)
    {
        var topN = _options.TopN;

        // Fan-in: most depended-on
        var fanInTop = files.OrderByDescending(f => f.Dependents.Count)
            .Where(f => f.Dependents.Count > 0)
            .Take(topN)
            .ToList();

        if (fanInTop.Count > 0)
        {
            _formatter.WriteMessage(_output, GDAnsiColors.Bold($"Top {Math.Min(topN, fanInTop.Count)} by Fan-in (most depended-on):"));
            foreach (var file in fanInTop)
            {
                var relPath = GetRelativePath(file.FilePath, projectRoot);
                _formatter.WriteMessage(_output, $"  {GDAnsiColors.Bold(relPath)}: {GDAnsiColors.Cyan($"{file.Dependents.Count}")} dependents");
                if (_options.Explain)
                    WriteExplainIncoming(file, projectRoot, handler);
            }
            _formatter.WriteMessage(_output, "");
        }

        // Fan-out: most dependencies
        var fanOutTop = files.OrderByDescending(f => f.DirectDependencyCount)
            .Where(f => f.DirectDependencyCount > 0)
            .Take(topN)
            .ToList();

        if (fanOutTop.Count > 0)
        {
            _formatter.WriteMessage(_output, GDAnsiColors.Bold($"Top {Math.Min(topN, fanOutTop.Count)} by Fan-out (most dependencies):"));
            foreach (var file in fanOutTop)
            {
                var relPath = GetRelativePath(file.FilePath, projectRoot);
                _formatter.WriteMessage(_output, $"  {GDAnsiColors.Bold(relPath)}: {GDAnsiColors.Cyan($"{file.DirectDependencyCount}")} dependencies");
                if (_options.Explain)
                    WriteExplainOutgoing(file, projectRoot, handler);
            }
            _formatter.WriteMessage(_output, "");
        }

        // Combined coupling
        var coupledTop = files.OrderByDescending(f => f.Dependents.Count + f.DirectDependencyCount)
            .Where(f => f.Dependents.Count + f.DirectDependencyCount > 0)
            .Take(topN)
            .ToList();

        if (coupledTop.Count > 0)
        {
            _formatter.WriteMessage(_output, GDAnsiColors.Bold($"Top {Math.Min(topN, coupledTop.Count)} by Coupling (fan-in + fan-out):"));
            foreach (var file in coupledTop)
            {
                var relPath = GetRelativePath(file.FilePath, projectRoot);
                var total = file.Dependents.Count + file.DirectDependencyCount;
                _formatter.WriteMessage(_output,
                    $"  {GDAnsiColors.Bold(relPath)}: {GDAnsiColors.Cyan($"{total}")} ({file.Dependents.Count} in + {file.DirectDependencyCount} out)");
            }
            _formatter.WriteMessage(_output, "");
        }
    }

    private void WriteExplainIncoming(GDFileDependencyInfo file, string projectRoot, IGDDependencyHandler handler)
    {
        var edges = handler.GetFileEdges(file.FilePath);
        foreach (var edge in edges.Incoming.Take(_options.TopN))
        {
            var fromRel = GetRelativePath(edge.FromPath, projectRoot);
            _formatter.WriteMessage(_output, $"    <- {GDAnsiColors.Dim(fromRel)} ({EdgeKindLabel(edge.Kind)})");
        }
        if (edges.Incoming.Count > _options.TopN)
            _formatter.WriteMessage(_output, $"    {GDAnsiColors.Dim($"... and {edges.Incoming.Count - _options.TopN} more")}");
    }

    private void WriteExplainOutgoing(GDFileDependencyInfo file, string projectRoot, IGDDependencyHandler handler)
    {
        var edges = handler.GetFileEdges(file.FilePath);
        foreach (var edge in edges.Outgoing.Take(_options.TopN))
        {
            var toRel = GetRelativePath(edge.ToPath, projectRoot);
            _formatter.WriteMessage(_output, $"    -> {GDAnsiColors.Dim(toRel)} ({EdgeKindLabel(edge.Kind)})");
        }
        if (edges.Outgoing.Count > _options.TopN)
            _formatter.WriteMessage(_output, $"    {GDAnsiColors.Dim($"... and {edges.Outgoing.Count - _options.TopN} more")}");
    }

    #endregion

    #region Scene Metrics

    private void WriteSceneMetrics(GDProjectSemanticModel projectModel, string projectRoot)
    {
        var sceneReport = projectModel.SceneFlow.AnalyzeProject();
        if (sceneReport.TotalScenes == 0)
            return;

        _formatter.WriteMessage(_output, GDAnsiColors.Bold("Scene Dependencies:"));
        _formatter.WriteMessage(_output, $"  Scenes:                   {GDAnsiColors.Cyan($"{sceneReport.TotalScenes}")}");
        _formatter.WriteMessage(_output, $"  Sub-scene edges:          {GDAnsiColors.Cyan($"{sceneReport.StaticSubSceneCount}")}");
        _formatter.WriteMessage(_output, $"  Code instantiation edges: {GDAnsiColors.Cyan($"{sceneReport.CodeInstantiationCount}")}");
        _formatter.WriteMessage(_output, "");

        // Scene hubs: scenes with most edges
        var sceneHubs = sceneReport.AllEdges
            .GroupBy(e => e.SourceScene)
            .Select(g =>
            {
                var subScene = g.Count(e => e.EdgeType == GDSceneFlowEdgeType.StaticSubScene);
                var codeInst = g.Count(e => e.EdgeType != GDSceneFlowEdgeType.StaticSubScene);
                return new { Scene = g.Key, Total = g.Count(), SubScene = subScene, CodeInst = codeInst };
            })
            .OrderByDescending(x => x.Total)
            .Take(_options.TopN)
            .ToList();

        if (sceneHubs.Count > 0)
        {
            _formatter.WriteMessage(_output, GDAnsiColors.Bold($"Top {Math.Min(_options.TopN, sceneHubs.Count)} Scene Hubs:"));
            foreach (var hub in sceneHubs)
            {
                var relPath = GetRelativePath(hub.Scene, projectRoot);
                _formatter.WriteMessage(_output,
                    $"  {GDAnsiColors.Bold(relPath)}: {GDAnsiColors.Cyan($"{hub.Total}")} edges ({hub.SubScene} sub-scene, {hub.CodeInst} code)");

                if (_options.Explain)
                {
                    var edges = sceneReport.AllEdges.Where(e => e.SourceScene == hub.Scene).Take(_options.TopN);
                    foreach (var edge in edges)
                    {
                        var targetRel = GetRelativePath(edge.TargetScene, projectRoot);
                        _formatter.WriteMessage(_output, $"    -> {GDAnsiColors.Dim(targetRel)} ({edge.EdgeType})");
                    }
                }
            }
            _formatter.WriteMessage(_output, "");
        }

        // Scene warnings
        if (sceneReport.Warnings.Count > 0)
        {
            _formatter.WriteMessage(_output, "  Scene Warnings:");
            foreach (var warning in sceneReport.Warnings)
            {
                _formatter.WriteMessage(_output, $"    - {GDAnsiColors.Yellow(warning.Message)}");
                if (!string.IsNullOrEmpty(warning.ScenePath))
                    _formatter.WriteMessage(_output, $"      at {GDAnsiColors.Dim(warning.ScenePath)}");
            }
            _formatter.WriteMessage(_output, "");
        }
    }

    #endregion

    #region Signal Metrics

    private void WriteSignalMetrics(GDProjectSemanticModel projectModel, string projectRoot)
    {
        var allConnections = projectModel.SignalConnectionRegistry.GetAllConnections();
        if (allConnections.Count == 0)
            return;

        var codeCount = allConnections.Count(c => !c.IsSceneConnection);
        var sceneCount = allConnections.Count(c => c.IsSceneConnection);

        _formatter.WriteMessage(_output, GDAnsiColors.Bold($"Signal Connections: {GDAnsiColors.Cyan($"{allConnections.Count}")} ({codeCount} code, {sceneCount} scene)"));
        _formatter.WriteMessage(_output, "");

        // Signal hubs
        var signalHubs = allConnections
            .GroupBy(c => c.SourceFilePath)
            .Select(g =>
            {
                var code = g.Count(c => !c.IsSceneConnection);
                var scene = g.Count(c => c.IsSceneConnection);
                return new { File = g.Key, Total = g.Count(), Code = code, Scene = scene, Connections = g.ToList() };
            })
            .OrderByDescending(x => x.Total)
            .Take(_options.TopN)
            .ToList();

        if (signalHubs.Count > 0)
        {
            _formatter.WriteMessage(_output, GDAnsiColors.Bold($"Top {Math.Min(_options.TopN, signalHubs.Count)} Signal Hubs:"));
            foreach (var hub in signalHubs)
            {
                var relPath = GetRelativePath(hub.File, projectRoot);
                _formatter.WriteMessage(_output,
                    $"  {GDAnsiColors.Bold(relPath)}: {GDAnsiColors.Cyan($"{hub.Total}")} connections ({hub.Code} code, {hub.Scene} scene)");

                if (_options.Explain)
                {
                    foreach (var conn in hub.Connections.Take(_options.TopN))
                    {
                        var kind = conn.IsSceneConnection ? "scene" : "code";
                        _formatter.WriteMessage(_output,
                            $"    {GDAnsiColors.Dim($"{conn.EmitterType}.{conn.SignalName} -> {conn.CallbackMethodName} ({kind})")}");
                    }
                    if (hub.Connections.Count > _options.TopN)
                        _formatter.WriteMessage(_output, $"    {GDAnsiColors.Dim($"... and {hub.Connections.Count - _options.TopN} more")}");
                }
            }
            _formatter.WriteMessage(_output, "");
        }
    }

    #endregion

    #region Directory Grouping

    private void WriteDirectoryGroupedOutput(
        List<GDFileDependencyInfo> files,
        GDProjectDependencyReport report,
        string projectRoot)
    {
        var depth = _options.GroupDepth;

        WriteHeader(report, files, projectRoot);
        WriteCycles(report, projectRoot);

        // Build directory-to-directory edges
        var dirEdges = new Dictionary<(string from, string to), Dictionary<string, int>>();

        foreach (var file in files)
        {
            var fromDir = GetDirPrefix(GetRelativePath(file.FilePath, projectRoot), depth);

            if (!string.IsNullOrEmpty(file.ExtendsScript))
            {
                var toDir = GetDirPrefix(GetRelativePath(file.ExtendsScript, projectRoot), depth);
                if (fromDir != toDir)
                    AddDirEdge(dirEdges, fromDir, toDir, "extends");
            }
            else if (file.ExtendsProjectClass)
            {
                foreach (var dep in file.Dependencies.Take(1))
                {
                    var toDir = GetDirPrefix(GetRelativePath(dep, projectRoot), depth);
                    if (fromDir != toDir)
                        AddDirEdge(dirEdges, fromDir, toDir, "extends");
                }
            }

            foreach (var preload in file.Preloads)
            {
                var toDir = GetDirPrefix(GetRelativePath(preload, projectRoot), depth);
                if (fromDir != toDir)
                    AddDirEdge(dirEdges, fromDir, toDir, "preload");
            }

            foreach (var load in file.Loads)
            {
                var toDir = GetDirPrefix(GetRelativePath(load, projectRoot), depth);
                if (fromDir != toDir)
                    AddDirEdge(dirEdges, fromDir, toDir, "load");
            }
        }

        if (dirEdges.Count == 0)
        {
            _formatter.WriteMessage(_output, "No cross-directory dependencies found.");
            return;
        }

        var sortedEdges = dirEdges
            .Select(kvp => new
            {
                From = kvp.Key.from,
                To = kvp.Key.to,
                Total = kvp.Value.Values.Sum(),
                Breakdown = kvp.Value
            })
            .OrderByDescending(x => x.Total)
            .Take(_options.TopN * 3)
            .ToList();

        _formatter.WriteMessage(_output, GDAnsiColors.Bold($"Module Dependencies (depth={depth}):"));
        foreach (var edge in sortedEdges)
        {
            var breakdown = string.Join(", ", edge.Breakdown.Select(kv => $"{kv.Value} {kv.Key}"));
            _formatter.WriteMessage(_output,
                $"  {GDAnsiColors.Bold(edge.From)} -> {GDAnsiColors.Bold(edge.To)}: {GDAnsiColors.Cyan($"{edge.Total}")} edges ({breakdown})");
        }
        _formatter.WriteMessage(_output, "");
    }

    private static string GetDirPrefix(string relativePath, int depth)
    {
        var normalized = relativePath.Replace('\\', '/');
        var parts = normalized.Split('/');
        if (parts.Length <= depth)
            return string.Join("/", parts.Take(parts.Length - 1)) + "/";
        return string.Join("/", parts.Take(depth)) + "/";
    }

    private static void AddDirEdge(
        Dictionary<(string from, string to), Dictionary<string, int>> dirEdges,
        string fromDir, string toDir, string kind)
    {
        var key = (fromDir, toDir);
        if (!dirEdges.TryGetValue(key, out var counts))
        {
            counts = new Dictionary<string, int>();
            dirEdges[key] = counts;
        }
        counts[kind] = counts.GetValueOrDefault(kind) + 1;
    }

    #endregion

    #region Single File Output

    private void WriteSingleFileOutput(GDFileDependencyInfo info, string projectRoot, IGDDependencyHandler handler)
    {
        var relPath = GetRelativePath(info.FilePath, projectRoot);

        _formatter.WriteMessage(_output, $"Dependencies for: {GDAnsiColors.Bold(relPath)}");
        _formatter.WriteMessage(_output, "");

        if (!string.IsNullOrEmpty(info.ExtendsClass))
        {
            _formatter.WriteMessage(_output, $"  Extends: {GDAnsiColors.Cyan(info.ExtendsClass)}");
        }
        else if (!string.IsNullOrEmpty(info.ExtendsScript))
        {
            _formatter.WriteMessage(_output, $"  Extends: {GDAnsiColors.Cyan(info.ExtendsScript)}");
        }

        if (_options.Explain)
        {
            var edges = handler.GetFileEdges(info.FilePath);

            if (edges.Outgoing.Count > 0)
            {
                _formatter.WriteMessage(_output, $"  Outgoing edges ({GDAnsiColors.Cyan($"{edges.Outgoing.Count}")}):");
                foreach (var edge in edges.Outgoing)
                {
                    var toRel = GetRelativePath(edge.ToPath, projectRoot);
                    _formatter.WriteMessage(_output, $"    -> {GDAnsiColors.Dim(toRel)} ({EdgeKindLabel(edge.Kind)})");
                }
            }

            if (edges.Incoming.Count > 0)
            {
                _formatter.WriteMessage(_output, $"  Incoming edges ({GDAnsiColors.Cyan($"{edges.Incoming.Count}")}):");
                foreach (var edge in edges.Incoming)
                {
                    var fromRel = GetRelativePath(edge.FromPath, projectRoot);
                    _formatter.WriteMessage(_output, $"    <- {GDAnsiColors.Dim(fromRel)} ({EdgeKindLabel(edge.Kind)})");
                }
            }
        }
        else
        {
            if (info.Preloads.Count > 0)
            {
                _formatter.WriteMessage(_output, "  Preloads:");
                foreach (var preload in info.Preloads)
                {
                    _formatter.WriteMessage(_output, $"    - {GDAnsiColors.Dim(preload)}");
                }
            }

            if (info.Loads.Count > 0)
            {
                _formatter.WriteMessage(_output, "  Loads:");
                foreach (var load in info.Loads)
                {
                    _formatter.WriteMessage(_output, $"    - {GDAnsiColors.Dim(load)}");
                }
            }

            if (info.Dependencies.Count > 0)
            {
                _formatter.WriteMessage(_output, $"  Transitive Dependencies: {GDAnsiColors.Cyan($"{info.Dependencies.Count}")}");
                foreach (var dep in info.Dependencies.Take(10))
                {
                    var depRel = GetRelativePath(dep, projectRoot);
                    _formatter.WriteMessage(_output, $"    - {GDAnsiColors.Dim(depRel)}");
                }
                if (info.Dependencies.Count > 10)
                    _formatter.WriteMessage(_output, $"    {GDAnsiColors.Dim($"... and {info.Dependencies.Count - 10} more")}");
            }

            if (info.Dependents.Count > 0)
            {
                _formatter.WriteMessage(_output, $"  Dependents: {GDAnsiColors.Cyan($"{info.Dependents.Count}")}");
                foreach (var dep in info.Dependents.Take(10))
                {
                    var depRel = GetRelativePath(dep, projectRoot);
                    _formatter.WriteMessage(_output, $"    - {GDAnsiColors.Dim(depRel)}");
                }
                if (info.Dependents.Count > 10)
                    _formatter.WriteMessage(_output, $"    {GDAnsiColors.Dim($"... and {info.Dependents.Count - 10} more")}");
            }
        }

        if (info.IsInCycle)
        {
            _formatter.WriteMessage(_output, "");
            _formatter.WriteMessage(_output, GDAnsiColors.Red("  WARNING: This file is part of a circular dependency!"));
            if (info.CycleMembers != null && info.CycleMembers.Count > 0)
            {
                _formatter.WriteMessage(_output, "  Cycle members:");
                foreach (var member in info.CycleMembers)
                {
                    var memberRel = GetRelativePath(member, projectRoot);
                    _formatter.WriteMessage(_output, $"    - {GDAnsiColors.Red(memberRel)}");
                }
            }
        }
    }

    #endregion

    #region Helpers

    private static string EdgeKindLabel(GDDependencyEdgeKind kind) => kind switch
    {
        GDDependencyEdgeKind.Extends => "extends",
        GDDependencyEdgeKind.ExtendsPath => "extends",
        GDDependencyEdgeKind.Preload => "preload",
        GDDependencyEdgeKind.Load => "load",
        GDDependencyEdgeKind.SceneScript => "scene-script",
        GDDependencyEdgeKind.SceneSubScene => "sub-scene",
        GDDependencyEdgeKind.SignalCode => "signal",
        GDDependencyEdgeKind.SignalScene => "signal-scene",
        _ => "unknown"
    };

    #endregion
}
