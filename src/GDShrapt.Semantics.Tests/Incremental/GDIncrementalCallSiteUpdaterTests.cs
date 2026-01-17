using FluentAssertions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Unit tests for GDIncrementalCallSiteUpdater.
/// </summary>
[TestClass]
public class GDIncrementalCallSiteUpdaterTests
{
    private readonly GDScriptReader _reader = new GDScriptReader();

    #region AddMethod Tests

    [TestMethod]
    public void AddMethod_RegistersNewCallSites()
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
"));

        var oldTree = project.GetScript("C:/project/game.gd")?.Class;

        // Simulate adding a new method that calls Player.attack
        var newCode = @"
class_name Game

var player: Player

func _ready():
    player.attack(null)
";
        var newTree = _reader.ParseFileContent(newCode);

        var updater = new GDIncrementalCallSiteUpdater();

        // Act
        updater.UpdateSemanticModel(
            project,
            "C:/project/game.gd",
            oldTree,
            newTree,
            System.Array.Empty<GDTextChange>());

        // Assert
        var registry = project.CallSiteRegistry;
        registry.Should().NotBeNull();

        // The call site should be registered
        var callSites = registry!.GetCallSitesInFile("C:/project/game.gd");
        callSites.Should().NotBeEmpty();
    }

    [TestMethod]
    public void RemoveMethod_UnregistersCallSites()
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

        // Build initial registry
        project.BuildCallSiteRegistry();

        var oldTree = project.GetScript("C:/project/game.gd")?.Class;

        // Simulate removing the _ready method
        var newCode = @"
class_name Game

var player: Player
";
        var newTree = _reader.ParseFileContent(newCode);

        var updater = new GDIncrementalCallSiteUpdater();

        // Act
        updater.UpdateSemanticModel(
            project,
            "C:/project/game.gd",
            oldTree,
            newTree,
            System.Array.Empty<GDTextChange>());

        // Assert
        var registry = project.CallSiteRegistry;
        var callSites = registry!.GetCallSitesInFile("C:/project/game.gd");
        // After removing the method, no call sites should remain
        callSites.Should().BeEmpty();
    }

    [TestMethod]
    public void ModifyMethod_UpdatesCallSites()
    {
        // Arrange
        var project = CreateProjectWithRegistry(
            ("C:/project/player.gd", @"
class_name Player

func attack(target):
    pass

func defend():
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

        // Modify the method to call defend instead of attack
        var newCode = @"
class_name Game

var player: Player

func _ready():
    player.defend()
";
        var newTree = _reader.ParseFileContent(newCode);

        var updater = new GDIncrementalCallSiteUpdater();

        // Act
        updater.UpdateSemanticModel(
            project,
            "C:/project/game.gd",
            oldTree,
            newTree,
            System.Array.Empty<GDTextChange>());

        // Assert
        var registry = project.CallSiteRegistry;
        var callSites = registry!.GetCallSitesInFile("C:/project/game.gd");
        // Should have call sites now (the new defend call)
        callSites.Should().NotBeEmpty();
    }

    #endregion

    #region InvalidateFile Tests

    [TestMethod]
    public void InvalidateFile_ClearsAllCallSitesFromFile()
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

func _process(delta):
    player.attack(null)
"));

        project.BuildCallSiteRegistry();

        var updater = new GDIncrementalCallSiteUpdater();

        // Act
        updater.InvalidateFile(project, "C:/project/game.gd");

        // Assert
        var registry = project.CallSiteRegistry;
        registry!.GetCallSitesInFile("C:/project/game.gd").Should().BeEmpty();
    }

    #endregion

    #region GetAffectedFiles Tests

    [TestMethod]
    public void GetAffectedFiles_ReturnsFilesReferencingChangedClass()
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
"),
            ("C:/project/other.gd", @"
class_name Other

func test():
    pass
"));

        var updater = new GDIncrementalCallSiteUpdater();

        // Act
        var affectedFiles = updater.GetAffectedFiles(project, "C:/project/player.gd");

        // Assert
        affectedFiles.Should().Contain("C:/project/game.gd");
        affectedFiles.Should().NotContain("C:/project/other.gd");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void UpdateSemanticModel_NullProject_ThrowsArgumentNullException()
    {
        // Arrange
        var updater = new GDIncrementalCallSiteUpdater();
        var tree = _reader.ParseFileContent("var x = 1");

        // Act & Assert
        var act = () => updater.UpdateSemanticModel(null!, "test.gd", null, tree, System.Array.Empty<GDTextChange>());
        act.Should().Throw<System.ArgumentNullException>();
    }

    [TestMethod]
    public void UpdateSemanticModel_NullFilePath_ThrowsArgumentNullException()
    {
        // Arrange
        var project = CreateProjectWithRegistry();
        var updater = new GDIncrementalCallSiteUpdater();
        var tree = _reader.ParseFileContent("var x = 1");

        // Act & Assert
        var act = () => updater.UpdateSemanticModel(project, null!, null, tree, System.Array.Empty<GDTextChange>());
        act.Should().Throw<System.ArgumentNullException>();
    }

    [TestMethod]
    public void UpdateSemanticModel_NoRegistry_DoesNothing()
    {
        // Arrange - project without registry
        var project = new GDScriptProject("var x = 1");
        var updater = new GDIncrementalCallSiteUpdater();
        var tree = _reader.ParseFileContent("var x = 1");

        // Act & Assert - should not throw
        var act = () => updater.UpdateSemanticModel(project, "test.gd", null, tree, System.Array.Empty<GDTextChange>());
        act.Should().NotThrow();
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
