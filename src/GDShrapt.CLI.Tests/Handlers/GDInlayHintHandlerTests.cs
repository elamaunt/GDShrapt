using System.IO;
using System.Linq;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;
using GDProjectLoader = GDShrapt.Semantics.GDProjectLoader;

namespace GDShrapt.CLI.Tests.Handlers;

[TestClass]
public class GDInlayHintHandlerTests
{
    private string? _tempProjectPath;
    private GDScriptProject? _project;
    private GDInlayHintHandler? _handler;

    [TestCleanup]
    public void Cleanup()
    {
        _project?.Dispose();
        if (_tempProjectPath != null)
            TestProjectHelper.DeleteTempProject(_tempProjectPath);
    }

    private void SetupProject(params (string name, string content)[] scripts)
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(scripts);
        _project = GDProjectLoader.LoadProject(_tempProjectPath);
        _handler = new GDInlayHintHandler(_project);
    }

    [TestMethod]
    public void InlayHint_TypeInferredVar_ColonEquals_NoDoubleColon()
    {
        // var x := 5  →  hint should be " int" (no colon), placed after the existing colon
        SetupProject(("test.gd", @"extends Node

var x := 5
"));

        var filePath = Path.Combine(_tempProjectPath!, "test.gd");
        var hints = _handler!.GetInlayHints(filePath, 1, 10);

        var hint = hints.FirstOrDefault(h => h.Label.Contains("int"));
        hint.Should().NotBeNull("expected an inlay hint for 'x'");
        hint!.Label.Should().NotStartWith(":", "hint should not add a colon when one already exists");
        hint.Label.Trim().Should().Be("int");
    }

    [TestMethod]
    public void InlayHint_VarWithoutColon_HasColon()
    {
        // var x = 5  →  hint should be ": int" (with colon)
        SetupProject(("test.gd", @"extends Node

var x = 5
"));

        var filePath = Path.Combine(_tempProjectPath!, "test.gd");
        var hints = _handler!.GetInlayHints(filePath, 1, 10);

        var hint = hints.FirstOrDefault(h => h.Label.Contains("int"));
        hint.Should().NotBeNull("expected an inlay hint for 'x'");
        hint!.Label.Should().StartWith(":");
    }

    [TestMethod]
    public void InlayHint_ExplicitType_NoHint()
    {
        // var x: int = 5  →  no hint expected (type is already explicit)
        SetupProject(("test.gd", @"extends Node

var x: int = 5
"));

        var filePath = Path.Combine(_tempProjectPath!, "test.gd");
        var hints = _handler!.GetInlayHints(filePath, 1, 10);

        // Should not have a hint for x since type is explicitly declared
        var hint = hints.FirstOrDefault(h => h.Label.Contains("int") && h.Line == 3);
        hint.Should().BeNull("no hint expected when type is explicitly declared");
    }

    [TestMethod]
    public void InlayHint_LocalVar_ColonEquals_NoDoubleColon()
    {
        // Local var x := 5  →  hint should not have colon prefix
        SetupProject(("test.gd", @"extends Node

func _ready():
    var x := 5
"));

        var filePath = Path.Combine(_tempProjectPath!, "test.gd");
        var hints = _handler!.GetInlayHints(filePath, 1, 10);

        var hint = hints.FirstOrDefault(h => h.Label.Contains("int"));
        hint.Should().NotBeNull("expected an inlay hint for local var 'x'");
        hint!.Label.Should().NotStartWith(":", "hint should not add a colon when one already exists");
    }

    [TestMethod]
    public void InlayHint_LocalVar_NoColon_HasColon()
    {
        // Local var x = 5  →  hint should have ": int"
        SetupProject(("test.gd", @"extends Node

func _ready():
    var x = 5
"));

        var filePath = Path.Combine(_tempProjectPath!, "test.gd");
        var hints = _handler!.GetInlayHints(filePath, 1, 10);

        var hint = hints.FirstOrDefault(h => h.Label.Contains("int"));
        hint.Should().NotBeNull("expected an inlay hint for local var 'x'");
        hint!.Label.Should().StartWith(":");
    }
}
