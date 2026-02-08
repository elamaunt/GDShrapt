using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests.TypeInference.Level5_CrossFile;

/// <summary>
/// Tests for class-level container type inference.
/// Verifies that untyped class-level Array/Dictionary variables
/// can have their element types inferred from usage patterns.
/// </summary>
[TestClass]
public class ClassLevelContainerInferenceTests
{
    #region Single-File Tests

    [TestMethod]
    public void ClassLevelDictionary_SingleFile_InfersValueTypeFromAssignment()
    {
        // Arrange
        var code = @"
class_name TestClass

var entities = {}

func create_entity():
    var eid = 1
    entities[eid] = Node2D.new()
";
        var model = BuildSemanticModel(code);

        // Act
        var profile = model.GetClassContainerProfile("TestClass", "entities");

        // Assert
        Assert.IsNotNull(profile, "Class container profile should be collected");
        Assert.IsTrue(profile.IsDictionary, "Container should be detected as Dictionary");

        var inferredType = profile.ComputeInferredType();
        Assert.AreEqual("Node2D", inferredType.EffectiveElementType.DisplayName,
            "Value type should be inferred from Node2D.new() assignment");
    }

    [TestMethod]
    public void ClassLevelArray_SingleFile_InfersElementTypeFromAppend()
    {
        // Arrange
        var code = @"
class_name TestClass

var items = []

func add_item():
    items.append(Node2D.new())
";
        var model = BuildSemanticModel(code);

        // Act
        var profile = model.GetClassContainerProfile("TestClass", "items");

        // Assert
        Assert.IsNotNull(profile, "Class container profile should be collected");
        Assert.IsFalse(profile.IsDictionary, "Container should be detected as Array");

        var inferredType = profile.ComputeInferredType();
        Assert.AreEqual("Node2D", inferredType.EffectiveElementType.DisplayName,
            "Element type should be inferred from append(Node2D.new())");
    }

    [TestMethod]
    public void ClassLevelArray_SingleFile_InfersElementTypeFromPushBack()
    {
        // Arrange
        var code = @"
class_name TestClass

var queue = []

func enqueue():
    queue.push_back(""hello"")
";
        var model = BuildSemanticModel(code);

        // Act
        var profile = model.GetClassContainerProfile("TestClass", "queue");

        // Assert
        Assert.IsNotNull(profile);

        var inferredType = profile.ComputeInferredType();
        Assert.AreEqual("String", inferredType.EffectiveElementType.DisplayName);
    }

    [TestMethod]
    public void ClassLevelDictionary_MultipleAssignments_InfersUnionType()
    {
        // Arrange
        var code = @"
class_name TestClass

var cache = {}

func setup():
    cache[""node""] = Node2D.new()
    cache[""sprite""] = Sprite2D.new()
";
        var model = BuildSemanticModel(code);

        // Act
        var profile = model.GetClassContainerProfile("TestClass", "cache");

        // Assert
        Assert.IsNotNull(profile);

        var inferredType = profile.ComputeInferredType();
        Assert.IsTrue(inferredType.ElementUnionType.IsUnion,
            "Multiple different types should create a union");
        Assert.IsTrue(inferredType.ElementUnionType.Types.Contains(GDSemanticType.FromRuntimeTypeName("Node2D")));
        Assert.IsTrue(inferredType.ElementUnionType.Types.Contains(GDSemanticType.FromRuntimeTypeName("Sprite2D")));
    }

    [TestMethod]
    public void ClassLevelDictionary_SingleFile_InfersKeyType()
    {
        // Arrange
        var code = @"
class_name TestClass

var mapping = {}

func add():
    mapping[""key1""] = 1
    mapping[""key2""] = 2
";
        var model = BuildSemanticModel(code);

        // Act
        var profile = model.GetClassContainerProfile("TestClass", "mapping");

        // Assert
        Assert.IsNotNull(profile);
        Assert.IsTrue(profile.IsDictionary);

        var inferredType = profile.ComputeInferredType();
        Assert.AreEqual("String", inferredType.EffectiveKeyType?.DisplayName,
            "Key type should be inferred from string keys");
        Assert.AreEqual("int", inferredType.EffectiveElementType.DisplayName,
            "Value type should be inferred from int values");
    }

    [TestMethod]
    public void ClassLevelContainer_MultipleMethodsContribute()
    {
        // Arrange
        var code = @"
class_name TestClass

var items = []

func add_node():
    items.append(Node.new())

func add_node2d():
    items.append(Node2D.new())
";
        var model = BuildSemanticModel(code);

        // Act
        var profile = model.GetClassContainerProfile("TestClass", "items");

        // Assert
        Assert.IsNotNull(profile);

        var inferredType = profile.ComputeInferredType();
        Assert.IsTrue(inferredType.ElementUnionType.Types.Contains(GDSemanticType.FromRuntimeTypeName("Node")));
        Assert.IsTrue(inferredType.ElementUnionType.Types.Contains(GDSemanticType.FromRuntimeTypeName("Node2D")));
    }

    [TestMethod]
    public void ClassLevelContainer_WithInitializer_CollectsInitializerTypes()
    {
        // Arrange
        var code = @"
class_name TestClass

var items = [Node.new(), Node2D.new()]

func add():
    items.append(Sprite2D.new())
";
        var model = BuildSemanticModel(code);

        // Act
        var profile = model.GetClassContainerProfile("TestClass", "items");

        // Assert
        Assert.IsNotNull(profile);

        var inferredType = profile.ComputeInferredType();
        // Should have types from both initializer and append
        Assert.IsTrue(inferredType.ElementUnionType.Types.Contains(GDSemanticType.FromRuntimeTypeName("Node")));
        Assert.IsTrue(inferredType.ElementUnionType.Types.Contains(GDSemanticType.FromRuntimeTypeName("Node2D")));
        Assert.IsTrue(inferredType.ElementUnionType.Types.Contains(GDSemanticType.FromRuntimeTypeName("Sprite2D")));
    }

    #endregion

    #region Confidence Tests

    [TestMethod]
    public void ClassLevelContainer_KnownValueType_ReturnsPotentialConfidence()
    {
        // Arrange
        var code = @"
class_name TestClass

var entities = {}

func create():
    entities[0] = Node2D.new()

func access():
    var pos = entities[0].position
";
        var model = BuildSemanticModel(code);
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        // Find the member access: entities[0].position
        var memberAccess = classDecl!.AllNodes
            .OfType<GDMemberOperatorExpression>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "position");

        Assert.IsNotNull(memberAccess, "Should find position member access");

        // Act
        var confidence = model.GetMemberAccessConfidence(memberAccess);

        // Assert
        Assert.AreEqual(GDReferenceConfidence.Potential, confidence,
            "Access to known container element should have Potential confidence");
    }

    [TestMethod]
    public void ClassLevelContainer_UnknownValueType_StillReturnsPotentialForIndexer()
    {
        // Arrange - container without any assignments
        var code = @"
class_name TestClass

var data = {}

func access():
    var x = data[0].foo
";
        var model = BuildSemanticModel(code);
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        // Find the member access: data[0].foo
        var memberAccess = classDecl!.AllNodes
            .OfType<GDMemberOperatorExpression>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "foo");

        Assert.IsNotNull(memberAccess, "Should find foo member access");

        // Act
        var confidence = model.GetMemberAccessConfidence(memberAccess);

        // Assert
        // Even without known type, indexer access should be Potential (duck-typing pattern)
        Assert.AreEqual(GDReferenceConfidence.Potential, confidence,
            "Indexer-based access should have Potential confidence");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void TypedContainer_NotTrackedAsClassContainer()
    {
        // Arrange - typed containers should NOT be tracked (they have explicit types)
        var code = @"
class_name TestClass

var entities: Dictionary[int, Node2D] = {}

func create():
    entities[0] = Node2D.new()
";
        var model = BuildSemanticModel(code);

        // Act
        var profile = model.GetClassContainerProfile("TestClass", "entities");

        // Assert
        Assert.IsNull(profile,
            "Typed containers should not be tracked (type is already known)");
    }

    [TestMethod]
    public void LocalContainer_NotTrackedAsClassContainer()
    {
        // Arrange - local variables should NOT be in class container profiles
        var code = @"
class_name TestClass

func test():
    var local_items = []
    local_items.append(Node.new())
";
        var model = BuildSemanticModel(code);

        // Act
        var profile = model.GetClassContainerProfile("TestClass", "local_items");

        // Assert
        Assert.IsNull(profile,
            "Local container should not be in class container profiles");
    }

    #endregion

    #region No Initializer Tests

    [TestMethod]
    public void ClassContainer_NoInitializer_AssignedDictionary_IsTracked()
    {
        // Arrange - variable without initializer but assigned dictionary
        var code = @"
class_name TestClass

var data

func test():
    data = {}
    data[0] = Node.new()
";
        var model = BuildSemanticModel(code);

        // Act
        var profile = model.GetClassContainerProfile("TestClass", "data");

        // Assert
        Assert.IsNotNull(profile,
            "Variable without initializer but assigned Dictionary should be tracked");
        Assert.IsTrue(profile.IsDictionary, "Container should be detected as Dictionary");

        var inferredType = profile.ComputeInferredType();
        Assert.AreEqual("Node", inferredType.EffectiveElementType.DisplayName,
            "Value type should be inferred from Node.new() assignment");
    }

    [TestMethod]
    public void ClassContainer_NoInitializer_AssignedArray_IsTracked()
    {
        // Arrange
        var code = @"
class_name TestClass

var items

func setup():
    items = []
    items.append(Node2D.new())
";
        var model = BuildSemanticModel(code);

        // Act
        var profile = model.GetClassContainerProfile("TestClass", "items");

        // Assert
        Assert.IsNotNull(profile,
            "Variable without initializer but assigned Array should be tracked");
        Assert.IsFalse(profile.IsDictionary, "Container should be detected as Array");

        var inferredType = profile.ComputeInferredType();
        Assert.AreEqual("Node2D", inferredType.EffectiveElementType.DisplayName);
    }

    [TestMethod]
    public void ClassContainer_NullInitializer_AssignedDictionary_IsTracked()
    {
        // Arrange
        var code = @"
class_name TestClass

var data = null

func init():
    data = {}
    data[""key""] = ""value""
";
        var model = BuildSemanticModel(code);

        // Act
        var profile = model.GetClassContainerProfile("TestClass", "data");

        // Assert
        Assert.IsNotNull(profile,
            "Variable with null initializer but assigned Dictionary should be tracked");
        Assert.IsTrue(profile.IsDictionary);

        var inferredType = profile.ComputeInferredType();
        Assert.AreEqual("String", inferredType.EffectiveElementType.DisplayName);
    }

    [TestMethod]
    public void ClassContainer_MultipleAssignments_AllDictionaries_IsTracked()
    {
        // Arrange
        var code = @"
class_name TestClass

var cache

func init_empty():
    cache = {}

func init_with_value():
    cache = {}
    cache[""x""] = 1
";
        var model = BuildSemanticModel(code);

        // Act
        var profile = model.GetClassContainerProfile("TestClass", "cache");

        // Assert
        Assert.IsNotNull(profile,
            "Variable with multiple Dictionary assignments should be tracked");
        Assert.IsTrue(profile.IsDictionary);
    }

    [TestMethod]
    public void ClassContainer_MixedAssignments_ArrayAndDict_TrackedAsUnion()
    {
        // Arrange - both Array and Dictionary assignments
        var code = @"
class_name TestClass

var data

func use_as_dict():
    data = {}
    data[""key""] = 1

func use_as_array():
    data = []
    data.append(2)
";
        var model = BuildSemanticModel(code);

        // Act
        var profile = model.GetClassContainerProfile("TestClass", "data");

        // Assert
        Assert.IsNotNull(profile,
            "Variable with mixed Array/Dict assignments should be tracked as Union");
        Assert.IsTrue(profile.IsUnion, "Should be detected as Union type");
        Assert.IsTrue(profile.IsArray, "Should include Array");
        Assert.IsTrue(profile.IsDictionary, "Should include Dictionary");
    }

    [TestMethod]
    public void ClassContainer_NonContainerAssignment_NotTracked()
    {
        // Arrange - assigned from function call (not literal container)
        var code = @"
class_name TestClass

var data

func init():
    data = get_something()
    data[0] = 1
";
        var model = BuildSemanticModel(code);

        // Act
        var profile = model.GetClassContainerProfile("TestClass", "data");

        // Assert
        Assert.IsNull(profile,
            "Variable assigned from non-literal should not be tracked");
    }

    [TestMethod]
    public void ClassContainer_NoInitializer_NoAssignments_NotTracked()
    {
        // Arrange - variable without initializer and without any assignments
        var code = @"
class_name TestClass

var data

func use():
    var x = data[0]
";
        var model = BuildSemanticModel(code);

        // Act
        var profile = model.GetClassContainerProfile("TestClass", "data");

        // Assert
        Assert.IsNull(profile,
            "Variable without initializer and without assignments should not be tracked");
    }

    [TestMethod]
    public void ClassContainer_ContainerAssignment_InMultipleMethods_Tracked()
    {
        // Arrange
        var code = @"
class_name TestClass

var cache

func _ready():
    cache = {}

func add(k, v):
    cache[k] = v

func get_value(k):
    return cache[k]
";
        var model = BuildSemanticModel(code);

        // Act
        var profile = model.GetClassContainerProfile("TestClass", "cache");

        // Assert
        Assert.IsNotNull(profile,
            "Variable with container assignment in any method should be tracked");
        Assert.IsTrue(profile.IsDictionary);
    }

    #endregion

    #region Helper Methods

    private static GDSemanticModel BuildSemanticModel(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        if (classDecl == null)
            throw new System.Exception("Failed to parse code");

        var reference = new GDScriptReference("test://virtual/test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null, null, null);

        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        return collector.BuildSemanticModel();
    }

    #endregion
}
