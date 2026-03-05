using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.LSP.Tests;

[TestClass]
public class GDNodePathCompletionTests
{
    private static readonly string TestProjectPath = GetTestProjectPath();

    private static string GetTestProjectPath()
    {
        var baseDir = System.IO.Directory.GetCurrentDirectory();
        var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return System.IO.Path.Combine(projectRoot, "testproject", "GDShrapt.TestProject");
    }

    private static (GDScriptProject project, IGDCompletionHandler handler) SetupProjectAndHandler()
    {
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions
        {
            EnableSceneTypesProvider = true
        });
        project.LoadScripts();
        project.LoadScenes();
        project.AnalyzeAll();

        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());

        var handler = registry.GetService<IGDCompletionHandler>()!;
        return (project, handler);
    }

    [TestMethod]
    public void NodePathCompletion_TopLevel_ReturnsDirectChildren()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // scene_references.gd is attached to main.tscn root node
        var request = new GDCompletionRequest
        {
            FilePath = System.IO.Path.Combine(TestProjectPath, "test_scripts", "scene_references.gd"),
            Line = 1,
            Column = 1,
            CompletionType = GDCompletionType.NodePath,
            NodePathPrefix = "$"
        };

        // Act
        var items = handler.GetCompletions(request);

        // Assert — main.tscn has Player, EnemyContainer, UI as direct children
        items.Should().NotBeEmpty("scene_references.gd is attached to main.tscn which has children");

        var labels = items.Select(i => i.Label).ToList();
        labels.Should().Contain("Player");
        labels.Should().Contain("EnemyContainer");
        labels.Should().Contain("UI");
    }

    [TestMethod]
    public void NodePathCompletion_NestedPath_ReturnsChildrenOfNode()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // Request children of Player node (main.tscn has Player/CollisionShape2D and Player/Sprite2D)
        var request = new GDCompletionRequest
        {
            FilePath = System.IO.Path.Combine(TestProjectPath, "test_scripts", "scene_references.gd"),
            Line = 1,
            Column = 1,
            CompletionType = GDCompletionType.NodePath,
            NodePathPrefix = "Player"
        };

        // Act
        var items = handler.GetCompletions(request);

        // Assert
        items.Should().NotBeEmpty("Player node has children CollisionShape2D and Sprite2D");

        var labels = items.Select(i => i.Label).ToList();
        labels.Should().Contain("CollisionShape2D");
        labels.Should().Contain("Sprite2D");
    }

    [TestMethod]
    public void NodePathCompletion_ItemsHaveTypeDetail()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        var request = new GDCompletionRequest
        {
            FilePath = System.IO.Path.Combine(TestProjectPath, "test_scripts", "scene_references.gd"),
            Line = 1,
            Column = 1,
            CompletionType = GDCompletionType.NodePath,
            NodePathPrefix = "$"
        };

        // Act
        var items = handler.GetCompletions(request);

        // Assert — each item should show node type as detail
        var player = items.FirstOrDefault(i => i.Label == "Player");
        player.Should().NotBeNull();
        player!.Detail.Should().Be("CharacterBody2D");

        var ui = items.FirstOrDefault(i => i.Label == "UI");
        ui.Should().NotBeNull();
        ui!.Detail.Should().Be("Control");
    }

    [TestMethod]
    public void NodePathCompletion_ScriptNotInScene_ReturnsEmpty()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // ai_controller.gd is not attached to any scene
        var request = new GDCompletionRequest
        {
            FilePath = System.IO.Path.Combine(TestProjectPath, "test_scripts", "ai_controller.gd"),
            Line = 1,
            Column = 1,
            CompletionType = GDCompletionType.NodePath,
            NodePathPrefix = "$"
        };

        // Act
        var items = handler.GetCompletions(request);

        // Assert — no scene uses this script, so no node paths
        items.Should().BeEmpty();
    }
}
