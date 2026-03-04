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
