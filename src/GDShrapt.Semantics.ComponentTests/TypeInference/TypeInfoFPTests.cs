using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Tests for type inference through GetTypeInfo().
/// Verifies that initializer inference, method return types, and expression types are resolved.
/// Remaining [Ignore] tests document known gaps (e.g., TypesMap generic return types).
/// </summary>
[TestClass]
[TestCategory("ManualVerification")]
public class TypeInfoFPTests
{
    // ================================================================
    // Category A: := operator should infer type from RHS literal
    // ================================================================

    [TestMethod]
    public void WalrusOperator_IntLiteral_ShouldInferInt()
    {
        var code = @"
var public_var := 42
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(model);

        var typeInfo = model.TypeSystem.GetTypeInfo("public_var");
        Assert.IsNotNull(typeInfo, "Type info should be available for public_var");
        Assert.AreEqual("int", typeInfo.InferredType.DisplayName,
            "var x := 42 should infer int type");
        Assert.AreEqual(GDTypeConfidence.Certain, typeInfo.Confidence);
    }

    [TestMethod]
    public void WalrusOperator_StringLiteral_ShouldInferString()
    {
        var code = @"
var inferred_string := ""hello""
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(model);

        var typeInfo = model.TypeSystem.GetTypeInfo("inferred_string");
        Assert.IsNotNull(typeInfo);
        Assert.AreEqual("String", typeInfo.InferredType.DisplayName);
    }

    [TestMethod]
    public void ConstWithLiteral_ShouldInferType()
    {
        var code = @"
const MY_CONSTANT := 42
const STRING_CONSTANT := ""test""
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(model);

        var intConst = model.TypeSystem.GetTypeInfo("MY_CONSTANT");
        Assert.IsNotNull(intConst);
        Assert.AreEqual("int", intConst.InferredType.DisplayName,
            "const X := 42 should infer int");

        var strConst = model.TypeSystem.GetTypeInfo("STRING_CONSTANT");
        Assert.IsNotNull(strConst);
        Assert.AreEqual("String", strConst.InferredType.DisplayName,
            "const X := \"test\" should infer String");
    }

    [TestMethod]
    public void WalrusOperator_Constructor_ShouldInferType()
    {
        var code = @"
var inferred_vector := Vector2(10, 20)
var inferred_color := Color.RED
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(model);

        var vecInfo = model.TypeSystem.GetTypeInfo("inferred_vector");
        Assert.IsNotNull(vecInfo);
        Assert.AreEqual("Vector2", vecInfo.InferredType.DisplayName);

        var colorInfo = model.TypeSystem.GetTypeInfo("inferred_color");
        Assert.IsNotNull(colorInfo);
        Assert.AreEqual("Color", colorInfo.InferredType.DisplayName);
    }

    // ================================================================
    // Category B: Built-in method return types not inferred
    // ================================================================

    [TestMethod]

    public void StringMethod_ToUpper_ShouldReturnString()
    {
        var code = @"
func test():
    var my_string: String = """"
    var upper = my_string.to_upper()
    var length = my_string.length()
    return [upper, length]
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(model);

        var upperInfo = model.TypeSystem.GetTypeInfo("upper");
        Assert.IsNotNull(upperInfo);
        Assert.AreEqual("String", upperInfo.InferredType.DisplayName,
            "String.to_upper() should return String");

        var lengthInfo = model.TypeSystem.GetTypeInfo("length");
        Assert.IsNotNull(lengthInfo);
        Assert.AreEqual("int", lengthInfo.InferredType.DisplayName,
            "String.length() should return int");
    }

    [TestMethod]

    public void ArrayMethod_Size_ShouldReturnInt()
    {
        var code = @"
func test():
    var arr = [1, 2, 3]
    var size = arr.size()
    var is_empty = arr.is_empty()
    return [size, is_empty]
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(model);

        var sizeInfo = model.TypeSystem.GetTypeInfo("size");
        Assert.IsNotNull(sizeInfo);
        Assert.AreEqual("int", sizeInfo.InferredType.DisplayName,
            "Array.size() should return int");

        var emptyInfo = model.TypeSystem.GetTypeInfo("is_empty");
        Assert.IsNotNull(emptyInfo);
        Assert.AreEqual("bool", emptyInfo.InferredType.DisplayName,
            "Array.is_empty() should return bool");
    }

    [TestMethod]

    public void DictMethod_Has_ShouldReturnBool()
    {
        var code = @"
func test():
    var dict: Dictionary = {}
    var has_key = dict.has(""key"")
    var keys = dict.keys()
    return [has_key, keys]
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(model);

        var hasInfo = model.TypeSystem.GetTypeInfo("has_key");
        Assert.IsNotNull(hasInfo);
        Assert.AreEqual("bool", hasInfo.InferredType.DisplayName,
            "Dictionary.has() should return bool");

        var keysInfo = model.TypeSystem.GetTypeInfo("keys");
        Assert.IsNotNull(keysInfo);
        Assert.AreEqual("Array", keysInfo.InferredType.DisplayName,
            "Dictionary.keys() should return Array");
    }

    [TestMethod]

    public void Vector2Method_Length_ShouldReturnFloat()
    {
        var code = @"
func test():
    var vec: Vector2 = Vector2(3, 4)
    var length = vec.length()
    var normalized = vec.normalized()
    return [length, normalized]
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(model);

        var lengthInfo = model.TypeSystem.GetTypeInfo("length");
        Assert.IsNotNull(lengthInfo);
        Assert.AreEqual("float", lengthInfo.InferredType.DisplayName,
            "Vector2.length() should return float");

        var normInfo = model.TypeSystem.GetTypeInfo("normalized");
        Assert.IsNotNull(normInfo);
        Assert.AreEqual("Vector2", normInfo.InferredType.DisplayName,
            "Vector2.normalized() should return Vector2");
    }

    [TestMethod]

    public void ColorMethod_Darkened_ShouldReturnColor()
    {
        var code = @"
func test():
    var c: Color = Color.RED
    var darkened = c.darkened(0.2)
    var html = c.to_html()
    return [darkened, html]
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(model);

        var darkInfo = model.TypeSystem.GetTypeInfo("darkened");
        Assert.IsNotNull(darkInfo);
        Assert.AreEqual("Color", darkInfo.InferredType.DisplayName,
            "Color.darkened() should return Color");

        var htmlInfo = model.TypeSystem.GetTypeInfo("html");
        Assert.IsNotNull(htmlInfo);
        Assert.AreEqual("String", htmlInfo.InferredType.DisplayName,
            "Color.to_html() should return String");
    }

    [TestMethod]
    public void NodeMethod_GetParent_ShouldReturnNode()
    {
        var code = @"
extends Node

func test():
    var parent = get_parent()
    var children = get_children()
    var child_count = get_child_count()
    return [parent, children, child_count]
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(model);

        var parentInfo = model.TypeSystem.GetTypeInfo("parent");
        Assert.IsNotNull(parentInfo);
        Assert.AreEqual("Node", parentInfo.InferredType.DisplayName,
            "get_parent() should return Node");

        var countInfo = model.TypeSystem.GetTypeInfo("child_count");
        Assert.IsNotNull(countInfo);
        Assert.AreEqual("int", countInfo.InferredType.DisplayName,
            "get_child_count() should return int");
    }

    // ================================================================
    // Category C: Typed array subscript should return element type
    // ================================================================

    [TestMethod]

    public void TypedArraySubscript_ShouldReturnElementType()
    {
        var code = @"
func test():
    var points: Array[Vector2] = []
    var p = points[0]
    return p
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(model);

        var pInfo = model.TypeSystem.GetTypeInfo("p");
        Assert.IsNotNull(pInfo);
        Assert.AreEqual("Vector2", pInfo.InferredType.DisplayName,
            "Array[Vector2][0] should return Vector2");
    }

    // ================================================================
    // Category D: Arithmetic on typed operands
    // ================================================================

    [TestMethod]

    public void IntArithmetic_ShouldReturnInt()
    {
        var code = @"
func test(x: int, y: int, z: int):
    var result = x + y + z
    return result
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(model);

        var resultInfo = model.TypeSystem.GetTypeInfo("result");
        Assert.IsNotNull(resultInfo);
        Assert.AreEqual("int", resultInfo.InferredType.DisplayName,
            "int + int + int should infer int");
    }

    [TestMethod]

    public void FunctionCallWithReturnType_ShouldInfer()
    {
        var code = @"
func calculate(amount: int) -> int:
    return amount * 2

func test():
    var result = calculate(10)
    return result
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(model);

        var resultInfo = model.TypeSystem.GetTypeInfo("result");
        Assert.IsNotNull(resultInfo);
        Assert.AreEqual("int", resultInfo.InferredType.DisplayName,
            "Calling function with -> int should infer int");
    }

    // ================================================================
    // Category F: Property access on typed objects
    // ================================================================

    [TestMethod]

    public void PropertyAccess_ShouldInferPropertyType()
    {
        var code = @"
var _health: int = 100

var health: int:
    get:
        return _health
    set(value):
        _health = value

func test():
    var h = health
    return h
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(model);

        var hInfo = model.TypeSystem.GetTypeInfo("h");
        Assert.IsNotNull(hInfo);
        Assert.AreEqual("int", hInfo.InferredType.DisplayName,
            "Reading typed property should infer property type");
    }

    [TestMethod]

    public void ConstructorCall_ShouldInferClassType()
    {
        var code = @"
class Inner:
    var value: int = 0

func test():
    var instance = Inner.new()
    return instance
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(model);

        var instanceInfo = model.TypeSystem.GetTypeInfo("instance");
        Assert.IsNotNull(instanceInfo);
        Assert.AreEqual("Inner", instanceInfo.InferredType.DisplayName,
            "Inner.new() should infer Inner type");
    }

    // ================================================================
    // Category G: Duck-typed string references must not corrupt parameter types
    // Regression: RegisterSymbol for duck-typed symbols caused name collisions
    // ================================================================

    [TestMethod]
    public void DuckTypedStringRef_ShouldNotCorruptParameterType()
    {
        // Regression: RegisterSymbol for duck-typed symbols created name collisions.
        // The call("value") string literal must not overwrite the parameter symbol.
        // Method order matters: dynamic_access is declared BEFORE set_value,
        // so the string literal "value" is visited first during AST traversal.
        var code = @"
func dynamic_access(target: Node) -> void:
    target.call(""value"")

func set_value(value: int) -> void:
    print(value)
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(model);

        var typeInfo = model.TypeSystem.GetTypeInfo("value");
        Assert.IsNotNull(typeInfo, "Type info should be available for parameter 'value'");
        Assert.AreEqual("int", typeInfo.InferredType.DisplayName,
            "Parameter 'value: int' must retain int type even when call(\"value\") string literal exists");
        Assert.AreEqual(GDTypeConfidence.Certain, typeInfo.Confidence,
            "Parameter with explicit type annotation must have Certain confidence");
    }

    [TestMethod]
    public void HasMethodStringRef_ShouldNotCorruptParameterType()
    {
        // has_method("value") must not overwrite the parameter named 'value'
        var code = @"
func check(target: Node) -> bool:
    return target.has_method(""value"")

func process(value: float) -> float:
    return value * 2.0
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(model);

        var typeInfo = model.TypeSystem.GetTypeInfo("value");
        Assert.IsNotNull(typeInfo, "Type info should be available for parameter 'value'");
        Assert.AreEqual("float", typeInfo.InferredType.DisplayName,
            "Parameter 'value: float' must retain float type even when has_method(\"value\") exists");
    }

    [TestMethod]
    public void MultipleStringRefs_ShouldNotCorruptSameNameSymbols()
    {
        // Multiple string-literal duck-typed refs must not overwrite real symbols
        var code = @"
func dynamic_access(obj: Node) -> void:
    obj.call(""count"")
    obj.set(""count"", 42)
    if obj.has_method(""count""):
        pass

var count: int = 0

func increment(count: int) -> int:
    return count + 1
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(model);

        var varInfo = model.TypeSystem.GetTypeInfo("count");
        Assert.IsNotNull(varInfo, "Type info should be available for 'count'");
        Assert.AreEqual("int", varInfo.InferredType.DisplayName,
            "Variable 'count: int' must retain int type despite call/set/has_method string refs");
    }

    #region Helpers

    private static (GDClassDeclaration?, GDSemanticModel?) AnalyzeCode(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        if (classDecl == null)
            return (null, null);

        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null, null, null);

        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        var semanticModel = collector.BuildSemanticModel();

        return (classDecl, semanticModel);
    }

    #endregion
}
