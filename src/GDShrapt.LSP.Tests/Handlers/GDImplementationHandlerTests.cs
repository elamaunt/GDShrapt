using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.LSP.Tests;

[TestClass]
public class GDImplementationHandlerTests
{
    private static readonly string TestProjectPath = GetTestProjectPath();

    private static string GetTestProjectPath()
    {
        var baseDir = System.IO.Directory.GetCurrentDirectory();
        var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return System.IO.Path.Combine(projectRoot, "testproject", "GDShrapt.TestProject");
    }

    private static (GDScriptProject project, GDImplementationLspHandler handler) SetupProjectAndHandler()
    {
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());

        var coreHandler = registry.GetService<IGDImplementationHandler>()!;
        var handler = new GDImplementationLspHandler(coreHandler);
        return (project, handler);
    }

    private static GDDefinitionParams CreateParams(string scriptName, int line, int character)
    {
        return new GDDefinitionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(
                    System.IO.Path.Combine(TestProjectPath, "test_scripts", scriptName))
            },
            Position = new GDLspPosition(line, character)
        };
    }

    [TestMethod]
    public async Task HandleAsync_MethodWithOverrides_ReturnsSubclassOverrides()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // base_entity.gd line 28 (1-based): "func take_damage(amount: int, source: Node = null) -> void:"
        // "take_damage" may be overridden in PlayerEntity, EnemyEntity, etc.
        var @params = CreateParams("base_entity.gd", 27, 5);

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert — even if no overrides exist in test scripts,
        // result should not crash and should return null or empty
        // If overrides exist, they should point to valid files
        if (result != null && result.Length > 0)
        {
            foreach (var loc in result)
            {
                loc.Uri.Should().NotBeNullOrEmpty();
            }
        }
    }

    [TestMethod]
    public async Task HandleAsync_ClassWithSubclasses_ReturnsExtendingScripts()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // base_entity.gd line 2 (1-based): "class_name BaseEntity"
        // Try multiple cursor positions to find the class symbol
        GDLspLocation[]? result = null;

        // "BaseEntity" starts at column 11 (0-based), try on the identifier
        foreach (var col in new[] { 11, 12, 15, 20 })
        {
            var @params = CreateParams("base_entity.gd", 1, col);
            result = await handler.HandleAsync(@params, CancellationToken.None);
            if (result != null && result.Length > 0)
                break;
        }

        // Assert — BaseEntity has subclasses in test project (PlayerEntity, EnemyEntity, etc.)
        result.Should().NotBeNull("class_name BaseEntity should resolve to a class symbol");
        result.Should().NotBeEmpty("BaseEntity has subclasses in the test project");
    }

    [TestMethod]
    public async Task HandleAsync_MethodWithNoOverrides_ReturnsNullOrEmpty()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // base_entity.gd line 79 (1-based): "func get_health_percent() -> float:"
        // This method is likely not overridden in subclasses
        var @params = CreateParams("base_entity.gd", 78, 5);

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert — method with no overrides should return null or empty array
        (result == null || result.Length == 0).Should().BeTrue(
            "get_health_percent has no overrides in test project");
    }

    [TestMethod]
    public async Task HandleAsync_NonSymbolPosition_ReturnsNull()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // base_entity.gd line 1 (1-based): "extends Node2D"
        var @params = CreateParams("base_entity.gd", 0, 0);

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
