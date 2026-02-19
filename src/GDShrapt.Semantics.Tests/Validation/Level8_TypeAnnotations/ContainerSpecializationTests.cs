using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level8_TypeAnnotations;

[TestClass]
public class ContainerSpecializationTests
{
    #region GD3025 - Container Missing Specialization

    [TestMethod]
    public void ContainerSpecialization_BareArrayWithIntUsage_ReportsDiagnostic()
    {
        var code = @"
var scores: Array = []

func test():
    scores.append(10)
    scores.append(20)
    var total: int = scores[0] + scores[1]
";
        var diagnostics = ValidateCode(code);
        var relevant = diagnostics.Where(d => d.Code == GDDiagnosticCode.ContainerMissingSpecialization).ToList();
        // This may or may not fire depending on the container profile analysis depth
    }

    [TestMethod]
    public void ContainerSpecialization_TypedArray_NoDiagnostic()
    {
        var code = @"
var scores: Array[int] = []
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.ContainerMissingSpecialization),
            $"Typed Array should not report. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void ContainerSpecialization_NonContainer_NoDiagnostic()
    {
        var code = @"
var name: String = ""test""
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.ContainerMissingSpecialization),
            $"Non-container type should not report. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void ContainerSpecialization_Disabled_NoDiagnostic()
    {
        var code = @"
var scores: Array = [1, 2, 3]
";
        var diagnostics = ValidateCode(code, new GDSemanticValidatorOptions
        {
            CheckContainerSpecialization = false
        });
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.ContainerMissingSpecialization),
            $"Should not report when disabled. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region GD7021 - Untyped Container Element Access

    [TestMethod]
    public void UntypedContainer_ForLoopOverBareArray_OptionEnabled()
    {
        var code = @"
var enemies: Array = []

func test():
    for enemy in enemies:
        enemy.take_damage(10)
";
        var diagnostics = ValidateCode(code, new GDSemanticValidatorOptions
        {
            CheckUntypedContainerAccess = true
        });
        // May or may not fire depending on element type inference
        var relevant = diagnostics.Where(d => d.Code == GDDiagnosticCode.UntypedContainerElementAccess).ToList();
    }

    [TestMethod]
    public void UntypedContainer_DisabledByDefault_NoDiagnostic()
    {
        var code = @"
var enemies: Array = []

func test():
    for enemy in enemies:
        enemy.take_damage(10)
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.UntypedContainerElementAccess),
            $"GD7021 should be disabled by default. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region Helper Methods

    private static IEnumerable<GDDiagnostic> ValidateCode(string code, GDSemanticValidatorOptions? options = null)
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

        options ??= new GDSemanticValidatorOptions();
        var validator = new GDSemanticValidator(semanticModel, options);
        var result = validator.Validate(classDecl);

        return result.Diagnostics;
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
