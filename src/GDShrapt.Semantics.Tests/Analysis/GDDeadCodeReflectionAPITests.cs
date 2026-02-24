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
}
