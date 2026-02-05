using FsCheck;

namespace GDShrapt.Semantics.StressTests.PropertyTests;

/// <summary>
/// Property-based tests for type inference.
/// Uses FsCheck to generate random inputs and verify invariants hold.
/// </summary>
[TestClass]
[TestCategory("StressTests")]
[TestCategory("PropertyBased")]
public class TypeInferencePropertyTests
{
    /// <summary>
    /// Type inference should be deterministic - same input always produces same output.
    /// </summary>
    [TestMethod]
    [Timeout(120000)]
    public void TypeInference_IsDeterministic()
    {
        // Arrange
        var config = Configuration.QuickThrowOnFailure;
        config.MaxNbOfTest = 20;

        // Act & Assert
        Prop.ForAll(
            Arb.From(Gen.Choose(5, 30)),
            (int fileCount) =>
            {
                using var project = SyntheticProjectGenerator.GenerateLargeProject(fileCount);

                // Analyze twice
                project.AnalyzeAll();
                var firstResults = CollectAllTypes(project);

                // Re-analyze (should produce identical results)
                project.AnalyzeAll();
                var secondResults = CollectAllTypes(project);

                return firstResults.SequenceEqual(secondResults);
            }).Check(config);
    }

    /// <summary>
    /// Symbol lookup should be consistent across multiple calls.
    /// </summary>
    [TestMethod]
    [Timeout(60000)]
    public void SymbolLookup_IsIdempotent()
    {
        var config = Configuration.QuickThrowOnFailure;
        config.MaxNbOfTest = 50;

        Prop.ForAll(
            Arb.From(Gen.Choose(10, 50)),
            (int fileCount) =>
            {
                using var project = SyntheticProjectGenerator.GenerateLargeProject(fileCount);
                project.AnalyzeAll();

                // Lookup same symbol multiple times
                foreach (var script in project.ScriptFiles.Take(10))
                {
                    var semanticModel = script.SemanticModel;
                    if (semanticModel == null) continue;

                    var first = semanticModel.FindSymbol("health");
                    var second = semanticModel.FindSymbol("health");
                    var third = semanticModel.FindSymbol("health");

                    // All lookups should return same result
                    if (first != null || second != null || third != null)
                    {
                        var consistent = (first == null) == (second == null) && (second == null) == (third == null);
                        if (!consistent) return false;
                    }
                }

                return true;
            }).Check(config);
    }

    /// <summary>
    /// Inheritance chain resolution should be transitive.
    /// Each level should have its own symbols, and extends clause should form a chain.
    /// </summary>
    [TestMethod]
    [Timeout(60000)]
    public void InheritanceResolution_IsTransitive()
    {
        var config = Configuration.QuickThrowOnFailure;
        config.MaxNbOfTest = 10;

        Prop.ForAll(
            Arb.From(Gen.Choose(3, 15)),
            (int depth) =>
            {
                using var project = SyntheticProjectGenerator.GenerateDeepInheritanceProject(depth);
                project.AnalyzeAll();

                // Verify each level has its own symbols and correct extends
                for (int level = 0; level < depth; level++)
                {
                    var script = project.ScriptFiles.FirstOrDefault(s => s.TypeName == $"Level{level}");
                    if (script?.SemanticModel == null)
                    {
                        Console.WriteLine($"Failed: Level{level} not found or not analyzed");
                        return false;
                    }

                    var semanticModel = script.SemanticModel;
                    var symbol = semanticModel.FindSymbol($"level_{level}_var");
                    if (symbol == null)
                    {
                        Console.WriteLine($"Failed: level_{level}_var not found in Level{level}");
                        return false;
                    }

                    // Verify extends chain (except Level0 which extends Node)
                    if (level > 0)
                    {
                        var extendsType = script.Class?.Extends?.Type?.BuildName();
                        if (extendsType != $"Level{level - 1}")
                        {
                            Console.WriteLine($"Failed: Level{level} should extend Level{level - 1}, got {extendsType}");
                            return false;
                        }
                    }
                }

                return true;
            }).Check(config);
    }

    /// <summary>
    /// Method resolution should be consistent with the class hierarchy.
    /// </summary>
    [TestMethod]
    [Timeout(60000)]
    public void MethodResolution_RespectsHierarchy()
    {
        var config = Configuration.QuickThrowOnFailure;
        config.MaxNbOfTest = 10;

        Prop.ForAll(
            Arb.From(Gen.Choose(5, 12)),
            (int depth) =>
            {
                using var project = SyntheticProjectGenerator.GenerateDeepInheritanceProject(depth);
                project.AnalyzeAll();

                // Each level should have its own get_level method
                foreach (var script in project.ScriptFiles)
                {
                    var semanticModel = script.SemanticModel;
                    if (semanticModel == null) continue;

                    var getLevel = semanticModel.FindSymbol("get_level");
                    if (getLevel == null)
                    {
                        Console.WriteLine($"Failed: get_level not found in {script.TypeName}");
                        return false;
                    }
                }

                return true;
            }).Check(config);
    }

    /// <summary>
    /// Generated code should always parse without errors.
    /// </summary>
    [TestMethod]
    [Timeout(60000)]
    public void GeneratedCode_AlwaysParses()
    {
        var config = Configuration.QuickThrowOnFailure;
        config.MaxNbOfTest = 30;

        Prop.ForAll(
            Arb.From(Gen.Choose(1, 100)),
            (int seed) =>
            {
                // Generate various types of code using the seed
                var codes = new[]
                {
                    GDScriptCodeGenerator.GenerateEntityClass(seed, "Node"),
                    GDScriptCodeGenerator.GenerateDeepInheritanceClass(seed % 10, 10),
                    GDScriptCodeGenerator.GenerateManyReferencesClass("symbol", seed % 50 + 10),
                    GDScriptCodeGenerator.GenerateLongMethod(seed % 100 + 50),
                    GDScriptCodeGenerator.GenerateComplexTypesScript(seed % 20 + 5)
                };

                var reader = new GDScriptReader();
                foreach (var code in codes)
                {
                    try
                    {
                        var parsed = reader.ParseFileContent(code);
                        if (parsed.AllInvalidTokens.Any())
                        {
                            Console.WriteLine($"Parse errors for seed {seed}:");
                            foreach (var token in parsed.AllInvalidTokens.Take(5))
                            {
                                Console.WriteLine($"  Invalid: {token}");
                            }
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception for seed {seed}: {ex.Message}");
                        return false;
                    }
                }

                return true;
            }).Check(config);
    }

    /// <summary>
    /// Analysis should complete for any valid project configuration.
    /// </summary>
    [TestMethod]
    [Timeout(120000)]
    public void Analysis_AlwaysCompletes()
    {
        var config = Configuration.QuickThrowOnFailure;
        config.MaxNbOfTest = 15;

        Prop.ForAll(
            Arb.From(Gen.Choose(5, 50)),
            (int fileCount) =>
            {
                try
                {
                    using var project = SyntheticProjectGenerator.GenerateLargeProject(fileCount);
                    project.AnalyzeAll();

                    // Verify all scripts were analyzed
                    var analyzed = project.ScriptFiles.Count(s => s.SemanticModel != null);
                    return analyzed == fileCount;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Analysis failed for {fileCount} files: {ex.Message}");
                    return false;
                }
            }).Check(config);
    }

    /// <summary>
    /// Combined stress project should analyze correctly.
    /// </summary>
    [TestMethod]
    [Timeout(120000)]
    public void CombinedStress_AnalyzesCorrectly()
    {
        var config = Configuration.QuickThrowOnFailure;
        config.MaxNbOfTest = 10;

        Prop.ForAll(
            Arb.From(Gen.Choose(5, 20)),
            Arb.From(Gen.Choose(3, 10)),
            Arb.From(Gen.Choose(20, 100)),
            (int files, int depth, int refs) =>
            {
                try
                {
                    using var project = SyntheticProjectGenerator.GenerateCombinedStressProject(files, depth, refs);
                    project.AnalyzeAll();

                    // All scripts should be analyzed
                    var allAnalyzed = project.ScriptFiles.All(s => s.SemanticModel != null);
                    if (!allAnalyzed)
                    {
                        Console.WriteLine($"Not all scripts analyzed: files={files}, depth={depth}, refs={refs}");
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed: files={files}, depth={depth}, refs={refs}: {ex.Message}");
                    return false;
                }
            }).Check(config);
    }

    /// <summary>
    /// Collects all type information from a project for comparison.
    /// </summary>
    private static List<string> CollectAllTypes(GDScriptProject project)
    {
        var results = new List<string>();
        foreach (var script in project.ScriptFiles.OrderBy(s => s.FullPath))
        {
            var semanticModel = script.SemanticModel;
            if (semanticModel == null) continue;

            foreach (var symbol in semanticModel.Symbols.OrderBy(s => s.Name))
            {
                var typeInfo = semanticModel.TypeSystem.GetTypeInfo(symbol.Name);
                var type = typeInfo?.InferredType?.DisplayName ?? "null";
                results.Add($"{script.TypeName}.{symbol.Name}:{type}");
            }
        }
        return results;
    }
}
