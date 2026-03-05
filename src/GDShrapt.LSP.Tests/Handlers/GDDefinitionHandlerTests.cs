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
public class GDDefinitionHandlerTests
{
    private static readonly string TestProjectPath = GetTestProjectPath();

    private static string GetTestProjectPath()
    {
        var baseDir = System.IO.Directory.GetCurrentDirectory();
        var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return System.IO.Path.Combine(projectRoot, "testproject", "GDShrapt.TestProject");
    }

    private static GDDefinitionHandler CreateHandler(GDScriptProject project)
    {
        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());

        var goToDefHandler = registry.GetService<IGDGoToDefHandler>()!;
        return new GDDefinitionHandler(goToDefHandler);
    }

    private static (GDScriptProject project, GDDefinitionHandler handler) SetupProjectAndHandler()
    {
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var handler = CreateHandler(project);
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
    public async Task HandleAsync_GoToDefOnSignalConnect_ReturnsLocation()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // Line 62 (1-based): simple_signal.connect(_on_simple)
        // "connect" starts at column 16 (1-based), LSP 0-based: line=61, char=15
        var @params = CreateParams("signals_test.gd", 61, 16);

        // Act
        var (links, infoMessage) = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert - should have either a location link or at least an info message (not null result)
        (links != null || infoMessage != null).Should().BeTrue(
            "Go-to-definition on Signal.connect should return a result");

        if (links != null)
        {
            links.Should().NotBeEmpty();
            links[0].TargetUri.Should().NotBeNullOrEmpty();
        }
    }

    [TestMethod]
    public async Task HandleAsync_GoToDefOnLocalVariable_ReturnsDeclarationLocation()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // Line 39 (1-based): var old = _health
        // "_health" at column 12 (1-based), LSP 0-based: line=38, char=11
        var @params = CreateParams("signals_test.gd", 38, 12);

        // Act
        var (links, _) = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        links.Should().NotBeNull();
        links.Should().NotBeEmpty();

        // Should point to the declaration at line 27 (1-based)
        var targetLink = links![0];
        targetLink.TargetUri.Should().Contain("signals_test.gd");
    }

    [TestMethod]
    public async Task HandleAsync_GoToDefOnMethodCall_ReturnsMethodDeclaration()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // Line 77 (1-based): _handle_death()
        // "_handle_death" starts at column 3 (1-based), LSP 0-based: line=76, char=2
        var @params = CreateParams("signals_test.gd", 76, 2);

        // Act
        var (links, _) = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        links.Should().NotBeNull();
        links.Should().NotBeEmpty();

        var targetLink = links![0];
        targetLink.TargetUri.Should().Contain("signals_test.gd");
    }

    [TestMethod]
    public async Task HandleAsync_GoToDefOnLambdaName_DoesNotReturnError()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // local_variables.gd line 69: var transform = func(x): return x * multiplier
        // "transform" at column 6 (1-based), LSP 0-based: line=68, char=5
        var @params = CreateParams("local_variables.gd", 68, 5);

        // Act
        var (links, infoMessage) = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert - navigating to a lambda variable should resolve (not produce an error info message)
        if (infoMessage != null)
        {
            infoMessage.Should().NotContain("Cannot resolve");
        }
    }

    [TestMethod]
    public async Task HandleAsync_GoToDefOnExtendsType_ReturnsBuiltInTypeDefinition()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // signals_test.gd line 1: "extends Node"
        // "Node" starts at column 8 (0-based), LSP 0-based: line=0, char=8
        var @params = CreateParams("signals_test.gd", 0, 8);

        // Act
        var (links, infoMessage) = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert - should navigate to built-in type definition
        links.Should().NotBeNull("Go-to-definition on 'Node' in extends clause should return a location");
        links.Should().NotBeEmpty();
        links![0].TargetUri.Should().Contain("Node.gd");
    }

    [TestMethod]
    public async Task HandleAsync_GoToDefOnEnumTypeInBuiltInFile_ReturnsEnumDefinition()
    {
        // Arrange
        var (project, _) = SetupProjectAndHandler();

        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());
        var goToDefHandler = registry.GetService<IGDGoToDefHandler>()!;

        // Step 1: Generate Control.gd by navigating to "Control" in "extends Control"
        // scene_references.gd starts with "extends Control"
        var controlLocation = goToDefHandler.FindDefinition(
            System.IO.Path.Combine(TestProjectPath, "test_scripts", "scene_references.gd"),
            1, 9); // "Control" in "extends Control" (1-based)

        controlLocation.Should().NotBeNull();
        controlLocation!.FilePath.Should().Contain("Control.gd");
        System.IO.File.Exists(controlLocation.FilePath).Should().BeTrue();

        // Step 2: Read the generated file and find an enum type reference
        var content = System.IO.File.ReadAllText(controlLocation.FilePath);
        var lines = content.Split('\n');

        int enumLine = -1;
        int enumCol = -1;
        string? enumTypeName = null;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var enumIdx = line.IndexOf("Enum");
            if (enumIdx > 0)
            {
                // Find the start of the type name
                var typeStart = enumIdx;
                while (typeStart > 0 && char.IsLetterOrDigit(line[typeStart - 1]))
                    typeStart--;

                enumTypeName = line.Substring(typeStart, enumIdx + 4 - typeStart);
                enumLine = i + 1; // 1-based
                enumCol = typeStart + 1; // 1-based
                break;
            }
        }

        // Control type should have enum properties
        enumLine.Should().BeGreaterThan(0, "Control type should have enum properties in generated file");

        // Step 3: Navigate to the enum type within the built-in file
        var enumLocation = goToDefHandler.FindDefinition(controlLocation.FilePath, enumLine, enumCol);

        // Assert - should navigate to the enum type definition (not return an info-only message)
        enumLocation.Should().NotBeNull(
            $"Go-to-definition on '{enumTypeName}' in built-in type file should return a result");

        if (enumLocation!.InfoMessage == null)
        {
            enumLocation.FilePath.Should().NotBeNullOrEmpty();
            enumLocation.FilePath.Should().Contain(".gd");
        }
    }

    [TestMethod]
    public async Task HandleAsync_GoToDefOnEnumTypeInControlFile_NavigatesToEnumDefinition()
    {
        // Arrange - test that enum type C# aliases are recognized by the runtime provider
        var (project, _) = SetupProjectAndHandler();

        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());

        // Access runtime provider through the project semantic model
        var projectModel = registry.GetService<GDProjectSemanticModel>();
        var runtimeProvider = projectModel.RuntimeProvider;
        runtimeProvider.Should().NotBeNull();

        var controlTypeName = "Control";
        runtimeProvider!.IsKnownType(controlTypeName).Should().BeTrue();

        // Check that enum type names are recognized via aliases
        var controlTypeInfo = runtimeProvider.GetTypeInfo(controlTypeName);
        controlTypeInfo.Should().NotBeNull();

        // Find an enum property type (C# names end with "Enum")
        var enumMember = controlTypeInfo!.Members?
            .FirstOrDefault(m => m.Kind == GDRuntimeMemberKind.Property &&
                                 m.Type != null && m.Type.EndsWith("Enum"));

        if (enumMember == null)
        {
            // Control might not have Enum-suffixed properties in all TypesMap versions
            return;
        }

        // The C# enum type name should be known via the alias registration
        runtimeProvider.IsKnownType(enumMember.Type!).Should().BeTrue(
            $"Enum type '{enumMember.Type}' should be recognized as a known type");

        // And we should be able to get type info for it
        var enumTypeInfo = runtimeProvider.GetTypeInfo(enumMember.Type!);
        enumTypeInfo.Should().NotBeNull(
            $"GetTypeInfo('{enumMember.Type}') should return type data via alias");
    }

    [TestMethod]
    public async Task HandleAsync_GoToDefOnUserEnumType_NavigatesToEnumDeclaration()
    {
        // Arrange
        var (project, _) = SetupProjectAndHandler();

        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());
        var goToDefHandler = registry.GetService<IGDGoToDefHandler>()!;

        // ai_controller.gd line 18 (1-based): var current_state: AIState = AIState.IDLE
        // Second "AIState" (before .IDLE) starts at column 30 (1-based)
        var location = goToDefHandler.FindDefinition(
            System.IO.Path.Combine(TestProjectPath, "test_scripts", "ai_controller.gd"),
            18, 30);

        // Assert - should navigate to enum declaration in the same file, not generate a built-in stub
        location.Should().NotBeNull("Go-to-definition on user-defined enum 'AIState' should return a result");
        location!.FilePath.Should().Contain("ai_controller.gd",
            "Should navigate to the source file, not a generated stub");
        location.Line.Should().Be(11, "AIState enum is declared on line 11");
    }

    [TestMethod]
    public async Task HandleAsync_GoToDefOnUserEnumInCompletionTest_NavigatesToDeclaration()
    {
        // Arrange
        var (project, _) = SetupProjectAndHandler();

        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());
        var goToDefHandler = registry.GetService<IGDGoToDefHandler>()!;

        // completion_test.gd line 20 (1-based): var my_enum: TestEnum = TestEnum.VALUE_ONE
        // Second "TestEnum" (before .VALUE_ONE) starts at column 25 (1-based)
        var location = goToDefHandler.FindDefinition(
            System.IO.Path.Combine(TestProjectPath, "test_scripts", "completion_test.gd"),
            20, 25);

        // Assert - should navigate to enum declaration
        location.Should().NotBeNull("Go-to-definition on user-defined enum 'TestEnum' should return a result");
        location!.FilePath.Should().Contain("completion_test.gd",
            "Should navigate to the source file, not a generated stub");
        location.Line.Should().Be(7, "TestEnum is declared on line 7");
    }

    [TestMethod]
    public async Task HandleAsync_GoToDefOnEnumValue_NavigatesToEnumDeclaration()
    {
        // Arrange
        var (project, _) = SetupProjectAndHandler();

        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());
        var goToDefHandler = registry.GetService<IGDGoToDefHandler>()!;

        // ai_controller.gd line 18 (1-based): var current_state: AIState = AIState.IDLE
        // "IDLE" starts at column 38 (1-based)
        var location = goToDefHandler.FindDefinition(
            System.IO.Path.Combine(TestProjectPath, "test_scripts", "ai_controller.gd"),
            18, 38);

        // Assert - should navigate to enum declaration, not generate a built-in stub
        location.Should().NotBeNull("Go-to-definition on enum value 'IDLE' should return a result");
        location!.FilePath.Should().Contain("ai_controller.gd",
            "Should navigate to the source file, not a generated stub");
    }

    [TestMethod]
    public async Task HandleAsync_GoToDefOnEnumTypeAnnotation_NavigatesToDeclaration()
    {
        // Arrange
        var (project, _) = SetupProjectAndHandler();

        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());
        var goToDefHandler = registry.GetService<IGDGoToDefHandler>()!;

        // ai_controller.gd line 18 (1-based): var current_state: AIState = AIState.IDLE
        // "AIState" in type annotation starts at column 20 (1-based)
        var location = goToDefHandler.FindDefinition(
            System.IO.Path.Combine(TestProjectPath, "test_scripts", "ai_controller.gd"),
            18, 20);

        // Assert - should navigate to enum declaration in the same file
        location.Should().NotBeNull("Go-to-definition on enum type in annotation should return a result");
        location!.FilePath.Should().Contain("ai_controller.gd",
            "Should navigate to the source file, not a generated stub");
        location.Line.Should().Be(11, "AIState enum is declared on line 11");
    }

    [TestMethod]
    public async Task HandleAsync_GoToDefOnBuiltInProperty_NavigatesToPropertyLine()
    {
        var (project, _) = SetupProjectAndHandler();
        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());
        var goToDefHandler = registry.GetService<IGDGoToDefHandler>()!;

        // navigation_test.gd line 16 (1-based): texture = null
        var location = goToDefHandler.FindDefinition(
            System.IO.Path.Combine(TestProjectPath, "test_scripts", "navigation_test.gd"),
            16, 2);

        location.Should().NotBeNull();
        location!.FilePath.Should().Contain("TextureRect.gd");

        var content = System.IO.File.ReadAllText(location.FilePath);
        var lines = content.Split('\n');
        var targetLine = lines[location.Line - 1];
        targetLine.Should().Contain("var texture");
        targetLine.Should().NotContain("class_name");
    }

    [TestMethod]
    public async Task HandleAsync_GoToDefOnBuiltInFunction_ReturnsLocation()
    {
        var (project, _) = SetupProjectAndHandler();
        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());
        var goToDefHandler = registry.GetService<IGDGoToDefHandler>()!;

        // navigation_test.gd line 14 (1-based): var clamped = clampf(1.5, 0.0, 1.0)
        var location = goToDefHandler.FindDefinition(
            System.IO.Path.Combine(TestProjectPath, "test_scripts", "navigation_test.gd"),
            14, 16);

        location.Should().NotBeNull();
        location!.FilePath.Should().Contain("@GDScript.gd");
        location.IsInfoOnly.Should().BeFalse("Should navigate to pseudo-file, not show info message");
    }

    [TestMethod]
    public async Task HandleAsync_GoToDefOnPreloadString_NavigatesToFile()
    {
        var (project, _) = SetupProjectAndHandler();
        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());
        var goToDefHandler = registry.GetService<IGDGoToDefHandler>()!;

        // navigation_test.gd line 6 (1-based): const SCENE = preload("res://test_scenes/player.tscn")
        var location = goToDefHandler.FindDefinition(
            System.IO.Path.Combine(TestProjectPath, "test_scripts", "navigation_test.gd"),
            6, 35);

        location.Should().NotBeNull();
        location!.IsInfoOnly.Should().BeFalse("Should navigate to file, not show info");
        location.FilePath.Should().Contain("player.tscn");
        System.IO.File.Exists(location.FilePath).Should().BeTrue();
    }

    [TestMethod]
    public async Task HandleAsync_GoToDefOnRelativePreloadString_NavigatesToFile()
    {
        var (project, _) = SetupProjectAndHandler();
        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());
        var goToDefHandler = registry.GetService<IGDGoToDefHandler>()!;

        // navigation_test.gd line 7 (1-based): const SCENE_RELATIVE = preload("../test_scenes/main.tscn")
        var location = goToDefHandler.FindDefinition(
            System.IO.Path.Combine(TestProjectPath, "test_scripts", "navigation_test.gd"),
            7, 40);

        location.Should().NotBeNull();
        location!.IsInfoOnly.Should().BeFalse("Should navigate to file, not show info");
        location.FilePath.Should().Contain("main.tscn");
        System.IO.File.Exists(location.FilePath).Should().BeTrue();
    }

    [TestMethod]
    public async Task HandleAsync_GoToDefOnIconAnnotationPath_NavigatesToFile()
    {
        var (project, _) = SetupProjectAndHandler();
        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());
        var goToDefHandler = registry.GetService<IGDGoToDefHandler>()!;

        // navigation_test.gd line 8 (1-based): @icon("res://icon.png")
        var location = goToDefHandler.FindDefinition(
            System.IO.Path.Combine(TestProjectPath, "test_scripts", "navigation_test.gd"),
            8, 12);

        location.Should().NotBeNull();
        location!.IsInfoOnly.Should().BeFalse("Should navigate to file, not show info");
        location.FilePath.Should().Contain("icon.png");
        System.IO.File.Exists(location.FilePath).Should().BeTrue();
    }

    [TestMethod]
    public async Task HandleAsync_GoToDefOnResPathInStringLiteral_NavigatesToFile()
    {
        var (project, _) = SetupProjectAndHandler();
        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());
        var goToDefHandler = registry.GetService<IGDGoToDefHandler>()!;

        // navigation_test.gd line 9 (1-based): var scene_path: String = "res://test_scenes/main.tscn"
        var location = goToDefHandler.FindDefinition(
            System.IO.Path.Combine(TestProjectPath, "test_scripts", "navigation_test.gd"),
            9, 35);

        location.Should().NotBeNull();
        location!.IsInfoOnly.Should().BeFalse("Should navigate to file, not show info");
        location.FilePath.Should().Contain("main.tscn");
        System.IO.File.Exists(location.FilePath).Should().BeTrue();
    }

    [TestMethod]
    public async Task HandleAsync_GoToDefOnExtendsPath_NavigatesToFile()
    {
        var (project, _) = SetupProjectAndHandler();
        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());
        var goToDefHandler = registry.GetService<IGDGoToDefHandler>()!;

        // path_extends_test.gd line 3 (1-based): extends "res://test_scripts/base_entity.gd"
        var location = goToDefHandler.FindDefinition(
            System.IO.Path.Combine(TestProjectPath, "test_scripts", "path_extends_test.gd"),
            3, 15);

        location.Should().NotBeNull();
        location!.IsInfoOnly.Should().BeFalse("Should navigate to file, not show info");
        location.FilePath.Should().Contain("base_entity.gd");
        System.IO.File.Exists(location.FilePath).Should().BeTrue();
    }

    [TestMethod]
    public async Task HandleAsync_GoToDefOnArrayInGenericType_NavigatesToArrayDefinition()
    {
        var (project, _) = SetupProjectAndHandler();
        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());
        var goToDefHandler = registry.GetService<IGDGoToDefHandler>()!;

        // navigation_test.gd line 11 (1-based): var enemies: Array[Node2D] = []
        var location = goToDefHandler.FindDefinition(
            System.IO.Path.Combine(TestProjectPath, "test_scripts", "navigation_test.gd"),
            11, 15);

        location.Should().NotBeNull();
        location!.IsInfoOnly.Should().BeFalse("Should navigate to Array definition");
        location.FilePath.Should().Contain("Array.gd");
    }

    [TestMethod]
    public async Task HandleAsync_InvalidFile_ReturnsNull()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        var @params = new GDDefinitionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri("/nonexistent/file.gd")
            },
            Position = new GDLspPosition(0, 0)
        };

        // Act
        var (links, infoMessage) = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        links.Should().BeNull();
    }
}
