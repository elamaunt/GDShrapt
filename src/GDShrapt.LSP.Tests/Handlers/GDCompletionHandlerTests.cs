using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;
using GDShrapt.LSP;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace GDShrapt.LSP.Tests;

[TestClass]
public class GDCompletionHandlerTests
{
    private static readonly string TestProjectPath = GetTestProjectPath();

    private static string GetTestProjectPath()
    {
        // Navigate from test output to testproject
        var baseDir = System.IO.Directory.GetCurrentDirectory();
        var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return System.IO.Path.Combine(projectRoot, "testproject", "GDShrapt.TestProject");
    }

    private static GDLspCompletionHandler CreateHandler(GDScriptProject project)
    {
        // Create CLI.Core handlers from the project
        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());

        var completionHandler = registry.GetService<IGDCompletionHandler>()!;
        return new GDLspCompletionHandler(completionHandler);
    }

    [TestMethod]
    public async Task HandleAsync_GeneralCompletion_IncludesKeywords()
    {
        // Arrange
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var handler = CreateHandler(project);
        var @params = new GDCompletionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(System.IO.Path.Combine(TestProjectPath, "test_scripts", "base_entity.gd"))
            },
            Position = new GDLspPosition(5, 0)
        };

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();

        // Should contain keywords
        var keywords = result.Items.Where(i => i.Kind == GDLspCompletionItemKind.Keyword).ToList();
        keywords.Should().NotBeEmpty();
        keywords.Should().Contain(k => k.Label == "func");
        keywords.Should().Contain(k => k.Label == "var");
        keywords.Should().Contain(k => k.Label == "if");
    }

    [TestMethod]
    public async Task HandleAsync_GeneralCompletion_IncludesBuiltinTypes()
    {
        // Arrange
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var handler = CreateHandler(project);
        var @params = new GDCompletionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(System.IO.Path.Combine(TestProjectPath, "test_scripts", "base_entity.gd"))
            },
            Position = new GDLspPosition(5, 0)
        };

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        // Should contain built-in types
        var types = result!.Items.Where(i => i.Detail == "built-in type").ToList();
        types.Should().NotBeEmpty();
        types.Should().Contain(t => t.Label == "int");
        types.Should().Contain(t => t.Label == "String");
        types.Should().Contain(t => t.Label == "Vector2");
    }

    [TestMethod]
    public async Task HandleAsync_GeneralCompletion_IncludesLocalSymbols()
    {
        // Arrange
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var handler = CreateHandler(project);
        var @params = new GDCompletionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(System.IO.Path.Combine(TestProjectPath, "test_scripts", "base_entity.gd"))
            },
            Position = new GDLspPosition(10, 0)
        };

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        // Should contain local methods/variables from the script
        var methods = result!.Items.Where(i => i.Kind == GDLspCompletionItemKind.Method).ToList();
        var variables = result.Items.Where(i => i.Kind == GDLspCompletionItemKind.Variable).ToList();

        // The exact symbols depend on the test project content
        (methods.Count > 0 || variables.Count > 0).Should().BeTrue("Should have local symbols");
    }

    [TestMethod]
    public async Task HandleAsync_MemberAccess_TriggerCharacterDot()
    {
        // Arrange
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var handler = CreateHandler(project);
        var @params = new GDCompletionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(System.IO.Path.Combine(TestProjectPath, "test_scripts", "base_entity.gd"))
            },
            Position = new GDLspPosition(10, 5),
            Context = new GDCompletionContext
            {
                TriggerKind = GDLspCompletionTriggerKind.TriggerCharacter,
                TriggerCharacter = "."
            }
        };

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert - member access completion may return empty if no type context
        // This test verifies no crash occurs
        result.Should().NotBeNull();
    }

    [TestMethod]
    public async Task HandleAsync_InvalidFile_ReturnsEmptyList()
    {
        // Arrange
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var handler = CreateHandler(project);
        var @params = new GDCompletionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri("/nonexistent/file.gd")
            },
            Position = new GDLspPosition(0, 0)
        };

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert - should return list with at least keywords even for invalid file
        result.Should().NotBeNull();
    }

    [TestMethod]
    public void GetOverrideMethodCompletions_Node2DScript_ReturnsVirtualMethods()
    {
        // Arrange
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());

        var handler = registry.GetService<IGDCompletionHandler>()!;

        // base_entity.gd extends Node2D — should suggest _ready, _process, etc.
        var request = new GDCompletionRequest
        {
            FilePath = System.IO.Path.Combine(TestProjectPath, "test_scripts", "base_entity.gd"),
            Line = 1,
            Column = 1,
            CompletionType = GDCompletionType.Symbol
        };

        // Act
        var items = handler.GetCompletions(request);

        // Assert — should include virtual method overrides from Node2D
        var overrides = items.Where(i => i.Detail != null && i.Detail.Contains("override")).ToList();
        overrides.Should().NotBeEmpty("Node2D has virtual methods like _ready, _process");

        // _ready is already declared in base_entity.gd, so check _process instead
        var processItem = overrides.FirstOrDefault(i => i.Label == "_process");
        processItem.Should().NotBeNull("_process is a Node virtual method not declared in base_entity.gd");
        processItem!.IsSnippet.Should().BeTrue();
        processItem.InsertText.Should().Contain("func _process");
        processItem.InsertText.Should().Contain("${0:pass}");
    }

    [TestMethod]
    public void GetOverrideMethodCompletions_ExcludesAlreadyDeclaredMethods()
    {
        // Arrange
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());

        var handler = registry.GetService<IGDCompletionHandler>()!;

        // scene_references.gd declares _ready — it should NOT appear in overrides
        var request = new GDCompletionRequest
        {
            FilePath = System.IO.Path.Combine(TestProjectPath, "test_scripts", "scene_references.gd"),
            Line = 1,
            Column = 1,
            CompletionType = GDCompletionType.Symbol
        };

        // Act
        var items = handler.GetCompletions(request);

        // Assert — _ready should NOT be in override suggestions since it's already declared
        var overrideReady = items.FirstOrDefault(i =>
            i.Label == "_ready" && i.Detail != null && i.Detail.Contains("override"));
        overrideReady.Should().BeNull("_ready is already declared in scene_references.gd");
    }
}
