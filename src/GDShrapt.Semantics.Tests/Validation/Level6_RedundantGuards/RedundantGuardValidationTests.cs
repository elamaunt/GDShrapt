using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level6_RedundantGuards;

/// <summary>
/// Level 6: Redundant guard validation tests.
/// Tests validate that redundant type guards and null checks are detected.
/// </summary>
[TestClass]
public class RedundantGuardValidationTests
{
    #region Redundant Type Guard - Should Report

    [TestMethod]
    public void TypedVariable_IsCheck_ReportsRedundant()
    {
        var code = @"
func test():
    var arr: Array = []
    if arr is Array:
        pass
";
        var diagnostics = ValidateCode(code);
        var redundantDiagnostics = FilterRedundantDiagnostics(diagnostics);
        Assert.IsTrue(redundantDiagnostics.Any(d => d.Code == GDDiagnosticCode.RedundantTypeGuard),
            $"Expected GD7010 for redundant is check. Found: {FormatDiagnostics(redundantDiagnostics)}");
    }

    [TestMethod]
    public void NestedIsCheck_SameType_ReportsRedundant()
    {
        var code = @"
func test(x):
    if x is Array:
        if x is Array:
            pass
";
        var diagnostics = ValidateCode(code);
        var redundantDiagnostics = FilterRedundantDiagnostics(diagnostics);
        Assert.IsTrue(redundantDiagnostics.Any(d => d.Code == GDDiagnosticCode.RedundantNarrowedTypeGuard),
            $"Expected GD7011 for nested redundant is check. Found: {FormatDiagnostics(redundantDiagnostics)}");
    }

    #endregion

    #region Redundant Null Check - Should Report

    [TestMethod]
    public void PrimitiveVariable_NullCheck_ReportsRedundant()
    {
        var code = @"
func test():
    var x: int = 5
    if x != null:
        pass
";
        var diagnostics = ValidateCode(code);
        var redundantDiagnostics = FilterRedundantDiagnostics(diagnostics);
        Assert.IsTrue(redundantDiagnostics.Any(d => d.Code == GDDiagnosticCode.RedundantNullCheck),
            $"Expected GD7013 for redundant null check on int. Found: {FormatDiagnostics(redundantDiagnostics)}");
    }

    #endregion

    #region Different Type Guard - Should NOT Report

    [TestMethod]
    public void NestedIsCheck_DifferentType_NoDiagnostic()
    {
        var code = @"
func test(x):
    if x is Array:
        if x is PackedByteArray:
            pass
";
        var diagnostics = ValidateCode(code);
        var redundantDiagnostics = FilterRedundantDiagnostics(diagnostics);
        Assert.AreEqual(0, redundantDiagnostics.Count,
            $"Different type is not redundant. Found: {FormatDiagnostics(redundantDiagnostics)}");
    }

    #endregion

    #region Function Returns Null - Should NOT Report

    [TestMethod]
    public void NullCheck_AfterFunctionThatReturnsNull_ShouldNotBeRedundant()
    {
        var code = @"
extends Node

func find_item(items, predicate):
    for item in items:
        if predicate.call(item):
            return item
    return null

func use_item():
    var item = find_item([], func(x): return true)
    if item == null:
        return
    print(item)
";
        var diagnostics = ValidateCode(code);
        var gd7013 = diagnostics.Where(d => d.Code == GDDiagnosticCode.RedundantNullCheck).ToList();
        Assert.AreEqual(0, gd7013.Count,
            $"Null check should NOT be redundant when function can return null. Found: {FormatDiagnostics(gd7013)}");
    }

    [TestMethod]
    public void NullCheck_AfterChainedFunctionThatReturnsNull_ShouldNotBeRedundant()
    {
        var code = @"
extends Node

func parse_factor(tokens, pos):
    if pos >= tokens.size():
        return null
    return {""value"": tokens[pos], ""pos"": pos + 1}

func parse_term(tokens, pos):
    var result = parse_factor(tokens, pos)
    if result == null:
        return null
    var value = result[""value""]
    return {""value"": value, ""pos"": result[""pos""]}
";
        var diagnostics = ValidateCode(code);
        var gd7013 = diagnostics.Where(d => d.Code == GDDiagnosticCode.RedundantNullCheck).ToList();
        Assert.AreEqual(0, gd7013.Count,
            $"Null check should NOT be redundant when called function returns null. Found: {FormatDiagnostics(gd7013)}");
    }

    #endregion

    #region Variant Variable - Should NOT Report

    [TestMethod]
    public void VariantVariable_IsCheck_NoDiagnostic()
    {
        var code = @"
func test(x):
    if x is Array:
        pass
";
        var diagnostics = ValidateCode(code);
        var redundantDiagnostics = FilterRedundantDiagnostics(diagnostics);
        Assert.AreEqual(0, redundantDiagnostics.Count,
            $"Variant variable type check is not redundant. Found: {FormatDiagnostics(redundantDiagnostics)}");
    }

    [TestMethod]
    public void VariantVariable_NullCheck_NoDiagnostic()
    {
        var code = @"
func test(x):
    if x != null:
        pass
";
        var diagnostics = ValidateCode(code);
        var redundantDiagnostics = FilterRedundantDiagnostics(diagnostics);
        Assert.AreEqual(0, redundantDiagnostics.Count,
            $"Variant variable null check is not redundant. Found: {FormatDiagnostics(redundantDiagnostics)}");
    }

    #endregion

    #region Project-Level GD7013 Investigation

    [TestMethod]
    [TestCategory("Integration")]
    public void ProjectLevel_NullCheck_AfterFunctionReturningNull_InvestigateGD7013()
    {
        var project = TestProjectFixture.Project;
        var projectModel = TestProjectFixture.ProjectModel;

        // Find cyclic_inference.gd
        var scriptFile = project.ScriptFiles
            .FirstOrDefault(f => f.FullPath != null && f.FullPath.Replace('\\', '/').Contains("cyclic_inference.gd"));
        Assert.IsNotNull(scriptFile, "cyclic_inference.gd not found in test project");

        var semanticModel = scriptFile.SemanticModel;
        Assert.IsNotNull(semanticModel, "Semantic model not available for cyclic_inference.gd");

        // Run semantic validator in project context
        var options = new GDSemanticValidatorOptions
        {
            CheckTypes = true,
            CheckMemberAccess = true,
            CheckRedundantGuards = true,
            RedundantGuardSeverity = GDDiagnosticSeverity.Hint,
            ProjectModel = projectModel
        };
        var validator = new GDSemanticValidator(semanticModel, options);
        var result = validator.Validate(scriptFile.Class!);

        // Check GD7013 diagnostics
        var gd7013 = result.Diagnostics
            .Where(d => d.Code == GDDiagnosticCode.RedundantNullCheck)
            .ToList();

        // Debug: print all GD7013 with details
        foreach (var d in gd7013)
        {
            Console.WriteLine($"GD7013 at {d.StartLine}:{d.StartColumn}: {d.Message}");

            // Get flow variable type for the variable in question
            var varName = d.Message.Contains("'") ?
                d.Message.Split('\'')[1] : "unknown";
            var node = semanticModel.GetNodeAtPosition(d.StartLine, d.StartColumn);
            if (node != null)
            {
                var flowVar = semanticModel.GetVariableTypeAt(varName, node);
                if (flowVar != null)
                {
                    Console.WriteLine($"  DeclaredType: {flowVar.DeclaredType?.DisplayName ?? "null"}");
                    Console.WriteLine($"  EffectiveType: {flowVar.EffectiveTypeFormatted}");
                    Console.WriteLine($"  IsGuaranteedNonNull: {flowVar.IsGuaranteedNonNull}");
                    Console.WriteLine($"  IsPotentiallyNull: {flowVar.IsPotentiallyNull}");
                    Console.WriteLine($"  CurrentType types: {string.Join(", ", flowVar.CurrentType.Types.Select(t => t.DisplayName))}");
                }
            }
        }

        // Lines 258 and 285 (0-based) check `right == null` after parse_term/parse_factor
        // which explicitly return null. These should NOT be flagged.
        var falsePositives = gd7013
            .Where(d => d.StartLine == 258 || d.StartLine == 285)
            .ToList();

        Assert.AreEqual(0, falsePositives.Count,
            $"GD7013 should NOT fire for null checks on results of functions that return null. " +
            $"Found {falsePositives.Count} false positive(s) at lines: " +
            $"{string.Join(", ", falsePositives.Select(d => $"{d.StartLine + 1}:{d.StartColumn}"))}. " +
            $"All GD7013: {string.Join("; ", gd7013.Select(d => $"L{d.StartLine + 1}: {d.Message}"))}");
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
            CheckTypes = true,
            CheckMemberAccess = true,
            CheckRedundantGuards = true,
            RedundantGuardSeverity = GDDiagnosticSeverity.Hint
        };
        var validator = new GDSemanticValidator(semanticModel, options);
        var result = validator.Validate(classDecl);

        return result.Diagnostics;
    }

    private static List<GDDiagnostic> FilterRedundantDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.RedundantTypeGuard ||
            d.Code == GDDiagnosticCode.RedundantNarrowedTypeGuard ||
            d.Code == GDDiagnosticCode.RedundantHasMethodCheck ||
            d.Code == GDDiagnosticCode.RedundantNullCheck ||
            d.Code == GDDiagnosticCode.RedundantTruthinessCheck).ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
