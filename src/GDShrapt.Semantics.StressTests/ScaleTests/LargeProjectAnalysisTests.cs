namespace GDShrapt.Semantics.StressTests.ScaleTests;

/// <summary>
/// Tests for analyzing large projects at scale.
/// Validates that analysis completes within acceptable time and memory bounds.
/// </summary>
[TestClass]
[TestCategory("StressTests")]
[TestCategory("Scale")]
public class LargeProjectAnalysisTests
{
    [TestMethod]
    [Timeout(60000)] // 1 minute timeout
    public void AnalyzeProject_100Files_CompletesWithinThreshold()
    {
        // Arrange
        const int fileCount = 100;
        using var project = SyntheticProjectGenerator.GenerateLargeProject(fileCount);
        var threshold = PerformanceThresholds.GetProjectAnalysisThreshold(fileCount);

        // Act
        var sw = Stopwatch.StartNew();
        project.AnalyzeAll();
        sw.Stop();

        // Assert
        sw.Elapsed.Should().BeLessThan(threshold,
            because: $"{fileCount}-file project analysis should complete within {threshold.TotalSeconds:F1}s");

        // Verify correctness
        project.ScriptFiles.Count().Should().Be(fileCount,
            because: "all scripts should be loaded");

        project.ScriptFiles.All(s => s.Class != null).Should().BeTrue(
            because: "all scripts should be parsed");

        project.ScriptFiles.All(s => s.SemanticModel != null).Should().BeTrue(
            because: "all scripts should be analyzed");

        // Output performance info
        Console.WriteLine($"[PERF] {fileCount} files analyzed in {sw.ElapsedMilliseconds}ms (threshold: {threshold.TotalMilliseconds}ms)");
    }

    [TestMethod]
    [Timeout(180000)] // 3 minute timeout
    public void AnalyzeProject_500Files_CompletesWithinThreshold()
    {
        // Arrange
        const int fileCount = 500;
        using var project = SyntheticProjectGenerator.GenerateLargeProject(fileCount);
        var threshold = PerformanceThresholds.GetProjectAnalysisThreshold(fileCount);

        // Act
        var sw = Stopwatch.StartNew();
        project.AnalyzeAll();
        sw.Stop();

        // Assert
        sw.Elapsed.Should().BeLessThan(threshold,
            because: $"{fileCount}-file project analysis should complete within {threshold.TotalSeconds:F1}s");

        project.ScriptFiles.Count().Should().Be(fileCount);
        project.ScriptFiles.All(s => s.SemanticModel != null).Should().BeTrue();

        Console.WriteLine($"[PERF] {fileCount} files analyzed in {sw.ElapsedMilliseconds}ms (threshold: {threshold.TotalMilliseconds}ms)");
    }

    [TestMethod]
    [Timeout(600000)] // 10 minute timeout
    [TestCategory("LongRunning")]
    public void AnalyzeProject_1000Files_CompletesWithinThreshold()
    {
        // Arrange
        const int fileCount = 1000;
        using var project = SyntheticProjectGenerator.GenerateLargeProject(fileCount);
        var threshold = PerformanceThresholds.GetProjectAnalysisThreshold(fileCount);

        // Act
        var sw = Stopwatch.StartNew();
        project.AnalyzeAll();
        sw.Stop();

        // Assert
        sw.Elapsed.Should().BeLessThan(threshold,
            because: $"{fileCount}-file project analysis should complete within {threshold.TotalSeconds:F1}s");

        project.ScriptFiles.Count().Should().Be(fileCount);
        project.ScriptFiles.All(s => s.SemanticModel != null).Should().BeTrue();

        Console.WriteLine($"[PERF] {fileCount} files analyzed in {sw.ElapsedMilliseconds}ms (threshold: {threshold.TotalMilliseconds}ms)");
    }

    [TestMethod]
    [Timeout(120000)]
    public void AnalyzeProject_100Files_MemoryWithinBounds()
    {
        // Arrange
        const int fileCount = 100;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryBefore = GC.GetTotalMemory(true);

        // Act
        using var project = SyntheticProjectGenerator.GenerateLargeProject(fileCount);
        project.AnalyzeAll();

        var memoryAfter = GC.GetTotalMemory(false);
        var memoryUsed = memoryAfter - memoryBefore;
        var memoryThreshold = PerformanceThresholds.GetMemoryThreshold(fileCount);

        // Assert
        memoryUsed.Should().BeLessThan(memoryThreshold,
            because: $"{fileCount}-file project should use less than {memoryThreshold / (1024 * 1024)}MB");

        Console.WriteLine($"[PERF] {fileCount} files: {memoryUsed / (1024 * 1024)}MB used (threshold: {memoryThreshold / (1024 * 1024)}MB)");
    }

    [TestMethod]
    [Timeout(300000)]
    [TestCategory("LongRunning")]
    public void AnalyzeProject_500Files_MemoryWithinBounds()
    {
        // Arrange
        const int fileCount = 500;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryBefore = GC.GetTotalMemory(true);

        // Act
        using var project = SyntheticProjectGenerator.GenerateLargeProject(fileCount);
        project.AnalyzeAll();

        var memoryAfter = GC.GetTotalMemory(false);
        var memoryUsed = memoryAfter - memoryBefore;
        var memoryThreshold = PerformanceThresholds.GetMemoryThreshold(fileCount);

        // Assert
        memoryUsed.Should().BeLessThan(memoryThreshold,
            because: $"{fileCount}-file project should use less than {memoryThreshold / (1024 * 1024)}MB");

        Console.WriteLine($"[PERF] {fileCount} files: {memoryUsed / (1024 * 1024)}MB used (threshold: {memoryThreshold / (1024 * 1024)}MB)");
    }

    [TestMethod]
    [Timeout(120000)]
    public void AnalyzeProject_CrossFileTypes_ResolveCorrectly()
    {
        // Arrange
        const int fileCount = 50;
        using var project = SyntheticProjectGenerator.GenerateLargeProject(fileCount);

        // Act
        project.AnalyzeAll();

        // Assert - verify cross-file type resolution works
        var derivedScripts = project.ScriptFiles
            .Where(s => s.TypeName != null && s.TypeName.StartsWith("Entity") && int.Parse(s.TypeName.Substring(6)) >= 5)
            .ToList();

        foreach (var script in derivedScripts.Take(10))
        {
            // Each derived entity should be analyzed and have its own symbols
            var semanticModel = script.SemanticModel;
            semanticModel.Should().NotBeNull();

            var symbols = semanticModel!.Symbols.ToList();
            symbols.Should().NotBeEmpty(
                because: $"{script.TypeName} should have its own declared symbols");

            // Verify extends clause is properly parsed
            var extendsType = script.Class?.Extends?.Type?.BuildName();
            extendsType.Should().NotBeNull(
                because: $"{script.TypeName} should extend a base class");
        }

        // Verify base classes have their health variable
        var baseScripts = project.ScriptFiles
            .Where(s => s.TypeName != null && s.TypeName.StartsWith("Entity") && int.Parse(s.TypeName.Substring(6)) < 5)
            .ToList();

        foreach (var script in baseScripts)
        {
            var semanticModel = script.SemanticModel!;
            var healthSymbol = semanticModel.FindSymbol("health");
            healthSymbol.Should().NotBeNull(
                because: $"Base class {script.TypeName} should have 'health' declared");
        }
    }

    [TestMethod]
    [Timeout(60000)]
    public void AnalyzeProject_ScalingBehavior_IsSubQuadratic()
    {
        // Arrange - measure times for different sizes
        var sizes = new[] { 25, 50, 100 };
        var times = new List<(int size, long ms)>();

        foreach (var size in sizes)
        {
            using var project = SyntheticProjectGenerator.GenerateLargeProject(size);

            var sw = Stopwatch.StartNew();
            project.AnalyzeAll();
            sw.Stop();

            times.Add((size, sw.ElapsedMilliseconds));
            Console.WriteLine($"[PERF] {size} files: {sw.ElapsedMilliseconds}ms");
        }

        // Assert - scaling should be sub-quadratic (closer to O(n) or O(n log n))
        // If it were O(n^2), going from 25 to 100 files (4x) would take 16x longer
        // We expect roughly linear scaling, so 4x files should be ~4-8x time
        var ratio = (double)times[2].ms / times[0].ms;
        var sizeRatio = (double)sizes[2] / sizes[0]; // 4.0

        ratio.Should().BeLessThan(sizeRatio * 3,
            because: $"scaling from {sizes[0]} to {sizes[2]} files should be sub-quadratic (actual ratio: {ratio:F2}, expected < {sizeRatio * 3:F2})");

        Console.WriteLine($"[PERF] Scaling ratio: {ratio:F2}x for {sizeRatio:F1}x more files");
    }
}
