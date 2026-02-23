using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for dead code analysis false positive patterns:
/// 1. Callable(obj, dict["method"]) — method names from dictionaries
/// 2. emit_signal(variable) — dynamic signal emission via parameters
/// 3. set_X/is_X — setter/getter pair recognition
/// </summary>
[TestClass]
public class GDDeadCodeDynamicDispatchTests
{
    private static string CreateTempProject(params (string name, string content)[] scripts)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "gdshrapt_dispatch_test_" + Guid.NewGuid().ToString("N"));
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

    #region Callable from dictionaries

    [TestMethod]
    public void CallableFromDictionary_SelfDispatch_NotStrictDeadCode()
    {
        var script = @"extends Node

var registry := [
    {""method"": ""do_action""},
    {""method"": ""do_other""},
]

func _ready():
    for item in registry:
        Callable(self, item[""method""]).call()

func do_action():
    pass

func do_other():
    pass
";
        var tempPath = CreateTempProject(("caller.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "do_action" && i.Confidence == GDReferenceConfidence.Strict,
                "do_action is called via Callable(self, dict[\"method\"]) dispatch");

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "do_other" && i.Confidence == GDReferenceConfidence.Strict,
                "do_other is called via Callable(self, dict[\"method\"]) dispatch");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void CallableFromDictionary_CrossFile_NotStrictDeadCode()
    {
        var indexScript = @"extends Node
class_name EffectIndex

func _get_effects() -> Array:
    return [
        {""command"": ""speed"", ""method"": ""effect_speed""},
        {""command"": ""pause"", ""method"": ""effect_pause""}
    ]
";
        var subsystemScript = @"extends Node

var effect_list := []

func collect_effects():
    var idx = EffectIndex.new()
    for effect in idx._get_effects():
        var cb = Callable(self, effect[""method""])
        effect_list.append(cb)

func run_effects():
    for effect in effect_list:
        effect.call()

func effect_speed():
    pass

func effect_pause():
    pass
";
        var tempPath = CreateTempProject(
            ("index.gd", indexScript),
            ("subsystem.gd", subsystemScript));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "effect_speed" && i.Confidence == GDReferenceConfidence.Strict,
                "effect_speed is dispatched via Callable from dictionary returned by cross-file method");

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "effect_pause" && i.Confidence == GDReferenceConfidence.Strict,
                "effect_pause is dispatched via Callable from dictionary returned by cross-file method");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region Dynamic emit_signal

    [TestMethod]
    public void DynamicEmitSignal_ViaParameter_NotStrictDeadCode()
    {
        var script = @"extends Node

signal meta_clicked(meta: Variant)

func emit_meta_signal(meta: Variant, sig: String) -> void:
    emit_signal(sig, meta)

func _ready():
    emit_meta_signal(""data"", ""meta_clicked"")
";
        var tempPath = CreateTempProject(("emitter.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Signal && i.Name == "meta_clicked" && i.Confidence == GDReferenceConfidence.Strict,
                "meta_clicked is emitted dynamically via emit_signal(sig) where sig='meta_clicked' is passed as argument");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void DynamicEmitSignal_DirectLiteral_NotDeadCode()
    {
        var script = @"extends Node

signal data_ready

func _process(delta):
    emit_signal(""data_ready"")
";
        var tempPath = CreateTempProject(("emitter2.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Signal && i.Name == "data_ready",
                "data_ready is emitted via emit_signal(\"data_ready\") literal");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void DynamicEmitSignal_ViaBindPattern_NotStrictDeadCode()
    {
        var script = @"extends Node

signal meta_clicked(meta: Variant)

func emit_meta_signal(meta: Variant, sig: String) -> void:
    emit_signal(sig, meta)

func connect_meta_signals(text_node: Node) -> void:
    text_node.meta_clicked.connect(emit_meta_signal.bind(""meta_clicked""))
";
        var tempPath = CreateTempProject(("emitter_bind.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Signal && i.Name == "meta_clicked" && i.Confidence == GDReferenceConfidence.Strict,
                "meta_clicked is emitted via emit_signal(sig) where sig='meta_clicked' is bound via .bind()");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region Setter/getter pairs

    [TestMethod]
    public void SetterWithUsedGetter_NotStrictDeadCode()
    {
        var providerScript = @"extends Node
class_name MyProvider

var _flag := false

func set_flag(value: bool) -> void:
    _flag = value

func is_flag() -> bool:
    return _flag
";
        var consumerScript = @"extends Node

func _ready():
    var p = MyProvider.new()
    if p.is_flag():
        pass
";
        var tempPath = CreateTempProject(
            ("provider.gd", providerScript),
            ("consumer.gd", consumerScript));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "set_flag" && i.Confidence == GDReferenceConfidence.Strict,
                "set_flag is a setter whose getter is_flag() is called — should not be Strict dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void SetterWithoutGetter_RemainsStrictDeadCode()
    {
        var script = @"extends Node

func set_orphan(value: bool) -> void:
    pass
";
        var tempPath = CreateTempProject(("orphan.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "set_orphan" && i.Confidence == GDReferenceConfidence.Strict,
                "set_orphan has no corresponding getter — remains Strict dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region Property accessor declarations (var x: set = func_name)

    [TestMethod]
    public void PropertySetter_NotDeadCode()
    {
        var script = @"extends Node

var speed: float = 1.0:
    set = set_speed

func set_speed(value: float) -> void:
    speed = value
";
        var tempPath = CreateTempProject(("prop_setter.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "set_speed",
                "set_speed is a property setter accessor and should not be dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void PropertyGetter_NotDeadCode()
    {
        var script = @"extends Node

var health: int = 100:
    get = get_health

func get_health() -> int:
    return health
";
        var tempPath = CreateTempProject(("prop_getter.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "get_health",
                "get_health is a property getter accessor and should not be dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void PropertySetterAndGetter_NotDeadCode()
    {
        var script = @"extends Node

var position: Vector2 = Vector2.ZERO:
    set = set_position,
    get = get_position

func set_position(value: Vector2) -> void:
    position = value

func get_position() -> Vector2:
    return position
";
        var tempPath = CreateTempProject(("prop_both.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "set_position",
                "set_position is a property setter accessor and should not be dead code");

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "get_position",
                "get_position is a property getter accessor and should not be dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region @warning_ignore("unused_signal")

    [TestMethod]
    public void SignalWithWarningIgnore_NotDeadCode()
    {
        var script = @"extends Node

@warning_ignore(""unused_signal"")
signal my_event
";
        var tempPath = CreateTempProject(("annotated_signal.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Signal && i.Name == "my_event",
                "my_event has @warning_ignore(\"unused_signal\") and should not be dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void SignalWithoutWarningIgnore_StillDeadCode()
    {
        var script = @"extends Node

signal my_event
";
        var tempPath = CreateTempProject(("bare_signal.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Signal && i.Name == "my_event",
                "my_event has no @warning_ignore and should remain dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region Expression.execute() dispatch

    [TestMethod]
    public void ExpressionExecute_DirectCondition_NotDeadCode()
    {
        var script = @"extends Node

func _ready():
    var expr = Expression.new()
    expr.parse(""my_check()"")
    expr.execute([], self)

func my_check() -> bool:
    return true
";
        var tempPath = CreateTempProject(("expr_direct.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "my_check",
                "my_check is called via Expression.execute() with parsed string literal");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void ExpressionExecute_StringReplace_NotDeadCode()
    {
        var script = @"extends Node

func execute_expr(code: String):
    code = code.replace(""range("", ""d_range("")
    var expr = Expression.new()
    expr.parse(code)
    expr.execute([], self)

func d_range(a, b = null) -> Array:
    if b == null:
        return range(a)
    return range(a, b)
";
        var tempPath = CreateTempProject(("expr_replace.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "d_range",
                "d_range is extracted from string.replace() chain before Expression.execute()");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void ExpressionExecute_ConditionInStringLiteral_NotDeadCode()
    {
        var script = @"extends Node

var config = {""check"": ""is_valid() and count > 0""}

func is_valid() -> bool:
    return true
";
        var tempPath = CreateTempProject(("expr_string_literal.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "is_valid",
                "is_valid appears in a string literal that parses as GDScript containing a call");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region Godot string callbacks (call/call_deferred/callv)

    [TestMethod]
    public void CallWithStringMethod_NotDeadCode()
    {
        var script = @"extends Node

func _ready():
    call(""my_method"")

func my_method() -> void:
    pass
";
        var tempPath = CreateTempProject(("call_string.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "my_method",
                "my_method is called via call(\"my_method\") and should not be dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void CallDeferredWithStringMethod_NotDeadCode()
    {
        var script = @"extends Node

func _ready():
    call_deferred(""deferred_action"")

func deferred_action() -> void:
    pass
";
        var tempPath = CreateTempProject(("call_deferred.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "deferred_action",
                "deferred_action is called via call_deferred(\"deferred_action\") and should not be dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region Cross-file subclass calls

    [TestMethod]
    public void SubclassCallsInheritedMethod_NotDeadCode()
    {
        var parentScript = @"extends Node

func setup_shader() -> void:
    pass
";
        var childScript = @"extends ""res://parent.gd""

func _ready():
    setup_shader()
";
        var tempPath = CreateTempProject(
            ("parent.gd", parentScript),
            ("child.gd", childScript));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "setup_shader",
                "setup_shader is called from child class that extends parent");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region VNR bug — variable read via subscript

    [TestMethod]
    public void VariableReadViaSubscript_NotVNR()
    {
        var script = @"extends Node

var cache := {}

func fill():
    cache = {""a"": 1, ""b"": 2}

func get_item(key: String):
    return cache[key]
";
        var tempPath = CreateTempProject(("vnr_subscript.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "cache" && i.ReasonCode == GDDeadCodeReasonCode.VNR,
                "cache is written in fill() and read via subscript cache[key] in get_item()");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void VariableWrittenThenReadInSameClass_NotVNR()
    {
        var script = @"extends Node

var last_info := {}

func store():
    last_info = {""key"": ""value""}

func retrieve():
    var data = last_info[""key""]
    return data
";
        var tempPath = CreateTempProject(("vnr_read.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "last_info" && i.ReasonCode == GDDeadCodeReasonCode.VNR,
                "last_info is written then read via subscript in another method");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region Unreachable multi-line return

    [TestMethod]
    public void MultiLineReturn_NotUnreachable()
    {
        var script = @"extends Node

func get_path_key(a: String, b: String) -> String:
    return (
        ""prefix""
            .path_join(a)
            .path_join(b)
    )
";
        var tempPath = CreateTempProject(("multiline_return.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.ReasonCode == GDDeadCodeReasonCode.UCR &&
                     i.FilePath.Contains("multiline_return"),
                "Continuation lines of a multi-line return should not be flagged as unreachable");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region Negative tests (true positives remain)

    [TestMethod]
    public void TrulyUnusedFunction_RemainsStrictDeadCode()
    {
        var script = @"extends Node

func truly_unused():
    pass

func used_func():
    pass

func _ready():
    used_func()
";
        var tempPath = CreateTempProject(("genuinely_dead.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "truly_unused" && i.Confidence == GDReferenceConfidence.Strict,
                "truly_unused has no callers and no dynamic dispatch — remains Strict dead code");

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "used_func",
                "used_func is called from _ready and should not appear in dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region FP-8: Single-line match case function calls

    [TestMethod]
    public void MatchCaseSingleLine_FunctionCallTracked()
    {
        var script = @"extends Node

func prep_value(x):
    return x * 2

func _ready():
    var code = 1
    match code:
        0: prep_value(10)
        1: prep_value(20)
";

        var tempPath = CreateTempProject(("test", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "prep_value",
                "prep_value is called in single-line match case body and should not be dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion

    #region FP-9: StringName callback via TypesMap

    [TestMethod]
    public void StringNameCallback_MethodNotDeadCode()
    {
        var script = @"extends Node

func my_callback(result):
    pass

func _ready():
    call_deferred('my_callback')
";

        var tempPath = CreateTempProject(("test", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "my_callback",
                "my_callback is passed as StringName callback to call_deferred and should not be dead code");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion
}
