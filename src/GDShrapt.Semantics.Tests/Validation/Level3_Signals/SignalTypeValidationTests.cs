using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level3_Signals;

/// <summary>
/// Level 3: Signal type validation tests.
/// Tests validate that emit_signal argument types match signal parameter types.
/// </summary>
[TestClass]
public class SignalTypeValidationTests
{
    #region Valid Signal Emissions - No Diagnostics Expected

    [TestMethod]
    public void EmitSignal_CorrectTypes_NoDiagnostic()
    {
        var code = @"
signal value_changed(value: int)

func test():
    emit_signal(""value_changed"", 42)
";
        var diagnostics = ValidateCode(code);
        var signalDiagnostics = FilterSignalDiagnostics(diagnostics);
        Assert.AreEqual(0, signalDiagnostics.Count,
            $"emit_signal with correct types should have no errors. Found: {FormatDiagnostics(signalDiagnostics)}");
    }

    [TestMethod]
    public void EmitSignal_IntToFloat_NoDiagnostic()
    {
        // int is compatible with float (widening)
        var code = @"
signal value_changed(value: float)

func test():
    emit_signal(""value_changed"", 42)
";
        var diagnostics = ValidateCode(code);
        var signalDiagnostics = FilterSignalDiagnostics(diagnostics);
        Assert.AreEqual(0, signalDiagnostics.Count,
            $"int -> float should be compatible. Found: {FormatDiagnostics(signalDiagnostics)}");
    }

    [TestMethod]
    public void EmitSignal_VariantParameter_AcceptsAll()
    {
        var code = @"
signal data_received(data)

func test():
    emit_signal(""data_received"", 42)
    emit_signal(""data_received"", ""string"")
    emit_signal(""data_received"", [1, 2, 3])
";
        var diagnostics = ValidateCode(code);
        var signalDiagnostics = FilterSignalDiagnostics(diagnostics);
        Assert.AreEqual(0, signalDiagnostics.Count,
            $"Variant parameter should accept any type. Found: {FormatDiagnostics(signalDiagnostics)}");
    }

    [TestMethod]
    public void EmitSignal_MultipleParameters_AllCorrect_NoDiagnostic()
    {
        var code = @"
signal game_event(event_name: String, value: int, active: bool)

func test():
    emit_signal(""game_event"", ""player_score"", 100, true)
";
        var diagnostics = ValidateCode(code);
        var signalDiagnostics = FilterSignalDiagnostics(diagnostics);
        Assert.AreEqual(0, signalDiagnostics.Count,
            $"All correct types should have no errors. Found: {FormatDiagnostics(signalDiagnostics)}");
    }

    [TestMethod]
    public void EmitSignal_NullToReference_NoDiagnostic()
    {
        var code = @"
signal node_changed(node: Node)

func test():
    emit_signal(""node_changed"", null)
";
        var diagnostics = ValidateCode(code);
        var signalDiagnostics = FilterSignalDiagnostics(diagnostics);
        Assert.AreEqual(0, signalDiagnostics.Count,
            $"null should be compatible with reference types. Found: {FormatDiagnostics(signalDiagnostics)}");
    }

    #endregion

    #region Invalid Signal Emissions - Type Mismatch Expected

    [TestMethod]
    public void EmitSignal_StringToInt_ReportsTypeMismatch()
    {
        var code = @"
signal value_changed(value: int)

func test():
    emit_signal(""value_changed"", ""not a number"")
";
        var diagnostics = ValidateCode(code);
        var signalDiagnostics = FilterSignalDiagnostics(diagnostics);

        Assert.IsTrue(signalDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.EmitSignalTypeMismatch),
            $"Expected EmitSignalTypeMismatch for String -> int. Found: {FormatDiagnostics(signalDiagnostics)}");
    }

    [TestMethod]
    public void EmitSignal_ArrayToInt_ReportsTypeMismatch()
    {
        var code = @"
signal score_updated(score: int)

func test():
    emit_signal(""score_updated"", [1, 2, 3])
";
        var diagnostics = ValidateCode(code);
        var signalDiagnostics = FilterSignalDiagnostics(diagnostics);

        Assert.IsTrue(signalDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.EmitSignalTypeMismatch),
            $"Expected EmitSignalTypeMismatch for Array -> int. Found: {FormatDiagnostics(signalDiagnostics)}");
    }

    [TestMethod]
    public void EmitSignal_FloatToInt_ReportsTypeMismatch()
    {
        // float is NOT compatible with int (narrowing conversion)
        var code = @"
signal level_changed(level: int)

func test():
    emit_signal(""level_changed"", 3.14)
";
        var diagnostics = ValidateCode(code);
        var signalDiagnostics = FilterSignalDiagnostics(diagnostics);

        Assert.IsTrue(signalDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.EmitSignalTypeMismatch),
            $"Expected EmitSignalTypeMismatch for float -> int. Found: {FormatDiagnostics(signalDiagnostics)}");
    }

    [TestMethod]
    public void EmitSignal_SecondParameterMismatch_ReportsTypeMismatch()
    {
        var code = @"
signal player_action(action: String, value: int)

func test():
    emit_signal(""player_action"", ""jump"", ""not an int"")
";
        var diagnostics = ValidateCode(code);
        var signalDiagnostics = FilterSignalDiagnostics(diagnostics);

        Assert.IsTrue(signalDiagnostics.Any(d =>
            d.Code == GDDiagnosticCode.EmitSignalTypeMismatch),
            $"Expected EmitSignalTypeMismatch for second parameter. Found: {FormatDiagnostics(signalDiagnostics)}");
    }

    #endregion

    #region Dynamic Signal Names - Should Not Validate

    [TestMethod]
    public void EmitSignal_DynamicSignalName_NoDiagnostic()
    {
        // Cannot validate when signal name is dynamic
        var code = @"
signal value_changed(value: int)

func test(signal_name: String):
    emit_signal(signal_name, ""could be anything"")
";
        var diagnostics = ValidateCode(code);
        var signalDiagnostics = FilterSignalDiagnostics(diagnostics);
        Assert.AreEqual(0, signalDiagnostics.Count,
            $"Dynamic signal names should not be validated. Found: {FormatDiagnostics(signalDiagnostics)}");
    }

    #endregion

    #region P7: Signal.emit() Direct Call

    [TestMethod]
    public void P7_SignalEmit_DirectCall_IsRecognized()
    {
        // P7: signal.emit() should work without GD7003
        var code = @"
signal health_changed(old_value: int, new_value: int)

var _health: int

var tracked_health: int:
    set(value):
        var old = _health
        _health = value
        health_changed.emit(old, _health)
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.UnguardedMethodAccess ||
            d.Code == GDDiagnosticCode.UnguardedMethodCall).ToList();

        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"signal.emit() should be recognized without GD7003. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void P7_SignalConnect_DirectCall_IsRecognized()
    {
        // signal.connect() should also work
        var code = @"
signal health_changed(old_value: int, new_value: int)

func _ready():
    health_changed.connect(_on_health_changed)

func _on_health_changed(old_value: int, new_value: int):
    pass
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.UnguardedMethodAccess ||
            d.Code == GDDiagnosticCode.UnguardedMethodCall).ToList();

        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"signal.connect() should be recognized. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void P7_SignalIdentifier_HasSignalType()
    {
        // When accessing signal by identifier, it should have Signal type
        var code = @"
signal my_signal(value: int)

func test():
    var s = my_signal
";
        var diagnostics = ValidateCode(code);
        // No type errors expected
        var typeErrors = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.TypeMismatch).ToList();

        Assert.AreEqual(0, typeErrors.Count,
            $"Signal identifier should have valid type. Found: {FormatDiagnostics(typeErrors)}");
    }

    #endregion

    #region Helper Methods

    private static IEnumerable<GDDiagnostic> ValidateCode(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        if (classDecl == null)
            return Enumerable.Empty<GDDiagnostic>();

        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = GDDefaultRuntimeProvider.Instance;
        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        var semanticModel = collector.BuildSemanticModel();

        var options = new GDSemanticValidatorOptions
        {
            CheckTypes = true,
            CheckMemberAccess = true,
            CheckArgumentTypes = true,
            CheckIndexers = true,
            CheckSignalTypes = true
        };
        var validator = new GDSemanticValidator(semanticModel, options);
        var result = validator.Validate(classDecl);

        return result.Diagnostics;
    }

    private static List<GDDiagnostic> FilterSignalDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.EmitSignalTypeMismatch ||
            d.Code == GDDiagnosticCode.ConnectCallbackTypeMismatch).ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
