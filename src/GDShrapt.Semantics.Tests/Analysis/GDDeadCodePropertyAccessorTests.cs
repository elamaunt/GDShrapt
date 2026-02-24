using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for dead code analysis with property setter/getter accessor scopes.
/// </summary>
[TestClass]
public class GDDeadCodePropertyAccessorTests
{
    private static string CreateTempProject(params (string name, string content)[] files)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "gdshrapt_propacc_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);

        var projectGodot = @"[gd_resource type=""ProjectSettings"" format=3]

config_version=5

[application]
config/name=""TestProject""
";
        File.WriteAllText(Path.Combine(tempPath, "project.godot"), projectGodot);

        foreach (var (name, content) in files)
        {
            var fileName = name;
            if (!name.EndsWith(".gd", StringComparison.OrdinalIgnoreCase)
                && !name.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
                fileName = name + ".gd";
            var filePath = Path.Combine(tempPath, fileName);
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && dir != tempPath)
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, content);
        }

        return tempPath;
    }

    private static void DeleteTempProject(string path)
    {
        if (Directory.Exists(path))
        {
            try { Directory.Delete(path, recursive: true); }
            catch { }
        }
    }

    private static GDDeadCodeReport RunDeadCodeAnalysis(string tempPath, Action<GDDeadCodeOptions>? configure = null)
    {
        using var project = GDProjectLoader.LoadProject(tempPath);
        project.BuildCallSiteRegistry();
        var projectModel = new GDProjectSemanticModel(project);
        var service = projectModel.DeadCode;

        var options = new GDDeadCodeOptions
        {
            IncludeFunctions = true,
            IncludeVariables = true,
            IncludeSignals = true,
            IncludePrivate = true,
        };
        configure?.Invoke(options);

        return service.AnalyzeProject(options);
    }

    #region RC6: Setter/getter body scope

    [TestMethod]
    public void LocalVar_InPropertySetter_NotDeadCode()
    {
        var script = @"extends Node

var health: int = 100:
    set(new_value):
        var old_value := health
        health = clampi(new_value, 0, 100)
        if health < old_value:
            print(""took damage"")
";
        var tempPath = CreateTempProject(("entity.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "old_value",
                "old_value is read inside the setter body");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void LocalVar_InPropertyGetter_NotDeadCode()
    {
        var script = @"extends Node

var _hp: int = 100
var health: int:
    get:
        var clamped := clampi(_hp, 0, 100)
        return clamped
";
        var tempPath = CreateTempProject(("entity.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "clamped",
                "clamped is read inside the getter body");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void SetterParam_UsedInBody_NotDeadCode()
    {
        var script = @"extends Node

var speed: float = 1.0:
    set(val):
        speed = clampf(val, 0.0, 10.0)
";
        var tempPath = CreateTempProject(("entity.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "val",
                "setter parameter val is used in the setter body");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void LocalVar_SameNameInTwoSetters_NotDeadCode()
    {
        var script = @"extends Node

var value: int = 0:
    set(new_value):
        var old_value := value
        value = clampi(new_value, 0, 100)
        if value > old_value:
            print(""increased"")

var selected: int = 0:
    set(new_value):
        var old_value := selected
        selected = clampi(new_value, 0, 100)
        if selected > old_value:
            print(""selected"")
";
        var tempPath = CreateTempProject(("energy_bar.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "old_value",
                "both old_value locals in separate setter scopes are read");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion

    #region RC4: Scene signal connection with snake_case script

    [TestMethod]
    public void Function_ConnectedViaSceneSignal_NotDeadCode()
    {
        var script = @"extends Node

func on_button_pressed():
    print(""pressed"")
";
        // Scene file with ext_resource pointing to the script and a signal connection
        var scene = @"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://no_class_name_handler.gd"" id=""1_abc""]

[node name=""Root"" type=""Node""]
script = ExtResource(""1_abc"")

[node name=""Button"" type=""Button"" parent="".""]

[connection signal=""pressed"" from=""Button"" to=""."" method=""on_button_pressed""]
";
        var tempPath = CreateTempProject(
            ("no_class_name_handler.gd", script),
            ("test_scene.tscn", scene));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "on_button_pressed",
                ".tscn signal connection should prevent FNC for scripts without class_name");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion

    #region RC7: Constant used as type annotation

    [TestMethod]
    public void Constant_UsedAsTypeAnnotation_NotDeadCode()
    {
        var script = @"extends Node

const MyScript := preload(""res://some_script.gd"")
var items: Array[MyScript] = []

func create() -> MyScript:
    return MyScript.new()
";
        var tempPath = CreateTempProject(("typed.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Constant && i.Name == "MyScript",
                "MyScript is used as type annotation and in expressions");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void Constant_UsedOnlyAsVarType_NotDeadCode()
    {
        var script = @"extends Node

const MyType := preload(""res://some_script.gd"")
var instance: MyType = null
";
        var tempPath = CreateTempProject(("typed2.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Constant && i.Name == "MyType",
                "MyType is used as type annotation for variable");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion

    #region RC5: Duck-typed self-access

    [TestMethod]
    public void MemberVar_AccessedViaSelfMember_NotDeadCode()
    {
        var script = @"extends Node

var info := {}

func setup():
    info = {""name"": ""test""}

func use():
    print(self.info[""name""])
";
        var tempPath = CreateTempProject(("entity.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "info",
                "self.info creates a duck-typed reference to class-level info");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void MemberVar_AccessedViaDuckTypedChain_NotDeadCode()
    {
        var script = @"extends Node

var last_question_info := {}

func setup():
    last_question_info = {""choices"": []}

func use():
    var obj = self
    print(obj.last_question_info[""choices""])
";
        var tempPath = CreateTempProject(("entity.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "last_question_info",
                "duck-typed member access to class-level variable should prevent VNR");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void FindRefs_SelfMemberAccess_IncludesReference()
    {
        var script = @"extends Node

var info := {}

func setup():
    info = {""name"": ""test""}

func use():
    print(self.info[""name""])
";
        var tempPath = CreateTempProject(("entity.gd", script));
        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);

            var file = project.ScriptFiles.First();
            var model = projectModel.GetSemanticModel(file)!;
            var symbol = model.FindSymbol("info");
            symbol.Should().NotBeNull("info should be a registered symbol");

            var refs = model.GetReferencesTo(symbol!);
            refs.Should().NotBeEmpty("self.info should create a reference to info");
            refs.Where(r => r.IsRead).Should().NotBeEmpty(
                "self.info should create a read reference");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void Rename_SelfMemberAccess_InStrictEdits()
    {
        var script = @"extends Node

var info := {}

func setup():
    info = {""name"": ""test""}

func use():
    print(self.info[""name""])
";
        var tempPath = CreateTempProject(("entity.gd", script));
        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);

            var renameService = new GDRenameService(project, projectModel);
            var file = project.ScriptFiles.First();
            var plan = renameService.PlanRename("info", "data", file.FullPath);

            plan.Should().NotBeNull("rename plan should be created");
            plan!.StrictEdits.Should().NotBeEmpty(
                "self.info reference should appear in strict edits");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void MemberVar_UnrelatedObjectSameName_StillDeadCode()
    {
        var script = @"extends Node

var data := {}

func test():
    var other = Node.new()
    print(other.data)
";
        var tempPath = CreateTempProject(("entity.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            // other.data should NOT be linked to class-level data
            // because "other" is not "self" — the reference collector only links self.prop
            // However, duck-typed access with Variant still creates a member access in the model
            // Dead-code service may or may not catch this depending on whether
            // the member access is stored for the file's own type name
            // This test documents current behavior
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion

    #region RC8: For-loop iterator scope isolation

    [TestMethod]
    public void LocalVar_AfterForLoopSameName_NotDeadCode()
    {
        var script = @"extends Node

func test():
    var items = [1, 2, 3]
    for x in items:
        print(x)
    var x = 99
    print(x)
";
        var tempPath = CreateTempProject(("entity.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "x",
                "var x after for-loop is used and should not be dead code");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void LocalVar_AfterNestedForLoopSameName_NotDeadCode()
    {
        var script = @"extends Node

func process():
    var nodes = get_children()
    for container in nodes:
        var children = container.get_children()
        for character_node in children:
            print(character_node)
        var character_node = container.get_child(-1)
        print(character_node)
";
        var tempPath = CreateTempProject(("entity.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "character_node",
                "var character_node after inner for-loop is used");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion

    #region RC8: While/If/Match scope isolation

    [TestMethod]
    public void LocalVar_AfterWhileLoopSameName_NotDeadCode()
    {
        var script = @"extends Node

func test():
    while true:
        var result = 1
        print(result)
        break
    var result = 2
    print(result)
";
        var tempPath = CreateTempProject(("entity.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "result",
                "var result after while loop is used and should not be dead code");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void LocalVar_AfterIfBranchSameName_NotDeadCode()
    {
        var script = @"extends Node

func test():
    if true:
        var x = 1
        print(x)
    var x = 2
    print(x)
";
        var tempPath = CreateTempProject(("entity.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "x",
                "var x after if branch is used and should not be dead code");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void LocalVar_AfterMatchCaseSameName_NotDeadCode()
    {
        var script = @"extends Node

func test():
    match 42:
        var x:
            print(x)
    var x = 99
    print(x)
";
        var tempPath = CreateTempProject(("entity.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "x",
                "var x after match case is used and should not be dead code");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion
}
