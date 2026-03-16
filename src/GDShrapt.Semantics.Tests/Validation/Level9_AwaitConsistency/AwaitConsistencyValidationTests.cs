using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level9_AwaitConsistency;

[TestClass]
public class AwaitConsistencyValidationTests
{
    #region Coroutine Call Without Await - Should Report

    [TestMethod]
    public void CoroutineCallWithoutAwait_ReportsGD5011()
    {
        var code = @"
func _ready():
    do_async_thing()

func do_async_thing():
    await get_tree().create_timer(1.0).timeout
";
        var diagnostics = ValidateCode(code);
        var awaitDiagnostics = FilterAwaitDiagnostics(diagnostics);
        Assert.IsTrue(awaitDiagnostics.Any(d => d.Code == GDDiagnosticCode.PossibleMissedAwait),
            $"Expected GD5011 for unawaited coroutine call. Found: {FormatDiagnostics(awaitDiagnostics)}");
    }

    [TestMethod]
    public void CoroutineCallWithoutAwait_MessageContainsMethodName()
    {
        var code = @"
func _ready():
    do_async_thing()

func do_async_thing():
    await get_tree().create_timer(1.0).timeout
";
        var diagnostics = ValidateCode(code);
        var awaitDiag = diagnostics.FirstOrDefault(d => d.Code == GDDiagnosticCode.PossibleMissedAwait);
        Assert.IsNotNull(awaitDiag, "Should report GD5011");
        Assert.IsTrue(awaitDiag.Message.Contains("do_async_thing"),
            $"Message should mention method name. Got: {awaitDiag.Message}");
    }

    #endregion

    #region Coroutine Call With Await - Should NOT Report

    [TestMethod]
    public void CoroutineCallWithAwait_NoDiagnostic()
    {
        var code = @"
func _ready():
    await do_async_thing()

func do_async_thing():
    await get_tree().create_timer(1.0).timeout
";
        var diagnostics = ValidateCode(code);
        var awaitDiagnostics = FilterAwaitDiagnostics(diagnostics);
        Assert.AreEqual(0, awaitDiagnostics.Count,
            $"Awaited coroutine should not trigger warning. Found: {FormatDiagnostics(awaitDiagnostics)}");
    }

    #endregion

    #region Non-Coroutine Call - Should NOT Report

    [TestMethod]
    public void NonCoroutineCall_NoDiagnostic()
    {
        var code = @"
func _ready():
    do_regular_thing()

func do_regular_thing():
    print(""hello"")
";
        var diagnostics = ValidateCode(code);
        var awaitDiagnostics = FilterAwaitDiagnostics(diagnostics);
        Assert.AreEqual(0, awaitDiagnostics.Count,
            $"Non-coroutine call should not trigger warning. Found: {FormatDiagnostics(awaitDiagnostics)}");
    }

    #endregion

    #region Self Call Without Await - Should Report

    [TestMethod]
    public void SelfCoroutineCallWithoutAwait_ReportsGD5011()
    {
        var code = @"
func _ready():
    self.do_async_thing()

func do_async_thing():
    await get_tree().create_timer(1.0).timeout
";
        var diagnostics = ValidateCode(code);
        var awaitDiagnostics = FilterAwaitDiagnostics(diagnostics);
        Assert.IsTrue(awaitDiagnostics.Any(d => d.Code == GDDiagnosticCode.PossibleMissedAwait),
            $"Expected GD5011 for self.coroutine(). Found: {FormatDiagnostics(awaitDiagnostics)}");
    }

    #endregion

    #region External Object Call - Should NOT Report (no cross-type inference)

    [TestMethod]
    public void ExternalObjectCall_NoDiagnostic()
    {
        var code = @"
func _ready():
    var obj: Node = Node.new()
    obj.some_method()
";
        var diagnostics = ValidateCode(code);
        var awaitDiagnostics = FilterAwaitDiagnostics(diagnostics);
        Assert.AreEqual(0, awaitDiagnostics.Count,
            $"External object call should not trigger warning. Found: {FormatDiagnostics(awaitDiagnostics)}");
    }

    #endregion

    #region Multiple Coroutines - Mixed Await Usage

    [TestMethod]
    public void MultipleCoroutines_OnlyUnawaited_ReportsGD5011()
    {
        var code = @"
func _ready():
    await async_a()
    async_b()

func async_a():
    await get_tree().create_timer(1.0).timeout

func async_b():
    await get_tree().create_timer(2.0).timeout
";
        var diagnostics = ValidateCode(code);
        var awaitDiagnostics = FilterAwaitDiagnostics(diagnostics);
        Assert.AreEqual(1, awaitDiagnostics.Count,
            $"Only async_b() should trigger warning. Found: {FormatDiagnostics(awaitDiagnostics)}");
        Assert.IsTrue(awaitDiagnostics[0].Message.Contains("async_b"),
            $"Warning should be for async_b. Got: {awaitDiagnostics[0].Message}");
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

        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null, null, null);
        scriptFile.Analyze(runtimeProvider);
        var semanticModel = scriptFile.SemanticModel!;

        var options = new GDSemanticValidatorOptions
        {
            CheckTypes = false,
            CheckMemberAccess = false,
            CheckArgumentTypes = false,
            CheckIndexers = false,
            CheckSignalTypes = false,
            CheckGenericTypes = false,
            CheckNullableAccess = false,
            CheckRedundantGuards = false,
            CheckDynamicCalls = false,
            CheckNodePaths = false,
            CheckNodeLifecycle = false,
            CheckReturnConsistency = false,
            CheckAnnotationNarrowing = false,
            CheckContainerSpecialization = false,
            CheckTypeWidening = false,
            CheckParameterTypeHints = false,
            CheckUntypedContainerAccess = false,
            CheckRedundantAnnotations = false,
            CheckAwaitConsistency = true
        };
        var validator = new GDSemanticValidator(semanticModel, options);
        var result = validator.Validate(classDecl);

        return result.Diagnostics;
    }

    private static List<GDDiagnostic> FilterAwaitDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.PossibleMissedAwait).ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
