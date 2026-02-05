namespace GDShrapt.Semantics.StressTests.MemoryTests;

/// <summary>
/// Tests for memory behavior during long-running analysis operations.
/// Validates that memory usage is stable and no leaks occur.
/// </summary>
[TestClass]
[TestCategory("StressTests")]
[TestCategory("Memory")]
public class LongRunningAnalysisTests
{
    [TestMethod]
    [Timeout(300000)] // 5 minutes
    [TestCategory("LongRunning")]
    public void RepeatedAnalysis_NoMemoryLeak()
    {
        // Arrange
        var memorySnapshots = new List<long>();
        const int iterations = 20;
        const int fileCount = 50;

        // Act - repeatedly create and dispose projects
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            using (var project = SyntheticProjectGenerator.GenerateLargeProject(fileCount))
            {
                project.AnalyzeAll();

                // Verify it actually analyzed
                project.ScriptFiles.All(s => s.SemanticModel != null).Should().BeTrue();
            }

            // Force GC and record memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            memorySnapshots.Add(GC.GetTotalMemory(true));

            if (iteration % 5 == 0)
            {
                Console.WriteLine($"[MEMORY] Iteration {iteration}: {memorySnapshots.Last() / (1024 * 1024)}MB");
            }
        }

        // Assert - memory should not grow significantly over time
        var firstQuarter = memorySnapshots.Take(5).Average();
        var lastQuarter = memorySnapshots.Skip(15).Average();

        // Allow up to 50% growth (some variance is expected due to GC timing)
        lastQuarter.Should().BeLessThan(firstQuarter * 1.5,
            because: $"memory should not grow significantly with repeated analysis (first: {firstQuarter / (1024 * 1024):F1}MB, last: {lastQuarter / (1024 * 1024):F1}MB)");

        Console.WriteLine($"[MEMORY] First quarter avg: {firstQuarter / (1024 * 1024):F1}MB");
        Console.WriteLine($"[MEMORY] Last quarter avg: {lastQuarter / (1024 * 1024):F1}MB");
        Console.WriteLine($"[MEMORY] Growth ratio: {lastQuarter / firstQuarter:F2}x");
    }

    [TestMethod]
    [Timeout(180000)]
    public void RepeatedTypeInference_NoMemoryLeak()
    {
        // Arrange
        using var project = SyntheticProjectGenerator.GenerateLargeProject(30);
        project.AnalyzeAll();

        var scripts = project.ScriptFiles.ToList();
        var memorySnapshots = new List<long>();
        const int iterations = 50;

        // Act - repeatedly perform type inference queries
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            foreach (var script in scripts)
            {
                var semanticModel = script.SemanticModel;
                if (semanticModel == null) continue;

                // Perform various type queries
                _ = semanticModel.TypeSystem.GetTypeInfo("health");
                _ = semanticModel.TypeSystem.GetTypeInfo("speed");
                _ = semanticModel.Symbols.ToList();
                _ = semanticModel.GetMethods().ToList();
            }

            if (iteration % 10 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                memorySnapshots.Add(GC.GetTotalMemory(true));
                Console.WriteLine($"[MEMORY] Query iteration {iteration}: {memorySnapshots.Last() / (1024 * 1024)}MB");
            }
        }

        // Assert - memory should be stable during queries
        if (memorySnapshots.Count >= 3)
        {
            var firstHalf = memorySnapshots.Take(memorySnapshots.Count / 2).Average();
            var secondHalf = memorySnapshots.Skip(memorySnapshots.Count / 2).Average();

            secondHalf.Should().BeLessThan(firstHalf * 1.3,
                because: "memory should be stable during repeated queries");
        }
    }

    [TestMethod]
    [Timeout(120000)]
    public void LargeProject_PeakMemory_WithinBounds()
    {
        // Arrange
        const int fileCount = 200;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long baselineMemory = GC.GetTotalMemory(true);
        long peakMemory = baselineMemory;

        // Act
        using (var project = SyntheticProjectGenerator.GenerateLargeProject(fileCount))
        {
            var afterCreation = GC.GetTotalMemory(false);
            peakMemory = Math.Max(peakMemory, afterCreation);

            project.AnalyzeAll();

            var afterAnalysis = GC.GetTotalMemory(false);
            peakMemory = Math.Max(peakMemory, afterAnalysis);

            Console.WriteLine($"[MEMORY] After creation: {afterCreation / (1024 * 1024)}MB");
            Console.WriteLine($"[MEMORY] After analysis: {afterAnalysis / (1024 * 1024)}MB");
        }

        var memoryUsed = peakMemory - baselineMemory;
        var threshold = PerformanceThresholds.GetMemoryThreshold(fileCount);

        // Assert
        memoryUsed.Should().BeLessThan(threshold,
            because: $"peak memory for {fileCount} files should be less than {threshold / (1024 * 1024)}MB");

        Console.WriteLine($"[MEMORY] Peak usage: {memoryUsed / (1024 * 1024)}MB (threshold: {threshold / (1024 * 1024)}MB)");
    }

    [TestMethod]
    [Timeout(120000)]
    public void ProjectDisposal_ReleasesMemory()
    {
        // Arrange
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var baselineMemory = GC.GetTotalMemory(true);

        // Act - create and fully analyze a project
        var project = SyntheticProjectGenerator.GenerateLargeProject(100);
        project.AnalyzeAll();

        var afterAnalysis = GC.GetTotalMemory(false);
        var memoryDuringAnalysis = afterAnalysis - baselineMemory;

        Console.WriteLine($"[MEMORY] During analysis: {memoryDuringAnalysis / (1024 * 1024)}MB");

        // Dispose and clean up
        project.Dispose();
        project = null!;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var afterDisposal = GC.GetTotalMemory(true);
        var memoryAfterDisposal = afterDisposal - baselineMemory;

        Console.WriteLine($"[MEMORY] After disposal: {memoryAfterDisposal / (1024 * 1024)}MB");

        // Assert - most memory should be released
        // Allow for some retained memory from static caches, etc.
        var retainedRatio = (double)memoryAfterDisposal / memoryDuringAnalysis;
        retainedRatio.Should().BeLessThan(0.3,
            because: $"disposal should release most memory (retained: {retainedRatio:P0})");
    }

    [TestMethod]
    [Timeout(180000)]
    [TestCategory("LongRunning")]
    public void IncrementalUpdates_NoMemoryAccumulation()
    {
        // Arrange
        using var project = SyntheticProjectGenerator.GenerateLargeProject(50, enableCallSiteRegistry: true);
        project.AnalyzeAll();
        project.BuildCallSiteRegistry();

        var targetScript = project.ScriptFiles.First();
        var originalContent = targetScript.Class?.ToString() ?? "";
        var memorySnapshots = new List<long>();

        // Act - repeatedly modify and reanalyze a single file
        for (int i = 0; i < 30; i++)
        {
            // Modify the file
            var modifiedContent = originalContent + $"\n# Comment {i}";
            targetScript.Reload(modifiedContent);
            targetScript.Analyze(project.CreateRuntimeProvider());

            // Restore original
            targetScript.Reload(originalContent);
            targetScript.Analyze(project.CreateRuntimeProvider());

            if (i % 10 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                memorySnapshots.Add(GC.GetTotalMemory(true));
                Console.WriteLine($"[MEMORY] Update iteration {i}: {memorySnapshots.Last() / (1024 * 1024)}MB");
            }
        }

        // Assert - memory should be stable
        if (memorySnapshots.Count >= 2)
        {
            var growthRatio = (double)memorySnapshots.Last() / memorySnapshots.First();
            growthRatio.Should().BeLessThan(1.5,
                because: $"memory should not accumulate during incremental updates (growth: {growthRatio:F2}x)");
        }
    }

    [TestMethod]
    [Timeout(120000)]
    public void DeepInheritance_MemoryEfficient()
    {
        // Arrange
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var baselineMemory = GC.GetTotalMemory(true);

        // Act - create deep inheritance project
        using (var project = SyntheticProjectGenerator.GenerateDeepInheritanceProject(15))
        {
            project.AnalyzeAll();

            // Exercise inheritance chain resolution
            var deepest = project.ScriptFiles.First(s => s.TypeName == "Level14");
            var semanticModel = deepest.SemanticModel!;

            for (int level = 0; level < 15; level++)
            {
                _ = semanticModel.FindSymbol($"level_{level}_var");
                _ = semanticModel.FindSymbol($"level_{level}_method");
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var afterCleanup = GC.GetTotalMemory(true);
        var retained = afterCleanup - baselineMemory;

        // Assert - deep inheritance should not cause excessive memory retention
        // Note: 100MB allows for static caches, JIT compilation, and framework overhead
        retained.Should().BeLessThan(100 * 1024 * 1024,
            because: "deep inheritance analysis should not retain excessive memory after disposal");

        Console.WriteLine($"[MEMORY] Retained after deep inheritance: {retained / (1024 * 1024)}MB");
    }
}
