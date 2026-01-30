using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.Incremental;

/// <summary>
/// Integration tests for incremental parser + semantic model integration.
/// Tests configuration options, end-to-end scenarios, and component interaction.
/// </summary>
[TestClass]
public class IncrementalIntegrationTests
{
    #region Incremental Parsing Configuration Tests

    [TestMethod]
    public void IncrementalParsing_Enabled_UsesMemberReparse()
    {
        // Arrange
        var config = new GDSemanticsConfig { EnableIncrementalParsing = true };
        var original = "extends Node\nvar x = 1\nfunc test():\n    pass";
        var project = CreateProjectWithConfig(config,
            ("test.gd", original));

        var script = project.ScriptFiles.First();
        var oldTree = script.Class;

        // "extends Node\n" = 13 chars, "var x = " = 8 chars, position of "1" = 21
        var posOfValue = original.IndexOf("= 1") + 2; // Position of "1"
        var changes = new[] { GDTextChange.Replace(posOfValue, 1, "42") };

        // Act
        var result = script.Reload("extends Node\nvar x = 42\nfunc test():\n    pass", changes);

        // Assert
        result.WasIncremental.Should().BeTrue("change is within a member, should use incremental parsing");
        result.Success.Should().BeTrue();
    }

    [TestMethod]
    public void IncrementalParsing_Disabled_UsesFullReparse()
    {
        // Arrange
        var config = new GDSemanticsConfig { EnableIncrementalParsing = false };
        var original = "extends Node\nvar x = 1\nfunc test():\n    pass";
        var project = CreateProjectWithConfig(config,
            ("test.gd", original));

        var script = project.ScriptFiles.First();
        var oldTree = script.Class;

        var posOfValue = original.IndexOf("= 1") + 2;
        var changes = new[] { GDTextChange.Replace(posOfValue, 1, "42") };

        // Act
        var result = script.Reload("extends Node\nvar x = 42\nfunc test():\n    pass", changes);

        // Assert
        result.WasIncremental.Should().BeFalse("incremental parsing is disabled");
        result.Success.Should().BeTrue();
    }

    [TestMethod]
    public void IncrementalParsing_Default_IsEnabled()
    {
        // Arrange
        var config = new GDSemanticsConfig();

        // Assert
        config.EnableIncrementalParsing.Should().BeTrue();
    }

    #endregion

    #region Incremental Analysis Configuration Tests

    [TestMethod]
    public void IncrementalAnalysis_Enabled_InvalidatesOnlyAffected()
    {
        // Arrange
        var config = new GDSemanticsConfig { EnableIncrementalAnalysis = true };
        var project = CreateProjectWithConfig(config,
            ("a.gd", "extends Node\nclass_name ClassA\nvar x = 1"),
            ("b.gd", "extends Node\nclass_name ClassB\nvar y = 2"));
        project.AnalyzeAll();

        var invalidatedPaths = new ConcurrentBag<string>();

        using var model = new GDProjectSemanticModel(project, subscribeToChanges: true, config);
        model.FileInvalidated += (s, path) => invalidatedPaths.Add(path);

        // Pre-cache both models
        foreach (var script in project.ScriptFiles)
            _ = model.GetSemanticModel(script);

        var scriptA = project.ScriptFiles.First(s => s.TypeName == "ClassA");

        // Act - Trigger incremental change via ProcessIncrementalChange
        var changeArgs = new GDScriptIncrementalChangeEventArgs(
            scriptA.FullPath!,
            scriptA,
            scriptA.Class,
            scriptA.Class,
            GDIncrementalChangeKind.Modified);

        // Use reflection to call ProcessIncrementalChange
        var processMethod = model.GetType().GetMethod("ProcessIncrementalChange",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        processMethod?.Invoke(model, new object[] { changeArgs });

        // Assert
        invalidatedPaths.Should().Contain(scriptA.FullPath);
        // ClassB should NOT be invalidated (no dependency)
        invalidatedPaths.Should().NotContain(project.ScriptFiles.First(s => s.TypeName == "ClassB").FullPath);
    }

    [TestMethod]
    public void IncrementalAnalysis_Disabled_InvalidatesAll()
    {
        // Arrange
        var config = new GDSemanticsConfig { EnableIncrementalAnalysis = false };
        var project = CreateProjectWithConfig(config,
            ("a.gd", "extends Node\nclass_name ClassA\nvar x = 1"),
            ("b.gd", "extends Node\nclass_name ClassB\nvar y = 2"));
        project.AnalyzeAll();

        var invalidationCount = 0;

        using var model = new GDProjectSemanticModel(project, subscribeToChanges: true, config);
        model.FileInvalidated += (s, path) => Interlocked.Increment(ref invalidationCount);

        // Pre-cache both models
        foreach (var script in project.ScriptFiles)
            _ = model.GetSemanticModel(script);

        var scriptA = project.ScriptFiles.First();

        // Act - Trigger incremental change
        var changeArgs = new GDScriptIncrementalChangeEventArgs(
            scriptA.FullPath!,
            scriptA,
            scriptA.Class,
            scriptA.Class,
            GDIncrementalChangeKind.Modified);

        var processMethod = model.GetType().GetMethod("ProcessIncrementalChange",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        processMethod?.Invoke(model, new object[] { changeArgs });

        // Assert - Should have invalidated (InvalidateAll is called)
        invalidationCount.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void IncrementalAnalysis_Default_IsEnabled()
    {
        // Arrange
        var config = new GDSemanticsConfig();

        // Assert
        config.EnableIncrementalAnalysis.Should().BeTrue();
    }

    #endregion

    #region Debounce Configuration Tests

    [TestMethod]
    public void FileChangeDebounceMs_Default_Is300()
    {
        // Arrange
        var config = new GDSemanticsConfig();

        // Assert
        config.FileChangeDebounceMs.Should().Be(300);
    }

    [TestMethod]
    public void FileChangeDebounceMs_CanBeConfigured()
    {
        // Arrange
        var config = new GDSemanticsConfig { FileChangeDebounceMs = 500 };

        // Assert
        config.FileChangeDebounceMs.Should().Be(500);
    }

    [TestMethod]
    public async Task Debounce_RapidChanges_CoalescedInvalidations()
    {
        // Arrange
        var config = new GDSemanticsConfig
        {
            FileChangeDebounceMs = 100,
            EnableIncrementalAnalysis = true
        };
        var project = CreateProjectWithConfig(config,
            ("test.gd", "extends Node\nvar x = 1"));
        project.AnalyzeAll();

        var invalidationCount = 0;

        using var model = new GDProjectSemanticModel(project, subscribeToChanges: true, config);
        model.FileInvalidated += (s, path) => Interlocked.Increment(ref invalidationCount);

        var script = project.ScriptFiles.First();

        // Act - Simulate rapid changes via OnIncrementalChange
        var onIncrementalChangeMethod = model.GetType().GetMethod("OnIncrementalChange",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        for (int i = 0; i < 5; i++)
        {
            var changeArgs = new GDScriptIncrementalChangeEventArgs(
                script.FullPath!,
                script,
                script.Class,
                script.Class,
                GDIncrementalChangeKind.Modified);

            onIncrementalChangeMethod?.Invoke(model, new object?[] { project, changeArgs });
            await Task.Delay(20); // 20ms between changes (much less than 100ms debounce)
        }

        // Wait for debounce to complete
        await Task.Delay(200);

        // Assert - Should have coalesced into fewer invalidations
        invalidationCount.Should().BeLessThan(5, "rapid changes should be debounced");
        invalidationCount.Should().BeGreaterThan(0, "at least one invalidation should occur");
    }

    #endregion

    #region End-to-End Tests

    [TestMethod]
    public void EndToEnd_MultiMemberEdit_SemanticModelValid()
    {
        // Arrange
        var original = "extends Node\nvar a = 1\nvar b = 2\nfunc test():\n    pass";
        var expected = "extends Node\nvar a = 100\nvar b = 200\nfunc test():\n    pass";

        var project = CreateProjectWithConfig(new GDSemanticsConfig { EnableIncrementalParsing = true },
            ("test.gd", original));
        project.AnalyzeAll();

        using var semanticModel = new GDProjectSemanticModel(project);
        var script = project.ScriptFiles.First();

        // Get positions for edits
        var posA = original.IndexOf("= 1") + 2;
        var posB = original.IndexOf("= 2") + 2;

        var changes = new[]
        {
            GDTextChange.Replace(posA, 1, "100"),
            GDTextChange.Replace(posB, 1, "200")
        };

        // Act
        var result = script.Reload(expected, changes);

        // Assert
        result.Success.Should().BeTrue();
        script.Class.Should().NotBeNull();
        script.Class!.ToOriginalString().Should().Be(expected);

        // Verify semantic model can be retrieved after change
        semanticModel.InvalidateFile(script.FullPath!);
        script.Analyze(project.CreateRuntimeProvider());
        var newSemanticModel = semanticModel.GetSemanticModel(script);
        newSemanticModel.Should().NotBeNull();
    }

    [TestMethod]
    public void EndToEnd_EditMethod_CallSitesUpdated()
    {
        // Arrange
        var config = new GDSemanticsConfig { EnableIncrementalParsing = true };
        var options = new GDScriptProjectOptions
        {
            EnableCallSiteRegistry = true,
            EnableSceneTypesProvider = false,
            SemanticsConfig = config,
            FileSystem = new GDInMemoryFileSystem()
        };

        var fileSystem = (GDInMemoryFileSystem)options.FileSystem;
        fileSystem.AddFile("C:/test/caller.gd", "extends Node\nfunc _ready():\n    helper()");
        fileSystem.AddFile("C:/test/callee.gd", "extends Node\nfunc helper():\n    pass");

        var context = new GDSyntheticProjectContext("C:/test", fileSystem);
        var project = new GDScriptProject(context, options);
        project.AddScript("C:/test/caller.gd", "extends Node\nfunc _ready():\n    helper()");
        project.AddScript("C:/test/callee.gd", "extends Node\nfunc helper():\n    pass");
        project.BuildCallSiteRegistry();

        var callee = project.ScriptFiles.First(s => s.FullPath!.Contains("callee"));
        var oldTree = callee.Class;

        // Act - Modify the helper method
        var newContent = "extends Node\nfunc helper():\n    print(1)";
        var changes = new[] { GDTextChange.Replace(31, 4, "print(1)") };
        var result = callee.Reload(newContent, changes);

        // Update call site registry
        project.OnFileChanged(callee.FullPath!, oldTree, callee.Class!, changes);

        // Assert
        result.Success.Should().BeTrue();
        project.CallSiteRegistry.Should().NotBeNull();
    }

    [TestMethod]
    public void EndToEnd_DependencyGraph_DependentsInvalidated()
    {
        // Arrange
        var config = new GDSemanticsConfig { EnableIncrementalAnalysis = true };
        var project = CreateProjectWithConfig(config,
            ("base.gd", "extends Node\nclass_name BaseClass\nvar base_var = 1"),
            ("child.gd", "extends BaseClass\nvar child_var = 2"));
        project.AnalyzeAll();

        var invalidatedPaths = new ConcurrentBag<string>();

        using var model = new GDProjectSemanticModel(project, subscribeToChanges: true, config);
        model.FileInvalidated += (s, path) => invalidatedPaths.Add(path);

        // Initialize dependency graph
        _ = model.DependencyGraph;

        // Pre-cache models
        foreach (var script in project.ScriptFiles)
            _ = model.GetSemanticModel(script);

        var baseScript = project.ScriptFiles.First(s => s.TypeName == "BaseClass");
        var childScript = project.ScriptFiles.First(s => s.TypeName != "BaseClass");

        // Act - Change base class
        var changeArgs = new GDScriptIncrementalChangeEventArgs(
            baseScript.FullPath!,
            baseScript,
            baseScript.Class,
            baseScript.Class,
            GDIncrementalChangeKind.Modified);

        var processMethod = model.GetType().GetMethod("ProcessIncrementalChange",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        processMethod?.Invoke(model, new object[] { changeArgs });

        // Assert - Both base and child should be invalidated
        invalidatedPaths.Should().Contain(baseScript.FullPath);
        // Child extends BaseClass, so it should be invalidated too
        invalidatedPaths.Should().Contain(childScript.FullPath);
    }

    [TestMethod]
    public void Configuration_FromProjectOptions_Applied()
    {
        // Arrange
        var semanticsConfig = new GDSemanticsConfig
        {
            EnableIncrementalParsing = false,
            EnableIncrementalAnalysis = false,
            FileChangeDebounceMs = 500
        };

        var options = new GDScriptProjectOptions
        {
            SemanticsConfig = semanticsConfig,
            EnableSceneTypesProvider = false,
            FileSystem = new GDInMemoryFileSystem()
        };

        var fileSystem = (GDInMemoryFileSystem)options.FileSystem;
        fileSystem.AddFile("C:/test/test.gd", "extends Node\nvar x = 1");

        var context = new GDSyntheticProjectContext("C:/test", fileSystem);
        var project = new GDScriptProject(context, options);
        project.AddScript("C:/test/test.gd", "extends Node\nvar x = 1");

        // Act
        var script = project.ScriptFiles.First();
        var changes = new[] { GDTextChange.Replace(22, 1, "42") };
        var result = script.Reload("extends Node\nvar x = 42", changes);

        // Assert - Should use full reparse because incremental parsing is disabled
        result.WasIncremental.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static GDScriptProject CreateProjectWithConfig(GDSemanticsConfig config, params (string name, string content)[] files)
    {
        var fileSystem = new GDInMemoryFileSystem();
        foreach (var (name, content) in files)
        {
            fileSystem.AddFile($"C:/test/{name}", content);
        }

        var options = new GDScriptProjectOptions
        {
            SemanticsConfig = config,
            EnableSceneTypesProvider = false,
            FileSystem = fileSystem
        };

        var context = new GDSyntheticProjectContext("C:/test", fileSystem);
        var project = new GDScriptProject(context, options);

        foreach (var (name, content) in files)
        {
            project.AddScript($"C:/test/{name}", content);
        }

        return project;
    }

    #endregion
}
