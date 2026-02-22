using GDShrapt.Abstractions;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests.TypeInference.Level5_CrossFile;

[TestClass]
public class CrossFileMapMethodReferenceTests
{
    #region Inherited method reference in map()

    [TestMethod]
    public void MapWithInheritedMethod_CorrectType_NoDiagnostic()
    {
        var project = CreateProject("""
class_name BaseEntity
extends Node

func format_name(x) -> String:
    return str(x)
""", """
class_name ChildEntity
extends BaseEntity

func test():
    var arr: Array[int] = [1, 2, 3]
    for s: String in arr.map(format_name):
        pass
""");

        var diagnostics = ValidateScript(project, "ChildEntity");
        var forLoopDiagnostics = FilterForLoopDiagnostics(diagnostics);
        Assert.AreEqual(0, forLoopDiagnostics.Count,
            $"Inherited method reference in map() should infer String return type. Found: {FormatDiagnostics(forLoopDiagnostics)}");
    }

    [TestMethod]
    public void MapWithInheritedMethod_WrongType_ReportsMismatch()
    {
        var project = CreateProject("""
class_name BaseEntity
extends Node

func format_name(x) -> String:
    return str(x)
""", """
class_name ChildEntity
extends BaseEntity

func test():
    var arr: Array[int] = [1, 2, 3]
    for n: int in arr.map(format_name):
        pass
""");

        var diagnostics = ValidateScript(project, "ChildEntity");
        var forLoopDiagnostics = FilterForLoopDiagnostics(diagnostics);
        Assert.IsTrue(forLoopDiagnostics.Count > 0,
            "Expected type mismatch: int variable iterating over map(inherited -> String) result");
    }

    #endregion

    #region Static/object method reference via member access in map()

    [TestMethod]
    public void MapWithObjectMethodReference_CorrectType_NoDiagnostic()
    {
        var code = @"
extends Node

func test():
    var arr: Array[int] = [1, 2, 3]
    for s: String in arr.map(str):
        pass
";
        var diagnostics = ValidateSingleCode(code);
        var forLoopDiagnostics = FilterForLoopDiagnostics(diagnostics);
        Assert.AreEqual(0, forLoopDiagnostics.Count,
            $"Built-in str() reference in map() should infer String. Found: {FormatDiagnostics(forLoopDiagnostics)}");
    }

    [TestMethod]
    public void FilterWithInheritedMethod_PreservesType_NoDiagnostic()
    {
        var project = CreateProject("""
class_name BaseEntity
extends Node

func is_valid_number(x) -> bool:
    return x > 0
""", """
class_name ChildEntity
extends BaseEntity

func test():
    var arr: Array[int] = [1, 2, 3]
    for n: int in arr.filter(is_valid_number):
        pass
""");

        var diagnostics = ValidateScript(project, "ChildEntity");
        var forLoopDiagnostics = FilterForLoopDiagnostics(diagnostics);
        Assert.AreEqual(0, forLoopDiagnostics.Count,
            $"filter() with inherited method ref should preserve Array[int] type. Found: {FormatDiagnostics(forLoopDiagnostics)}");
    }

    #endregion

    #region Helper Methods

    private static GDScriptProject CreateProject(params string[] scripts)
    {
        var project = new GDScriptProject(scripts);
        project.AnalyzeAll();
        return project;
    }

    private static IEnumerable<GDDiagnostic> ValidateScript(GDScriptProject project, string className)
    {
        var script = project.ScriptFiles.FirstOrDefault(s => s.TypeName == className);
        if (script?.SemanticModel == null || script.Class == null)
            return Enumerable.Empty<GDDiagnostic>();

        var options = new GDSemanticValidatorOptions
        {
            CheckTypes = true,
            CheckMemberAccess = true,
            CheckArgumentTypes = true
        };
        var validator = new GDSemanticValidator(script.SemanticModel, options);
        var result = validator.Validate(script.Class);

        return result.Diagnostics;
    }

    private static IEnumerable<GDDiagnostic> ValidateSingleCode(string code)
    {
        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null,
            null,
            null);
        scriptFile.Analyze(runtimeProvider);
        var semanticModel = scriptFile.SemanticModel!;

        var options = new GDSemanticValidatorOptions
        {
            CheckTypes = true,
            CheckMemberAccess = true,
            CheckArgumentTypes = true
        };
        var validator = new GDSemanticValidator(semanticModel, options);
        var result = validator.Validate(scriptFile.Class!);

        return result.Diagnostics;
    }

    private static List<GDDiagnostic> FilterTypeDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.TypeMismatch ||
            d.Code == GDDiagnosticCode.InvalidAssignment ||
            d.Code == GDDiagnosticCode.TypeAnnotationMismatch ||
            d.Code == GDDiagnosticCode.InvalidOperandType).ToList();
    }

    private static List<GDDiagnostic> FilterForLoopDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return FilterTypeDiagnostics(diagnostics)
            .Where(d => d.Message.Contains("for-loop"))
            .ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] L{d.StartLine}: {d.Message}"));
    }

    #endregion
}
