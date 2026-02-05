using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Tests for complex type inference: Array[T], Dictionary[K,V], nested generics.
/// Uses type_inference.gd from test project.
/// Validates that type annotations are correctly parsed and BuildName() returns expected strings.
/// </summary>
[TestClass]
public class ComplexTypesTests
{
    private static GDScriptFile? _script;
    private static GDClassDeclaration? _class;

    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        _script = TestProjectFixture.GetScript("type_inference.gd");
        Assert.IsNotNull(_script, "type_inference.gd should exist in test project");
        _class = _script.Class;
        Assert.IsNotNull(_class, "Class declaration should exist");
    }

    /// <summary>
    /// Helper to find a class-level variable by name directly from AST.
    /// </summary>
    private static GDVariableDeclaration? FindClassVariable(string name)
    {
        return _class?.Members
            .OfType<GDVariableDeclaration>()
            .FirstOrDefault(v => v.Identifier?.Sequence == name);
    }

    /// <summary>
    /// Helper to find a method by name.
    /// </summary>
    private static GDMethodDeclaration? FindMethod(string name)
    {
        return _class?.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == name);
    }

    /// <summary>
    /// Helper to find a local variable in a method.
    /// </summary>
    private static GDVariableDeclarationStatement? FindLocalVariable(GDMethodDeclaration method, string name)
    {
        return method.AllNodes
            .OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == name);
    }

    #region Array[T] Type Annotation Tests

    [TestMethod]
    public void TypedArray_Int_ResolvedCorrectly()
    {
        // var int_array: Array[int] = [1, 2, 3]
        var variable = FindClassVariable("int_array");
        Assert.IsNotNull(variable, "Variable 'int_array' should be found");

        var typeName = variable.Type?.BuildName();
        Assert.AreEqual("Array[int]", typeName, "Type should be Array[int]");
    }

    [TestMethod]
    public void TypedArray_String_ResolvedCorrectly()
    {
        // var string_array: Array[String] = ["a", "b", "c"]
        var variable = FindClassVariable("string_array");
        Assert.IsNotNull(variable, "Variable 'string_array' should be found");

        var typeName = variable.Type?.BuildName();
        Assert.AreEqual("Array[String]", typeName, "Type should be Array[String]");
    }

    [TestMethod]
    public void TypedArray_Vector2_ResolvedCorrectly()
    {
        // var vector_array: Array[Vector2] = [Vector2.ZERO, Vector2.ONE]
        var variable = FindClassVariable("vector_array");
        Assert.IsNotNull(variable, "Variable 'vector_array' should be found");

        var typeName = variable.Type?.BuildName();
        Assert.AreEqual("Array[Vector2]", typeName, "Type should be Array[Vector2]");
    }

    [TestMethod]
    public void TypedArray_GodotClass_ResolvedCorrectly()
    {
        // var entity_array: Array[Node2D] = []
        var variable = FindClassVariable("entity_array");
        Assert.IsNotNull(variable, "Variable 'entity_array' should be found");

        var typeName = variable.Type?.BuildName();
        Assert.AreEqual("Array[Node2D]", typeName, "Type should be Array[Node2D]");
    }

    [TestMethod]
    public void TypedArray_NestedArray_ResolvedCorrectly()
    {
        // var matrix: Array[Array[int]] = [[1, 2], [3, 4]]
        var variable = FindClassVariable("matrix");
        Assert.IsNotNull(variable, "Variable 'matrix' should be found");

        var typeName = variable.Type?.BuildName();
        Assert.AreEqual("Array[Array[int]]", typeName, "Type should be Array[Array[int]]");
    }

    #endregion

    #region Dictionary[K,V] Type Annotation Tests

    [TestMethod]
    public void TypedDictionary_StringInt_ResolvedCorrectly()
    {
        // var string_int_dict: Dictionary[String, int] = {"a": 1, "b": 2}
        var variable = FindClassVariable("string_int_dict");
        Assert.IsNotNull(variable, "Variable 'string_int_dict' should be found");

        var typeName = variable.Type?.BuildName();
        // Note: BuildName() returns without space after comma
        Assert.AreEqual("Dictionary[String,int]", typeName, "Type should be Dictionary[String,int]");
    }

    [TestMethod]
    public void TypedDictionary_StringFloat_ResolvedCorrectly()
    {
        // var string_float_dict: Dictionary[String, float] = {"x": 1.5, "y": 2.5}
        var variable = FindClassVariable("string_float_dict");
        Assert.IsNotNull(variable, "Variable 'string_float_dict' should be found");

        var typeName = variable.Type?.BuildName();
        // Note: BuildName() returns without space after comma
        Assert.AreEqual("Dictionary[String,float]", typeName, "Type should be Dictionary[String,float]");
    }

    [TestMethod]
    public void TypedDictionary_WithArrayValue_ResolvedCorrectly()
    {
        // var complex_dict: Dictionary[String, Array[int]] = {"nums": [1, 2, 3]}
        var variable = FindClassVariable("complex_dict");
        Assert.IsNotNull(variable, "Variable 'complex_dict' should be found");

        var typeName = variable.Type?.BuildName();
        // Note: BuildName() returns without space after comma
        Assert.AreEqual("Dictionary[String,Array[int]]", typeName, "Type should be Dictionary[String,Array[int]]");
    }

    #endregion

    #region Index Access Type Inference Tests

    [TestMethod]
    public void TypedArray_IndexAccess_ReturnsElementType()
    {
        // In test_typed_array_inference():
        // var items: Array[int] = [1, 2, 3]
        // var first := items[0]  # Should be int

        var method = FindMethod("test_typed_array_inference");
        Assert.IsNotNull(method, "Method test_typed_array_inference should exist");

        var indexerExpr = method.AllNodes
            .OfType<GDIndexerExpression>()
            .FirstOrDefault();
        Assert.IsNotNull(indexerExpr, "Indexer expression should be found");

        // Use SemanticModel which delegates to Analyzer for local variable type inference
        var semanticModel = _script!.SemanticModel;
        Assert.IsNotNull(semanticModel, "SemanticModel should be available");

        var typeNode = semanticModel.GetTypeNodeForExpression(indexerExpr);
        Assert.IsNotNull(typeNode, "Type node should be inferred for indexer");
        Assert.AreEqual("int", typeNode.BuildName(), "Element type should be int");
    }

    [TestMethod]
    public void TypedDictionary_IndexAccess_ReturnsValueType()
    {
        // In test_typed_dictionary_inference():
        // var dict: Dictionary[String, int] = {"x": 10, "y": 20}
        // var value := dict["x"]  # Should be int

        var method = FindMethod("test_typed_dictionary_inference");
        Assert.IsNotNull(method, "Method test_typed_dictionary_inference should exist");

        var indexerExpr = method.AllNodes
            .OfType<GDIndexerExpression>()
            .FirstOrDefault();
        Assert.IsNotNull(indexerExpr, "Indexer expression should be found");

        // Use SemanticModel which delegates to Analyzer for local variable type inference
        var semanticModel = _script!.SemanticModel;
        Assert.IsNotNull(semanticModel, "SemanticModel should be available");

        var typeNode = semanticModel.GetTypeNodeForExpression(indexerExpr);
        Assert.IsNotNull(typeNode, "Type node should be inferred for dictionary indexer");
        Assert.AreEqual("int", typeNode.BuildName(), "Value type should be int");
    }

    [TestMethod]
    public void NestedArray_IndexAccess_ReturnsInnerArrayType()
    {
        // In test_nested_array_inference():
        // var local_matrix: Array[Array[int]] = [[1, 2], [3, 4]]
        // var row := local_matrix[0]  # Should be Array[int]

        var method = FindMethod("test_nested_array_inference");
        Assert.IsNotNull(method, "Method test_nested_array_inference should exist");

        // Find the first indexer (local_matrix[0])
        var indexerExpr = method.AllNodes
            .OfType<GDIndexerExpression>()
            .FirstOrDefault();
        Assert.IsNotNull(indexerExpr, "Indexer expression should be found");

        // Use SemanticModel which delegates to Analyzer for local variable type inference
        var semanticModel = _script!.SemanticModel;
        Assert.IsNotNull(semanticModel, "SemanticModel should be available");

        var typeNode = semanticModel.GetTypeNodeForExpression(indexerExpr);
        Assert.IsNotNull(typeNode, "Type node should be inferred for nested array indexer");
        Assert.AreEqual("Array[int]", typeNode.BuildName(), "Row type should be Array[int]");
    }

    #endregion

    #region Untyped Container Tests

    [TestMethod]
    public void UntypedArray_HasArrayType()
    {
        // In test_untyped_containers():
        // var untyped_arr: Array = [1, "two", 3.0]

        var method = FindMethod("test_untyped_containers");
        Assert.IsNotNull(method, "Method test_untyped_containers should exist");

        var arrDecl = FindLocalVariable(method, "untyped_arr");
        Assert.IsNotNull(arrDecl, "Variable 'untyped_arr' should be declared");

        var typeName = arrDecl.Type?.BuildName();
        Assert.AreEqual("Array", typeName, "Type should be untyped Array");
    }

    [TestMethod]
    public void UntypedDictionary_HasDictionaryType()
    {
        // In test_untyped_containers():
        // var untyped_dict: Dictionary = {"a": 1}

        var method = FindMethod("test_untyped_containers");
        Assert.IsNotNull(method, "Method test_untyped_containers should exist");

        var dictDecl = FindLocalVariable(method, "untyped_dict");
        Assert.IsNotNull(dictDecl, "Variable 'untyped_dict' should be declared");

        var typeName = dictDecl.Type?.BuildName();
        Assert.AreEqual("Dictionary", typeName, "Type should be untyped Dictionary");
    }

    #endregion
}
