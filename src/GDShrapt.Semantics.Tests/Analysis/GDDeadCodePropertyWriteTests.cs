using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for dead code analysis with property/indexer write patterns and escape analysis.
/// </summary>
[TestClass]
public class GDDeadCodePropertyWriteTests
{
    private static string CreateTempProject(params (string name, string content)[] scripts)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "gdshrapt_propwrite_test_" + Guid.NewGuid().ToString("N"));
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
            IncludePrivate = true,
        };
        configure?.Invoke(options);

        return service.AnalyzeProject(options);
    }

    #region External source — NOT dead code

    [TestMethod]
    public void UniqueNodeRef_PropertyWrite_NotDeadCode()
    {
        var script = @"extends Control

@onready var bg_color: ColorRect = %BackgroundColor

func _ready():
	bg_color.color = Color.RED
";
        var tempPath = CreateTempProject(("ui.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "bg_color" && i.Confidence == GDReferenceConfidence.Strict,
                "bg_color is read to access .color property, writing a property on an external node is a meaningful side effect");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void GetNode_PropertyWrite_NotDeadCode()
    {
        var script = @"extends Node2D

func _ready():
	var label = get_node(""Label"")
	label.text = ""hello""
";
        var tempPath = CreateTempProject(("scene.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "label" && i.Confidence == GDReferenceConfidence.Strict,
                "label is read to access .text property on an external node");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void Parameter_PropertyWrite_NotDeadCode()
    {
        var script = @"extends Node

func configure(node: Node2D):
	node.position = Vector2(10, 20)
	node.visible = false

func _ready():
	configure($Child)
";
        var tempPath = CreateTempProject(("config.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "node" && i.Confidence == GDReferenceConfidence.Strict,
                "parameter node is read to set properties on it — external object, side effects matter");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void IndexerWrite_OnExternalVar_NotDeadCode()
    {
        var script = @"extends Node

var shared_data: Dictionary

func _ready():
	shared_data = get_meta(""data"")
	shared_data[""key""] = ""value""
";
        var tempPath = CreateTempProject(("indexer.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "shared_data" && i.Confidence == GDReferenceConfidence.Strict,
                "shared_data is read for indexer write — external data, side effects matter");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void MultiplePropertyWrites_NotDeadCode()
    {
        var script = @"extends Node2D

func _ready():
	var label = get_node(""Label"")
	label.text = ""hello""
	label.visible = true
	label.modulate = Color.RED
";
        var tempPath = CreateTempProject(("multi.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "label" && i.Confidence == GDReferenceConfidence.Strict,
                "multiple property writes on external source — all are meaningful side effects");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region Chain access — NOT dead code

    [TestMethod]
    public void ChainedPropertyWrite_NotDeadCode()
    {
        var script = @"extends Control

@onready var panel: Panel = $Panel

func _ready():
	panel.size.x = 100
";
        var tempPath = CreateTempProject(("chain.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "panel" && i.Confidence == GDReferenceConfidence.Strict,
                "panel is read through chain: panel.size.x = 100");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region Constructor source — isolated dead code (escape analysis)

    [TestMethod]
    public void LocalNew_OnlyPropertyWrite_IsDeadCode()
    {
        var script = @"extends Node

func _ready():
	var obj = RefCounted.new()
	obj.resource_name = ""test""
";
        var tempPath = CreateTempProject(("local_new.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "obj",
                "obj is locally constructed and never escapes — property write is dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void LocalArray_OnlyIndexerWrite_IsDeadCode()
    {
        var script = @"extends Node

func _ready():
	var arr = []
	arr[0] = 42
";
        var tempPath = CreateTempProject(("local_arr.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "arr",
                "arr is locally constructed array, never escapes — indexer write is dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void LocalDict_OnlyIndexerWrite_IsDeadCode()
    {
        var script = @"extends Node

func _ready():
	var d = {}
	d[""key""] = ""value""
";
        var tempPath = CreateTempProject(("local_dict.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "d",
                "d is locally constructed dict, never escapes — indexer write is dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void LocalValueType_PropertyWrite_IsDeadCode()
    {
        var script = @"extends Node

func _ready():
	var v = Vector2(1, 2)
	v.x = 5
";
        var tempPath = CreateTempProject(("local_vec.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "v",
                "v is locally constructed value type, never escapes — property write is dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region Constructor source — escapes (NOT dead code)

    [TestMethod]
    public void LocalNew_PropertyWriteAndReturn_NotDeadCode()
    {
        var script = @"extends Node

func create_resource() -> RefCounted:
	var obj = RefCounted.new()
	obj.resource_name = ""test""
	return obj
";
        var tempPath = CreateTempProject(("escape_return.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "obj",
                "obj escapes via return — not dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void LocalNew_PropertyWriteAndCallArg_NotDeadCode()
    {
        var script = @"extends Node

func _ready():
	var child = Node2D.new()
	child.position = Vector2(10, 20)
	add_child(child)
";
        var tempPath = CreateTempProject(("escape_call.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "child",
                "child escapes via add_child() call — not dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void LocalNew_PropertyWriteAndAssignOther_NotDeadCode()
    {
        var script = @"extends Node

var _stored

func _ready():
	var obj = RefCounted.new()
	obj.resource_name = ""test""
	_stored = obj
";
        var tempPath = CreateTempProject(("escape_assign.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "obj",
                "obj escapes via assignment to class-level _stored — not dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void LocalNew_PropertyWriteAndEmit_NotDeadCode()
    {
        var script = @"extends Node

signal resource_created(res)

func _ready():
	var obj = RefCounted.new()
	obj.resource_name = ""test""
	emit_signal(""resource_created"", obj)
";
        var tempPath = CreateTempProject(("escape_emit.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "obj",
                "obj escapes via emit_signal — not dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region Non-local variables — NOT dead code (scope check)

    [TestMethod]
    public void ClassVar_PropertyWrite_NotDeadCode()
    {
        var script = @"extends Node2D

var sprite: Sprite2D

func _ready():
	sprite = $Sprite2D
	sprite.visible = false
";
        var tempPath = CreateTempProject(("classvar.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "sprite" && i.Confidence == GDReferenceConfidence.Strict,
                "class-level variable — scope is broader, escape analysis does not apply");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void StaticVar_PropertyWrite_NotDeadCode()
    {
        var script = @"extends Node

static var config: Dictionary = {}

func _ready():
	config[""key""] = ""value""
";
        var tempPath = CreateTempProject(("staticvar.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "config" && i.Confidence == GDReferenceConfidence.Strict,
                "static class-level variable — scope is global, escape analysis does not apply");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region Compound assignments

    [TestMethod]
    public void CompoundAssign_PropertyWrite_NotDeadCode()
    {
        var script = @"extends Node2D

func _ready():
	var node = get_node(""Sprite"")
	node.position.x += 10
";
        var tempPath = CreateTempProject(("compound_prop.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "node" && i.Confidence == GDReferenceConfidence.Strict,
                "compound assignment on external source property — genuine read");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void CompoundAssign_SimpleVar_WithRead_NotDeadCode()
    {
        var script = @"extends Node

func _ready():
	var count = 0
	count += 1
	print(count)
";
        var tempPath = CreateTempProject(("compound_read.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "count" && i.Confidence == GDReferenceConfidence.Strict,
                "count is both compound-assigned and read via print()");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region Edge cases — NOT dead code

    [TestMethod]
    public void SelfPropertyWrite_NotDeadCode()
    {
        var script = @"extends Node2D

func _ready():
	self.position = Vector2.ZERO
	self.visible = false
";
        var tempPath = CreateTempProject(("self_prop.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "self",
                "self property write is always meaningful");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void PropertyWriteInLoop_NotDeadCode()
    {
        var script = @"extends Node

func _ready():
	var children = get_children()
	for child in children:
		child.visible = false
";
        var tempPath = CreateTempProject(("loop_prop.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "children" && i.Confidence == GDReferenceConfidence.Strict,
                "children is read in for loop — external source, property write in loop is meaningful");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void MethodCallOnVar_NotDeadCode()
    {
        var script = @"extends Node

func _ready():
	var node = get_node(""Child"")
	node.queue_free()
";
        var tempPath = CreateTempProject(("method_call.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "node" && i.Confidence == GDReferenceConfidence.Strict,
                "node is read to call method — not a property write pattern");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void PropertyWriteInConditional_NotDeadCode()
    {
        var script = @"extends Node2D

var flag: bool

func _ready():
	var label = get_node(""Label"")
	if flag:
		label.text = ""on""
	else:
		label.text = ""off""
";
        var tempPath = CreateTempProject(("cond_prop.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "label" && i.Confidence == GDReferenceConfidence.Strict,
                "label is read in both branches — external source property writes are meaningful");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region Negative cases — genuine dead code (no property write)

    [TestMethod]
    public void SimpleVar_WriteOnly_IsDeadCode()
    {
        var script = @"extends Node

func _ready():
	var x = 5
	x = 10
";
        var tempPath = CreateTempProject(("write_only.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "x",
                "x is only written, never read — dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion
}
