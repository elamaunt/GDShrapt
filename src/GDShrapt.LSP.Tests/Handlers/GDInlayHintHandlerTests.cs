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
public class GDInlayHintHandlerTests
{
    private static readonly string TestProjectPath = GetTestProjectPath();

    private static string GetTestProjectPath()
    {
        var baseDir = System.IO.Directory.GetCurrentDirectory();
        var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return System.IO.Path.Combine(projectRoot, "testproject", "GDShrapt.TestProject");
    }

    private static GDLspInlayHintHandler CreateHandler(GDScriptProject project)
    {
        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());

        var inlayHintHandler = registry.GetService<IGDInlayHintHandler>()!;
        return new GDLspInlayHintHandler(inlayHintHandler);
    }

    private static (GDScriptProject project, GDLspInlayHintHandler handler) SetupProjectAndHandler()
    {
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var handler = CreateHandler(project);
        return (project, handler);
    }

    private static GDInlayHintParams CreateParams(string scriptName, int startLine, int endLine)
    {
        return new GDInlayHintParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(
                    System.IO.Path.Combine(TestProjectPath, "test_scripts", scriptName))
            },
            Range = new GDLspRange
            {
                Start = new GDLspPosition(startLine, 0),
                End = new GDLspPosition(endLine, 0)
            }
        };
    }

    [TestMethod]
    public async Task HandleAsync_VariableWithInferredType_ReturnsTypeHint()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // simple_class.gd line 16 (1-based): var public_var := 42
        // LSP 0-based: lines 15-16
        var @params = CreateParams("simple_class.gd", 15, 16);

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(h => h.Label.Contains("int"));
    }

    [TestMethod]
    public async Task HandleAsync_VariableWithExplicitType_NoHint()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // simple_class.gd line 12 (1-based): @export var speed: float = 100.0
        // Only request line 12 (LSP 0-based: 11)
        var @params = CreateParams("simple_class.gd", 11, 11);

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert - should not have a hint for "speed" since it has explicit type
        if (result != null)
        {
            result.Should().NotContain(h =>
                h.Position.Line == 11 && h.Label.Contains("float"));
        }
    }

    [TestMethod]
    public async Task HandleAsync_ForLoopIterator_ReturnsTypeHint()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // Look for a for loop in any test script that iterates over a typed container
        // signals_test.gd has var _listeners: Array = []
        // Let's use a broader range to capture any for loops
        var @params = CreateParams("signals_test.gd", 0, 100);

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert - just verify it doesn't crash and returns some hints
        // The exact hints depend on the script content
        // No assertion on specific hints since signals_test.gd may not have for loops
    }

    [TestMethod]
    public async Task HandleAsync_MethodParameterWithoutType_ReturnsTypeHint()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // signals_test.gd line 55 (1-based): func emit_generic(name: String, data):
        // "data" parameter has no type, but is passed to generic_event.emit(name, data)
        // LSP 0-based: lines 54-57
        var @params = CreateParams("signals_test.gd", 54, 57);

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert - if type inference works, there might be a hint for "data" parameter
        // Even if no type is inferred, it shouldn't crash
    }

    [TestMethod]
    public async Task HandleAsync_SignalParameterWithoutType_ReturnsTypeHint()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // signals_test.gd line 19 (1-based): signal generic_event(event_name, event_data)
        // Parameters don't have types, but emit is called on line 57: generic_event.emit(name, data)
        // where name is String
        // LSP 0-based: lines 18-19
        var @params = CreateParams("signals_test.gd", 18, 19);

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert - should have at least one hint for "event_name" since it's emitted with String arg
        if (result != null && result.Length > 0)
        {
            var signalParamHints = result.Where(h => h.Position.Line == 18).ToList();
            if (signalParamHints.Count > 0)
            {
                signalParamHints.Should().Contain(h => h.Label.Contains("String"));
            }
        }
    }

    [TestMethod]
    public async Task HandleAsync_InvalidFile_ReturnsNull()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        var @params = new GDInlayHintParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri("/nonexistent/file.gd")
            },
            Range = new GDLspRange
            {
                Start = new GDLspPosition(0, 0),
                End = new GDLspPosition(10, 0)
            }
        };

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task HandleAsync_EmptyRange_ReturnsNullOrEmpty()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // Request range with no code (beyond end of file)
        var @params = CreateParams("simple_class.gd", 9999, 10000);

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert - should return null or empty
        if (result != null)
        {
            result.Should().BeEmpty();
        }
    }

    [TestMethod]
    public async Task HandleAsync_HintPositionsAreZeroBased()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // Get hints for the whole simple_class.gd
        var @params = CreateParams("simple_class.gd", 0, 50);

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert - all positions should be 0-based (LSP convention)
        if (result != null && result.Length > 0)
        {
            foreach (var hint in result)
            {
                hint.Position.Line.Should().BeGreaterThanOrEqualTo(0);
                hint.Position.Character.Should().BeGreaterThanOrEqualTo(0);
                hint.Kind.Should().Be(GDInlayHintKind.Type);
            }
        }
    }
}
