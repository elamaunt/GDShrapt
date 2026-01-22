namespace GDShrapt.Semantics.StressTests.ConcurrencyTests;

/// <summary>
/// Tests for concurrent access to semantic analysis.
/// Validates thread-safety of queries and incremental updates.
/// </summary>
[TestClass]
[TestCategory("StressTests")]
[TestCategory("Concurrency")]
public class ConcurrentQueryTests
{
    [TestMethod]
    [Timeout(120000)]
    public async Task ConcurrentQueries_MultipleThreads_NoExceptions()
    {
        // Arrange
        using var project = SyntheticProjectGenerator.GenerateLargeProject(100);
        project.AnalyzeAll();

        var scripts = project.ScriptFiles.ToList();
        var exceptions = new ConcurrentBag<Exception>();
        const int threadCount = 10;
        const int queriesPerThread = 100;

        // Act - concurrent queries from multiple threads
        var tasks = Enumerable.Range(0, threadCount).Select(threadId => Task.Run(() =>
        {
            try
            {
                var random = new Random(threadId); // Deterministic per thread
                for (int j = 0; j < queriesPerThread; j++)
                {
                    var script = scripts[random.Next(scripts.Count)];
                    var semanticModel = script.SemanticModel;

                    if (semanticModel == null)
                        continue;

                    // Various concurrent queries
                    _ = semanticModel.Symbols.ToList();
                    _ = semanticModel.GetMethods().ToList();
                    _ = semanticModel.FindSymbol("health");
                    _ = semanticModel.GetEffectiveType("health");

                    if (semanticModel != null)
                    {
                        _ = semanticModel.Symbols.ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }));

        await Task.WhenAll(tasks);

        // Assert
        exceptions.Should().BeEmpty(
            because: "concurrent read queries should not throw exceptions");

        Console.WriteLine($"[PERF] {threadCount} threads x {queriesPerThread} queries completed successfully");
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task ConcurrentSymbolLookup_SameSymbol_ConsistentResults()
    {
        // Arrange
        using var project = SyntheticProjectGenerator.GenerateLargeProject(50);
        project.AnalyzeAll();

        var targetScript = project.ScriptFiles.First(s => s.TypeName != null);
        var semanticModel = targetScript.SemanticModel!;

        var results = new ConcurrentBag<bool>();
        const int threadCount = 20;
        const int lookupsPerThread = 50;

        // Act - all threads look up the same symbol
        var tasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < lookupsPerThread; i++)
            {
                var symbol = semanticModel.FindSymbol("health");
                results.Add(symbol != null);
            }
        }));

        await Task.WhenAll(tasks);

        // Assert - all lookups should return the same result
        var allResults = results.ToList();
        allResults.Should().HaveCount(threadCount * lookupsPerThread);
        allResults.Distinct().Should().HaveCount(1,
            because: "all lookups of the same symbol should return consistent results");
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task ConcurrentEnumeration_ScriptFiles_NoCorruption()
    {
        // Arrange
        using var project = SyntheticProjectGenerator.GenerateLargeProject(100);
        project.AnalyzeAll();

        var exceptions = new ConcurrentBag<Exception>();
        var counts = new ConcurrentBag<int>();
        const int threadCount = 10;
        const int iterationsPerThread = 20;

        // Act - concurrent enumeration of ScriptFiles
        var tasks = Enumerable.Range(0, threadCount).Select(threadId => Task.Run(() =>
        {
            try
            {
                for (int i = 0; i < iterationsPerThread; i++)
                {
                    // Enumerate all scripts
                    var scriptList = project.ScriptFiles.ToList();
                    counts.Add(scriptList.Count);

                    // Verify each script has valid state
                    foreach (var script in scriptList)
                    {
                        // Access properties to verify state (discard results)
                        var typeName = script.TypeName;
                        var cls = script.Class;
                        var semanticModel = script.SemanticModel;
                    }
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }));

        await Task.WhenAll(tasks);

        // Assert
        exceptions.Should().BeEmpty(
            because: "concurrent enumeration should not throw");

        var countList = counts.ToList();
        countList.Should().AllBeEquivalentTo(100,
            because: "script count should be consistent across all enumerations");
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task ConcurrentTypeResolution_MultipleTypes_NoRaceConditions()
    {
        // Arrange
        using var project = SyntheticProjectGenerator.GenerateLargeProject(50);
        project.AnalyzeAll();

        var scripts = project.ScriptFiles.ToList();
        var exceptions = new ConcurrentBag<Exception>();
        var typeResults = new ConcurrentDictionary<string, string?>();

        // Act - concurrent type resolution
        var tasks = scripts.Take(20).Select(script => Task.Run(() =>
        {
            try
            {
                var semanticModel = script.SemanticModel;
                if (semanticModel == null)
                    return;

                // Resolve types for various symbols
                foreach (var symbolName in new[] { "health", "speed", "is_alive", "max_health" })
                {
                    var type = semanticModel.GetEffectiveType(symbolName);
                    var key = $"{script.TypeName}.{symbolName}";
                    typeResults.TryAdd(key, type);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }));

        await Task.WhenAll(tasks);

        // Assert
        exceptions.Should().BeEmpty(
            because: "concurrent type resolution should not throw");

        // Verify results are sensible
        var healthResults = typeResults.Where(kv => kv.Key.EndsWith(".health")).ToList();
        healthResults.Should().NotBeEmpty();
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task ConcurrentAnalysisAndQuery_MixedOperations_Stable()
    {
        // Arrange
        using var project = SyntheticProjectGenerator.GenerateLargeProject(50, enableCallSiteRegistry: false);
        // Don't analyze yet - we'll do it concurrently with queries

        var exceptions = new ConcurrentBag<Exception>();
        var analysisComplete = new ManualResetEventSlim(false);

        // Act - one thread analyzes while others query
        var analyzeTask = Task.Run(() =>
        {
            try
            {
                project.AnalyzeAll();
                analysisComplete.Set();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        var queryTasks = Enumerable.Range(0, 5).Select(threadId => Task.Run(() =>
        {
            try
            {
                var random = new Random();
                for (int i = 0; i < 100; i++)
                {
                    var scripts = project.ScriptFiles.ToList();
                    if (scripts.Count == 0)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    var script = scripts[random.Next(scripts.Count)];

                    // Query may return null if analysis not complete - that's OK
                    var cls = script.Class;
                    var symbols = script.SemanticModel?.Symbols.ToList();
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }));

        await Task.WhenAll(new[] { analyzeTask }.Concat(queryTasks));

        // Assert
        exceptions.Should().BeEmpty(
            because: "mixed analysis and query operations should be stable");

        analysisComplete.IsSet.Should().BeTrue(
            because: "analysis should have completed");
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task ConcurrentFindSymbol_AcrossScripts_Consistent()
    {
        // Arrange
        using var project = SyntheticProjectGenerator.GenerateLargeProject(30);
        project.AnalyzeAll();

        var scripts = project.ScriptFiles.ToList();
        var foundCounts = new ConcurrentDictionary<string, int>();
        const int threadCount = 10;

        // Act - each thread counts how many scripts have 'health' symbol
        var tasks = Enumerable.Range(0, threadCount).Select(threadId => Task.Run(() =>
        {
            int count = 0;
            foreach (var script in scripts)
            {
                var semanticModel = script.SemanticModel;
                if (semanticModel?.FindSymbol("health") != null)
                {
                    count++;
                }
            }
            foundCounts.TryAdd($"thread_{threadId}", count);
        }));

        await Task.WhenAll(tasks);

        // Assert - all threads should find the same count
        var counts = foundCounts.Values.ToList();
        counts.Distinct().Should().HaveCount(1,
            because: "all threads should find the same number of scripts with 'health' symbol");

        Console.WriteLine($"[PERF] {threadCount} threads consistently found 'health' in {counts.First()} scripts");
    }
}
