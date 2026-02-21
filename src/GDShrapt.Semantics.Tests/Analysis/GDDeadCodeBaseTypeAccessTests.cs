using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests that dead code analysis correctly handles cross-file member access
/// when the caller variable is typed as a base class of the actual script.
/// Example: var view: Control + view.apply_changes() where apply_changes() is in a script extending Control.
/// </summary>
[TestClass]
public class GDDeadCodeBaseTypeAccessTests
{
    private static string CreateTempProject(params (string name, string content)[] scripts)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "gdshrapt_basetype_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);

        var projectGodot = @"[gd_resource type=""ProjectSettings"" format=3]

config_version=5

[application]
config/name=""TestProject""
";
        File.WriteAllText(Path.Combine(tempPath, "project.godot"), projectGodot);

        foreach (var (name, content) in scripts)
        {
            var fileName = name.EndsWith(".gd", StringComparison.OrdinalIgnoreCase) ? name : name + ".gd";
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
            IncludePrivate = false,
        };
        configure?.Invoke(options);

        return service.AnalyzeProject(options);
    }

    [TestMethod]
    public void Function_CalledViaBaseTypeVariable_NotDeadCode()
    {
        var mainView = @"extends Control

func apply_changes() -> void:
    pass

func count_unsaved_files() -> int:
    return 0
";
        var plugin = @"extends Node

var main_view: Control

func _ready() -> void:
    main_view.apply_changes()
    var count = main_view.count_unsaved_files()
";
        var tempPath = CreateTempProject(("main_view.gd", mainView), ("plugin.gd", plugin));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(i => i.Kind == GDDeadCodeKind.Function && i.Name == "apply_changes",
                "apply_changes() is called via base-typed variable main_view: Control");
            report.Items.Should().NotContain(i => i.Kind == GDDeadCodeKind.Function && i.Name == "count_unsaved_files",
                "count_unsaved_files() is called via base-typed variable main_view: Control");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void Function_CalledViaExactType_NotDeadCode()
    {
        var myView = @"extends Control
class_name MyView

func do_work() -> void:
    pass
";
        var caller = @"extends Node

var view: MyView

func _ready() -> void:
    view.do_work()
";
        var tempPath = CreateTempProject(("my_view.gd", myView), ("caller.gd", caller));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(i => i.Kind == GDDeadCodeKind.Function && i.Name == "do_work",
                "do_work() is called via exact-typed variable view: MyView");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void Function_NotCalledAnywhere_StillDeadCode()
    {
        var scriptA = @"extends Control

func unused_func() -> void:
    pass
";
        var scriptB = @"extends Node

func _ready() -> void:
    pass
";
        var tempPath = CreateTempProject(("script_a.gd", scriptA), ("script_b.gd", scriptB));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().Contain(i => i.Kind == GDDeadCodeKind.Function && i.Name == "unused_func",
                "unused_func() is not called anywhere and should be dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void Function_CalledViaScriptGrandparentType_NotDeadCode()
    {
        // Script-based inheritance chain: grandchild.gd extends parent.gd extends Node
        var parent = @"extends Node
class_name ParentScript

func base_method() -> void:
    pass
";
        var grandchild = @"extends ParentScript

func my_method() -> void:
    pass
";
        var caller = @"extends Node

var x: ParentScript

func _ready() -> void:
    x.my_method()
";
        var tempPath = CreateTempProject(("parent.gd", parent), ("grandchild.gd", grandchild), ("caller.gd", caller));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            // grandchild.gd extends ParentScript → inheritance chain includes "ParentScript"
            // Caller has var x: ParentScript → member access indexed under ("ParentScript", "my_method")
            report.Items.Should().NotContain(i => i.Kind == GDDeadCodeKind.Function && i.Name == "my_method",
                "my_method() is called via script-grandparent-typed variable x: ParentScript");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void Variable_AccessedViaBaseType_NotDeadCode()
    {
        var scriptA = @"extends Node

var health: int = 100

func _ready() -> void:
    pass
";
        var scriptB = @"extends Node

var obj: Node

func _ready() -> void:
    var h = obj.health
";
        var tempPath = CreateTempProject(("entity.gd", scriptA), ("manager.gd", scriptB));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(i => i.Kind == GDDeadCodeKind.Variable && i.Name == "health",
                "health is read via base-typed variable obj: Node");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void Signal_ReferencedViaBaseType_NotDeadCode()
    {
        var scriptA = @"extends Node

signal died

func take_damage() -> void:
    died.emit()
";
        var scriptB = @"extends Node

var obj: Node

func _ready() -> void:
    obj.died.connect(_on_died)

func _on_died() -> void:
    pass
";
        var tempPath = CreateTempProject(("entity.gd", scriptA), ("listener.gd", scriptB));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(i => i.Kind == GDDeadCodeKind.Signal && i.Name == "died",
                "died signal is referenced via base-typed variable obj: Node");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void Function_CalledViaUnrelatedType_StillDeadCode()
    {
        var scriptA = @"extends Control

func my_func() -> void:
    pass
";
        // Area2D is not in Control's inheritance chain
        var scriptB = @"extends Node

var x: Area2D

func _ready() -> void:
    x.my_func()
";
        var tempPath = CreateTempProject(("view.gd", scriptA), ("caller.gd", scriptB));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().Contain(i => i.Kind == GDDeadCodeKind.Function && i.Name == "my_func",
                "my_func() is called on Area2D which is not in Control's inheritance chain");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void MultipleScripts_SameBaseType_BothNotDeadWhenCalledOnBase()
    {
        var scriptA = @"extends Control

func shared() -> void:
    pass
";
        var scriptC = @"extends Control

func shared() -> void:
    pass
";
        var scriptB = @"extends Node

var x: Control

func _ready() -> void:
    x.shared()
";
        var tempPath = CreateTempProject(("view_a.gd", scriptA), ("view_c.gd", scriptC), ("caller.gd", scriptB));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            // Both scripts extend Control and have shared() — a Control-typed access should cover both
            report.Items.Should().NotContain(i => i.Kind == GDDeadCodeKind.Function && i.Name == "shared",
                "shared() is called on Control-typed variable, covering all scripts extending Control");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void Function_CalledViaBuiltinGrandparentType_NotDeadCode()
    {
        // VBoxContainer extends Container extends Control in Godot's built-in hierarchy.
        // The caller uses var x: Control, but the script extends VBoxContainer.
        var scriptA = @"extends VBoxContainer

func custom_method() -> void:
    pass
";
        var scriptB = @"extends Node

var x: Control

func _ready() -> void:
    x.custom_method()
";
        var tempPath = CreateTempProject(("widget.gd", scriptA), ("caller.gd", scriptB));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(i => i.Kind == GDDeadCodeKind.Function && i.Name == "custom_method",
                "custom_method() is called via Control-typed variable, and VBoxContainer inherits from Control via built-in hierarchy");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void Variable_AccessedViaBuiltinGrandparentType_NotDeadCode()
    {
        var scriptA = @"extends VBoxContainer

var data: int = 42
";
        var scriptB = @"extends Node

var x: Control

func _ready() -> void:
    var d = x.data
";
        var tempPath = CreateTempProject(("widget.gd", scriptA), ("reader.gd", scriptB));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(i => i.Kind == GDDeadCodeKind.Variable && i.Name == "data",
                "data is read via Control-typed variable, and VBoxContainer inherits from Control via built-in hierarchy");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }
}
