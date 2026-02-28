using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level5_Nullable;

/// <summary>
/// Tests for _init() constructor-initialized variables nullable access validation.
/// Variables initialized in _init() should NOT produce GD7005/GD7007 warnings
/// because _init() is a constructor — guaranteed to complete before any method call.
/// </summary>
[TestClass]
public class InitConstructorNullableTests
{
    #region Should NOT Report - _init() guarantees initialization

    [TestMethod]
    public void InitInitialized_InAnyMethod_NoWarning()
    {
        var code = @"
extends RefCounted

var data

func _init():
    data = {}

func foo():
    data.clear()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Variable initialized in _init() should not report null in any method. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void InitInitialized_MultipleVars_NoWarning()
    {
        var code = @"
extends RefCounted

var controls
var camera
var arena

func _init(_controls, _camera, _arena):
    controls = _controls
    camera = _camera
    arena = _arena

func process():
    controls.update()
    camera.move()
    arena.reset()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Multiple variables initialized in _init() should not report null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void InitInitialized_TypedParams_NoWarning()
    {
        var code = @"
extends RefCounted

var controls: Resource
var camera: Resource

func _init(_controls: Resource, _camera: Resource):
    controls = _controls
    camera = _camera

func process():
    controls.get_path()
    camera.get_path()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Typed variables initialized in _init() should not report null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void InitInitialized_InReadyMethod_NoWarning()
    {
        var code = @"
extends Node

var data

func _init():
    data = {}

func _ready():
    data.clear()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Variable initialized in _init() should not report null in _ready(). Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void InitInitialized_InProcessMethod_NoWarning()
    {
        var code = @"
extends Node

var data

func _init():
    data = {}

func _process(delta):
    data.size()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Variable initialized in _init() should not report null in _process(). Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void InitInitialized_ChainedAccess_NoWarning()
    {
        var code = @"
extends RefCounted

var service

func _init():
    service = RefCounted.new()

func test():
    service.get_reference_count()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Chained access on _init()-initialized variable should not report null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void InitInitialized_NodeClass_NoWarning()
    {
        var code = @"
extends Node

var config

func _init():
    config = {}

func do_stuff():
    config.clear()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Variable initialized in _init() on Node class should not report null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void InitInitialized_ServicePattern_NoWarning()
    {
        var code = @"
class_name MyControlService
extends RefCounted

var controls: Resource
var camera: Resource
var participant: Resource
var arena: Resource
var input_service: RefCounted

func _init(_controls: Resource, _camera: Resource, _participant: Resource, _arena: Resource, _input: RefCounted):
    controls = _controls
    camera = _camera
    participant = _participant
    arena = _arena
    input_service = _input

func physics_process(delta):
    input_service.get_reference_count()
    controls.get_path()

func handle_input(event):
    input_service.get_reference_count()

func set_visibility(v: bool):
    controls.get_path()
    arena.get_path()

func select():
    participant.get_path()
    camera.get_path()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.AreEqual(0, nullDiagnostics.Count,
            $"Full service pattern with _init() should not report null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    #endregion

    #region Should Report - not guaranteed by _init()

    [TestMethod]
    public void InitInitialized_Conditional_ShouldWarn()
    {
        var code = @"
extends RefCounted

var data

func _init():
    if true:
        data = {}

func foo():
    data.clear()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.IsTrue(nullDiagnostics.Any(),
            "Variable conditionally initialized in _init() should report null warning");
    }

    [TestMethod]
    public void NotInitialized_NoInit_ShouldWarn()
    {
        var code = @"
extends RefCounted

var data

func foo():
    data.clear()
";
        var diagnostics = ValidateCode(code);
        var nullDiagnostics = FilterNullableDiagnostics(diagnostics);
        Assert.IsTrue(nullDiagnostics.Any(),
            "Variable not initialized at all should report null warning");
    }

    #endregion

    #region Helper Methods

    private static IEnumerable<GDDiagnostic> ValidateCode(string code)
    {
        var options = new GDSemanticValidatorOptions
        {
            CheckTypes = true,
            CheckMemberAccess = true,
            CheckNullableAccess = true,
            NullableAccessSeverity = GDDiagnosticSeverity.Warning
        };
        return ValidateCodeWithOptions(code, options);
    }

    private static IEnumerable<GDDiagnostic> ValidateCodeWithOptions(string code, GDSemanticValidatorOptions options)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        if (classDecl == null)
            return Enumerable.Empty<GDDiagnostic>();

        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null, null, null);
        scriptFile.Analyze(runtimeProvider);
        var semanticModel = scriptFile.SemanticModel!;

        var validator = new GDSemanticValidator(semanticModel, options);
        var result = validator.Validate(classDecl);

        return result.Diagnostics;
    }

    private static List<GDDiagnostic> FilterNullableDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.PotentiallyNullAccess ||
            d.Code == GDDiagnosticCode.PotentiallyNullIndexer ||
            d.Code == GDDiagnosticCode.PotentiallyNullMethodCall ||
            d.Code == GDDiagnosticCode.ClassVariableMayBeNull ||
            d.Code == GDDiagnosticCode.NullableTypeNotChecked).ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
