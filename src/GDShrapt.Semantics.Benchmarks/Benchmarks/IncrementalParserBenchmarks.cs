using System.Text;
using BenchmarkDotNet.Attributes;
using GDShrapt.Reader;

namespace GDShrapt.Semantics.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks comparing full reparse vs incremental parsing performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class IncrementalParserBenchmarks
{
    private string _smallFile = "";   // ~500 characters
    private string _mediumFile = "";  // ~5000 characters
    private string _largeFile = "";   // ~50000 characters

    private GDScriptReader _reader = null!;
    private GDScriptIncrementalReader _incrementalReader = null!;

    private GDClassDeclaration _smallTree = null!;
    private GDClassDeclaration _mediumTree = null!;
    private GDClassDeclaration _largeTree = null!;

    private string _smallFileEdited = "";
    private string _mediumFileEdited = "";
    private string _largeFileEdited = "";

    private GDTextChange _smallChange;
    private GDTextChange _mediumChange;
    private GDTextChange _largeChange;

    [GlobalSetup]
    public void Setup()
    {
        _reader = new GDScriptReader();
        _incrementalReader = new GDScriptIncrementalReader(_reader);

        _smallFile = GenerateFile(10);
        _mediumFile = GenerateFile(100);
        _largeFile = GenerateFile(1000);

        _smallTree = _reader.ParseFileContent(_smallFile);
        _mediumTree = _reader.ParseFileContent(_mediumFile);
        _largeTree = _reader.ParseFileContent(_largeFile);

        // Prepare edits - insert character in middle of file
        var smallPos = _smallFile.Length / 2;
        var mediumPos = _mediumFile.Length / 2;
        var largePos = _largeFile.Length / 2;

        _smallChange = GDTextChange.Insert(smallPos, "x");
        _mediumChange = GDTextChange.Insert(mediumPos, "x");
        _largeChange = GDTextChange.Insert(largePos, "x");

        _smallFileEdited = _smallFile.Insert(smallPos, "x");
        _mediumFileEdited = _mediumFile.Insert(mediumPos, "x");
        _largeFileEdited = _largeFile.Insert(largePos, "x");
    }

    private static string GenerateFile(int memberCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("extends Node");
        sb.AppendLine();

        // Variables
        for (int i = 0; i < memberCount; i++)
        {
            sb.AppendLine($"var var_{i} = {i}");
        }

        sb.AppendLine();

        // Methods (1/10 of variables count)
        for (int i = 0; i < memberCount / 10; i++)
        {
            sb.AppendLine($"func method_{i}():");
            sb.AppendLine($"\tvar local = {i}");
            sb.AppendLine($"\treturn local");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    #region Small File Benchmarks (~500 chars)

    [Benchmark(Baseline = true)]
    public GDClassDeclaration Small_FullReparse()
    {
        return _reader.ParseFileContent(_smallFileEdited);
    }

    [Benchmark]
    public GDClassDeclaration Small_Incremental()
    {
        var tree = (GDClassDeclaration)_smallTree.Clone();
        return _incrementalReader.ParseIncremental(tree, _smallFileEdited, new[] { _smallChange }).Tree;
    }

    #endregion

    #region Medium File Benchmarks (~5000 chars)

    [Benchmark]
    public GDClassDeclaration Medium_FullReparse()
    {
        return _reader.ParseFileContent(_mediumFileEdited);
    }

    [Benchmark]
    public GDClassDeclaration Medium_Incremental()
    {
        var tree = (GDClassDeclaration)_mediumTree.Clone();
        return _incrementalReader.ParseIncremental(tree, _mediumFileEdited, new[] { _mediumChange }).Tree;
    }

    #endregion

    #region Large File Benchmarks (~50000 chars)

    [Benchmark]
    public GDClassDeclaration Large_FullReparse()
    {
        return _reader.ParseFileContent(_largeFileEdited);
    }

    [Benchmark]
    public GDClassDeclaration Large_Incremental()
    {
        var tree = (GDClassDeclaration)_largeTree.Clone();
        return _incrementalReader.ParseIncremental(tree, _largeFileEdited, new[] { _largeChange }).Tree;
    }

    #endregion
}
