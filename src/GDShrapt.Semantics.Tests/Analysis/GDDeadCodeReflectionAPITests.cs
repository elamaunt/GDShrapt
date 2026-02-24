using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDDeadCodeReflectionAPITests
{
    private static string CreateTempProject(params (string name, string content)[] scripts)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "gdshrapt_refapi_test_" + Guid.NewGuid().ToString("N"));
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

    #region Method reflection APIs — arg[0]

    [TestMethod]
    public void Callv_StringMethod_NotDeadCode()
    {
        var script = @"extends Node

func _ready():
    callv(""my_method"", [])

func my_method() -> void:
    pass
";
        var tempPath = CreateTempProject(("callv_test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "my_method",
                "my_method is called via callv(\"my_method\", [])");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void Rpc_StringMethod_NotDeadCode()
    {
        var script = @"extends Node

func _ready():
    rpc(""sync_state"")

func sync_state() -> void:
    pass
";
        var tempPath = CreateTempProject(("rpc_test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "sync_state",
                "sync_state is called via rpc(\"sync_state\")");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion

    #region Method reflection APIs — arg[1] and arg[2]

    [TestMethod]
    public void RpcId_StringMethod_NotDeadCode()
    {
        var script = @"extends Node

func _ready():
    rpc_id(1, ""sync_state"")

func sync_state() -> void:
    pass
";
        var tempPath = CreateTempProject(("rpc_id_test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "sync_state",
                "sync_state is called via rpc_id(1, \"sync_state\")");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void CallGroup_StringMethod_NotDeadCode()
    {
        var script = @"extends Node

func _ready():
    get_tree().call_group(""enemies"", ""take_damage"")

func take_damage() -> void:
    pass
";
        var tempPath = CreateTempProject(("call_group_test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "take_damage",
                "take_damage is called via call_group(\"enemies\", \"take_damage\")");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void CallGroupFlags_StringMethod_NotDeadCode()
    {
        var script = @"extends Node

func _ready():
    get_tree().call_group_flags(0, ""enemies"", ""take_damage"")

func take_damage() -> void:
    pass
";
        var tempPath = CreateTempProject(("call_group_flags_test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "take_damage",
                "take_damage is called via call_group_flags(0, \"enemies\", \"take_damage\")");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion

    #region Signal reflection APIs

    [TestMethod]
    public void IsConnected_StringSignal_NotDeadCode()
    {
        var script = @"extends Node

signal health_changed

func _ready():
    if is_connected(""health_changed"", Callable()):
        pass
";
        var tempPath = CreateTempProject(("is_connected_test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Signal && i.Name == "health_changed",
                "health_changed is checked via is_connected(\"health_changed\", ...)");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void Disconnect_StringSignal_NotDeadCode()
    {
        var script = @"extends Node

signal health_changed

func _ready():
    disconnect(""health_changed"", Callable())
";
        var tempPath = CreateTempProject(("disconnect_test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Signal && i.Name == "health_changed",
                "health_changed is referenced via disconnect(\"health_changed\", ...)");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void HasUserSignal_StringSignal_NotDeadCode()
    {
        var script = @"extends Node

signal custom_event

func _ready():
    if has_user_signal(""custom_event""):
        pass
";
        var tempPath = CreateTempProject(("has_user_signal_test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Signal && i.Name == "custom_event",
                "custom_event is checked via has_user_signal(\"custom_event\")");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion

    #region Property reflection APIs

    [TestMethod]
    public void SetDeferred_StringProperty_NotDeadCode()
    {
        var script = @"extends Node

var my_prop = 0

func _ready():
    set_deferred(""my_prop"", 42)
";
        var tempPath = CreateTempProject(("set_deferred_test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "my_prop",
                "my_prop is set via set_deferred(\"my_prop\", 42)");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void GetIndexed_StringProperty_NotDeadCode()
    {
        var script = @"extends Node

var my_prop = 0

func _ready():
    var v = get_indexed(""my_prop"")
    print(v)
";
        var tempPath = CreateTempProject(("get_indexed_test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "my_prop",
                "my_prop is read via get_indexed(\"my_prop\")");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void SetIndexed_StringProperty_NotDeadCode()
    {
        var script = @"extends Node

var my_prop = 0

func _ready():
    set_indexed(""my_prop"", 100)
";
        var tempPath = CreateTempProject(("set_indexed_test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "my_prop",
                "my_prop is set via set_indexed(\"my_prop\", 100)");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion

    #region Member-based calls (obj.method())

    [TestMethod]
    public void MemberCallv_StringMethod_NotDeadCode()
    {
        var script = @"extends Node

func _ready():
    self.callv(""my_method"", [])

func my_method() -> void:
    pass
";
        var tempPath = CreateTempProject(("member_callv_test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "my_method",
                "my_method is called via self.callv(\"my_method\", [])");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void MemberSetDeferred_StringProperty_NotDeadCode()
    {
        var script = @"extends Node

var my_prop = 0

func _ready():
    self.set_deferred(""my_prop"", 42)
";
        var tempPath = CreateTempProject(("member_set_deferred_test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "my_prop",
                "my_prop is set via self.set_deferred(\"my_prop\", 42)");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void MemberDisconnect_StringSignal_NotDeadCode()
    {
        var script = @"extends Node

signal my_sig

func _ready():
    self.disconnect(""my_sig"", Callable())
";
        var tempPath = CreateTempProject(("member_disconnect_test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Signal && i.Name == "my_sig",
                "my_sig is referenced via self.disconnect(\"my_sig\", ...)");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion

    #region Self + String pattern

    [TestMethod]
    public void SelfString_RegisterCallback_NotDeadCode()
    {
        var script = @"extends Node

func my_callback() -> void:
    pass

func _ready():
    var ext = get_node(""/root/System"")
    ext.register(self, ""my_callback"")
";
        var tempPath = CreateTempProject(("self_string_register.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath, o => o.MaxConfidence = GDReferenceConfidence.Potential);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "my_callback",
                "my_callback is referenced via self+string pattern: ext.register(self, \"my_callback\")");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void SelfString_TweenCallback_NotDeadCode()
    {
        var script = @"extends Node

func on_complete() -> void:
    pass

func _ready():
    var tween = create_tween()
    tween.tween_callback(self, ""on_complete"")
";
        var tempPath = CreateTempProject(("self_string_tween.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath, o => o.MaxConfidence = GDReferenceConfidence.Potential);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "on_complete",
                "on_complete is referenced via self+string: tween.tween_callback(self, \"on_complete\")");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void SelfString_Godot3Connect_NotDeadCode()
    {
        var script = @"extends Node

func on_timeout() -> void:
    pass

func _ready():
    var timer = get_node(""Timer"")
    timer.connect(""timeout"", self, ""on_timeout"")
";
        var tempPath = CreateTempProject(("self_string_connect.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath, o => o.MaxConfidence = GDReferenceConfidence.Potential);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "on_timeout",
                "on_timeout is referenced via Godot 3.x connect(\"signal\", self, \"on_timeout\")");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void SelfString_Signal_NotDeadCode()
    {
        var script = @"extends Node

signal health_changed

func _ready():
    var bus = get_node(""/root/EventBus"")
    bus.register_signal(self, ""health_changed"")
";
        var tempPath = CreateTempProject(("self_string_signal.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath, o => o.MaxConfidence = GDReferenceConfidence.Potential);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Signal && i.Name == "health_changed",
                "health_changed is referenced via self+string: bus.register_signal(self, \"health_changed\")");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion

    #region Edge cases — should NOT match

    [TestMethod]
    public void SelfString_NoMatchingSymbol_NoFalseRef()
    {
        var script = @"extends Node

func _ready():
    var ext = get_node(""/root/X"")
    ext.register(self, ""nonexistent"")
";
        var tempPath = CreateTempProject(("self_string_no_match.gd", script));
        try
        {
            // Should not crash or produce false references
            var report = RunDeadCodeAnalysis(tempPath, o => o.MaxConfidence = GDReferenceConfidence.Potential);
            report.Should().NotBeNull();
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void SelfString_NonSelfObject_StillDeadCode()
    {
        var script = @"extends Node

func my_method() -> void:
    pass

func _ready():
    var other = get_node(""Other"")
    var ext = get_node(""/root/X"")
    ext.register(other, ""my_method"")
";
        var tempPath = CreateTempProject(("self_string_non_self.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath, o => o.MaxConfidence = GDReferenceConfidence.Potential);
            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "my_method",
                "my_method should remain dead code when other (not self) is passed");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void SelfString_LocalMethod_NotTriggered()
    {
        var script = @"extends Node

func register(obj, name) -> void:
    print(obj, name)

func my_method() -> void:
    pass

func _ready():
    register(self, ""my_method"")
";
        var tempPath = CreateTempProject(("self_string_local.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath, o => o.MaxConfidence = GDReferenceConfidence.Potential);
            // register is a local method — self+string pattern should not trigger
            // my_method should be dead code (register just prints, doesn't use reflection)
            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "my_method",
                "self+string should not trigger for local method calls");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void TrulyUnused_WithNewAPIs_StillDeadCode()
    {
        var script = @"extends Node

func truly_unused() -> void:
    pass

func _ready():
    callv(""other_method"", [])
";
        var tempPath = CreateTempProject(("truly_unused.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "truly_unused",
                "truly_unused should remain dead code when other methods are called via reflection");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion

    #region Typed Object + String pattern

    [TestMethod]
    public void TypedObject_Method_NotDeadCode()
    {
        var playerScript = @"class_name Player
extends Node

func take_damage(amount: int) -> void:
    pass
";
        var callerScript = @"extends Node

func _ready():
    var player: Player = Player.new()
    var ext = get_node(""/root/System"")
    ext.apply_action(player, ""take_damage"")
";
        var tempPath = CreateTempProject(
            ("player.gd", playerScript),
            ("caller.gd", callerScript));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath, o => o.MaxConfidence = GDReferenceConfidence.Potential);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "take_damage",
                "take_damage exists on Player and is referenced via typed-object+string pattern");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void TypedObject_Signal_NotDeadCode()
    {
        var playerScript = @"class_name Player
extends Node

signal health_changed
";
        var callerScript = @"extends Node

func _ready():
    var player: Player = Player.new()
    var bus = get_node(""/root/EventBus"")
    bus.register_signal(player, ""health_changed"")
";
        var tempPath = CreateTempProject(
            ("player.gd", playerScript),
            ("caller.gd", callerScript));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath, o => o.MaxConfidence = GDReferenceConfidence.Potential);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Signal && i.Name == "health_changed",
                "health_changed signal exists on Player and is referenced via typed-object+string");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void TypedObject_Property_NotDeadCode()
    {
        var playerScript = @"class_name Player
extends Node

var health: int = 100
";
        var callerScript = @"extends Node

func _ready():
    var player: Player = Player.new()
    var ext = get_node(""/root/Sys"")
    ext.set_prop(player, ""health"")
";
        var tempPath = CreateTempProject(
            ("player.gd", playerScript),
            ("caller.gd", callerScript));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath, o => o.MaxConfidence = GDReferenceConfidence.Potential);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "health",
                "health exists on Player and is referenced via typed-object+string");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void TypedObject_UntypedVariant_StillDeadCode()
    {
        var playerScript = @"class_name Player
extends Node

func take_damage(amount: int) -> void:
    pass
";
        var callerScript = @"extends Node

func _ready():
    var player = get_node(""/root/Player"")
    var ext = get_node(""/root/System"")
    ext.apply_action(player, ""take_damage"")
";
        var tempPath = CreateTempProject(
            ("player.gd", playerScript),
            ("caller.gd", callerScript));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath, o => o.MaxConfidence = GDReferenceConfidence.Potential);
            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "take_damage",
                "take_damage should be dead code when player is untyped (Variant)");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void TypedObject_LocalCall_NotTriggered()
    {
        var playerScript = @"class_name Player
extends Node

func take_damage(amount: int) -> void:
    pass
";
        var callerScript = @"extends Node

func apply_action(player: Player, method: String) -> void:
    print(player, method)

func _ready():
    var p: Player = Player.new()
    apply_action(p, ""take_damage"")
";
        var tempPath = CreateTempProject(
            ("player.gd", playerScript),
            ("caller.gd", callerScript));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath, o => o.MaxConfidence = GDReferenceConfidence.Potential);
            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "take_damage",
                "take_damage should be dead code — apply_action is a LOCAL method, guard should skip");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion

    #region Cross-file const pattern

    [TestMethod]
    public void CrossFileConst_Method_NotDeadCode()
    {
        var globalsScript = @"class_name Globals
extends RefCounted

const METHOD_NAME = ""take_damage""
";
        var playerScript = @"extends Node

func take_damage(amount: int) -> void:
    pass

func _ready():
    var ext = get_node(""/root/System"")
    ext.call_method(self, Globals.METHOD_NAME)
";
        var tempPath = CreateTempProject(
            ("globals.gd", globalsScript),
            ("player.gd", playerScript));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath, o => o.MaxConfidence = GDReferenceConfidence.Potential);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "take_damage",
                "take_damage is referenced via cross-file const Globals.METHOD_NAME");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void CrossFileConst_ExplicitType_NotDeadCode()
    {
        var globalsScript = @"class_name Globals
extends RefCounted

const METHOD: String = ""take_damage""
";
        var playerScript = @"extends Node

func take_damage(amount: int) -> void:
    pass

func _ready():
    var ext = get_node(""/root/System"")
    ext.call_method(self, Globals.METHOD)
";
        var tempPath = CreateTempProject(
            ("globals.gd", globalsScript),
            ("player.gd", playerScript));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath, o => o.MaxConfidence = GDReferenceConfidence.Potential);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "take_damage",
                "take_damage is referenced via cross-file const with explicit type: Globals.METHOD: String");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void CrossFileConst_NonConst_StillDeadCode()
    {
        var globalsScript = @"class_name Globals
extends RefCounted

var METHOD_NAME = ""take_damage""
";
        var playerScript = @"extends Node

func take_damage(amount: int) -> void:
    pass

func _ready():
    var ext = get_node(""/root/System"")
    ext.call_method(self, Globals.METHOD_NAME)
";
        var tempPath = CreateTempProject(
            ("globals.gd", globalsScript),
            ("player.gd", playerScript));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath, o => o.MaxConfidence = GDReferenceConfidence.Potential);
            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "take_damage",
                "take_damage should remain dead code — Globals.METHOD_NAME is not a const");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion

    #region Call-site flow pattern

    [TestMethod]
    public void CallSiteFlow_DirectString_NotDeadCode()
    {
        var registryScript = @"class_name Registry
extends Node

func register(obj, name: String) -> void:
    var ext = get_node(""/root/System"")
    ext.apply(obj, name)
";
        var playerScript = @"extends Node

func take_damage(amount: int) -> void:
    pass

func _ready():
    var reg: Registry = get_node(""/root/Registry"")
    reg.register(self, ""take_damage"")
";
        var tempPath = CreateTempProject(
            ("registry.gd", registryScript),
            ("player.gd", playerScript));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath, o => o.MaxConfidence = GDReferenceConfidence.Potential);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "take_damage",
                "take_damage is referenced via call-site flow: register(self, \"take_damage\") -> ext.apply(obj, name)");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion

    #region Preload Alias Type Resolution

    [TestMethod]
    public void PreloadAlias_Method_NotDeadCode()
    {
        var targetScript = @"extends Control

func take_damage(amount: int) -> void:
    pass
";
        var callerScript = @"extends Node

const Target := preload(""res://target.gd"")

func _ready():
    var t: Target = Target.new()
    t.take_damage(10)
";
        var tempPath = CreateTempProject(
            ("target.gd", targetScript),
            ("caller.gd", callerScript));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "take_damage",
                "take_damage is called via preload alias type annotation");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void PreloadAlias_Property_NotDeadCode()
    {
        var targetScript = @"extends Resource

var health: int = 100
";
        var callerScript = @"extends Node

const Stats := preload(""res://stats.gd"")

func _ready():
    var s: Stats = Stats.new()
    print(s.health)
";
        var tempPath = CreateTempProject(
            ("stats.gd", targetScript),
            ("caller.gd", callerScript));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "health",
                "health is accessed via preload alias type annotation");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void PreloadAlias_Constant_NotDeadCode()
    {
        var targetScript = @"extends Node

const SPEED: int = 100
";
        var callerScript = @"extends Node

const Constants := preload(""res://constants.gd"")

func _ready():
    print(Constants.SPEED)
";
        var tempPath = CreateTempProject(
            ("constants.gd", targetScript),
            ("caller.gd", callerScript));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "SPEED",
                "SPEED is accessed via preload alias Constants.SPEED");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void PreloadAlias_WithClassName_StillWorks()
    {
        var targetScript = @"class_name Player
extends CharacterBody2D

func take_damage(amount: int) -> void:
    pass
";
        var callerScript = @"extends Node

func _ready():
    var p: Player = Player.new()
    p.take_damage(10)
";
        var tempPath = CreateTempProject(
            ("player.gd", targetScript),
            ("caller.gd", callerScript));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "take_damage",
                "take_damage is called via class_name type annotation (regression test)");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void PreloadAlias_NoCaller_StillDeadCode()
    {
        var targetScript = @"extends Node

func unused_method() -> void:
    pass
";
        var callerScript = @"extends Node

const Target := preload(""res://target.gd"")
";
        var tempPath = CreateTempProject(
            ("target.gd", targetScript),
            ("caller.gd", callerScript));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "unused_method",
                "unused_method has no callers even though it's preloaded");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void PreloadAlias_MultipleAliases_AllResolved()
    {
        var targetScript = @"extends Control

func take_damage(amount: int) -> void:
    pass
";
        var callerA = @"extends Node

const TargetA := preload(""res://target.gd"")

func _ready():
    var t: TargetA = TargetA.new()
    t.take_damage(10)
";
        var callerB = @"extends Node

const TargetB := preload(""res://target.gd"")

func _ready():
    var t: TargetB = TargetB.new()
    t.take_damage(20)
";
        var tempPath = CreateTempProject(
            ("target.gd", targetScript),
            ("caller_a.gd", callerA),
            ("caller_b.gd", callerB));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "take_damage",
                "take_damage is called from two files via different preload aliases");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion

    #region RC1 — Built-in name shadow

    [TestMethod]
    public void LocalVar_ShadowingBuiltInName_NotDeadCode()
    {
        var script = @"extends Node

func _ready():
    var array := []
    array.append(1)
    array.append(2)
    print(array)
";
        var tempPath = CreateTempProject(("builtin_shadow.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "array",
                "local 'array' shadows built-in name but is used in the method");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void LocalVar_DictionaryShadow_NotDeadCode()
    {
        var script = @"extends Node

func _ready():
    var dictionary := {}
    dictionary[""key""] = 1
    return dictionary
";
        var tempPath = CreateTempProject(("dict_shadow.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "dictionary",
                "local 'dictionary' shadows built-in name but is used in the method");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void LocalVar_StringShadow_NotDeadCode()
    {
        var script = @"extends Node

func test():
    var string := ""hello""
    string += "" world""
    return string
";
        var tempPath = CreateTempProject(("string_shadow.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "string",
                "local 'string' shadows built-in name but is used (concat + return)");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void LocalVar_ObjectShadow_NotDeadCode()
    {
        var script = @"extends Node

func test():
    var object: Object = null
    if object:
        print(object)
";
        var tempPath = CreateTempProject(("object_shadow.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "object",
                "local 'object' shadows built-in name but is used (condition + print)");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void LocalVar_ColorShadow_NotDeadCode()
    {
        var script = @"extends Node

func test():
    var color := Color.RED
    color = color.lerp(Color.BLUE, 0.5)
    print(color)
";
        var tempPath = CreateTempProject(("color_shadow.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "color",
                "local 'color' shadows built-in type name but is used (lerp + print)");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void LocalVar_SignalShadow_NotDeadCode()
    {
        var script = @"extends Node

func test():
    var signal := ""ready""
    print(signal)
";
        var tempPath = CreateTempProject(("signal_shadow.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "signal",
                "local 'signal' shadows built-in type name but is used (print)");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion

    #region RC2 — Block scope collapse

    [TestMethod]
    public void LocalVar_SameNameDifferentForLoops_NotDeadCode()
    {
        var script = @"extends Node

func _ready():
    var items_a := [""a.gd"", ""b.gd""]
    for item in items_a:
        var possible_script = load(item)
        if possible_script:
            print(possible_script)

    var items_b := [""c.gd"", ""d.gd""]
    for item in items_b:
        var possible_script = load(item)
        if possible_script:
            print(possible_script)
";
        var tempPath = CreateTempProject(("for_scope.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "possible_script",
                "both 'possible_script' variables in different for loops are used");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void LocalVar_SameNameIfElseBranches_NotDeadCode()
    {
        var script = @"extends Node

func test(flag: bool):
    if flag:
        var v_length := 10.0
        print(v_length)
    else:
        var v_length := 20.0
        print(v_length)
";
        var tempPath = CreateTempProject(("if_else_scope.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "v_length",
                "both 'v_length' variables in if/else branches are used");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void LocalVar_SameNameIfElifBranches_NotDeadCode()
    {
        var script = @"extends Node

func test(value: int):
    if value == 1:
        var result := ""one""
        print(result)
    elif value == 2:
        var result := ""two""
        print(result)
";
        var tempPath = CreateTempProject(("if_elif_scope.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "result",
                "both 'result' variables in if/elif branches are used");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void LocalVar_SameNameMatchBranches_NotDeadCode()
    {
        var script = @"extends Node

func test(action: int):
    match action:
        0:
            var ev := ""idle""
            print(ev)
        1:
            var ev := ""walk""
            print(ev)
        2:
            var ev := ""run""
            print(ev)
";
        var tempPath = CreateTempProject(("match_scope.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "ev",
                "all 'ev' variables in different match branches are used");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void LocalVar_SameNameWhileBlocks_NotDeadCode()
    {
        var script = @"extends Node

func test():
    var i := 0
    while i < 3:
        var temp := i * 2
        print(temp)
        i += 1
";
        var tempPath = CreateTempProject(("while_scope.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "temp",
                "'temp' inside while block is used");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion

    #region RC3 — Inner class member identity

    [TestMethod]
    public void InnerClassMember_ReadViaMemberAccess_NotDeadCode()
    {
        var script = @"extends Node

class InnerData:
    var field := 0
    var name := """"

func _ready():
    var obj := InnerData.new()
    obj.field = 42
    print(obj.field)
    print(obj.name)
";
        var tempPath = CreateTempProject(("inner_class.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "field",
                "inner class 'field' is read via member access");
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "name",
                "inner class 'name' is read via member access");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void InnerClassMember_MultipleFields_NotDeadCode()
    {
        var script = @"extends Node

class InnerData:
    var new_events := 0
    var updated_events := 0
    var new_timelines := 0

func _ready():
    var data := InnerData.new()
    data.new_events = 5
    data.updated_events = 3
    data.new_timelines = 10
    print(data.new_events)
    print(data.updated_events)
    print(data.new_timelines)
";
        var tempPath = CreateTempProject(("inner_multi_fields.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "new_events",
                "inner class 'new_events' is written and read via member access");
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "updated_events",
                "inner class 'updated_events' is written and read via member access");
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "new_timelines",
                "inner class 'new_timelines' is written and read via member access");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void InnerClassMember_WriteOnly_StillDeadCode()
    {
        var script = @"extends Node

class InnerData:
    var unused_field := 0

func _ready():
    var obj := InnerData.new()
    obj.unused_field = 42
";
        var tempPath = CreateTempProject(("inner_write_only.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "unused_field",
                "inner class 'unused_field' is only written, never read — should be dead code");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion
}
