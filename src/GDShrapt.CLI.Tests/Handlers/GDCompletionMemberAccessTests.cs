using System.IO;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;
using GDProjectLoader = GDShrapt.Semantics.GDProjectLoader;

namespace GDShrapt.CLI.Tests.Handlers;

[TestClass]
public class GDCompletionMemberAccessTests
{
    private string? _tempProjectPath;
    private GDScriptProject? _project;
    private GDCompletionHandler? _handler;

    [TestCleanup]
    public void Cleanup()
    {
        _project?.Dispose();
        if (_tempProjectPath != null)
            TestProjectHelper.DeleteTempProject(_tempProjectPath);
    }

    private GDProjectSemanticModel? _projectModel;

    private string SetupProject(params (string name, string content)[] scripts)
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(scripts);
        _project = GDProjectLoader.LoadProject(_tempProjectPath);
        _projectModel = new GDProjectSemanticModel(_project);
        _handler = new GDCompletionHandler(_project, _projectModel.RuntimeProvider, _projectModel, _project.SceneTypesProvider);
        return _tempProjectPath;
    }

    #region Self access

    [TestMethod]
    public void SelfDot_ShowsAllClassMembers()
    {
        var projectPath = SetupProject(("test.gd", @"extends Node

var health: int = 100
var _private_var: int = 0

signal health_changed()

func _ready():
    self.

func take_damage(amount: int) -> void:
    health -= amount
"));
        var filePath = Path.Combine(projectPath, "test.gd");

        var items = _handler!.GetCompletions(new GDCompletionRequest
        {
            FilePath = filePath,
            Line = 9,
            Column = 10,
            CompletionType = GDCompletionType.MemberAccess,
            MemberAccessExpression = "self"
        });

        items.Should().NotBeEmpty();

        // Should show public members
        items.Should().Contain(i => i.Label == "health");

        // Should show private members (self access)
        items.Should().Contain(i => i.Label == "_private_var");

        // Should show methods
        items.Should().Contain(i => i.Label == "take_damage");

        // Should show signals
        items.Should().Contain(i => i.Label == "health_changed");
    }

    #endregion

    #region Variable member access

    [TestMethod]
    public void TypedVariable_ShowsMembersOfType()
    {
        var projectPath = SetupProject(("test.gd", @"extends Node

var position: Vector2 = Vector2.ZERO

func _ready():
    position.
"));
        var filePath = Path.Combine(projectPath, "test.gd");

        var items = _handler!.GetCompletions(new GDCompletionRequest
        {
            FilePath = filePath,
            Line = 6,
            Column = 14,
            CompletionType = GDCompletionType.MemberAccess,
            MemberAccessExpression = "position"
        });

        items.Should().NotBeEmpty();

        // Vector2 members
        items.Should().Contain(i => i.Label == "x");
        items.Should().Contain(i => i.Label == "y");
        items.Should().Contain(i => i.Label == "length");
        items.Should().Contain(i => i.Label == "normalized");
    }

    #endregion

    #region Static member access

    [TestMethod]
    public void StaticType_ShowsStaticMembers()
    {
        var projectPath = SetupProject(("test.gd", @"extends Node

func _ready():
    Vector2.
"));
        var filePath = Path.Combine(projectPath, "test.gd");

        var items = _handler!.GetCompletions(new GDCompletionRequest
        {
            FilePath = filePath,
            Line = 4,
            Column = 13,
            CompletionType = GDCompletionType.MemberAccess,
            MemberAccessExpression = "Vector2"
        });

        items.Should().NotBeEmpty();

        // Vector2 static constants
        items.Should().Contain(i => i.Label == "ZERO");
        items.Should().Contain(i => i.Label == "ONE");
        items.Should().Contain(i => i.Label == "UP");
    }

    #endregion

    #region Super access

    [TestMethod]
    public void SuperDot_ShowsBaseTypeMembers()
    {
        var projectPath = SetupProject(("test.gd", @"extends Node

func _ready():
    super.
"));
        var filePath = Path.Combine(projectPath, "test.gd");

        var items = _handler!.GetCompletions(new GDCompletionRequest
        {
            FilePath = filePath,
            Line = 4,
            Column = 10,
            CompletionType = GDCompletionType.MemberAccess,
            MemberAccessExpression = "super"
        });

        items.Should().NotBeEmpty();

        // Should contain base class (Node) methods
        items.Should().Contain(i => i.Label == "add_child");
        items.Should().Contain(i => i.Label == "get_parent");
    }

    #endregion

    #region Private member filtering

    [TestMethod]
    public void ExternalObject_HidesPrivateMembers()
    {
        var projectPath = SetupProject(
            ("player.gd", @"class_name Player
extends Node

var health: int = 100
var _internal_state: int = 0

func get_health() -> int:
    return health

func _process_internal():
    pass
"),
            ("game.gd", @"extends Node

var player: Player

func _ready():
    player.
"));
        var filePath = Path.Combine(projectPath, "game.gd");

        var items = _handler!.GetCompletions(new GDCompletionRequest
        {
            FilePath = filePath,
            Line = 6,
            Column = 11,
            CompletionType = GDCompletionType.MemberAccess,
            MemberAccessExpression = "player"
        });

        items.Should().NotBeEmpty();

        // Should show public members
        items.Should().Contain(i => i.Label == "health");
        items.Should().Contain(i => i.Label == "get_health");

        // Should hide private members (starts with _)
        items.Should().NotContain(i => i.Label == "_internal_state");
        items.Should().NotContain(i => i.Label == "_process_internal");
    }

    #endregion

    #region GetMemberCompletions (direct API)

    [TestMethod]
    public void GetMemberCompletions_Node_ReturnsMethods()
    {
        SetupProject(("test.gd", "extends Node\n"));

        var items = _handler!.GetMemberCompletions("Node");

        items.Should().NotBeEmpty();
        items.Should().Contain(i => i.Label == "add_child");
        items.Should().Contain(i => i.Label == "get_parent");
        items.Should().Contain(i => i.Label == "name");
    }

    [TestMethod]
    public void GetMemberCompletions_UnknownType_ReturnsEmpty()
    {
        SetupProject(("test.gd", "extends Node\n"));

        var items = _handler!.GetMemberCompletions("NonExistentType");

        items.Should().BeEmpty();
    }

    #endregion

    #region Type completions

    [TestMethod]
    public void GetTypeCompletions_ContainsCommonTypes()
    {
        SetupProject(("test.gd", "extends Node\n"));

        var items = _handler!.GetTypeCompletions();

        items.Should().NotBeEmpty();
        items.Should().Contain(i => i.Label == "int");
        items.Should().Contain(i => i.Label == "float");
        items.Should().Contain(i => i.Label == "String");
        items.Should().Contain(i => i.Label == "Array");
        items.Should().Contain(i => i.Label == "Dictionary");
        items.Should().Contain(i => i.Label == "Node");
        items.Should().Contain(i => i.Label == "Vector2");
    }

    #endregion

    #region Keyword completions

    [TestMethod]
    public void GetKeywordCompletions_ReturnsKeywords()
    {
        SetupProject(("test.gd", "extends Node\n"));

        var items = _handler!.GetKeywordCompletions();

        items.Should().NotBeEmpty();
        items.Should().Contain(i => i.Label == "if");
        items.Should().Contain(i => i.Label == "for");
        items.Should().Contain(i => i.Label == "while");
        items.Should().Contain(i => i.Label == "return");
        items.Should().Contain(i => i.Label == "var");
    }

    #endregion
}
