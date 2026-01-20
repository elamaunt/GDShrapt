using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using GDShrapt.Abstractions;
using GDShrapt.Semantics.StressTests.Infrastructure;
using System.Linq;

namespace GDShrapt.Semantics.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for find references and cross-file symbol resolution.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ReferenceCollectionBenchmarks
{
    private GDScriptProject? _manyRefs100;
    private GDScriptProject? _manyRefs500;
    private GDScriptProject? _crossFileProject;

    [GlobalSetup]
    public void Setup()
    {
        _manyRefs100 = SyntheticProjectGenerator.GenerateManyReferencesProject(100);
        _manyRefs100.AnalyzeAll();

        _manyRefs500 = SyntheticProjectGenerator.GenerateManyReferencesProject(500);
        _manyRefs500.AnalyzeAll();

        _crossFileProject = SyntheticProjectGenerator.GenerateLargeProject(50, enableCallSiteRegistry: true);
        _crossFileProject.AnalyzeAll();
        _crossFileProject.BuildCallSiteRegistry();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _manyRefs100?.Dispose();
        _manyRefs500?.Dispose();
        _crossFileProject?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public int FindReferences_100Refs()
    {
        var script = _manyRefs100!.ScriptFiles.First();
        var analyzer = script.Analyzer!;

        var symbol = analyzer.FindSymbol("target_symbol");
        if (symbol == null) return 0;

        var refs = analyzer.SemanticModel?.GetReferencesTo(symbol);
        return refs?.Count ?? 0;
    }

    [Benchmark]
    public int FindReferences_500Refs()
    {
        var script = _manyRefs500!.ScriptFiles.First();
        var analyzer = script.Analyzer!;

        var symbol = analyzer.FindSymbol("target_symbol");
        if (symbol == null) return 0;

        var refs = analyzer.SemanticModel?.GetReferencesTo(symbol);
        return refs?.Count ?? 0;
    }

    [Benchmark]
    public int EnumerateAllSymbols_50Files()
    {
        int count = 0;
        foreach (var script in _crossFileProject!.ScriptFiles)
        {
            count += script.Analyzer?.Symbols.Count() ?? 0;
        }
        return count;
    }

    [Benchmark]
    public int FindSymbolAcrossProject()
    {
        int found = 0;
        foreach (var script in _crossFileProject!.ScriptFiles)
        {
            if (script.Analyzer?.FindSymbol("health") != null)
                found++;
        }
        return found;
    }
}

/// <summary>
/// Benchmarks for call site registry operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class CallSiteRegistryBenchmarks
{
    private GDScriptProject? _project;

    [GlobalSetup]
    public void Setup()
    {
        _project = SyntheticProjectGenerator.GenerateLargeProject(100, enableCallSiteRegistry: true);
        _project.AnalyzeAll();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _project?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void BuildCallSiteRegistry_100Files()
    {
        _project!.BuildCallSiteRegistry();
    }

    [Benchmark]
    public void RebuildCallSiteRegistry_100Files()
    {
        // Build twice to measure rebuild cost
        _project!.BuildCallSiteRegistry();
        _project.BuildCallSiteRegistry();
    }
}
