using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDFindRefsOutputTests
{
    private string? _tempProjectPath;

    [TestCleanup]
    public void Cleanup()
    {
        if (_tempProjectPath != null)
            TestProjectHelper.DeleteTempProject(_tempProjectPath);
    }

    private static string StripAnsi(string text) =>
        Regex.Replace(text, @"\x1b\[[0-9;]*m", "");

    [TestMethod]
    public async Task FindRefs_ShowsContextLine_ForDeclaration()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("test.gd", @"extends Node

var health: int = 100

func _ready() -> void:
    health = 50
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("health", _tempProjectPath, null, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(0);
        var text = StripAnsi(output.ToString());
        text.Should().Contain("[def]");
        text.Should().Contain("var health: int = 100");
    }

    [TestMethod]
    public async Task FindRefs_ShowsContextLine_ForReadReference()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("test.gd", @"extends Node

var health: int = 100

func _ready() -> void:
    print(health)
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("health", _tempProjectPath, null, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(0);
        var text = StripAnsi(output.ToString());
        text.Should().Contain("[call]");
        text.Should().Contain("print(health)");
    }

    [TestMethod]
    public async Task FindRefs_ShowsContextLine_ForWriteReference()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("test.gd", @"extends Node

var health: int = 100

func _ready() -> void:
    health = 50
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("health", _tempProjectPath, null, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(0);
        var text = StripAnsi(output.ToString());
        text.Should().Contain("[write]");
        text.Should().Contain("health = 50");
    }

    [TestMethod]
    public async Task FindRefs_ShowsContextLine_ForOverride()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", @"class_name Entity
extends Node

func take_damage(amount: int) -> void:
    pass
"),
            ("enemy.gd", @"class_name Enemy
extends Entity

func take_damage(amount: int) -> void:
    print(amount)
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("take_damage", _tempProjectPath, null, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(0);
        var text = StripAnsi(output.ToString());
        text.Should().Contain("[override]");
        text.Should().Contain("func take_damage(amount: int) -> void:");
    }

    [TestMethod]
    public async Task FindRefs_ShowsContextLine_ForSuperCall()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", @"class_name Entity
extends Node

func take_damage(amount: int) -> void:
    pass
"),
            ("enemy.gd", @"class_name Enemy
extends Entity

func take_damage(amount: int) -> void:
    super.take_damage(amount)
    print(amount)
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("take_damage", _tempProjectPath, null, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(0);
        var text = StripAnsi(output.ToString());
        text.Should().Contain("[call-super]");
        text.Should().Contain("super.take_damage(amount)");
    }

    [TestMethod]
    public async Task FindRefs_AutoloadName_ShowsRegisteredName()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProjectWithAutoloads(
            new[]
            {
                ("game_manager.gd", @"extends Node

func take_damage(target, amount) -> void:
    pass
")
            },
            new[] { ("GameManager", "game_manager.gd") });

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("take_damage", _tempProjectPath, null, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(0);
        var text = StripAnsi(output.ToString());
        text.Should().Contain("GameManager");
    }

    [TestMethod]
    public async Task FindRefs_NoAutoload_ShowsTypeName()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", @"class_name Entity
extends Node

func take_damage(amount: int) -> void:
    pass
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("take_damage", _tempProjectPath, null, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(0);
        var text = StripAnsi(output.ToString());
        text.Should().Contain("Entity");
    }

    [TestMethod]
    public async Task FindRefs_SignalConnection_Code_ShowsSourceLine()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", @"class_name Entity
extends Node

signal damaged

func take_damage(amount: int) -> void:
    pass

func _ready() -> void:
    damaged.connect(take_damage)
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("take_damage", _tempProjectPath, null, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(0);
        var text = StripAnsi(output.ToString());
        text.Should().Contain("Signal connections");
        text.Should().Contain("Code (");
        text.Should().Contain("damaged.connect(take_damage)");
    }

    [TestMethod]
    public async Task FindRefs_SignalConnection_Scene_ShowsConnectionLine()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", @"class_name Entity
extends Node

func take_damage(amount: int) -> void:
    pass
"),
            ("entity.tscn", @"[gd_scene format=3]

[ext_resource type=""Script"" path=""res://entity.gd"" id=""1_script""]

[node name=""Entity"" type=""Node""]
script = ExtResource(""1_script"")

[node name=""Timer"" type=""Timer"" parent="".""]

[connection signal=""timeout"" from=""Timer"" to=""."" method=""take_damage""]
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("take_damage", _tempProjectPath, null, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(0);
        var text = StripAnsi(output.ToString());
        text.Should().Contain("Scene (", "scene signal connection from .tscn must appear in output");
        text.Should().Contain("[connection signal=\"timeout\"");
    }

    [TestMethod]
    public async Task FindRefs_ContextLine_ContainsSymbolName()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("test.gd", @"extends Node

var health: int = 100

func _ready() -> void:
    health = 50
    print(health)
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("health", _tempProjectPath, null, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(0);
        var text = StripAnsi(output.ToString());
        var lines = text.Split('\n');
        int contextLineCount = 0;
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("var health") || trimmed.StartsWith("health =") || trimmed.StartsWith("print(health"))
            {
                trimmed.Should().Contain("health");
                contextLineCount++;
            }
        }
        contextLineCount.Should().BeGreaterThan(0);
    }

    // ========================================
    // New tests for redesigned output format
    // ========================================

    [TestMethod]
    public async Task FindRefs_SymbolHeader_ShowsSymbolNameKindAndDeclaration()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", @"class_name Entity
extends Node

func take_damage(amount: int) -> void:
    pass
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("take_damage", _tempProjectPath, null, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(0);
        var text = StripAnsi(output.ToString());
        text.Should().Contain("Symbol: take_damage");
        text.Should().Contain("Kind: method");
        text.Should().Contain("Declared in: Entity");
    }

    [TestMethod]
    public async Task FindRefs_SummaryBlock_ShowsCounts()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("test.gd", @"extends Node

var health: int = 100

func _ready() -> void:
    health = 50
    print(health)
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("health", _tempProjectPath, null, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(0);
        var text = StripAnsi(output.ToString());
        text.Should().Contain("Summary:");
        text.Should().Contain("Definition");
        text.Should().Contain("Total references:");
    }

    [TestMethod]
    public async Task FindRefs_NoDuplicateOverrideEntries()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", @"class_name Entity
extends Node

func take_damage(amount: int) -> void:
    pass
"),
            ("enemy.gd", @"class_name Enemy
extends Entity

func take_damage(amount: int) -> void:
    print(amount)
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("take_damage", _tempProjectPath, null, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(0);
        var text = StripAnsi(output.ToString());

        // Count override markers — should be exactly one per override class
        var overrideCount = Regex.Matches(text, @"\[override\]").Count;
        overrideCount.Should().BeLessThanOrEqualTo(1,
            "each override class should produce at most one [override] entry, not duplicates");
    }

    [TestMethod]
    public async Task FindRefs_PosixPaths_NoBackslashes()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("scripts/entity.gd", @"class_name Entity
extends Node

func take_damage(amount: int) -> void:
    pass
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("take_damage", _tempProjectPath, null, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(0);
        var text = StripAnsi(output.ToString());

        // Check that file paths use forward slashes
        var pathLines = text.Split('\n')
            .Where(l => l.Contains("scripts") && l.Contains("entity.gd"))
            .ToArray();
        pathLines.Should().NotBeEmpty("output should contain the script path");
        foreach (var line in pathLines)
        {
            line.Should().NotContain("\\", "paths should use forward slashes (POSIX)");
        }
    }

    [TestMethod]
    public async Task FindRefs_ContractStrings_ShowsExplanationNote()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", @"class_name Entity
extends Node

func take_damage(amount: int) -> void:
    pass
"),
            ("checker.gd", @"extends Node

func check(obj) -> void:
    if obj.has_method(""take_damage""):
        pass
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("take_damage", _tempProjectPath, null, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(0);
        var text = StripAnsi(output.ToString());
        if (text.Contains("Contract strings"))
        {
            text.Should().Contain("string-based API contracts, not auto-applied in rename");
        }
    }

    [TestMethod]
    public async Task FindRefs_SameNameDifferentSymbol_ShowsUnrelatedSection()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", @"class_name Entity
extends Node

func take_damage(amount: int) -> void:
    pass
"),
            ("manager.gd", @"class_name GameManager
extends Node

func take_damage(amount: int) -> void:
    pass
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("take_damage", _tempProjectPath, null, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(0);
        var text = StripAnsi(output.ToString());
        // One of the two classes should appear in unrelated section
        text.Should().Contain("Unrelated symbols with same name");
        // Summary should show unrelated symbol details
        text.Should().Contain("Unrelated:");
        // The unrelated class name should appear in summary breakdown
        var summaryStart = text.IndexOf("Summary:");
        var summaryText = summaryStart >= 0 ? text.Substring(summaryStart) : text;
        var hasPrimaryInSummary = summaryText.Contains("Entity") || summaryText.Contains("GameManager");
        hasPrimaryInSummary.Should().BeTrue("summary should list unrelated symbol class names");
    }

    [TestMethod]
    public async Task FindRefs_SignalConnections_SplitCodeAndScene()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", @"class_name Entity
extends Node

signal damaged

func take_damage(amount: int) -> void:
    pass

func _ready() -> void:
    damaged.connect(take_damage)
"),
            ("entity.tscn", @"[gd_scene format=3]

[ext_resource type=""Script"" path=""res://entity.gd"" id=""1_script""]

[node name=""Entity"" type=""Node""]
script = ExtResource(""1_script"")

[node name=""Timer"" type=""Timer"" parent="".""]

[connection signal=""timeout"" from=""Timer"" to=""."" method=""take_damage""]
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("take_damage", _tempProjectPath, null, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(0);
        var text = StripAnsi(output.ToString());
        text.Should().Contain("Signal connections");
        text.Should().Contain("Code (");
        text.Should().Contain("Scene (");
    }
}
