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
public class GDHoverHandlerTests
{
    private static readonly string TestProjectPath = GetTestProjectPath();

    private static string GetTestProjectPath()
    {
        var baseDir = System.IO.Directory.GetCurrentDirectory();
        var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return System.IO.Path.Combine(projectRoot, "testproject", "GDShrapt.TestProject");
    }

    private static GDLspHoverHandler CreateHandler(GDScriptProject project)
    {
        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());

        var hoverHandler = registry.GetService<IGDHoverHandler>()!;
        return new GDLspHoverHandler(hoverHandler);
    }

    private static (GDScriptProject project, GDLspHoverHandler handler) SetupProjectAndHandler()
    {
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var handler = CreateHandler(project);
        return (project, handler);
    }

    private static GDHoverParams CreateParams(string scriptName, int line, int character)
    {
        return new GDHoverParams
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
    public async Task HandleAsync_HoverOnSignalConnect_ShowsMethodSignature()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // Line 62 (1-based): simple_signal.connect(_on_simple)
        // "connect" starts at column 16 (1-based), LSP is 0-based so line=61, char=15
        var @params = CreateParams("signals_test.gd", 61, 16);

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Contents.Should().NotBeNull();
        result.Contents.Value.Should().Contain("func connect(");
    }

    [TestMethod]
    public async Task HandleAsync_HoverOnSignalEmit_ShowsMethodSignature()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // Line 34 (1-based): simple_signal.emit()
        // "emit" starts at column 17 (1-based), LSP 0-based: line=33, char=16
        var @params = CreateParams("signals_test.gd", 33, 16);

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Contents.Should().NotBeNull();
        result.Contents.Value.Should().Contain("func emit(");
    }

    [TestMethod]
    public async Task HandleAsync_HoverOnParameter_ShowsVarKeywordWithParameterAnnotation()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // Line 37 (1-based): func emit_health_change(new_health: int):
        // "new_health" starts at column 25 (1-based), LSP 0-based: line=36, char=24
        var @params = CreateParams("signals_test.gd", 36, 24);

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Contents.Should().NotBeNull();

        var content = result.Contents.Value;
        content.Should().Contain("var ");
        content.Should().Contain("(parameter)");
    }

    [TestMethod]
    public async Task HandleAsync_HoverOnVariable_ShowsVarKeyword()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // Line 27 (1-based): var _health: int = 100
        // "_health" starts at column 5 (1-based), LSP 0-based: line=26, char=4
        var @params = CreateParams("signals_test.gd", 26, 4);

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Contents.Should().NotBeNull();

        var content = result.Contents.Value;
        content.Should().Contain("var ");
        content.Should().NotContain("(parameter)");
    }

    [TestMethod]
    public async Task HandleAsync_HoverOnMethod_ShowsFuncSignature()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // Line 32 (1-based): func emit_simple():
        // "emit_simple" starts at column 6 (1-based), LSP 0-based: line=31, char=5
        var @params = CreateParams("signals_test.gd", 31, 5);

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Contents.Should().NotBeNull();

        result.Contents.Value.Should().Contain("func ");
    }

    [TestMethod]
    public async Task HandleAsync_InvalidFile_ReturnsNull()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        var @params = new GDHoverParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri("/nonexistent/file.gd")
            },
            Position = new GDLspPosition(0, 0)
        };

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
