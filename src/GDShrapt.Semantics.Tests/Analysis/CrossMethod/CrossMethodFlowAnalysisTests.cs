using FluentAssertions;
using GDShrapt.Reader;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.Analysis.CrossMethod;

[TestClass]
public class CrossMethodFlowAnalysisTests
{
    #region Method Safety Tests

    [TestMethod]
    public void LifecycleMethod_Process_IsSafe()
    {
        var code = @"
extends Node

func _process(delta):
    pass
";
        var model = CreateSemanticModel(code);
        var safety = model.GetMethodOnreadySafety("_process");
        safety.Should().Be(GDMethodOnreadySafety.Safe);
    }

    [TestMethod]
    public void LifecycleMethod_PhysicsProcess_IsSafe()
    {
        var code = @"
extends Node

func _physics_process(delta):
    pass
";
        var model = CreateSemanticModel(code);
        var safety = model.GetMethodOnreadySafety("_physics_process");
        safety.Should().Be(GDMethodOnreadySafety.Safe);
    }

    [TestMethod]
    public void LifecycleMethod_Ready_IsSafe()
    {
        var code = @"
extends Node

func _ready():
    pass
";
        var model = CreateSemanticModel(code);
        var safety = model.GetMethodOnreadySafety("_ready");
        safety.Should().Be(GDMethodOnreadySafety.Safe);
    }

    [TestMethod]
    public void LifecycleMethod_Input_IsSafe()
    {
        var code = @"
extends Node

func _input(event):
    pass
";
        var model = CreateSemanticModel(code);
        var safety = model.GetMethodOnreadySafety("_input");
        safety.Should().Be(GDMethodOnreadySafety.Safe);
    }

    [TestMethod]
    public void LifecycleMethod_Draw_IsSafe()
    {
        var code = @"
extends Node2D

func _draw():
    pass
";
        var model = CreateSemanticModel(code);
        var safety = model.GetMethodOnreadySafety("_draw");
        safety.Should().Be(GDMethodOnreadySafety.Safe);
    }

    [TestMethod]
    public void MethodWithNoCallers_IsUnsafe()
    {
        var code = @"
extends Node

func orphan_method():
    pass
";
        var model = CreateSemanticModel(code);
        var safety = model.GetMethodOnreadySafety("orphan_method");
        safety.Should().Be(GDMethodOnreadySafety.Unsafe);
    }

    [TestMethod]
    public void MethodCalledOnlyFromProcess_IsSafe()
    {
        var code = @"
extends Node

func _process(delta):
    helper()

func helper():
    pass
";
        var model = CreateSemanticModel(code);
        var safety = model.GetMethodOnreadySafety("helper");
        safety.Should().Be(GDMethodOnreadySafety.Safe);
    }

    [TestMethod]
    public void MethodCalledFromMultipleSafeMethods_IsSafe()
    {
        var code = @"
extends Node

func _process(delta):
    helper()

func _input(event):
    helper()

func helper():
    pass
";
        var model = CreateSemanticModel(code);
        var safety = model.GetMethodOnreadySafety("helper");
        safety.Should().Be(GDMethodOnreadySafety.Safe);
    }

    [TestMethod]
    public void MethodCalledFromUnsafeMethod_IsUnsafe()
    {
        var code = @"
extends Node

func unsafe_method():
    shared()

func shared():
    pass
";
        var model = CreateSemanticModel(code);
        var safety = model.GetMethodOnreadySafety("shared");
        safety.Should().Be(GDMethodOnreadySafety.Unsafe);
    }

    [TestMethod]
    public void MethodCalledFromMixedMethods_IsUnsafe()
    {
        var code = @"
extends Node

func _process(delta):
    shared()

func unsafe_method():
    shared()

func shared():
    pass
";
        var model = CreateSemanticModel(code);
        var safety = model.GetMethodOnreadySafety("shared");
        safety.Should().Be(GDMethodOnreadySafety.Unsafe);
    }

    [TestMethod]
    public void TransitiveSafety_ThreeLevels()
    {
        var code = @"
extends Node

func _process(delta):
    level1()

func level1():
    level2()

func level2():
    level3()

func level3():
    pass
";
        var model = CreateSemanticModel(code);

        model.GetMethodOnreadySafety("level1").Should().Be(GDMethodOnreadySafety.Safe);
        model.GetMethodOnreadySafety("level2").Should().Be(GDMethodOnreadySafety.Safe);
        model.GetMethodOnreadySafety("level3").Should().Be(GDMethodOnreadySafety.Safe);
    }

    [TestMethod]
    public void RecursiveMethod_CalledFromSafe_IsSafe()
    {
        var code = @"
extends Node

func _process(delta):
    recursive(5)

func recursive(n):
    if n > 0:
        recursive(n - 1)
";
        var model = CreateSemanticModel(code);
        var safety = model.GetMethodOnreadySafety("recursive");
        safety.Should().Be(GDMethodOnreadySafety.Safe);
    }

    #endregion

    #region Assignment Path Analysis Tests

    [TestMethod]
    public void UnconditionalAssignment_InReady_IsGuaranteed()
    {
        var code = @"
extends Node
var data

func _ready():
    data = {}
";
        var model = CreateSemanticModel(code);
        var state = model.GetCrossMethodFlowState();

        state.Should().NotBeNull();
        state!.GuaranteedAfterReady.Should().Contain("data");
        state.MayBeNullAfterReady.Should().NotContain("data");
    }

    [TestMethod]
    public void ConditionalAssignment_InIfWithoutElse_MayBeNull()
    {
        var code = @"
extends Node
var data
var condition = true

func _ready():
    if condition:
        data = {}
";
        var model = CreateSemanticModel(code);
        var state = model.GetCrossMethodFlowState();

        state.Should().NotBeNull();
        state!.MayBeNullAfterReady.Should().Contain("data");
    }

    [TestMethod]
    public void ConditionalAssignment_InIfWithElse_BothBranches_IsGuaranteed()
    {
        var code = @"
extends Node
var data
var condition = true

func _ready():
    if condition:
        data = {}
    else:
        data = []
";
        var model = CreateSemanticModel(code);
        var state = model.GetCrossMethodFlowState();

        state.Should().NotBeNull();
        state!.GuaranteedAfterReady.Should().Contain("data");
        state.MayBeNullAfterReady.Should().NotContain("data");
    }

    [TestMethod]
    public void OnreadyVariable_IsAlwaysGuaranteed()
    {
        var code = @"
extends Node

@onready var label = $Label
";
        var model = CreateSemanticModel(code);
        var state = model.GetCrossMethodFlowState();

        state.Should().NotBeNull();
        state!.GuaranteedAfterReady.Should().Contain("label");
    }

    [TestMethod]
    public void MultipleVariables_MixedInitialization()
    {
        var code = @"
extends Node
var a
var b
var condition = true

func _ready():
    a = 1
    if condition:
        b = 2
";
        var model = CreateSemanticModel(code);
        var state = model.GetCrossMethodFlowState();

        state.Should().NotBeNull();
        state!.GuaranteedAfterReady.Should().Contain("a");
        state!.MayBeNullAfterReady.Should().Contain("b");
    }

    #endregion

    #region Method Flow Summary Tests

    [TestMethod]
    public void MethodFlowSummary_CalledMethods_Tracked()
    {
        var code = @"
extends Node

func caller():
    callee1()
    callee2()

func callee1():
    pass

func callee2():
    pass
";
        var model = CreateSemanticModel(code);
        var summary = model.GetMethodFlowSummary("caller");

        summary.Should().NotBeNull();
        summary!.CalledMethods.Should().Contain("callee1");
        summary.CalledMethods.Should().Contain("callee2");
    }

    [TestMethod]
    public void MethodFlowSummary_SelfMethodCall_Tracked()
    {
        var code = @"
extends Node

func caller():
    self.callee()

func callee():
    pass
";
        var model = CreateSemanticModel(code);
        var summary = model.GetMethodFlowSummary("caller");

        summary.Should().NotBeNull();
        summary!.CalledMethods.Should().Contain("callee");
    }

    #endregion

    #region Call Graph Tests

    [TestMethod]
    public void CallGraph_ContainsCallerGraph()
    {
        var code = @"
extends Node

func _process(delta):
    helper()

func helper():
    pass
";
        var model = CreateSemanticModel(code);
        var state = model.GetCrossMethodFlowState();

        state.Should().NotBeNull();
        state!.CallerGraph.Should().ContainKey("helper");
        state.CallerGraph["helper"].Should().Contain("_process");
    }

    [TestMethod]
    public void CallGraph_ContainsCallGraph()
    {
        var code = @"
extends Node

func _process(delta):
    helper()

func helper():
    pass
";
        var model = CreateSemanticModel(code);
        var state = model.GetCrossMethodFlowState();

        state.Should().NotBeNull();
        state!.CallGraph.Should().ContainKey("_process");
        state.CallGraph["_process"].Should().Contain("helper");
    }

    #endregion

    #region Query API Tests

    [TestMethod]
    public void HasConditionalReadyInitialization_True_WhenConditional()
    {
        var code = @"
extends Node
var data
var condition = true

func _ready():
    if condition:
        data = {}
";
        var model = CreateSemanticModel(code);
        model.HasConditionalReadyInitialization("data").Should().BeTrue();
    }

    [TestMethod]
    public void HasConditionalReadyInitialization_False_WhenUnconditional()
    {
        var code = @"
extends Node
var data

func _ready():
    data = {}
";
        var model = CreateSemanticModel(code);
        model.HasConditionalReadyInitialization("data").Should().BeFalse();
    }

    [TestMethod]
    public void IsVariableSafeAtMethod_True_ForSafeMethod()
    {
        var code = @"
extends Node

@onready var label = $Label

func _process(delta):
    helper()

func helper():
    pass
";
        var model = CreateSemanticModel(code);
        model.IsVariableSafeAtMethod("label", "helper").Should().BeTrue();
    }

    [TestMethod]
    public void IsVariableSafeAtMethod_False_ForUnsafeMethod()
    {
        var code = @"
extends Node

@onready var label = $Label

func unsafe_method():
    pass
";
        var model = CreateSemanticModel(code);
        model.IsVariableSafeAtMethod("label", "unsafe_method").Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static GDSemanticModel CreateSemanticModel(string code)
    {
        var reference = new GDScriptReference("test://virtual/test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        scriptFile.Analyze();
        return scriptFile.SemanticModel!;
    }

    #endregion
}
