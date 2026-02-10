using FluentAssertions;
using GDShrapt.Semantics.Incremental.Tracking;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.Incremental;

[TestClass]
public class GDDependencyGraphSceneTests
{
    private GDDependencyGraph _graph = null!;

    [TestInitialize]
    public void Setup()
    {
        _graph = new GDDependencyGraph();
    }

    [TestMethod]
    public void SetSceneDependencies_AddsScriptToSceneDependents()
    {
        // Arrange
        var scenePath = "C:/project/main.tscn";
        var scriptPaths = new[] { "C:/project/player.gd", "C:/project/enemy.gd" };

        // Act
        _graph.SetSceneDependencies(scenePath, scriptPaths);

        // Assert
        var dependents = _graph.GetDependents(scenePath);
        dependents.Should().Contain("C:/project/player.gd");
        dependents.Should().Contain("C:/project/enemy.gd");
    }

    [TestMethod]
    public void SetSceneDependencies_ScriptsHaveSceneAsDependency()
    {
        // Arrange
        var scenePath = "C:/project/main.tscn";
        var scriptPaths = new[] { "C:/project/player.gd" };

        // Act
        _graph.SetSceneDependencies(scenePath, scriptPaths);

        // Assert
        var dependencies = _graph.GetDependencies("C:/project/player.gd");
        dependencies.Should().Contain(scenePath);
    }

    [TestMethod]
    public void SetSceneDependencies_GetTransitiveDependents_ReturnsAffectedScripts()
    {
        // Arrange
        var scenePath = "C:/project/level.tscn";
        var scriptPaths = new[] { "C:/project/npc.gd", "C:/project/door.gd" };

        _graph.SetSceneDependencies(scenePath, scriptPaths);

        // Act
        var transitive = _graph.GetTransitiveDependents(scenePath);

        // Assert
        transitive.Should().HaveCount(2);
        transitive.Should().Contain("C:/project/npc.gd");
        transitive.Should().Contain("C:/project/door.gd");
    }

    [TestMethod]
    public void SetSceneDependencies_UpdateReplacesOldDependencies()
    {
        // Arrange
        var scenePath = "C:/project/scene.tscn";
        _graph.SetSceneDependencies(scenePath, new[] { "C:/project/old_script.gd" });

        // Act — update with new scripts
        _graph.SetSceneDependencies(scenePath, new[] { "C:/project/new_script.gd" });

        // Assert
        var dependents = _graph.GetDependents(scenePath);
        dependents.Should().Contain("C:/project/new_script.gd");
        dependents.Should().NotContain("C:/project/old_script.gd");

        // Old script should no longer depend on the scene
        var oldDeps = _graph.GetDependencies("C:/project/old_script.gd");
        oldDeps.Should().NotContain(scenePath);
    }

    [TestMethod]
    public void SetSceneDependencies_EmptyScriptList_ClearsDependents()
    {
        // Arrange
        var scenePath = "C:/project/scene.tscn";
        _graph.SetSceneDependencies(scenePath, new[] { "C:/project/player.gd" });

        // Act
        _graph.SetSceneDependencies(scenePath, Array.Empty<string>());

        // Assert
        var dependents = _graph.GetDependents(scenePath);
        dependents.Should().BeEmpty();
    }

    [TestMethod]
    public void SetSceneDependencies_MultipleScenes_ShareScript()
    {
        // Arrange
        var scene1 = "C:/project/scene1.tscn";
        var scene2 = "C:/project/scene2.tscn";
        var sharedScript = "C:/project/shared.gd";

        // Act
        _graph.SetSceneDependencies(scene1, new[] { sharedScript });
        _graph.SetSceneDependencies(scene2, new[] { sharedScript });

        // Assert — shared script depends on both scenes
        var deps = _graph.GetDependencies(sharedScript);
        deps.Should().Contain(scene1);
        deps.Should().Contain(scene2);
    }

    [TestMethod]
    public void SetSceneDependencies_CoexistsWithRegularDependencies()
    {
        // Arrange — script already has regular (extends/preload) dependency
        _graph.SetDependencies("C:/project/player.gd", new[] { "C:/project/base.gd" });

        // Act — add scene dependency
        _graph.SetSceneDependencies("C:/project/main.tscn", new[] { "C:/project/player.gd" });

        // Assert — both regular and scene dependencies should exist
        var deps = _graph.GetDependencies("C:/project/player.gd");
        deps.Should().Contain("C:/project/base.gd");
        deps.Should().Contain("C:/project/main.tscn");
    }

    [TestMethod]
    public void RemoveFile_ScenePath_CleansUpSceneDependencies()
    {
        // Arrange
        var scenePath = "C:/project/removed.tscn";
        _graph.SetSceneDependencies(scenePath, new[] { "C:/project/player.gd" });

        // Act
        _graph.RemoveFile(scenePath);

        // Assert
        var dependents = _graph.GetDependents(scenePath);
        dependents.Should().BeEmpty();

        var playerDeps = _graph.GetDependencies("C:/project/player.gd");
        playerDeps.Should().NotContain(scenePath);
    }

    [TestMethod]
    public void Clear_RemovesAllSceneDependencies()
    {
        // Arrange
        _graph.SetSceneDependencies("C:/project/s1.tscn", new[] { "C:/project/a.gd" });
        _graph.SetSceneDependencies("C:/project/s2.tscn", new[] { "C:/project/b.gd" });

        // Act
        _graph.Clear();

        // Assert
        _graph.FileCount.Should().Be(0);
    }
}
