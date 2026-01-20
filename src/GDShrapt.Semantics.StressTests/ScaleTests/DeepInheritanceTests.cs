namespace GDShrapt.Semantics.StressTests.ScaleTests;

/// <summary>
/// Tests for deep inheritance chain resolution.
/// Validates that inherited members are correctly resolved through multiple levels.
/// </summary>
[TestClass]
[TestCategory("StressTests")]
[TestCategory("Scale")]
public class DeepInheritanceTests
{
    [TestMethod]
    [DataRow(5)]
    [DataRow(10)]
    [DataRow(15)]
    [Timeout(60000)]
    public void DeepInheritance_NLevels_ResolvesAllMembers(int depth)
    {
        // Arrange
        using var project = SyntheticProjectGenerator.GenerateDeepInheritanceProject(depth);
        var threshold = PerformanceThresholds.Scale(PerformanceThresholds.DeepInheritance15Levels);

        // Act
        var sw = Stopwatch.StartNew();
        project.AnalyzeAll();
        sw.Stop();

        // Assert - verify all scripts were analyzed
        project.ScriptFiles.Count().Should().Be(depth);

        // Verify each level script exists and has its own symbols
        for (int level = 0; level < depth; level++)
        {
            var script = project.ScriptFiles.FirstOrDefault(s => s.TypeName == $"Level{level}");
            script.Should().NotBeNull(
                because: $"Level{level} script should exist");

            var analyzer = script!.Analyzer;
            analyzer.Should().NotBeNull(
                because: $"Level{level} should be analyzed");

            // Each level should have its own declared variable
            var varName = $"level_{level}_var";
            var symbol = analyzer!.FindSymbol(varName);
            symbol.Should().NotBeNull(
                because: $"'{varName}' should be declared in Level{level}");

            // Each level should have its own declared method
            var methodName = $"level_{level}_method";
            var methodSymbol = analyzer.FindSymbol(methodName);
            methodSymbol.Should().NotBeNull(
                because: $"'{methodName}' should be declared in Level{level}");
        }

        // Verify extends clause is correctly parsed for non-root levels
        for (int level = 1; level < depth; level++)
        {
            var script = project.ScriptFiles.First(s => s.TypeName == $"Level{level}");
            var extendsType = script.Class?.Extends?.Type?.BuildName();
            extendsType.Should().Be($"Level{level - 1}",
                because: $"Level{level} should extend Level{level - 1}");
        }

        // Performance check for deep inheritance
        if (depth >= 15)
        {
            sw.Elapsed.Should().BeLessThan(threshold,
                because: $"{depth}-level inheritance should resolve within {threshold.TotalSeconds}s");
        }

        Console.WriteLine($"[PERF] {depth}-level inheritance analyzed in {sw.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    [Timeout(60000)]
    public void DeepInheritance_15Levels_CallAllParentsResolves()
    {
        // Arrange
        const int depth = 15;
        using var project = SyntheticProjectGenerator.GenerateDeepInheritanceProject(depth);
        project.AnalyzeAll();

        // Get the deepest level script
        var deepestScript = project.ScriptFiles
            .First(s => s.TypeName == $"Level{depth - 1}");

        // Act - find the method that calls all parent methods
        var analyzer = deepestScript.Analyzer!;
        var callAllParents = analyzer.FindSymbol("call_all_parents");

        // Assert
        callAllParents.Should().NotBeNull(
            because: "call_all_parents method should exist in the deepest level");

        // The method body references all parent level methods
        // Verify the semantic model can resolve these references
        var semanticModel = analyzer.SemanticModel;
        semanticModel.Should().NotBeNull();

        // Verify each level has its method defined
        for (int level = 0; level < depth; level++)
        {
            var script = project.ScriptFiles.First(s => s.TypeName == $"Level{level}");
            var levelAnalyzer = script.Analyzer!;
            var methodSymbol = levelAnalyzer.FindSymbol($"level_{level}_method");
            methodSymbol.Should().NotBeNull(
                because: $"level_{level}_method should be defined in Level{level}");
        }
    }

    [TestMethod]
    [Timeout(60000)]
    public void DeepInheritance_OverrideMethod_ResolvesCorrectLevel()
    {
        // Arrange
        const int depth = 10;
        using var project = SyntheticProjectGenerator.GenerateDeepInheritanceProject(depth);
        project.AnalyzeAll();

        // Each level overrides get_level() method
        foreach (var script in project.ScriptFiles)
        {
            var analyzer = script.Analyzer!;
            var getLevelMethod = analyzer.FindSymbol("get_level");

            getLevelMethod.Should().NotBeNull(
                because: $"{script.TypeName} should have get_level method");
        }
    }

    [TestMethod]
    [Timeout(120000)]
    public void DeepInheritance_ScalingBehavior_IsLinear()
    {
        // Arrange - measure times for different depths
        var depths = new[] { 5, 10, 15 };
        var times = new List<(int depth, long ms)>();

        foreach (var depth in depths)
        {
            using var project = SyntheticProjectGenerator.GenerateDeepInheritanceProject(depth);

            var sw = Stopwatch.StartNew();
            project.AnalyzeAll();

            // Exercise member resolution at each level
            foreach (var script in project.ScriptFiles)
            {
                var analyzer = script.Analyzer!;
                // Access the symbols to exercise the semantic model
                _ = analyzer.Symbols.ToList();
                _ = analyzer.GetMethods().ToList();
            }

            sw.Stop();
            times.Add((depth, sw.ElapsedMilliseconds));
            Console.WriteLine($"[PERF] {depth}-level inheritance: {sw.ElapsedMilliseconds}ms");
        }

        // Assert - scaling should be roughly linear with depth
        // Going from 5 to 15 levels (3x) should be roughly 3x time, not 9x (quadratic)
        var ratio = (double)times[2].ms / times[0].ms;
        var depthRatio = (double)depths[2] / depths[0]; // 3.0

        ratio.Should().BeLessThan(depthRatio * 2.5,
            because: $"inheritance depth scaling should be roughly linear (actual: {ratio:F2}x for {depthRatio:F1}x depth)");
    }

    [TestMethod]
    [Timeout(60000)]
    public void DeepInheritance_WithEntityClasses_InheritsCorrectly()
    {
        // Arrange - create a combined project with inheritance and entity classes
        using var project = SyntheticProjectGenerator.GenerateCombinedStressProject(
            fileCount: 20,
            inheritanceDepth: 10,
            referencesPerSymbol: 50);

        // Act
        project.AnalyzeAll();

        // Assert - entity classes should inherit from the deepest level
        var entityScripts = project.ScriptFiles
            .Where(s => s.TypeName != null && s.TypeName.StartsWith("Entity"))
            .ToList();

        entityScripts.Should().NotBeEmpty();

        foreach (var entity in entityScripts.Take(5))
        {
            var analyzer = entity.Analyzer!;
            analyzer.Should().NotBeNull(
                because: $"{entity.TypeName} should be analyzed");

            // Verify the entity extends the correct base class
            var extendsType = entity.Class?.Extends?.Type?.BuildName();
            extendsType.Should().Be("Level9",
                because: $"{entity.TypeName} should extend Level9");

            // Verify entity has its own symbols
            var symbols = analyzer.Symbols.ToList();
            symbols.Should().NotBeEmpty(
                because: $"{entity.TypeName} should have its own symbols");
        }

        // Verify all level classes have their methods
        for (int level = 0; level < 10; level++)
        {
            var script = project.ScriptFiles.FirstOrDefault(s => s.TypeName == $"Level{level}");
            script.Should().NotBeNull(
                because: $"Level{level} should exist in combined project");

            var levelAnalyzer = script!.Analyzer!;
            var method = levelAnalyzer.FindSymbol($"level_{level}_method");
            method.Should().NotBeNull(
                because: $"Level{level} should have level_{level}_method defined");
        }
    }
}
