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
        _formatter.WriteMessage(_output, GDAnsiColors.Bold("[Size]"));
        _formatter.WriteMessage(_output, $"   Files:        {GDAnsiColors.Cyan($"{metrics.FileCount,8}")}");
        _formatter.WriteMessage(_output, $"   Total Lines:  {GDAnsiColors.Cyan($"{metrics.TotalLines,8:N0}")}");
        _formatter.WriteMessage(_output, $"   Code Lines:   {GDAnsiColors.Cyan($"{metrics.CodeLines,8:N0}")}");
        _formatter.WriteMessage(_output, $"   Comment Lines:{GDAnsiColors.Cyan($"{metrics.CommentLines,8:N0}")}");
        _formatter.WriteMessage(_output, "");

        // Structure metrics
        _formatter.WriteMessage(_output, GDAnsiColors.Bold("[Structure]"));
        _formatter.WriteMessage(_output, $"   Classes:      {GDAnsiColors.Cyan($"{metrics.ClassCount,8}")}");
        _formatter.WriteMessage(_output, $"   Methods:      {GDAnsiColors.Cyan($"{metrics.MethodCount,8}")}");
        _formatter.WriteMessage(_output, $"   Signals:      {GDAnsiColors.Cyan($"{metrics.SignalCount,8}")}");
        _formatter.WriteMessage(_output, "");

        // Scenes
        _formatter.WriteMessage(_output, GDAnsiColors.Bold("[Scenes]"));
        _formatter.WriteMessage(_output, $"   Scenes:       {GDAnsiColors.Cyan($"{sceneReport.TotalScenes,8}")}");
        _formatter.WriteMessage(_output, $"   Sub-scenes:   {GDAnsiColors.Cyan($"{sceneReport.StaticSubSceneCount,8}")}");
        _formatter.WriteMessage(_output, $"   Code inst.:   {GDAnsiColors.Cyan($"{sceneReport.CodeInstantiationCount,8}")}");
        _formatter.WriteMessage(_output, "");

        // Resources
        _formatter.WriteMessage(_output, GDAnsiColors.Bold("[Resources]"));
        _formatter.WriteMessage(_output, $"   Total:        {GDAnsiColors.Cyan($"{resourceReport.TotalResources,8}")}");
        foreach (var cat in resourceReport.ResourcesByCategory.OrderByDescending(c => c.Value))
        {
            _formatter.WriteMessage(_output, $"   {GDAnsiColors.Dim($"{cat.Key + ":",-13}")}{GDAnsiColors.Cyan($"{cat.Value,8}")}");
        }
        _formatter.WriteMessage(_output, $"   Unused:       {GDAnsiColors.Cyan($"{resourceReport.UnusedResources.Count,8}")}");
        _formatter.WriteMessage(_output, $"   Missing:      {GDAnsiColors.Cyan($"{resourceReport.MissingResources.Count,8}")}");
        _formatter.WriteMessage(_output, "");

        // Signal Connections
        _formatter.WriteMessage(_output, GDAnsiColors.Bold("[Signal Connections]"));
        _formatter.WriteMessage(_output, $"   Total:        {GDAnsiColors.Cyan($"{signalTotal,8}")}");
        _formatter.WriteMessage(_output, $"   From scenes:  {GDAnsiColors.Cyan($"{sceneSignalCount,8}")}");
        _formatter.WriteMessage(_output, $"   From code:    {GDAnsiColors.Cyan($"{signalTotal - sceneSignalCount,8}")}");
        _formatter.WriteMessage(_output, "");

        // Complexity metrics
        _formatter.WriteMessage(_output, GDAnsiColors.Bold("[Complexity]"));
        _formatter.WriteMessage(_output, $"   Avg CC:       {ColorCC(metrics.AverageComplexity, "F2"),8}");
        _formatter.WriteMessage(_output, $"   Avg MI:       {ColorMI(metrics.AverageMaintainability, "F2"),8}");
        _formatter.WriteMessage(_output, "");

        // Type coverage
        _formatter.WriteMessage(_output, GDAnsiColors.Bold("[Type Coverage]"));
        _formatter.WriteMessage(_output, $"   Annotated:    {GDAnsiColors.Cyan($"{coverage.AnnotationCoverage,7:F1}%")}");
        _formatter.WriteMessage(_output, $"   Effective:    {GDAnsiColors.Cyan($"{coverage.EffectiveCoverage,7:F1}%")}");
        _formatter.WriteMessage(_output, $"   Type Safety:  {GDAnsiColors.Cyan($"{coverage.TypeSafetyScore,7:F1}%")}");
        _formatter.WriteMessage(_output, "");

        // Health indicators
        _formatter.WriteMessage(_output, GDAnsiColors.Bold("[Health]"));
        WriteHealthIndicator("Complexity", metrics.AverageComplexity <= 10 ? "Good" : metrics.AverageComplexity <= 20 ? "Fair" : "High");
        WriteHealthIndicator("Maintainability", metrics.AverageMaintainability >= 65 ? "Good" : metrics.AverageMaintainability >= 40 ? "Fair" : "Low");
        WriteHealthIndicator("Type Safety", coverage.TypeSafetyScore >= 80 ? "Good" : coverage.TypeSafetyScore >= 50 ? "Fair" : "Low");
        WriteHealthIndicator("Scene Deps", sceneReport.Warnings.Count > 0 ? "Warning" : "Good");
        WriteHealthIndicator("Resources", resourceReport.MissingResources.Count > 0 ? "Warning" : resourceReport.UnusedResources.Count > 0 ? "Fair" : "Good");

        return Task.FromResult(GDExitCode.Success);
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

    private void WriteHealthIndicator(string name, string status)
    {
        Func<string, string> color = status switch
        {
            "Good" => GDAnsiColors.Green,
            "Fair" => GDAnsiColors.Yellow,
            "High" or "Low" => GDAnsiColors.Red,
            "Warning" => GDAnsiColors.Yellow,
            _ => GDAnsiColors.Dim
        };
        var indicator = status switch
        {
            "Good" => "+",
            "Fair" => "~",
            "High" or "Low" or "Warning" => "!",
            _ => "?"
        };
        _formatter.WriteMessage(_output, $"   {name}: {color($"[{indicator}] {status}")}");
    }
}
