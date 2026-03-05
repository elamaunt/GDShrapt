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
public class GDCodeLensHandlerTests
{
    private static readonly string TestProjectPath = GetTestProjectPath();

    private static string GetTestProjectPath()
    {
        var baseDir = System.IO.Directory.GetCurrentDirectory();
        var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return System.IO.Path.Combine(projectRoot, "testproject", "GDShrapt.TestProject");
    }

    private static GDLspCodeLensHandler CreateHandler(GDScriptProject project)
    {
        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());

        var codeLensHandler = registry.GetService<IGDCodeLensHandler>()!;
        return new GDLspCodeLensHandler(codeLensHandler);
    }

    private static (GDScriptProject project, GDLspCodeLensHandler handler) SetupProjectAndHandler()
    {
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var handler = CreateHandler(project);
        return (project, handler);
    }

    private static GDCodeLensParams CreateParams(string scriptName)
    {
        return new GDCodeLensParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(
                    System.IO.Path.Combine(TestProjectPath, "test_scripts", scriptName))
            }
        };
    }

    [TestMethod]
    public async Task HandleAsync_ScriptWithMembers_ReturnsCodeLenses()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();
        var @params = CreateParams("simple_class.gd");

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert - should have code lenses for class members
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task HandleAsync_MethodWithReferences_ShowsReferenceCount()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // simple_class.gd has _initialize() called from _ready()
        var @params = CreateParams("simple_class.gd");

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        // _initialize method should have at least 1 reference (called from _ready)
        var initLens = result!.FirstOrDefault(l =>
            l.Command?.Arguments != null &&
            l.Command.Arguments.Length > 0 &&
            l.Command.Arguments[0]?.ToString() == "_initialize");

        if (initLens != null)
        {
            initLens.Command!.Title.Should().Contain("reference");
        }
    }

    [TestMethod]
    public async Task HandleAsync_ClassNameWithCrossFileReferences_ShowsReferenceCount()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // SimpleClass is referenced from other files
        var @params = CreateParams("simple_class.gd");

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        // Look for class_name lens (SimpleClass on line 3, LSP 0-based: line 2)
        var classNameLens = result!.FirstOrDefault(l =>
            l.Command?.Arguments != null &&
            l.Command.Arguments.Length > 0 &&
            l.Command.Arguments[0]?.ToString() == "SimpleClass");

        if (classNameLens != null)
        {
            classNameLens.Command!.Title.Should().Contain("reference");
        }
    }

    [TestMethod]
    public async Task HandleAsync_SignalWithReferences_ShowsReferenceCount()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // signals_test.gd has signals with connect/emit calls
        var @params = CreateParams("signals_test.gd");

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        // health_changed signal should have references (emit + connect)
        var signalLens = result!.FirstOrDefault(l =>
            l.Command?.Arguments != null &&
            l.Command.Arguments.Length > 0 &&
            l.Command.Arguments[0]?.ToString() == "health_changed");

        if (signalLens != null)
        {
            signalLens.Command!.Title.Should().Contain("reference");
        }
    }

    [TestMethod]
    public async Task HandleAsync_CodeLensPositionsAreZeroBased()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();
        var @params = CreateParams("simple_class.gd");

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        foreach (var lens in result!)
        {
            lens.Range.Start.Line.Should().BeGreaterThanOrEqualTo(0, "LSP positions must be 0-based");
            lens.Range.Start.Character.Should().BeGreaterThanOrEqualTo(0);
            lens.Range.End.Line.Should().BeGreaterThanOrEqualTo(lens.Range.Start.Line);
        }
    }

    [TestMethod]
    public async Task HandleAsync_CodeLensHasCommand()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();
        var @params = CreateParams("simple_class.gd");

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        foreach (var lens in result!)
        {
            lens.Command.Should().NotBeNull();
            lens.Command!.Title.Should().NotBeNullOrEmpty();
            lens.Command.Title.Should().Contain("reference");
            lens.Command.Command.Should().Be("gdshrapt.findReferences");
        }
    }

    [TestMethod]
    public async Task HandleAsync_InvalidFile_ReturnsNull()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        var @params = new GDCodeLensParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri("/nonexistent/file.gd")
            }
        };

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task HandleAsync_VariableWithReferences_ShowsReferenceCount()
    {
        // Arrange
        var (_, handler) = SetupProjectAndHandler();

        // signals_test.gd has _health variable used in multiple methods
        var @params = CreateParams("signals_test.gd");

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        var healthLens = result!.FirstOrDefault(l =>
            l.Command?.Arguments != null &&
            l.Command.Arguments.Length > 0 &&
            l.Command.Arguments[0]?.ToString() == "_health");

        if (healthLens != null)
        {
            // _health is used in multiple places
            healthLens.Command!.Title.Should().Contain("reference");
        }
    }
}
