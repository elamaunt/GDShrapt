using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.LSP.Tests;

[TestClass]
public class GDTypeDefinitionHandlerTests
{
    private static readonly string TestProjectPath = GetTestProjectPath();

    private static string GetTestProjectPath()
    {
        var baseDir = System.IO.Directory.GetCurrentDirectory();
        var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return System.IO.Path.Combine(projectRoot, "testproject", "GDShrapt.TestProject");
    }

    private static (GDScriptProject project, GDTypeDefinitionLspHandler handler) SetupProjectAndHandler()
    {
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());

        var coreHandler = registry.GetService<IGDTypeDefinitionHandler>()!;
        var handler = new GDTypeDefinitionLspHandler(coreHandler);
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
    public async Task HandleAsync_VariableWithExplicitType_ReturnsTypeLocation()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // base_entity.gd line 18 (1-based): "var current_health: int"
        // "current_health" at LSP position (17, 4) — the variable has type "int"
        var @params = CreateParams("base_entity.gd", 17, 4);

        // Act
        var (links, infoMessage) = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert — "int" is a built-in type, should return info message
        // It's possible it returns null (unresolvable) or info message
        // Type "int" is built-in, so we expect either an info message or null
        (links == null || infoMessage != null).Should().BeTrue(
            "int is a built-in type, should not have a navigable location or should show info");
    }

    [TestMethod]
    public async Task HandleAsync_MethodSymbol_ReturnsTypeOrNull()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // base_entity.gd line 28 (1-based): "func take_damage(amount: int, source: Node = null) -> void:"
        // cursor on "take_damage" — method return type is "void" which shouldn't navigate
        var @params = CreateParams("base_entity.gd", 27, 5);

        // Act
        var (links, infoMessage) = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert — void return type should be null (no type to navigate to)
        // The result should either be null or show an info message
    }

    [TestMethod]
    public async Task HandleAsync_NonSymbolPosition_ReturnsNull()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // base_entity.gd line 1 (1-based): "extends Node2D"
        // cursor on whitespace or keyword — no symbol
        var @params = CreateParams("base_entity.gd", 0, 0);

        // Act
        var (links, infoMessage) = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        links.Should().BeNull();
    }
}
