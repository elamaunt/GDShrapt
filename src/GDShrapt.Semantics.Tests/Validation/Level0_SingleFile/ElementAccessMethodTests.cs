using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level0;

/// <summary>
/// Tests for element access methods that should return the container's element type.
/// Methods like front(), back(), pop_front(), etc. should return the element type,
/// not Variant.
/// </summary>
[TestClass]
public class ElementAccessMethodTests
{
    #region TypeInferenceMetadata Loading Test

    [TestMethod]
    public void TypeInferenceMetadata_FrontMethod_HasReturnTypeRole()
    {
        var provider = new GDGodotTypesProvider();
        var memberInfo = provider.GetMember("Array", "front");
        Assert.IsNotNull(memberInfo, "front method should exist on Array");
        Assert.AreEqual("element", memberInfo.ReturnTypeRole,
            $"front() should have ReturnTypeRole='element', but got '{memberInfo.ReturnTypeRole}'");
    }

    [TestMethod]
    public void TypeInferenceMetadata_DictGetMethod_HasReturnTypeRole()
    {
        var provider = new GDGodotTypesProvider();
        var memberInfo = provider.GetMember("Dictionary", "get");
        Assert.IsNotNull(memberInfo, "get method should exist on Dictionary");
        Assert.AreEqual("value", memberInfo.ReturnTypeRole,
            $"get() should have ReturnTypeRole='value', but got '{memberInfo.ReturnTypeRole}'");
    }

    #endregion

    #region Array.front() and Array.back() Tests

    [TestMethod]
    public void ArrayFront_TypedArray_ReturnsElementType()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3]
    var first: int = arr.front()
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"front() on Array[int] should return int. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ArrayBack_TypedArray_ReturnsElementType()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3]
    var last: int = arr.back()
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"back() on Array[int] should return int. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ArrayFront_InferredArray_ReturnsElementType()
    {
        var code = @"
extends Node
func test():
    var arr = [1, 2, 3]
    var first: int = arr.front()
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"front() on inferred Array[int] should return int. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Array.pick_random() Tests

    [TestMethod]
    public void ArrayPickRandom_TypedArray_ReturnsElementType()
    {
        var code = @"
extends Node
func test():
    var arr: Array[String] = [""a"", ""b"", ""c""]
    var random: String = arr.pick_random()
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"pick_random() on Array[String] should return String. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Array.pop_* Methods Tests

    [TestMethod]
    public void ArrayPopFront_TypedArray_ReturnsElementType()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3]
    var popped: int = arr.pop_front()
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"pop_front() on Array[int] should return int. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ArrayPopBack_TypedArray_ReturnsElementType()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3]
    var popped: int = arr.pop_back()
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"pop_back() on Array[int] should return int. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ArrayPopAt_TypedArray_ReturnsElementType()
    {
        var code = @"
extends Node
func test():
    var arr: Array[int] = [1, 2, 3]
    var popped: int = arr.pop_at(1)
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"pop_at() on Array[int] should return int. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Dictionary.get() Tests

    [TestMethod]
    public void DictGet_TypedDict_ReturnsValueType()
    {
        var code = @"
extends Node
func test():
    var dict: Dictionary[String, int] = {}
    var value: int = dict.get(""key"")
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"get() on Dictionary[String,int] should return int. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void DictGet_TypedDictWithDefault_ReturnsValueType()
    {
        var code = @"
extends Node
func test():
    var dict: Dictionary[String, int] = {}
    var value: int = dict.get(""key"", 0)
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"get() with default on Dictionary[String,int] should return int. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Dictionary.keys() and Dictionary.values() Tests

    [TestMethod]
    public void DictKeys_TypedDict_ReturnsKeyArray()
    {
        var code = @"
extends Node
func test():
    var dict: Dictionary[String, int] = {}
    var keys: Array[String] = dict.keys()
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"keys() on Dictionary[String,int] should return Array[String]. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void DictValues_TypedDict_ReturnsValueArray()
    {
        var code = @"
extends Node
func test():
    var dict: Dictionary[String, int] = {}
    var values: Array[int] = dict.values()
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"values() on Dictionary[String,int] should return Array[int]. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Dictionary.find_key() Tests

    [TestMethod]
    public void DictFindKey_TypedDict_ReturnsKeyType()
    {
        var code = @"
extends Node
func test():
    var dict: Dictionary[String, int] = {""a"": 1}
    var key: String = dict.find_key(1)
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeMismatchDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"find_key() on Dictionary[String,int] should return String. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    #endregion

    #region Method Chaining Tests

    [TestMethod]
    public void ArrayFront_ThenMethodCall_NoDiagnostic()
    {
        var code = @"
extends Node
func test():
    var nodes: Array[Node2D] = []
    var pos = nodes.front().position
";
        // Debug: Check type inference directly
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var provider = new GDGodotTypesProvider();
        var engine = new GDTypeInferenceEngine(provider);

        // Find the expression nodes.front().position
        var testMethod = classDecl?.AllNodes.OfType<GDMethodDeclaration>().FirstOrDefault(m => m.Identifier?.Sequence == "test");
        var varStmt = testMethod?.AllNodes.OfType<GDVariableDeclarationStatement>().LastOrDefault();
        var initExpr = varStmt?.Initializer;

        System.Console.WriteLine($"initExpr type: {initExpr?.GetType().Name}");
        if (initExpr is GDMemberOperatorExpression memberExpr)
        {
            System.Console.WriteLine($"memberExpr.Identifier: {memberExpr.Identifier?.Sequence}");
            System.Console.WriteLine($"memberExpr.CallerExpression type: {memberExpr.CallerExpression?.GetType().Name}");

            var callerType = engine.InferType(memberExpr.CallerExpression);
            System.Console.WriteLine($"CallerType from InferType: {callerType}");

            if (memberExpr.CallerExpression is GDCallExpression callExpr)
            {
                System.Console.WriteLine($"callExpr.CallerExpression type: {callExpr.CallerExpression?.GetType().Name}");
                if (callExpr.CallerExpression is GDMemberOperatorExpression innerMember)
                {
                    System.Console.WriteLine($"innerMember.Identifier: {innerMember.Identifier?.Sequence}");
                    var innerCallerType = engine.InferType(innerMember.CallerExpression);
                    System.Console.WriteLine($"innerCallerType: {innerCallerType}");
                }
            }
        }

        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"front() on Array[Node2D] should return Node2D, allowing .position access. Found: {FormatDiagnostics(unguardedDiagnostics)}");
    }

    [TestMethod]
    public void DictGet_ThenMethodCall_NoDiagnostic()
    {
        var code = @"
extends Node
func test():
    var nodes: Dictionary[String, Node2D] = {}
    var pos = nodes.get(""key"").position
";
        var diagnostics = ValidateCode(code);
        var unguardedDiagnostics = FilterUnguardedDiagnostics(diagnostics);
        Assert.AreEqual(0, unguardedDiagnostics.Count,
            $"get() on Dictionary[String,Node2D] should return Node2D. Found: {FormatDiagnostics(unguardedDiagnostics)}");
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
            null,
            null,
            null);
        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        var semanticModel = collector.BuildSemanticModel();

        var options = new GDSemanticValidatorOptions
        {
            CheckTypes = true,
            CheckMemberAccess = true,
            CheckArgumentTypes = true
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

    private static List<GDDiagnostic> FilterUnguardedDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.UnguardedPropertyAccess ||
            d.Code == GDDiagnosticCode.UnguardedMethodCall).ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] L{d.StartLine}: {d.Message}"));
    }

    #endregion
}
