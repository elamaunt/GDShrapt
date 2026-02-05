using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests.TypeInference.Level0;

/// <summary>
/// Tests for array addition type inference.
/// Verifies that Array[int] + Array[int] = Array[int], not Array.
/// </summary>
[TestClass]
public class ArrayAdditionTypeInferenceTests
{
    #region Same Type Addition

    [TestMethod]
    public void ArrayAddition_SameElementType_PreservesType()
    {
        var code = @"
extends Node
func test():
    var a: Array[int] = [1, 2]
    var b: Array[int] = [3, 4]
    var c = a + b
    var elem: int = c[0]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Array[int] + Array[int] should be Array[int]. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ArrayAddition_InferredSameType_PreservesType()
    {
        var code = @"
extends Node
func test():
    var a = [1, 2, 3]
    var b = [4, 5, 6]
    var c = a + b
    var elem: int = c[0]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Inferred int array + int array should be Array[int]. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ArrayAddition_ChainedOperations_PreservesType()
    {
        var code = @"
extends Node
func test():
    var a: Array[int] = [1]
    var b: Array[int] = [2]
    var c: Array[int] = [3]
    var result = a + b + c
    var elem: int = result[0]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Chained Array[int] addition should preserve type. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Typed + Untyped

    [TestMethod]
    public void ArrayAddition_TypedPlusUntyped_ReturnsArray()
    {
        var code = @"
extends Node
func test():
    var a: Array[int] = [1, 2]
    var b: Array = []
    var c = a + b
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Array[int] + Array should be valid. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ArrayAddition_UntypedPlusTyped_ReturnsArray()
    {
        var code = @"
extends Node
func test():
    var a: Array = []
    var b: Array[int] = [1, 2]
    var c = a + b
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Array + Array[int] should be valid. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Numeric Widening

    [TestMethod]
    public void ArrayAddition_IntPlusFloat_ReturnsArrayFloat()
    {
        var code = @"
extends Node
func test():
    var a: Array[int] = [1, 2]
    var b: Array[float] = [1.0, 2.0]
    var c = a + b
    var elem: float = c[0]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Array[int] + Array[float] should be Array[float]. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ArrayAddition_FloatPlusInt_ReturnsArrayFloat()
    {
        var code = @"
extends Node
func test():
    var a: Array[float] = [1.0, 2.0]
    var b: Array[int] = [3, 4]
    var c = a + b
    var elem: float = c[0]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Array[float] + Array[int] should be Array[float]. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Incompatible Types

    [TestMethod]
    public void ArrayAddition_StringPlusInt_ReturnsArray()
    {
        var code = @"
extends Node
func test():
    var a: Array[String] = [""a"", ""b""]
    var b: Array[int] = [1, 2]
    var c = a + b
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Array[String] + Array[int] should be valid (produces untyped Array). Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region String Arrays

    [TestMethod]
    public void ArrayAddition_StringArrays_PreservesType()
    {
        var code = @"
extends Node
func test():
    var a: Array[String] = [""a"", ""b""]
    var b: Array[String] = [""c"", ""d""]
    var c = a + b
    var elem: String = c[0]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Array[String] + Array[String] should be Array[String]. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Empty Arrays

    [TestMethod]
    public void ArrayAddition_EmptyArrays_PreservesType()
    {
        var code = @"
extends Node
func test():
    var a: Array[int] = []
    var b: Array[int] = []
    var c = a + b
    var elem: int = c[0]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Empty Array[int] + empty Array[int] should be Array[int]. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ArrayAddition_TypedPlusEmpty_PreservesType()
    {
        var code = @"
extends Node
func test():
    var a: Array[int] = [1, 2]
    a = a + []
    var elem: int = a[0]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Array[int] + [] should preserve type. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Union Type Tests - Explicit Typing

    [TestMethod]
    public void ArrayAddition_StringPlusInt_ReturnsUnionType()
    {
        var code = @"
extends Node
func test():
    var a: Array[String] = [""a"", ""b""]
    var b: Array[int] = [1, 2]
    var c = a + b
";
        var model = CreateSemanticModel(code);
        var cType = GetVariableType(model, "c");

        Assert.AreEqual("Array[String|int]", cType,
            $"Array[String] + Array[int] should be Array[String|int] (types sorted alphabetically)");
    }

    [TestMethod]
    public void ArrayAddition_ChainedUnion_CombinesAllTypes()
    {
        var code = @"
extends Node
func test():
    var a: Array[int] = [1]
    var b: Array[String] = [""x""]
    var c: Array[bool] = [true]
    var result = a + b + c
";
        var model = CreateSemanticModel(code);
        var resultType = GetVariableType(model, "result");

        Assert.AreEqual("Array[String|bool|int]", resultType,
            $"Array[int] + Array[String] + Array[bool] should be Array[String|bool|int]");
    }

    [TestMethod]
    public void ArrayAddition_NodePlusSprite_ReturnsUnionType()
    {
        var code = @"
extends Node
func test():
    var a: Array[Node] = []
    var b: Array[Sprite2D] = []
    var c = a + b
";
        var model = CreateSemanticModel(code);
        var cType = GetVariableType(model, "c");

        Assert.AreEqual("Array[Node|Sprite2D]", cType,
            $"Array[Node] + Array[Sprite2D] should be Array[Node|Sprite2D]");
    }

    [TestMethod]
    public void ArrayAddition_UnionPlusSingle_ExtendsUnion()
    {
        var code = @"
extends Node
func test():
    var a: Array[int] = [1]
    var b: Array[String] = [""x""]
    var ab = a + b
    var c: Array[bool] = [true]
    var result = ab + c
";
        var model = CreateSemanticModel(code);
        var resultType = GetVariableType(model, "result");

        Assert.AreEqual("Array[String|bool|int]", resultType,
            $"Array[String|int] + Array[bool] should be Array[String|bool|int]");
    }

    #endregion

    #region Helper Methods

    private static GDSemanticModel CreateSemanticModel(string code)
    {
        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null,
            null,
            null);
        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        return collector.BuildSemanticModel();
    }

    private static string GetVariableType(GDSemanticModel model, string varName)
    {
        // Find the variable declaration and get its inferred type
        var varDecl = model.ScriptFile.Class?.AllNodes
            .OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == varName);

        if (varDecl?.Initializer != null)
        {
            return model.GetExpressionType(varDecl.Initializer);
        }

        return null;
    }

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
            null,
            null,
            null);
        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        var semanticModel = collector.BuildSemanticModel();

        var options = new GDSemanticValidatorOptions
        {
            CheckTypes = true,
            CheckMemberAccess = true,
            CheckArgumentTypes = true,
            CheckIndexers = true
        };
        var validator = new GDSemanticValidator(semanticModel, options);
        var result = validator.Validate(classDecl);

        return result.Diagnostics;
    }

    private static List<GDDiagnostic> FilterTypeMismatchDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.TypeMismatch ||
            d.Code == GDDiagnosticCode.InvalidAssignment).ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] L{d.StartLine}: {d.Message}"));
    }

    #endregion
}
