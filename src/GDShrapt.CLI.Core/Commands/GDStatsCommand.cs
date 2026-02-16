using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Command to show project statistics summary.
/// Combines metrics and type coverage for a quick overview.
/// </summary>
public class GDStatsCommand : GDProjectCommandBase
{
    public override string Name => "stats";
    public override string Description => "Show project statistics summary";

    public GDStatsCommand(
        string projectPath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        GDProjectConfig? config = null,
        IGDLogger? logger = null)
        : base(projectPath, formatter, output, config, logger)
    {
    }

    protected override Task<int> ExecuteOnProjectAsync(
        GDScriptProject project,
        string projectRoot,
        GDProjectConfig config,
        CancellationToken cancellationToken)
    {
        var projectModel = new GDProjectSemanticModel(project);
        var metricsHandler = Registry?.GetService<IGDMetricsHandler>() ?? new GDMetricsHandler(projectModel);
        var coverageHandler = Registry?.GetService<IGDTypeCoverageHandler>() ?? new GDTypeCoverageHandler(projectModel);

        var metrics = metricsHandler.AnalyzeProject();
        var coverage = coverageHandler.AnalyzeProject();

        var sceneReport = projectModel.SceneFlow.AnalyzeProject();
        var resourceReport = projectModel.ResourceFlow.AnalyzeProject();
        var allSignalConnections = projectModel.SignalConnectionRegistry.GetAllConnections();
        var signalTotal = allSignalConnections.Count;
        var sceneSignalCount = allSignalConnections.Count(c => c.IsSceneConnection);

        // Header
        _formatter.WriteMessage(_output, "╔════════════════════════════════════════════════════════════════╗");
        _formatter.WriteMessage(_output, "║                    GDScript Project Statistics                 ║");
        _formatter.WriteMessage(_output, "╚════════════════════════════════════════════════════════════════╝");
        _formatter.WriteMessage(_output, "");

        // Size metrics
        _formatter.WriteMessage(_output, "[Size]");
        _formatter.WriteMessage(_output, $"   Files:        {metrics.FileCount,8}");
        _formatter.WriteMessage(_output, $"   Total Lines:  {metrics.TotalLines,8:N0}");
        _formatter.WriteMessage(_output, $"   Code Lines:   {metrics.CodeLines,8:N0}");
        _formatter.WriteMessage(_output, $"   Comment Lines:{metrics.CommentLines,8:N0}");
        _formatter.WriteMessage(_output, "");

        // Structure metrics
        _formatter.WriteMessage(_output, "[Structure]");
        _formatter.WriteMessage(_output, $"   Classes:      {metrics.ClassCount,8}");
        _formatter.WriteMessage(_output, $"   Methods:      {metrics.MethodCount,8}");
        _formatter.WriteMessage(_output, $"   Signals:      {metrics.SignalCount,8}");
        _formatter.WriteMessage(_output, "");

        // Scenes
        _formatter.WriteMessage(_output, "[Scenes]");
        _formatter.WriteMessage(_output, $"   Scenes:       {sceneReport.TotalScenes,8}");
        _formatter.WriteMessage(_output, $"   Sub-scenes:   {sceneReport.StaticSubSceneCount,8}");
        _formatter.WriteMessage(_output, $"   Code inst.:   {sceneReport.CodeInstantiationCount,8}");
        _formatter.WriteMessage(_output, "");

        // Resources
        _formatter.WriteMessage(_output, "[Resources]");
        _formatter.WriteMessage(_output, $"   Total:        {resourceReport.TotalResources,8}");
        foreach (var cat in resourceReport.ResourcesByCategory.OrderByDescending(c => c.Value))
        {
            _formatter.WriteMessage(_output, $"   {cat.Key + ":",-13}{cat.Value,8}");
        }
        _formatter.WriteMessage(_output, $"   Unused:       {resourceReport.UnusedResources.Count,8}");
        _formatter.WriteMessage(_output, $"   Missing:      {resourceReport.MissingResources.Count,8}");
        _formatter.WriteMessage(_output, "");

        // Signal Connections
        _formatter.WriteMessage(_output, "[Signal Connections]");
        _formatter.WriteMessage(_output, $"   Total:        {signalTotal,8}");
        _formatter.WriteMessage(_output, $"   From scenes:  {sceneSignalCount,8}");
        _formatter.WriteMessage(_output, $"   From code:    {signalTotal - sceneSignalCount,8}");
        _formatter.WriteMessage(_output, "");

        // Complexity metrics
        _formatter.WriteMessage(_output, "[Complexity]");
        _formatter.WriteMessage(_output, $"   Avg CC:       {metrics.AverageComplexity,8:F2}");
        _formatter.WriteMessage(_output, $"   Avg MI:       {metrics.AverageMaintainability,8:F2}");
        _formatter.WriteMessage(_output, "");

        // Type coverage
        _formatter.WriteMessage(_output, "[Type Coverage]");
        _formatter.WriteMessage(_output, $"   Annotated:    {coverage.AnnotationCoverage,7:F1}%");
        _formatter.WriteMessage(_output, $"   Effective:    {coverage.EffectiveCoverage,7:F1}%");
        _formatter.WriteMessage(_output, $"   Type Safety:  {coverage.TypeSafetyScore,7:F1}%");
        _formatter.WriteMessage(_output, "");

        // Health indicators
        _formatter.WriteMessage(_output, "[Health]");
        WriteHealthIndicator("Complexity", metrics.AverageComplexity <= 10 ? "Good" : metrics.AverageComplexity <= 20 ? "Fair" : "High");
        WriteHealthIndicator("Maintainability", metrics.AverageMaintainability >= 65 ? "Good" : metrics.AverageMaintainability >= 40 ? "Fair" : "Low");
        WriteHealthIndicator("Type Safety", coverage.TypeSafetyScore >= 80 ? "Good" : coverage.TypeSafetyScore >= 50 ? "Fair" : "Low");
        WriteHealthIndicator("Scene Deps", sceneReport.Warnings.Count > 0 ? "Warning" : "Good");
        WriteHealthIndicator("Resources", resourceReport.MissingResources.Count > 0 ? "Warning" : resourceReport.UnusedResources.Count > 0 ? "Fair" : "Good");

        return Task.FromResult(GDExitCode.Success);
    }

    private void WriteHealthIndicator(string name, string status)
    {
        var indicator = status switch
        {
            "Good" => "+",
            "Fair" => "~",
            "High" => "!",
            "Low" => "!",
            "Warning" => "!",
            _ => "?"
        };
        _formatter.WriteMessage(_output, $"   {name}: [{indicator}] {status}");
    }
}
