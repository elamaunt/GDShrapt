namespace GDShrapt.Semantics.StressTests.ConcurrencyTests;

/// <summary>
/// Stress tests for parallel semantic analysis.
/// Validates correctness and performance of parallel AnalyzeAll().
/// </summary>
[TestClass]
[TestCategory("StressTests")]
[TestCategory("Concurrency")]
public class ParallelAnalysisStressTests
{
    [TestMethod]
    [Timeout(120000)]
    public void ParallelAnalyzeAll_100Files_FasterThanSequential()
    {
        // Arrange - Create two identical projects
        var sequentialConfig = new GDSemanticsConfig { EnableParallelAnalysis = false };
        var parallelConfig = new GDSemanticsConfig { EnableParallelAnalysis = true, MaxDegreeOfParallelism = -1 };

        // Sequential baseline
        using var sequentialProject = CreateProjectWithConfig(100, sequentialConfig);
        var swSequential = Stopwatch.StartNew();
        sequentialProject.AnalyzeAll();
        swSequential.Stop();

        // Parallel analysis
        using var parallelProject = CreateProjectWithConfig(100, parallelConfig);
        var swParallel = Stopwatch.StartNew();
        parallelProject.AnalyzeAll();
        swParallel.Stop();

        // Assert
        var sequentialCount = sequentialProject.ScriptFiles.Count(s => s.SemanticModel != null);
        var parallelCount = parallelProject.ScriptFiles.Count(s => s.SemanticModel != null);

        sequentialCount.Should().Be(100, "all files should be analyzed sequentially");
        parallelCount.Should().Be(100, "all files should be analyzed in parallel");

        var speedup = (double)swSequential.ElapsedMilliseconds / Math.Max(1, swParallel.ElapsedMilliseconds);
        Console.WriteLine($"[PERF] Sequential: {swSequential.ElapsedMilliseconds}ms, Parallel: {swParallel.ElapsedMilliseconds}ms, Speedup: {speedup:F2}x");

        // On multi-core systems, parallel should be faster (allow for variability)
        if (Environment.ProcessorCount > 1 && swSequential.ElapsedMilliseconds > 500)
        {
            speedup.Should().BeGreaterThan(1.0, "parallel should be faster than sequential on multi-core systems");
        }
    }

    [TestMethod]
    [Timeout(180000)]
    public void ParallelAnalyzeAll_200Files_NoExceptions()
    {
        // Arrange
        var config = new GDSemanticsConfig { EnableParallelAnalysis = true, MaxDegreeOfParallelism = -1 };
        using var project = CreateProjectWithConfig(200, config);

        // Act
        Action act = () => project.AnalyzeAll();

        // Assert
        act.Should().NotThrow("parallel analysis of 200 files should complete without exceptions");

        var analyzedCount = project.ScriptFiles.Count(s => s.SemanticModel != null);
        analyzedCount.Should().Be(200, "all files should be analyzed");

        Console.WriteLine($"[PERF] Successfully analyzed {analyzedCount} files in parallel");
    }

    [TestMethod]
    [Timeout(60000)]
    public void ParallelAnalyzeAll_CancellationToken_StopsGracefully()
    {
        // Arrange
        var config = new GDSemanticsConfig { EnableParallelAnalysis = true, MaxDegreeOfParallelism = -1 };
        using var project = CreateProjectWithConfig(100, config);
        using var cts = new CancellationTokenSource();

        // Cancel after 50ms
        cts.CancelAfter(50);

        // Act & Assert
        Action act = () => project.AnalyzeAll(cts.Token);
        act.Should().Throw<OperationCanceledException>("cancellation should be honored");

        // Some files may have been analyzed before cancellation
        var analyzedCount = project.ScriptFiles.Count(s => s.SemanticModel != null);
        Console.WriteLine($"[PERF] Analyzed {analyzedCount} files before cancellation");
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task ParallelAnalyzeAll_ConcurrentWithQueries_NoRaceConditions()
    {
        // Arrange
        var config = new GDSemanticsConfig { EnableParallelAnalysis = true, MaxDegreeOfParallelism = -1 };
        using var project = CreateProjectWithConfig(50, config);
        var exceptions = new ConcurrentBag<Exception>();
        var analysisComplete = new ManualResetEventSlim(false);

        // Act - Run analysis and queries concurrently
        var analysisTask = Task.Run(() =>
        {
            try
            {
                project.AnalyzeAll();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
            finally
            {
                analysisComplete.Set();
            }
        });

        var queryTasks = Enumerable.Range(0, 5).Select(_ => Task.Run(() =>
        {
            try
            {
                var random = new Random();
                for (int i = 0; i < 100; i++)
                {
                    // Query ScriptFiles and semantic models during analysis
                    var scripts = project.ScriptFiles.ToList();
                    foreach (var s in scripts.Take(10))
                    {
                        var symbols = s.SemanticModel?.Symbols.ToList();
                        var typeName = s.TypeName;
                        var classDecl = s.Class;
                    }
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }));

        await Task.WhenAll(new[] { analysisTask }.Concat(queryTasks));

        // Assert
        exceptions.Should().BeEmpty("concurrent analysis and queries should not throw");

        var analyzedCount = project.ScriptFiles.Count(s => s.SemanticModel != null);
        analyzedCount.Should().Be(50, "all files should be analyzed");
    }

    [TestMethod]
    [Timeout(180000)]
    public void ParallelAnalyzeAll_MemoryPressure_NoLeaks()
    {
        // Arrange - Force GC to get baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var initialMemory = GC.GetTotalMemory(true);

        // Act - Multiple cycles of parallel analysis
        for (int cycle = 0; cycle < 5; cycle++)
        {
            var config = new GDSemanticsConfig { EnableParallelAnalysis = true, MaxDegreeOfParallelism = -1 };
            using var project = CreateProjectWithConfig(50, config);
            project.AnalyzeAll();

            var analyzedCount = project.ScriptFiles.Count(s => s.SemanticModel != null);
            analyzedCount.Should().Be(50, $"cycle {cycle}: all files should be analyzed");
        }

        // Force GC to release memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);

        // Assert - Memory should not grow significantly (allow 50MB growth)
        var memoryGrowth = finalMemory - initialMemory;
        Console.WriteLine($"[MEM] Initial: {initialMemory / 1024 / 1024}MB, Final: {finalMemory / 1024 / 1024}MB, Growth: {memoryGrowth / 1024 / 1024}MB");

        memoryGrowth.Should().BeLessThan(50 * 1024 * 1024,
            "memory should be released after project disposal");
    }

    [TestMethod]
    [Timeout(120000)]
    public void ParallelAnalyzeAll_DifferentDegrees_AllComplete()
    {
        foreach (var degree in new[] { 1, 2, 4, -1 }) // -1 = auto (ProcessorCount)
        {
            // Arrange
            var config = new GDSemanticsConfig { EnableParallelAnalysis = true, MaxDegreeOfParallelism = degree };
            using var project = CreateProjectWithConfig(30, config);

            // Act
            var sw = Stopwatch.StartNew();
            project.AnalyzeAll();
            sw.Stop();

            // Assert
            var analyzedCount = project.ScriptFiles.Count(s => s.SemanticModel != null);
            analyzedCount.Should().Be(30, $"all files should be analyzed with degree={degree}");

            var effectiveDegree = degree < 0 ? Environment.ProcessorCount : degree;
            Console.WriteLine($"[PERF] Degree={effectiveDegree}: {sw.ElapsedMilliseconds}ms");
        }
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task AnalyzeAllAsync_CompletesSuccessfully()
    {
        // Arrange
        var config = new GDSemanticsConfig { EnableParallelAnalysis = true, MaxDegreeOfParallelism = -1 };
        using var project = CreateProjectWithConfig(50, config);

        // Act
        await project.AnalyzeAllAsync();

        // Assert
        var analyzedCount = project.ScriptFiles.Count(s => s.SemanticModel != null);
        analyzedCount.Should().Be(50, "all files should be analyzed asynchronously");
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task AnalyzeAllAsync_CancellationToken_Honored()
    {
        // Arrange
        var config = new GDSemanticsConfig { EnableParallelAnalysis = true, MaxDegreeOfParallelism = -1 };
        using var project = CreateProjectWithConfig(100, config);
        using var cts = new CancellationTokenSource();

        // Cancel after 30ms
        cts.CancelAfter(30);

        // Act & Assert
        Func<Task> act = () => project.AnalyzeAllAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    [Timeout(180000)]
    public void ParallelAnalyzeAll_WithCallSiteRegistry_BuildsCorrectly()
    {
        // Arrange
        var config = new GDSemanticsConfig { EnableParallelAnalysis = true, MaxDegreeOfParallelism = -1 };
        using var project = SyntheticProjectGenerator.GenerateLargeProject(50, enableCallSiteRegistry: true);

        // Act
        project.AnalyzeAll();
        project.BuildCallSiteRegistry();

        // Assert
        var analyzedCount = project.ScriptFiles.Count(s => s.SemanticModel != null);
        analyzedCount.Should().Be(50);

        var callSiteRegistry = project.CallSiteRegistry;
        callSiteRegistry.Should().NotBeNull("call site registry should be enabled");
        callSiteRegistry!.Count.Should().BeGreaterThan(0, "call sites should be registered");

        Console.WriteLine($"[PERF] Analyzed {analyzedCount} files, registered {callSiteRegistry.Count} call sites");
    }

    [TestMethod]
    [Timeout(120000)]
    public void SequentialFallback_WhenDisabled_StillWorks()
    {
        // Arrange - explicitly disable parallel
        var config = new GDSemanticsConfig { EnableParallelAnalysis = false };
        using var project = CreateProjectWithConfig(50, config);

        // Act
        project.AnalyzeAll();

        // Assert
        var analyzedCount = project.ScriptFiles.Count(s => s.SemanticModel != null);
        analyzedCount.Should().Be(50, "sequential fallback should analyze all files");
    }

    [TestMethod]
    [Timeout(120000)]
    public void ParallelAnalyzeAll_ZeroDegree_FallsBackToSequential()
    {
        // Arrange - degree 0 means sequential
        var config = new GDSemanticsConfig { EnableParallelAnalysis = true, MaxDegreeOfParallelism = 0 };
        using var project = CreateProjectWithConfig(30, config);

        // Act
        project.AnalyzeAll();

        // Assert
        var analyzedCount = project.ScriptFiles.Count(s => s.SemanticModel != null);
        analyzedCount.Should().Be(30, "degree=0 should fall back to sequential analysis");
    }

    /// <summary>
    /// Creates a project with the specified configuration.
    /// </summary>
    private static GDScriptProject CreateProjectWithConfig(int fileCount, GDSemanticsConfig config)
    {
        var scripts = new List<(string path, string content)>();

        // Create base classes (10% of files, minimum 1)
        int baseClassCount = Math.Max(1, fileCount / 10);
        for (int i = 0; i < baseClassCount; i++)
        {
            scripts.Add((
                $"C:/synthetic/bases/base_{i}.gd",
                Infrastructure.GDScriptCodeGenerator.GenerateEntityClass(i, "Node")
            ));
        }

        // Create derived classes
        for (int i = baseClassCount; i < fileCount; i++)
        {
            int baseIndex = i % baseClassCount;
            string baseClass = $"Entity{baseIndex}";
            scripts.Add((
                $"C:/synthetic/entities/entity_{i}.gd",
                Infrastructure.GDScriptCodeGenerator.GenerateEntityWithCrossReferences(i, baseClass, i)
            ));
        }

        var fileSystem = new GDInMemoryFileSystem();
        var context = new GDSyntheticProjectContext("C:/synthetic", fileSystem);
        var options = new GDScriptProjectOptions
        {
            EnableSceneTypesProvider = false,
            EnableCallSiteRegistry = false,
            FileSystem = fileSystem,
            SemanticsConfig = config
        };

        var project = new GDScriptProject(context, options);

        foreach (var (path, content) in scripts)
        {
            project.AddScript(path, content);
        }

        return project;
    }
}
