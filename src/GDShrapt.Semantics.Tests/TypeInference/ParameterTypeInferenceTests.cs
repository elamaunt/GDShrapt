using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for parameter type inference using duck typing.
/// Parameters should have their types inferred from usage patterns
/// like method calls, property access, iteration, and indexing.
/// </summary>
[TestClass]
public class ParameterTypeInferenceTests
{
    #region Method Call Constraints

    [TestMethod]
    public void AnalyzeMethod_DictionaryGetMethod_InfersDictionaryConstraint()
    {
        // Arrange - parameter uses .get() method
        var code = @"
extends Node

func process(data):
    return data.get(""key"")
";
        var method = ParseMethod(code, "process");
        Assert.IsNotNull(method);

        // Act
        var constraints = GDParameterUsageAnalyzer.AnalyzeMethod(method);

        // Assert
        Assert.IsTrue(constraints.ContainsKey("data"), "Should have constraints for 'data' parameter");
        var dataConstraints = constraints["data"];
        Assert.IsTrue(dataConstraints.RequiredMethods.Contains("get"),
            "data.get() should add 'get' method constraint");
    }

    [TestMethod]
    public void AnalyzeMethod_ArrayAppendMethod_InfersArrayConstraint()
    {
        // Arrange - parameter uses .append() method
        var code = @"
extends Node

func add_item(collection, item):
    collection.append(item)
";
        var method = ParseMethod(code, "add_item");
        Assert.IsNotNull(method);

        // Act
        var constraints = GDParameterUsageAnalyzer.AnalyzeMethod(method);

        // Assert
        Assert.IsTrue(constraints.ContainsKey("collection"), "Should have constraints for 'collection'");
        var collectionConstraints = constraints["collection"];
        Assert.IsTrue(collectionConstraints.RequiredMethods.Contains("append"),
            "collection.append() should add 'append' method constraint");
    }

    [TestMethod]
    public void AnalyzeMethod_StringMethods_InfersStringConstraint()
    {
        // Arrange - parameter uses string methods
        var code = @"
extends Node

func process_text(text):
    var parts = text.split("","")
    return text.substr(0, 10)
";
        var method = ParseMethod(code, "process_text");
        Assert.IsNotNull(method);

        // Act
        var constraints = GDParameterUsageAnalyzer.AnalyzeMethod(method);

        // Assert
        Assert.IsTrue(constraints.ContainsKey("text"), "Should have constraints for 'text'");
        var textConstraints = constraints["text"];
        Assert.IsTrue(textConstraints.RequiredMethods.Contains("split"),
            "text.split() should add 'split' method constraint");
        Assert.IsTrue(textConstraints.RequiredMethods.Contains("substr"),
            "text.substr() should add 'substr' method constraint");
    }

    #endregion

    #region Property Access Constraints

    [TestMethod]
    public void AnalyzeMethod_PropertyAccess_AddsPropertyConstraint()
    {
        // Arrange - parameter accesses properties
        var code = @"
extends Node

func process(player):
    var hp = player.health
    player.position = Vector2.ZERO
";
        var method = ParseMethod(code, "process");
        Assert.IsNotNull(method);

        // Act
        var constraints = GDParameterUsageAnalyzer.AnalyzeMethod(method);

        // Assert
        Assert.IsTrue(constraints.ContainsKey("player"), "Should have constraints for 'player'");
        var playerConstraints = constraints["player"];
        Assert.IsTrue(playerConstraints.RequiredProperties.Contains("health"),
            "player.health should add 'health' property constraint");
        Assert.IsTrue(playerConstraints.RequiredProperties.Contains("position"),
            "player.position should add 'position' property constraint");
    }

    [TestMethod]
    public void AnalyzeMethod_VectorProperties_InfersVector2()
    {
        // Arrange - parameter accesses x and y (Vector2 pattern)
        var code = @"
extends Node

func get_magnitude(vec):
    return sqrt(vec.x * vec.x + vec.y * vec.y)
";
        var method = ParseMethod(code, "get_magnitude");
        Assert.IsNotNull(method);

        // Act
        var constraints = GDParameterUsageAnalyzer.AnalyzeMethod(method);

        // Assert
        Assert.IsTrue(constraints.ContainsKey("vec"), "Should have constraints for 'vec'");
        var vecConstraints = constraints["vec"];
        Assert.IsTrue(vecConstraints.RequiredProperties.Contains("x"),
            "vec.x should add 'x' property constraint");
        Assert.IsTrue(vecConstraints.RequiredProperties.Contains("y"),
            "vec.y should add 'y' property constraint");
    }

    #endregion

    #region Iteration Constraints

    [TestMethod]
    public void AnalyzeMethod_ForLoop_SetsIterableConstraint()
    {
        // Arrange - parameter is used in for loop
        var code = @"
extends Node

func process(items):
    for item in items:
        print(item)
";
        var method = ParseMethod(code, "process");
        Assert.IsNotNull(method);

        // Act
        var constraints = GDParameterUsageAnalyzer.AnalyzeMethod(method);

        // Assert
        Assert.IsTrue(constraints.ContainsKey("items"), "Should have constraints for 'items'");
        var itemsConstraints = constraints["items"];
        Assert.IsTrue(itemsConstraints.IsIterable,
            "for x in items should mark items as iterable");
    }

    #endregion

    #region Indexing Constraints

    [TestMethod]
    public void AnalyzeMethod_IndexAccess_SetsIndexableConstraint()
    {
        // Arrange - parameter is indexed
        var code = @"
extends Node

func get_first(arr):
    return arr[0]
";
        var method = ParseMethod(code, "get_first");
        Assert.IsNotNull(method);

        // Act
        var constraints = GDParameterUsageAnalyzer.AnalyzeMethod(method);

        // Assert
        Assert.IsTrue(constraints.ContainsKey("arr"), "Should have constraints for 'arr'");
        var arrConstraints = constraints["arr"];
        Assert.IsTrue(arrConstraints.IsIndexable,
            "arr[0] should mark arr as indexable");
    }

    #endregion

    #region Type Check Constraints

    [TestMethod]
    public void AnalyzeMethod_IsTypeCheck_AddsPossibleType()
    {
        // Arrange - parameter is type-checked
        var code = @"
extends Node

func process(obj):
    if obj is Node:
        obj.queue_free()
";
        var method = ParseMethod(code, "process");
        Assert.IsNotNull(method);

        // Act
        var constraints = GDParameterUsageAnalyzer.AnalyzeMethod(method);

        // Assert
        Assert.IsTrue(constraints.ContainsKey("obj"), "Should have constraints for 'obj'");
        var objConstraints = constraints["obj"];
        Assert.IsTrue(objConstraints.PossibleTypes.Contains("Node"),
            "'obj is Node' should add 'Node' as possible type");
    }

    #endregion

    #region Type Resolution Tests

    [TestMethod]
    public void ResolveFromConstraints_DictionaryMethods_InfersDictionary()
    {
        // Arrange
        var constraints = new GDParameterConstraints("data");
        constraints.AddRequiredMethod("get");

        var resolver = new GDParameterTypeResolver(new GDGodotTypesProvider());

        // Act
        var result = resolver.ResolveFromConstraints(constraints);

        // Assert
        Assert.AreEqual("Dictionary", result.TypeName,
            "Parameter with .get() method should be inferred as Dictionary");
    }

    [TestMethod]
    public void ResolveFromConstraints_ArrayMethods_InfersArray()
    {
        // Arrange
        var constraints = new GDParameterConstraints("items");
        constraints.AddRequiredMethod("append");

        var resolver = new GDParameterTypeResolver(new GDGodotTypesProvider());

        // Act
        var result = resolver.ResolveFromConstraints(constraints);

        // Assert
        Assert.AreEqual("Array", result.TypeName,
            "Parameter with .append() method should be inferred as Array");
    }

    [TestMethod]
    public void ResolveFromConstraints_StringMethods_InfersString()
    {
        // Arrange
        var constraints = new GDParameterConstraints("text");
        constraints.AddRequiredMethod("substr");

        var resolver = new GDParameterTypeResolver(new GDGodotTypesProvider());

        // Act
        var result = resolver.ResolveFromConstraints(constraints);

        // Assert
        Assert.AreEqual("String", result.TypeName,
            "Parameter with .substr() method should be inferred as String");
    }

    [TestMethod]
    public void ResolveFromConstraints_VectorProperties_InfersVector2()
    {
        // Arrange
        var constraints = new GDParameterConstraints("vec");
        constraints.AddRequiredProperty("x");
        constraints.AddRequiredProperty("y");

        var resolver = new GDParameterTypeResolver(new GDGodotTypesProvider());

        // Act
        var result = resolver.ResolveFromConstraints(constraints);

        // Assert
        Assert.AreEqual("Vector2", result.TypeName,
            "Parameter with x and y properties should be inferred as Vector2");
    }

    [TestMethod]
    public void ResolveFromConstraints_Vector3Properties_InfersVector3()
    {
        // Arrange
        var constraints = new GDParameterConstraints("vec");
        constraints.AddRequiredProperty("x");
        constraints.AddRequiredProperty("y");
        constraints.AddRequiredProperty("z");

        var resolver = new GDParameterTypeResolver(new GDGodotTypesProvider());

        // Act
        var result = resolver.ResolveFromConstraints(constraints);

        // Assert
        Assert.AreEqual("Vector3", result.TypeName,
            "Parameter with x, y, z properties should be inferred as Vector3");
    }

    [TestMethod]
    public void ResolveFromConstraints_NodeMethods_InfersNode()
    {
        // Arrange
        var constraints = new GDParameterConstraints("node");
        constraints.AddRequiredMethod("get_node");

        var resolver = new GDParameterTypeResolver(new GDGodotTypesProvider());

        // Act
        var result = resolver.ResolveFromConstraints(constraints);

        // Assert
        Assert.AreEqual("Node", result.TypeName,
            "Parameter with .get_node() method should be inferred as Node");
    }

    [TestMethod]
    public void ResolveFromConstraints_IterableOnly_InfersArray()
    {
        // Arrange
        var constraints = new GDParameterConstraints("items");
        constraints.AddIterableConstraint();

        var resolver = new GDParameterTypeResolver(new GDGodotTypesProvider());

        // Act
        var result = resolver.ResolveFromConstraints(constraints);

        // Assert
        Assert.AreEqual("Array", result.TypeName,
            "Iterable-only constraint should infer Array");
    }

    [TestMethod]
    public void ResolveFromConstraints_TypeCheck_ReturnsCheckedType()
    {
        // Arrange
        var constraints = new GDParameterConstraints("obj");
        constraints.AddPossibleType("Node2D");

        var resolver = new GDParameterTypeResolver(new GDGodotTypesProvider());

        // Act
        var result = resolver.ResolveFromConstraints(constraints);

        // Assert
        Assert.AreEqual("Node2D", result.TypeName,
            "Type check should return the checked type");
        // Type check from 'is' uses High confidence during resolution
        Assert.IsTrue(result.Confidence >= GDTypeConfidence.Medium,
            $"Type check should have at least medium confidence. Got: {result.Confidence}");
    }

    #endregion

    #region GDSemanticModel Integration Tests

    [TestMethod]
    public void InferParameterTypes_Integration_ReturnsInferredTypes()
    {
        // Arrange
        var code = @"
extends Node

func process_data(items, callback):
    for item in items:
        callback.call(item)
";
        var (classDecl, semanticModel, analyzer) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "process_data");
        Assert.IsNotNull(method);

        // Act
        var paramTypes = semanticModel.InferParameterTypes(method);

        // Assert
        Assert.IsTrue(paramTypes.ContainsKey("items"), "Should infer type for 'items'");
        var itemsType = paramTypes["items"];
        Assert.AreEqual("Array", itemsType.TypeName,
            "items should be inferred as Array (iterable)");
    }

    [TestMethod]
    public void InferParameterType_ExplicitType_ReturnsExplicitType()
    {
        // Arrange
        var code = @"
extends Node

func process(data: Dictionary):
    return data.get(""key"")
";
        var (classDecl, semanticModel, analyzer) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "process");
        Assert.IsNotNull(method);

        var param = method.Parameters?.FirstOrDefault();
        Assert.IsNotNull(param);

        // Act
        var inferredType = semanticModel.InferParameterType(param);

        // Assert
        Assert.AreEqual("Dictionary", inferredType.TypeName,
            "Should return explicit type annotation");
        Assert.AreEqual(GDTypeConfidence.Certain, inferredType.Confidence,
            "Explicit type should have certain confidence");
    }

    #endregion

    #region Helper Methods

    private static GDMethodDeclaration? ParseMethod(string code, string methodName)
    {
        var reference = new GDScriptReference("test://virtual/param_test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        return scriptFile.Class?.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == methodName);
    }

    private static (GDClassDeclaration?, GDSemanticModel?, GDScriptAnalyzer?) AnalyzeCode(string code)
    {
        var reference = new GDScriptReference("test://virtual/param_test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        if (scriptFile.Class == null)
            return (null, null, null);

        var runtimeProvider = new GDGodotTypesProvider();
        scriptFile.Analyze(runtimeProvider);

        return (scriptFile.Class, scriptFile.Analyzer?.SemanticModel, scriptFile.Analyzer);
    }

    #endregion
}
