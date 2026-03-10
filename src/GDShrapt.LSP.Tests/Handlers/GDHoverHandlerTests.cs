using System;
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
    public async Task HandleAsync_HoverOnInheritedBuiltInProperty_ShowsCorrectType()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // simple_class.gd line 28 (1-based): position.x += speed * delta
        // "x" at column 11 (1-based), LSP 0-based: line=27, char=10
        var @params = CreateParams("simple_class.gd", 27, 10);

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert - should show float type from Vector2.x, not SimpleClass
        result.Should().NotBeNull();
        result!.Contents.Should().NotBeNull();

        var content = result.Contents.Value;
        content.Should().Contain("float");
        content.Should().NotContain("SimpleClass");
    }

    [TestMethod]
    public async Task HandleAsync_HoverOnEnumValue_ShowsEnumValueInfo()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // ai_controller.gd line 18 (1-based): var current_state: AIState = AIState.IDLE
        // "IDLE" at column 39 (1-based), LSP 0-based: line=17, char=38
        var @params = CreateParams("ai_controller.gd", 17, 38);

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert - should show enum value hover, not Unknown
        result.Should().NotBeNull();
        result!.Contents.Should().NotBeNull();

        var content = result.Contents.Value;
        content.Should().Contain("IDLE");
        content.Should().NotContain("Unknown");
    }

    [TestMethod]
    public async Task HandleAsync_HoverOnBuiltInProperty_ShowsSnakeCaseName()
    {
        var (_, handler) = SetupProjectAndHandler();

        // navigation_test.gd line 16 (1-based): texture = null
        // "texture" at column 2 (1-based), LSP 0-based: line=15, char=1
        var @params = CreateParams("navigation_test.gd", 15, 1);

        var result = await handler.HandleAsync(@params, CancellationToken.None);

        result.Should().NotBeNull();
        var content = result!.Contents.Value;
        content.Should().Contain("texture");
        content.Should().NotContain("var Texture");
        content.Should().Contain("Texture2D");
    }

    [TestMethod]
    public async Task HandleAsync_HoverOnBuiltInFunction_ShowsFunctionSignature()
    {
        var (_, handler) = SetupProjectAndHandler();

        // navigation_test.gd line 14 (1-based): var clamped = clampf(1.5, 0.0, 1.0)
        // "clampf" at column 16 (1-based), LSP 0-based: line=13, char=15
        var @params = CreateParams("navigation_test.gd", 13, 15);

        var result = await handler.HandleAsync(@params, CancellationToken.None);

        result.Should().NotBeNull();
        var content = result!.Contents.Value;
        content.Should().Contain("clampf");
    }

    [TestMethod]
    public async Task HandleAsync_HoverOnPrint_ShowsFunctionSignature()
    {
        var (_, handler) = SetupProjectAndHandler();

        // navigation_test.gd line 17 (1-based): print(clamped)
        // "print" at column 2 (1-based), LSP 0-based: line=16, char=1
        var @params = CreateParams("navigation_test.gd", 16, 1);

        var result = await handler.HandleAsync(@params, CancellationToken.None);

        result.Should().NotBeNull();
        var content = result!.Contents.Value;
        content.Should().Contain("print");
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

    [TestMethod]
    public async Task HandleAsync_HoverOnGetNodeExpression_ReturnsNodeType()
    {
        var (_, handler) = SetupProjectAndHandler();

        // scene_nodes.gd line 13: @onready var animation_player: AnimationPlayer = $AnimationPlayer
        // "$AnimationPlayer" starts at column 55 (1-based), LSP 0-based: line=12, char=54
        var @params = CreateParams("scene_nodes.gd", 12, 54);

        var result = await handler.HandleAsync(@params, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Contents.Should().NotBeNull();
        result.Contents.Value.Should().Contain("AnimationPlayer");
    }

    [TestMethod]
    public async Task HandleAsync_HoverOnGetNodeExpression_DoesNotHang()
    {
        var (_, handler) = SetupProjectAndHandler();

        // scene_nodes.gd line 13: $AnimationPlayer
        var @params = CreateParams("scene_nodes.gd", 12, 54);

        Func<Task> act = async () =>
        {
            await handler.HandleAsync(@params, CancellationToken.None);
        };

        await act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task HandleAsync_CancelledToken_ThrowsOrReturnsQuickly()
    {
        var (_, handler) = SetupProjectAndHandler();

        var @params = CreateParams("scene_nodes.gd", 12, 54);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = async () =>
        {
            await handler.HandleAsync(@params, cts.Token);
        };

        await act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task HandleAsync_HoverOnInferredGetNode_DoesNotHang()
    {
        var (_, handler) = SetupProjectAndHandler();

        // scene_nodes.gd line 17: @onready var ui_container = $UI
        // "$UI" at column 32 (1-based), LSP 0-based: line=16, char=31
        var @params = CreateParams("scene_nodes.gd", 16, 31);

        Func<Task> act = async () =>
        {
            await handler.HandleAsync(@params, CancellationToken.None);
        };

        await act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void SignalNames_AreNormalized_ToSnakeCase()
    {
        var assemblyData = GDShrapt.TypesMap.GDTypeHelper.ExtractTypeDatasFromManifest();

        // AnimationMixer (parent of AnimationPlayer) defines animation_finished
        var typesToCheck = new[] { "AnimationMixer", "AnimationPlayer" };
        var foundSignal = false;

        foreach (var typeName in typesToCheck)
        {
            if (!assemblyData.TypeDatas.TryGetValue(typeName, out var types))
                continue;

            foreach (var typeData in types.Values)
            {
                if (typeData.SignalDatas == null)
                    continue;

                foreach (var kvp in typeData.SignalDatas)
                {
                    kvp.Value.GDScriptName.Should().Be(kvp.Key,
                        $"Signal '{kvp.Key}' in {typeName} should have GDScriptName matching the dictionary key (snake_case)");

                    if (kvp.Key == "animation_finished")
                    {
                        foundSignal = true;
                        kvp.Value.GDScriptName.Should().Be("animation_finished",
                            "animation_finished signal should use snake_case name, not PascalCase");
                    }
                }
            }
        }

        foundSignal.Should().BeTrue("should find animation_finished signal in AnimationMixer or AnimationPlayer");
    }
}
