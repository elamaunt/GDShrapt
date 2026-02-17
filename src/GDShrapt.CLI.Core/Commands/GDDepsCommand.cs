using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Command to analyze file dependencies.
/// Shows extends, preloads, loads, and detects circular dependencies.
/// </summary>
public class GDDepsCommand : GDProjectCommandBase
{
    private readonly GDDepsOptions _options;

    public override string Name => "deps";
    public override string Description => "Show file dependencies and detect cycles";

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

        // Single file or project-wide
        if (!string.IsNullOrEmpty(_options.FilePath))
        {
            var fullPath = Path.GetFullPath(Path.Combine(projectRoot, _options.FilePath));
            var info = handler.AnalyzeFile(fullPath);
            WriteSingleFileOutput(info, projectRoot);
            return Task.FromResult(GDExitCode.Success);
        }

        // Project-wide analysis
        var report = handler.AnalyzeProject();
        WriteProjectOutput(report, projectRoot, projectModel);

        // Fail on cycles
        if (_options.FailOnCycles && report.HasCycles)
        {
            _formatter.WriteError(_output, $"Found {report.CycleCount} circular dependencies");
            return Task.FromResult(GDExitCode.WarningsOrHints);
        }

        return Task.FromResult(GDExitCode.Success);
    }

    private void WriteSingleFileOutput(GDFileDependencyInfo info, string projectRoot)
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
            _formatter.WriteMessage(_output, $"  Dependents (files that depend on this): {GDAnsiColors.Cyan($"{info.Dependents.Count}")}");
            foreach (var dep in info.Dependents.Take(10))
            {
                var depRel = GetRelativePath(dep, projectRoot);
                _formatter.WriteMessage(_output, $"    - {GDAnsiColors.Dim(depRel)}");
            }
            if (info.Dependents.Count > 10)
                _formatter.WriteMessage(_output, $"    {GDAnsiColors.Dim($"... and {info.Dependents.Count - 10} more")}");
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

    private void WriteProjectOutput(GDProjectDependencyReport report, string projectRoot, GDProjectSemanticModel projectModel)
    {
        _formatter.WriteMessage(_output, GDAnsiColors.Bold("Project Dependency Analysis:"));
        _formatter.WriteMessage(_output, $"  Total Files:           {GDAnsiColors.Cyan($"{report.TotalFiles}")}");
        _formatter.WriteMessage(_output, $"  Circular Dependencies: {(report.CycleCount > 0 ? GDAnsiColors.Red($"{report.CycleCount}") : GDAnsiColors.Cyan($"{report.CycleCount}"))}");
        _formatter.WriteMessage(_output, $"  Files in Cycles:       {(report.FilesInCycles > 0 ? GDAnsiColors.Red($"{report.FilesInCycles}") : GDAnsiColors.Cyan($"{report.FilesInCycles}"))}");
        _formatter.WriteMessage(_output, "");

        // Show cycles
        if (report.HasCycles)
        {
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

        // Show most coupled files
        if (_options.ShowCoupled)
        {
            var mostCoupled = report.MostCoupled.Take(10).ToList();
            if (mostCoupled.Count > 0)
            {
                _formatter.WriteMessage(_output, GDAnsiColors.Bold("Most Coupled Files (most dependencies):"));
                foreach (var file in mostCoupled)
                {
                    var relPath = GetRelativePath(file.FilePath, projectRoot);
                    _formatter.WriteMessage(_output, $"  {GDAnsiColors.Bold(relPath)}: {GDAnsiColors.Cyan($"{file.DirectDependencyCount}")} direct deps");
                }
                _formatter.WriteMessage(_output, "");
            }
        }

        // Show most depended-on files
        if (_options.ShowDependent)
        {
            var mostDependent = report.MostDependent.Take(10).ToList();
            if (mostDependent.Count > 0)
            {
                _formatter.WriteMessage(_output, GDAnsiColors.Bold("Most Depended-On Files (most dependents):"));
                foreach (var file in mostDependent)
                {
                    var relPath = GetRelativePath(file.FilePath, projectRoot);
                    _formatter.WriteMessage(_output, $"  {GDAnsiColors.Bold(relPath)}: {GDAnsiColors.Cyan($"{file.Dependents.Count}")} dependents");
                }
                _formatter.WriteMessage(_output, "");
            }
        }

        // Scene dependencies
        if (_options.IncludeScenes)
        {
            var sceneReport = projectModel.SceneFlow.AnalyzeProject();
            if (sceneReport.TotalScenes > 0)
            {
                _formatter.WriteMessage(_output, GDAnsiColors.Bold("Scene Dependencies:"));
                _formatter.WriteMessage(_output, $"  Scenes:                    {GDAnsiColors.Cyan($"{sceneReport.TotalScenes}")}");
                _formatter.WriteMessage(_output, $"  Sub-scene edges:           {GDAnsiColors.Cyan($"{sceneReport.StaticSubSceneCount}")}");
                _formatter.WriteMessage(_output, $"  Code instantiation edges:  {GDAnsiColors.Cyan($"{sceneReport.CodeInstantiationCount}")}");

                if (sceneReport.Warnings.Count > 0)
                {
                    _formatter.WriteMessage(_output, "");
                    _formatter.WriteMessage(_output, "  Scene Warnings:");
                    foreach (var warning in sceneReport.Warnings)
                    {
                        _formatter.WriteMessage(_output, $"    - {GDAnsiColors.Yellow(warning.Message)}");
                        if (!string.IsNullOrEmpty(warning.ScenePath))
                            _formatter.WriteMessage(_output, $"      at {GDAnsiColors.Dim(warning.ScenePath)}");
                    }
                }
                _formatter.WriteMessage(_output, "");
            }
        }

        // Signal connections
        if (_options.IncludeSignals)
        {
            var allConnections = projectModel.SignalConnectionRegistry.GetAllConnections();
            if (allConnections.Count > 0)
            {
                _formatter.WriteMessage(_output, GDAnsiColors.Bold($"Signal Connections: {GDAnsiColors.Cyan($"{allConnections.Count}")}"));
                var byFile = allConnections
                    .GroupBy(c => c.SourceFilePath)
                    .OrderByDescending(g => g.Count())
                    .Take(10);
                foreach (var group in byFile)
                {
                    var relPath = GetRelativePath(group.Key, projectRoot);
                    _formatter.WriteMessage(_output, $"  {GDAnsiColors.Bold(relPath)}: {GDAnsiColors.Cyan($"{group.Count()}")} connections");
                }
                _formatter.WriteMessage(_output, "");
            }
        }
    }
}

/// <summary>
/// Options for deps command.
/// </summary>
public class GDDepsOptions
{
    /// <summary>
    /// Optional specific file to analyze.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Show most coupled files.
    /// </summary>
    public bool ShowCoupled { get; set; } = true;

    /// <summary>
    /// Show most depended-on files.
    /// </summary>
    public bool ShowDependent { get; set; } = true;

    /// <summary>
    /// Fail if circular dependencies are found (for CI).
    /// </summary>
    public bool FailOnCycles { get; set; }

    /// <summary>
    /// Include scene→scene and scene→script dependencies.
    /// </summary>
    public bool IncludeScenes { get; set; } = true;

    /// <summary>
    /// Include signal connections as dependency edges.
    /// </summary>
    public bool IncludeSignals { get; set; } = true;
}
