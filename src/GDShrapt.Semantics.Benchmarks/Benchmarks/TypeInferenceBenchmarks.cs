using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using GDShrapt.Abstractions;
using GDShrapt.Semantics.StressTests.Infrastructure;
using System.Linq;

namespace GDShrapt.Semantics.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks specifically for type inference operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class TypeInferenceBenchmarks
{
    private GDScriptProject? _deepInheritance10;
    private GDScriptProject? _deepInheritance15;
    private GDScriptProject? _complexTypes;
    private GDScriptProject? _longMethod;

    [GlobalSetup]
    public void Setup()
    {
        _deepInheritance10 = SyntheticProjectGenerator.GenerateDeepInheritanceProject(10);
        _deepInheritance15 = SyntheticProjectGenerator.GenerateDeepInheritanceProject(15);
        _complexTypes = SyntheticProjectGenerator.GenerateComplexTypesProject(30);
        _longMethod = SyntheticProjectGenerator.GenerateLongMethodProject(500);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _deepInheritance10?.Dispose();
        _deepInheritance15?.Dispose();
        _complexTypes?.Dispose();
        _longMethod?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void AnalyzeDeepInheritance_10Levels()
    {
        _deepInheritance10!.AnalyzeAll();
    }

    [Benchmark]
    public void AnalyzeDeepInheritance_15Levels()
    {
        _deepInheritance15!.AnalyzeAll();
    }

    [Benchmark]
    public void ResolveInheritedMembers_10Levels()
    {
        _deepInheritance10!.AnalyzeAll();

        var deepest = _deepInheritance10.ScriptFiles.First(s => s.TypeName == "Level9");
        var analyzer = deepest.Analyzer!;

        for (int level = 0; level < 10; level++)
        {
            _ = analyzer.FindSymbol($"level_{level}_var");
            _ = analyzer.FindSymbol($"level_{level}_method");
        }
    }

    [Benchmark]
    public void ResolveInheritedMembers_15Levels()
    {
        _deepInheritance15!.AnalyzeAll();

        var deepest = _deepInheritance15.ScriptFiles.First(s => s.TypeName == "Level14");
        var analyzer = deepest.Analyzer!;

        for (int level = 0; level < 15; level++)
        {
            _ = analyzer.FindSymbol($"level_{level}_var");
            _ = analyzer.FindSymbol($"level_{level}_method");
        }
    }

    [Benchmark]
    public void AnalyzeComplexTypes_30Variants()
    {
        _complexTypes!.AnalyzeAll();

        var script = _complexTypes.ScriptFiles.First();
        var analyzer = script.Analyzer!;

        for (int i = 0; i < 30; i++)
        {
            _ = analyzer.GetEffectiveType($"variant_{i}");
        }
    }

    [Benchmark]
    public void AnalyzeLongMethod_500Lines()
    {
        _longMethod!.AnalyzeAll();

        var script = _longMethod.ScriptFiles.First();
        var analyzer = script.Analyzer!;

        _ = analyzer.GetMethods().ToList();
    }
}

/// <summary>
/// Benchmarks for symbol lookup operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class SymbolLookupBenchmarks
{
    private GDScriptProject? _project;
    private GDScriptFile? _targetScript;

    [GlobalSetup]
    public void Setup()
    {
        _project = SyntheticProjectGenerator.GenerateLargeProject(50, enableCallSiteRegistry: false);
        _project.AnalyzeAll();
        _targetScript = _project.ScriptFiles.First(s => s.TypeName != null);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _project?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public object? FindSymbol_SingleLookup()
    {
        return _targetScript!.Analyzer?.FindSymbol("health");
    }

    [Benchmark]
    public int FindSymbol_10Lookups()
    {
        var analyzer = _targetScript!.Analyzer!;
        int count = 0;

        for (int i = 0; i < 10; i++)
        {
            if (analyzer.FindSymbol("health") != null) count++;
            if (analyzer.FindSymbol("speed") != null) count++;
            if (analyzer.FindSymbol("is_alive") != null) count++;
        }

        return count;
    }

    [Benchmark]
    public int EnumerateAllSymbols()
    {
        return _targetScript!.Analyzer!.Symbols.Count();
    }

    [Benchmark]
    public int GetEffectiveType_MultipleSymbols()
    {
        var analyzer = _targetScript!.Analyzer!;
        int count = 0;

        foreach (var symbol in analyzer.Symbols.Take(20))
        {
            if (analyzer.GetEffectiveType(symbol.Name) != null)
                count++;
        }

        return count;
    }
}
