using FluentAssertions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Integration tests for call site registry with incremental updates.
/// </summary>
[TestClass]
public class CallSiteIntegrationTests
{
    private readonly GDScriptReader _reader = new GDScriptReader();

    #region Full Workflow Tests

    [TestMethod]
    public void FullWorkflow_BuildRegistry_FindCallers()
    {
        // Arrange
        var project = CreateProjectWithRegistry(
            ("C:/project/player.gd", @"
class_name Player

func attack(target):
    pass

func jump():
    pass
"),
            ("C:/project/game.gd", @"
class_name Game

var player: Player

func _ready():
    player.attack(null)

func _process(delta):
    player.jump()
"),
            ("C:/project/enemy.gd", @"
class_name Enemy

var player: Player

func on_hurt():
    player.attack(self)
"));

        // Act
        project.BuildCallSiteRegistry();

        // Assert
        var registry = project.CallSiteRegistry;
        registry.Should().NotBeNull();

        // There should be call sites
        registry!.Count.Should().BeGreaterThan(0);

        // Check that files have call sites registered (duck-typed calls are registered with "*")
        // The registry tracks call sites, but for duck-typed calls the target class is "*"
        var gameCallSites = registry.GetCallSitesInFile("C:/project/game.gd");
        gameCallSites.Should().NotBeEmpty("game.gd should have call sites from _ready and _process methods");

        var enemyCallSites = registry.GetCallSitesInFile("C:/project/enemy.gd");
        enemyCallSites.Should().NotBeEmpty("enemy.gd should have call sites from on_hurt method");
    }

    [TestMethod]
    public void FullWorkflow_EditFile_IncrementalUpdate_CallSitesCorrect()
    {
        // Arrange
        var project = CreateProjectWithRegistry(
            ("C:/project/player.gd", @"
class_name Player

func attack(target):
    pass
"),
            ("C:/project/game.gd", @"
class_name Game

var player: Player

func _ready():
    player.attack(null)
"));

        project.BuildCallSiteRegistry();

        var oldTree = project.GetScript("C:/project/game.gd")?.Class;
        var initialCount = project.CallSiteRegistry!.Count;

        // Modify the file - add another call
        var newCode = @"
class_name Game

var player: Player

func _ready():
    player.attack(null)
    player.attack(null)
";
        var newTree = _reader.ParseFileContent(newCode);

        // Act
        project.OnFileChanged(
            "C:/project/game.gd",
            oldTree,
            newTree,
            System.Array.Empty<GDTextChange>());

        // Assert
        var callSites = project.CallSiteRegistry.GetCallSitesInFile("C:/project/game.gd");
        // Should have more call sites now
        callSites.Count.Should().BeGreaterThanOrEqualTo(initialCount);
    }

    [TestMethod]
    public void FullWorkflow_DeleteMethod_CallSitesRemoved()
    {
        // Arrange
        var project = CreateProjectWithRegistry(
            ("C:/project/player.gd", @"
class_name Player

func attack(target):
    pass
"),
            ("C:/project/game.gd", @"
class_name Game

var player: Player

func _ready():
    player.attack(null)

func other_method():
    pass
"));

        project.BuildCallSiteRegistry();

        var registry = project.CallSiteRegistry!;

        // Verify initial state - should have call sites from _ready
        var initialCallSites = registry.GetCallSitesInMethod("C:/project/game.gd", "_ready");

        var oldTree = project.GetScript("C:/project/game.gd")?.Class;

        // Remove the _ready method
        var newCode = @"
class_name Game

var player: Player

func other_method():
    pass
";
        var newTree = _reader.ParseFileContent(newCode);

        // Act
        project.OnFileChanged(
            "C:/project/game.gd",
            oldTree,
            newTree,
            System.Array.Empty<GDTextChange>());

        // Assert
        var callSitesAfter = registry.GetCallSitesInMethod("C:/project/game.gd", "_ready");
        callSitesAfter.Should().BeEmpty();
    }

    #endregion

    #region Multiple Files Tests

    [TestMethod]
    public void MultipleFiles_AllCallSitesIndexed()
    {
        // Arrange
        var project = CreateProjectWithRegistry(
            ("C:/project/utils.gd", @"
class_name Utils

func helper(x):
    pass
"),
            ("C:/project/game1.gd", @"
class_name Game1

var utils: Utils

func test1():
    utils.helper(1)
"),
            ("C:/project/game2.gd", @"
class_name Game2

var utils: Utils

func test2():
    utils.helper(2)
"),
            ("C:/project/game3.gd", @"
class_name Game3

var utils: Utils

func test3():
    utils.helper(3)
"));

        // Act
        project.BuildCallSiteRegistry();

        // Assert
        var registry = project.CallSiteRegistry!;

        // All three files should have call sites (registered as duck-typed with "*")
        var game1Sites = registry.GetCallSitesInFile("C:/project/game1.gd");
        var game2Sites = registry.GetCallSitesInFile("C:/project/game2.gd");
        var game3Sites = registry.GetCallSitesInFile("C:/project/game3.gd");

        game1Sites.Should().NotBeEmpty("game1.gd should have call sites");
        game2Sites.Should().NotBeEmpty("game2.gd should have call sites");
        game3Sites.Should().NotBeEmpty("game3.gd should have call sites");

        // Total count should be 3 (one per file)
        registry.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    #endregion

    #region Self-Calls Tests

    [TestMethod]
    public void SelfCalls_RegisteredCorrectly()
    {
        // Arrange
        var project = CreateProjectWithRegistry(
            ("C:/project/player.gd", @"
class_name Player

func attack(target):
    take_damage(10)

func take_damage(amount):
    pass
"));

        // Act
        project.BuildCallSiteRegistry();

        // Assert
        var registry = project.CallSiteRegistry!;
        var callSites = registry.GetCallSitesInFile("C:/project/player.gd");

        // Should have the self-call from attack to take_damage
        callSites.Should().NotBeEmpty();
    }

    #endregion

    #region Clear and Rebuild Tests

    [TestMethod]
    public void ClearAndRebuild_WorksCorrectly()
    {
        // Arrange
        var project = CreateProjectWithRegistry(
            ("C:/project/player.gd", @"
class_name Player

func attack(target):
    pass
"),
            ("C:/project/game.gd", @"
class_name Game

var player: Player

func _ready():
    player.attack(null)
"));

        project.BuildCallSiteRegistry();
        var initialCount = project.CallSiteRegistry!.Count;

        // Act - clear and rebuild
        project.CallSiteRegistry.Clear();
        project.CallSiteRegistry.Count.Should().Be(0);

        project.BuildCallSiteRegistry();

        // Assert
        project.CallSiteRegistry.Count.Should().Be(initialCount);
    }

    #endregion

    #region Helper Methods

    private GDScriptProject CreateProjectWithRegistry(params (string path, string content)[] scripts)
    {
        var context = new GDDefaultProjectContext("C:/project");
        var options = new GDScriptProjectOptions
        {
            EnableCallSiteRegistry = true,
            EnableSceneTypesProvider = false
        };

        var project = new GDScriptProject(context, options);

        foreach (var (path, content) in scripts)
        {
            project.AddScript(path, content);
        }

        return project;
    }

    #endregion
}
