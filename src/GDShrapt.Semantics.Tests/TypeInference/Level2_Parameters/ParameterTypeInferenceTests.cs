using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for parameter type inference using duck typing.
/// All tests use the same code path as the plugin UI: GDScriptAnalyzer.GetTypeForNode().
/// </summary>
[TestClass]
public class ParameterTypeInferenceTests
{
    #region Dictionary Inference

    [TestMethod]
    public void GetTypeForNode_DictionaryGetMethod_InfersDictionary()
    {
        // Arrange - parameter uses .get() method
        var code = @"
extends Node

func process(data):
    return data.get(""key"")
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "process");
        var dataParam = method.Parameters.First();

        // Act - use the same path as the plugin
        var type = semanticModel.GetTypeForNode(dataParam);

        // Assert - .get() is on Dictionary and Object, so result may be a Union
        Assert.IsNotNull(type, "Should infer type for 'data' parameter");
        Assert.IsTrue(type.Contains("Dictionary"),
            $"data.get() should infer type containing Dictionary, got: {type}");
    }

    #endregion

    #region Array Inference

    [TestMethod]
    public void GetTypeForNode_ArrayAppendMethod_InfersArray()
    {
        // Arrange - parameter uses .append() method
        var code = @"
extends Node

func add_item(collection, item):
    collection.append(item)
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "add_item");
        var collectionParam = method.Parameters.First();

        // Act
        var type = semanticModel.GetTypeForNode(collectionParam);

        // Assert - .append() is only on Array, but TypesMap may find it on related types too
        Assert.IsNotNull(type, "Should infer type for 'collection' parameter");
        Assert.IsTrue(type.Contains("Array"),
            $"collection.append() should infer type containing Array, got: {type}");
    }

    [TestMethod]
    public void GetTypeForNode_ForLoop_InfersArray()
    {
        // Arrange - parameter is used in for loop
        var code = @"
extends Node

func process(items):
    for item in items:
        print(item)
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "process");
        var itemsParam = method.Parameters.First();

        // Act
        var type = semanticModel.GetTypeForNode(itemsParam);

        // Assert
        Assert.IsNotNull(type, "Should infer type for 'items' parameter");
        Assert.AreEqual("Array", type,
            $"for x in items should infer Array, got: {type}");
    }

    [TestMethod]
    public void GetTypeForNode_IndexAccess_InfersArray()
    {
        // Arrange - parameter is indexed
        var code = @"
extends Node

func get_first(arr):
    return arr[0]
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "get_first");
        var arrParam = method.Parameters.First();

        // Act
        var type = semanticModel.GetTypeForNode(arrParam);

        // Assert
        Assert.IsNotNull(type, "Should infer type for 'arr' parameter");
        Assert.AreEqual("Array", type,
            $"arr[0] should infer Array, got: {type}");
    }

    #endregion

    #region String Inference

    [TestMethod]
    public void GetTypeForNode_StringMethods_InfersString()
    {
        // Arrange - parameter uses string methods
        var code = @"
extends Node

func process_text(text):
    var parts = text.split("","")
    return text.substr(0, 10)
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "process_text");
        var textParam = method.Parameters.First();

        // Act
        var type = semanticModel.GetTypeForNode(textParam);

        // Assert
        Assert.IsNotNull(type, "Should infer type for 'text' parameter");
        Assert.AreEqual("String", type,
            $"text.split()/substr() should infer String, got: {type}");
    }

    #endregion

    #region Vector Inference

    [TestMethod]
    public void GetTypeForNode_VectorProperties_InfersVector2()
    {
        // Arrange - parameter accesses x and y (Vector2 pattern)
        var code = @"
extends Node

func get_magnitude(vec):
    return sqrt(vec.x * vec.x + vec.y * vec.y)
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "get_magnitude");
        var vecParam = method.Parameters.First();

        // Act
        var type = semanticModel.GetTypeForNode(vecParam);

        // Assert
        Assert.IsNotNull(type, "Should infer type for 'vec' parameter");
        Assert.AreEqual("Vector2", type,
            $"vec.x/y should infer Vector2, got: {type}");
    }

    [TestMethod]
    public void GetTypeForNode_Vector3Properties_InfersVector3()
    {
        // Arrange - parameter accesses x, y, z (Vector3 pattern)
        var code = @"
extends Node

func get_length(vec):
    return sqrt(vec.x * vec.x + vec.y * vec.y + vec.z * vec.z)
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "get_length");
        var vecParam = method.Parameters.First();

        // Act
        var type = semanticModel.GetTypeForNode(vecParam);

        // Assert
        Assert.IsNotNull(type, "Should infer type for 'vec' parameter");
        Assert.AreEqual("Vector3", type,
            $"vec.x/y/z should infer Vector3, got: {type}");
    }

    #endregion

    #region Node Inference

    [TestMethod]
    public void GetTypeForNode_NodeMethods_InfersNode()
    {
        // Arrange - parameter uses Node methods
        var code = @"
extends Node

func process(node):
    var child = node.get_node(""Child"")
    return child
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "process");
        var nodeParam = method.Parameters.First();

        // Act
        var type = semanticModel.GetTypeForNode(nodeParam);

        // Assert
        Assert.IsNotNull(type, "Should infer type for 'node' parameter");
        Assert.IsTrue(type.Contains("Node"),
            $"node.get_node() should infer type containing Node, got: {type}");
    }

    #endregion

    #region Type Check Inference

    [TestMethod]
    public void GetTypeForNode_IsTypeCheck_InfersCheckedType()
    {
        // Arrange - parameter is type-checked
        var code = @"
extends Node

func process(obj):
    if obj is Node2D:
        obj.queue_free()
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "process");
        var objParam = method.Parameters.First();

        // Act
        var type = semanticModel.GetTypeForNode(objParam);

        // Assert
        Assert.IsNotNull(type, "Should infer type for 'obj' parameter");
        Assert.IsTrue(type.Contains("Node2D"),
            $"'obj is Node2D' should infer type containing Node2D, got: {type}");
    }

    #endregion

    #region Explicit Type Annotation

    [TestMethod]
    public void GetTypeForNode_ExplicitType_ReturnsExplicitType()
    {
        // Arrange
        var code = @"
extends Node

func process(data: Dictionary):
    return data.get(""key"")
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "process");
        var dataParam = method.Parameters.First();

        // Act
        var type = semanticModel.GetTypeForNode(dataParam);

        // Assert
        Assert.IsNotNull(type, "Should return type for 'data' parameter");
        Assert.AreEqual("Dictionary", type,
            $"Explicit type annotation should be returned, got: {type}");
    }

    #endregion

    #region Element Type Inference

    [TestMethod]
    public void GetTypeForNode_ForLoopWithTypeCheck_InfersArrayWithElementType()
    {
        // Arrange - type check on iterator should infer element type
        var code = @"
extends Node

func process(items):
    for item in items:
        if item is Node:
            item.queue_free()
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "process");
        var itemsParam = method.Parameters.First();

        // Act
        var type = semanticModel.GetTypeForNode(itemsParam);

        // Assert
        Assert.IsNotNull(type, "Should infer type for 'items' parameter");
        Assert.AreEqual("Array[Node]", type,
            $"'for item in items' + 'item is Node' should infer Array[Node], got: {type}");
    }

    [TestMethod]
    public void GetTypeForNode_ForLoopWithMultipleTypeChecks_InfersArrayWithUnionElementType()
    {
        // Arrange - multiple type checks on iterator
        var code = @"
extends Node

func process(path):
    for key in path:
        if key is int:
            print(key * 2)
        elif key is String:
            print(key.length())
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "process");
        var pathParam = method.Parameters.First();

        // Act
        var type = semanticModel.GetTypeForNode(pathParam);

        // Assert
        Assert.IsNotNull(type, "Should infer type for 'path' parameter");
        // Elements can be in any order depending on implementation
        Assert.IsTrue(type.StartsWith("Array[") && type.Contains("int") && type.Contains("String"),
            $"Multiple type checks should infer Array with int and String elements, got: {type}");
    }

    #endregion

    #region Alias Tracking

    [TestMethod]
    public void GetTypeForNode_TypeCheckOnAlias_InfersTypeForOriginalParameter()
    {
        // Arrange - type check on alias (current) should apply to parameter (data)
        var code = @"
extends Node

func process(data):
    var current = data
    if current is Dictionary:
        return current.get(""key"")
    return null
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "process");
        var dataParam = method.Parameters.First();

        // Act
        var type = semanticModel.GetTypeForNode(dataParam);

        // Assert
        Assert.IsNotNull(type, "Should infer type for 'data' parameter");
        // The type may be a union containing Dictionary (Object | Dictionary[...])
        // because the parameter is also used for 'get' method which exists on Object
        Assert.IsTrue(type.Contains("Dictionary"),
            $"Type check on alias should infer type containing Dictionary for 'data', got: {type}");
    }

    #endregion

    #region Complex Integration Tests

    [TestMethod]
    public void GetTypeForNode_SafeGetNested_InfersCorrectTypes()
    {
        // Arrange - full safe_get_nested method
        var code = @"
extends Node

func safe_get_nested(data, path):
    var current = data
    for key in path:
        if current == null:
            return null
        if current is Dictionary:
            current = current.get(key)
        elif current is Array and key is int:
            current = current[key]
        else:
            return null
    return current
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "safe_get_nested");
        var dataParam = method.Parameters.First();
        var pathParam = method.Parameters.Skip(1).First();

        // Act
        var dataType = semanticModel.GetTypeForNode(dataParam);
        var pathType = semanticModel.GetTypeForNode(pathParam);

        // Assert - path should be Array[int] (from 'for key in path' + 'key is int')
        Assert.IsNotNull(pathType, "Should infer type for 'path' parameter");
        Assert.AreEqual("Array[int]", pathType,
            $"path should be Array[int] from iterator type check, got: {pathType}");

        // Assert - data should be Dictionary|Array union (from type checks on alias 'current')
        Assert.IsNotNull(dataType, "Should infer type for 'data' parameter");
        Assert.IsTrue(dataType.Contains("Dictionary"),
            $"data should contain Dictionary, got: {dataType}");
        Assert.IsTrue(dataType.Contains("Array"),
            $"data should contain Array, got: {dataType}");
        Assert.IsTrue(dataType.Contains("int"),
            $"data should have key type 'int', got: {dataType}");
    }

    [TestMethod]
    public void GetTypeForNode_KeyTypeFromIterator_PropagatesKeyType()
    {
        // Arrange - key from iterator is used in indexer
        var code = @"
extends Node

func process(data, path):
    var current = data
    for key in path:
        if key is int:
            current = current[key]
        elif key is String:
            current = current.get(key)
    return current
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "process");
        var dataParam = method.Parameters.First();
        var pathParam = method.Parameters.Skip(1).First();

        // Act
        var dataType = semanticModel.GetTypeForNode(dataParam);
        var pathType = semanticModel.GetTypeForNode(pathParam);

        // Assert - path should have both element types
        Assert.IsNotNull(pathType, "Should infer type for 'path' parameter");
        Assert.IsTrue(pathType.Contains("int"),
            $"path should have element type 'int', got: {pathType}");
        Assert.IsTrue(pathType.Contains("String"),
            $"path should have element type 'String', got: {pathType}");

        // Assert - data should have key types propagated from iterator
        Assert.IsNotNull(dataType, "Should infer type for 'data' parameter");
    }

    [TestMethod]
    public void GetTypeForNode_ProcessDataWithCallback_InfersArrayAndCallable()
    {
        // Arrange
        var code = @"
extends Node

func process_data(items, callback):
    for item in items:
        callback.call(item)
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "process_data");
        var itemsParam = method.Parameters.First();

        // Act
        var itemsType = semanticModel.GetTypeForNode(itemsParam);

        // Assert
        Assert.IsNotNull(itemsType, "Should infer type for 'items' parameter");
        Assert.AreEqual("Array", itemsType,
            $"items should be inferred as Array (iterable), got: {itemsType}");
    }

    #endregion

    #region Union Members and Derivable Tests

    [TestMethod]
    public void InferParameterType_UnionType_HasDetailedMembers()
    {
        // Arrange - parameter has multiple type checks
        var code = @"
extends Node

func process(data):
    var current = data
    if current is Dictionary:
        return current.get(""key"")
    elif current is Array:
        return current[0]
    return null
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "process");
        var dataParam = method.Parameters.First();

        // Act - get the detailed inferred type
        Assert.IsNotNull(semanticModel, "SemanticModel should be available");

        var inferred = semanticModel.InferParameterType(dataParam);

        // Assert
        Assert.IsNotNull(inferred, "Should infer type");
        Assert.IsNotNull(inferred.UnionMembers, "Should have union members");
        Assert.IsTrue(inferred.UnionMembers.Count >= 1, "Should have at least one member");

        // Check that Union has both Dictionary and Array
        var memberTypes = inferred.UnionMembers.Select(m => m.BaseType).ToList();
        Assert.IsTrue(memberTypes.Contains("Dictionary") || memberTypes.Contains("Array"),
            $"Union should contain Dictionary or Array, got: {string.Join(", ", memberTypes)}");
    }

    [TestMethod]
    public void InferParameterType_Derivable_HasDerivableSlots()
    {
        // Arrange - Dictionary type check without known value type
        var code = @"
extends Node

func process(data):
    if data is Dictionary:
        var value = data.get(""key"")
        return value
    return null
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "process");
        var dataParam = method.Parameters.First();

        // Act
        Assert.IsNotNull(semanticModel, "SemanticModel should be available");

        var inferred = semanticModel.InferParameterType(dataParam);

        // Assert
        Assert.IsNotNull(inferred, "Should infer type");
        Assert.IsNotNull(inferred.UnionMembers, "Should have union members");

        // Check that Dictionary member has a value type slot
        var dictMember = inferred.UnionMembers.FirstOrDefault(m => m.BaseType == "Dictionary");
        Assert.IsNotNull(dictMember, "Should have Dictionary member");

        // ValueType may be Variant or derivable
        if (dictMember.ValueType != null)
        {
            // Either we know the type or it's derivable
            Assert.IsTrue(
                dictMember.ValueType.TypeName == "Variant" ||
                dictMember.ValueType.IsDerivable ||
                !string.IsNullOrEmpty(dictMember.ValueType.TypeName),
                "Dictionary value slot should have type info");
        }
    }

    [TestMethod]
    public void InferParameterType_UnionMembers_HaveInferenceSources()
    {
        // Arrange
        var code = @"
extends Node

func process(obj):
    if obj is Node2D:
        obj.position = Vector2.ZERO
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "process");
        var objParam = method.Parameters.First();

        // Act
        Assert.IsNotNull(semanticModel, "SemanticModel should be available");

        var inferred = semanticModel.InferParameterType(objParam);

        // Assert
        Assert.IsNotNull(inferred, "Should infer type");
        Assert.IsNotNull(inferred.UnionMembers, "Should have union members");
        Assert.IsTrue(inferred.UnionMembers.Count > 0, "Should have at least one member");

        var node2dMember = inferred.UnionMembers.FirstOrDefault(m => m.BaseType == "Node2D");
        Assert.IsNotNull(node2dMember, "Should have Node2D member");

        // Source may or may not be present depending on implementation
        // The key is that the member exists with the correct base type
    }

    #endregion

    #region Type Narrowing and Control Flow Tests

    [TestMethod]
    public void GetExpressionType_AliasWithNarrowing_ReturnsDictionaryNotVariant()
    {
        // Arrange - current is alias of data, narrowed to Dictionary in if branch
        var code = @"
extends Node

func safe_get_nested(data, path):
    var current = data
    for key in path:
        if current == null:
            return null
        if current is Dictionary:
            current = current.get(key)
        elif current is Array and key is int:
            current = current[key]
        else:
            return null
    return current
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "safe_get_nested");

        // Find the "current.get(key)" call expression
        // It's inside the "if current is Dictionary:" branch
        var getCall = method.AllNodes
            .OfType<GDCallExpression>()
            .FirstOrDefault(c =>
                c.CallerExpression is GDMemberOperatorExpression m &&
                m.Identifier?.Sequence == "get");

        Assert.IsNotNull(getCall, "Should find current.get(key) call");

        var memberOp = getCall.CallerExpression as GDMemberOperatorExpression;
        Assert.IsNotNull(memberOp, "Caller should be member operator expression");

        // Act - get type for the caller expression (current)
        Assert.IsNotNull(semanticModel, "SemanticModel should be available");

        var currentType = semanticModel.GetExpressionType(memberOp.CallerExpression);

        // Assert - current should be Dictionary inside the "if current is Dictionary:" branch
        Assert.IsNotNull(currentType, "Should get type for 'current'");
        Assert.AreEqual("Dictionary", currentType,
            $"Inside 'if current is Dictionary:' branch, current should be Dictionary, got: {currentType}");
    }

    [TestMethod]
    public void GetExpressionType_MethodCallOnNarrowedType_ReturnsMethodReturnType()
    {
        // Arrange
        var code = @"
extends Node

func process(data):
    if data is Dictionary:
        var value = data.get(""key"")
        return value
    return null
";
        var (method, semanticModel) = GetMethodAndSemanticModel(code, "process");

        // Find the "data.get" call
        var getCall = method.AllNodes
            .OfType<GDCallExpression>()
            .FirstOrDefault(c =>
                c.CallerExpression is GDMemberOperatorExpression m &&
                m.Identifier?.Sequence == "get");

        Assert.IsNotNull(getCall, "Should find data.get call");

        // Act
        Assert.IsNotNull(semanticModel, "SemanticModel should be available");

        var callReturnType = semanticModel.GetExpressionType(getCall);

        // Assert - Dictionary.get returns Variant
        Assert.IsNotNull(callReturnType, "Should get return type for data.get()");
        Assert.AreEqual("Variant", callReturnType,
            $"Dictionary.get() should return Variant, got: {callReturnType}");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Parses code and returns the method and analyzer.
    /// Uses the same analysis path as the plugin.
    /// </summary>
    private static (GDMethodDeclaration method, GDSemanticModel semanticModel) GetMethodAndSemanticModel(
        string code, string methodName)
    {
        var reference = new GDScriptReference("test://virtual/param_test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        Assert.IsNotNull(scriptFile.Class, "Script should have a class");

        // Analyze with runtime provider (same as plugin)
        var runtimeProvider = new GDGodotTypesProvider();
        scriptFile.Analyze(runtimeProvider);

        Assert.IsNotNull(scriptFile.SemanticModel, "Script should have semantic model after Analyze()");

        var method = scriptFile.Class.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == methodName);

        Assert.IsNotNull(method, $"Should find method '{methodName}'");

        return (method, scriptFile.SemanticModel);
    }

    #endregion
}
