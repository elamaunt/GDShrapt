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
public class GDDocumentHighlightHandlerTests
{
    private static readonly string TestProjectPath = GetTestProjectPath();

    private static string GetTestProjectPath()
    {
        var baseDir = System.IO.Directory.GetCurrentDirectory();
        var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return System.IO.Path.Combine(projectRoot, "testproject", "GDShrapt.TestProject");
    }

    private static (GDScriptProject project, GDDocumentHighlightHandler handler) SetupProjectAndHandler()
    {
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());
        var goToDefHandler = registry.GetService<IGDGoToDefHandler>()!;

        var handler = new GDDocumentHighlightHandler(project, null, goToDefHandler);
        return (project, handler);
    }

    private static GDDocumentHighlightParams CreateParams(string scriptName, int line, int character)
    {
        return new GDDocumentHighlightParams
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
    public async Task Highlight_SignalDeclaration_RangeCoversSignalName()
    {
        // signals_test.gd line 9 (1-based): "signal simple_signal"
        // "simple_signal" starts at col 7 (0-based), length 13
        var (_, handler) = SetupProjectAndHandler();
        var @params = CreateParams("signals_test.gd", 8, 7); // 0-based line 8, col 7

        var result = await handler.HandleAsync(@params, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Length.Should().BeGreaterThan(0);

        // Find the declaration highlight (Write kind)
        var declHighlight = result.FirstOrDefault(h => h.Kind == GDDocumentHighlightKind.Write);
        declHighlight.Should().NotBeNull("declaration highlight should exist");

        // The range should start at "simple_signal" (col 7), NOT at "signal" (col 0)
        declHighlight!.Range.Start.Line.Should().Be(8, "declaration is on line 9 (0-based: 8)");
        declHighlight.Range.Start.Character.Should().Be(7, "should start at 'simple_signal', not at 'signal' keyword");
        declHighlight.Range.End.Character.Should().Be(7 + 13, "should cover the full signal name");
    }

    [TestMethod]
    public async Task Highlight_SignalReference_ReturnsDeclarationAndReferences()
    {
        // signals_test.gd line 34 (1-based): "\tsimple_signal.emit()"
        // "simple_signal" starts at col 1 (0-based, after tab)
        var (_, handler) = SetupProjectAndHandler();
        var @params = CreateParams("signals_test.gd", 33, 1); // 0-based line 33, col 1

        var result = await handler.HandleAsync(@params, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Length.Should().BeGreaterThanOrEqualTo(2, "should include declaration + at least one reference");

        // Declaration should be highlighted as Write
        var declHighlight = result.FirstOrDefault(h =>
            h.Kind == GDDocumentHighlightKind.Write && h.Range.Start.Line == 8);
        declHighlight.Should().NotBeNull("declaration highlight should be included");
    }

    [TestMethod]
    public async Task Highlight_MethodDeclaration_RangeCoversMethodName()
    {
        // signals_test.gd line 32 (1-based): "func emit_simple():"
        // "emit_simple" starts at col 5 (0-based), length 11
        var (_, handler) = SetupProjectAndHandler();
        var @params = CreateParams("signals_test.gd", 31, 5); // 0-based line 31, col 5

        var result = await handler.HandleAsync(@params, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Length.Should().BeGreaterThan(0);

        var declHighlight = result.FirstOrDefault(h => h.Kind == GDDocumentHighlightKind.Write);
        declHighlight.Should().NotBeNull("declaration highlight should exist");

        // Should start at "emit_simple" (col 5), NOT at "func" (col 0)
        declHighlight!.Range.Start.Line.Should().Be(31);
        declHighlight.Range.Start.Character.Should().Be(5, "should start at method name, not at 'func' keyword");
        declHighlight.Range.End.Character.Should().Be(5 + 11, "should cover the full method name");
    }

    [TestMethod]
    public async Task Highlight_VariableDeclaration_RangeCoversVariableName()
    {
        // signals_test.gd line 27 (1-based): "var _health: int = 100"
        // "_health" starts at col 4 (0-based), length 7
        var (_, handler) = SetupProjectAndHandler();
        var @params = CreateParams("signals_test.gd", 26, 4); // 0-based line 26, col 4

        var result = await handler.HandleAsync(@params, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Length.Should().BeGreaterThan(0);

        var declHighlight = result.FirstOrDefault(h => h.Kind == GDDocumentHighlightKind.Write);
        declHighlight.Should().NotBeNull("declaration highlight should exist");

        // Should start at "_health" (col 4), NOT at "var" (col 0)
        declHighlight!.Range.Start.Line.Should().Be(26);
        declHighlight.Range.Start.Character.Should().Be(4, "should start at variable name, not at 'var' keyword");
        declHighlight.Range.End.Character.Should().Be(4 + 7, "should cover the full variable name");
    }

    [TestMethod]
    public async Task Highlight_ExportVariable_RangeCoversVariableName()
    {
        // simple_class.gd line 12 (1-based): "@export var speed: float = 100.0"
        // "speed" starts at col 12 (0-based), length 5
        var (_, handler) = SetupProjectAndHandler();
        var @params = CreateParams("simple_class.gd", 11, 12); // 0-based line 11, col 12

        var result = await handler.HandleAsync(@params, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Length.Should().BeGreaterThan(0);

        var declHighlight = result.FirstOrDefault(h => h.Kind == GDDocumentHighlightKind.Write);
        declHighlight.Should().NotBeNull("declaration highlight should exist");

        // Should start at "speed" (col 12), NOT at "@export" (col 0) or "var" (col 8)
        declHighlight!.Range.Start.Line.Should().Be(11);
        declHighlight.Range.Start.Character.Should().Be(12, "should start at variable name, not at '@export' or 'var'");
        declHighlight.Range.End.Character.Should().Be(12 + 5, "should cover the full variable name");
    }
}
