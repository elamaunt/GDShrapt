using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using GDShrapt.Abstractions;
using GDShrapt.Semantics.StressTests.Infrastructure;

namespace GDShrapt.Semantics.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for full project analysis at different scales.
/// Tracks time and memory for LoadScripts + AnalyzeAll pipeline.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ProjectAnalysisBenchmarks
{
    private GDScriptProject? _project50;
    private GDScriptProject? _project100;
    private GDScriptProject? _project200;

    [GlobalSetup]
    public void Setup()
    {
        // Pre-generate projects (generation time not measured)
        _project50 = SyntheticProjectGenerator.GenerateLargeProject(50, enableCallSiteRegistry: false);
        _project100 = SyntheticProjectGenerator.GenerateLargeProject(100, enableCallSiteRegistry: false);
        _project200 = SyntheticProjectGenerator.GenerateLargeProject(200, enableCallSiteRegistry: false);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _project50?.Dispose();
        _project100?.Dispose();
        _project200?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void AnalyzeProject_50Files()
    {
        _project50!.AnalyzeAll();
    }

    [Benchmark]
    public void AnalyzeProject_100Files()
    {
        _project100!.AnalyzeAll();
    }

    [Benchmark]
    public void AnalyzeProject_200Files()
    {
        _project200!.AnalyzeAll();
    }

    [Benchmark]
    public GDScriptProject CreateAndAnalyze_50Files()
    {
        var project = SyntheticProjectGenerator.GenerateLargeProject(50, enableCallSiteRegistry: false);
        project.AnalyzeAll();
        return project;
    }

    [Benchmark]
    public GDScriptProject CreateAndAnalyze_100Files()
    {
        var project = SyntheticProjectGenerator.GenerateLargeProject(100, enableCallSiteRegistry: false);
        project.AnalyzeAll();
        return project;
    }
}

/// <summary>
/// Benchmarks for project creation and loading (without analysis).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ProjectCreationBenchmarks
{
    [Benchmark(Baseline = true)]
    public GDScriptProject CreateProject_50Files()
    {
        return SyntheticProjectGenerator.GenerateLargeProject(50, enableCallSiteRegistry: false);
    }

    [Benchmark]
    public GDScriptProject CreateProject_100Files()
    {
        return SyntheticProjectGenerator.GenerateLargeProject(100, enableCallSiteRegistry: false);
    }

    [Benchmark]
    public GDScriptProject CreateProject_200Files()
    {
        return SyntheticProjectGenerator.GenerateLargeProject(200, enableCallSiteRegistry: false);
    }
}
