using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Tests for duck typing analysis in GDShrapt.
/// </summary>
[TestClass]
public class DuckTypingTests
{
    #region Duck Type Collection Tests

    [TestMethod]
    public void DuckType_MethodCall_CollectsRequiredMethod()
    {
        // Arrange
        var code = @"
func process(obj):
    obj.move()
";
        var (_, semanticModel) = AnalyzeCode(code);

        // Act
        var duckType = semanticModel?.GetDuckType("obj");

        // Assert
        Assert.IsNotNull(duckType, "Duck type should be collected for untyped parameter");
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("move"), "Required method 'move' should be collected");
    }

    [TestMethod]
    public void DuckType_MultipleMethodCalls_CollectsAllMethods()
    {
        // Arrange
        var code = @"
func process(entity):
    entity.move()
    entity.attack()
    entity.take_damage(10)
";
        var (_, semanticModel) = AnalyzeCode(code);

        // Act
        var duckType = semanticModel?.GetDuckType("entity");

        // Assert
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("move"));
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("attack"));
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("take_damage"));
        Assert.AreEqual(3, duckType.RequiredMethods.Count);
    }

    [TestMethod]
    public void DuckType_PropertyAccess_CollectsRequiredProperty()
    {
        // Arrange
        var code = @"
func process(entity):
    var h = entity.health
";
        var (_, semanticModel) = AnalyzeCode(code);

        // Act
        var duckType = semanticModel?.GetDuckType("entity");

        // Assert
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.RequiredProperties.ContainsKey("health"), "Required property 'health' should be collected");
    }

    [TestMethod]
    public void DuckType_PropertyWrite_CollectsRequiredProperty()
    {
        // Arrange
        var code = @"
func process(entity):
    entity.health = 100
";
        var (_, semanticModel) = AnalyzeCode(code);

        // Act
        var duckType = semanticModel?.GetDuckType("entity");

        // Assert
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.RequiredProperties.ContainsKey("health"));
    }

    [TestMethod]
    public void DuckType_MixedAccess_CollectsBothMethodsAndProperties()
    {
        // Arrange
        var code = @"
func process(obj):
    obj.name = ""test""
    obj.update()
    var x = obj.position
";
        var (_, semanticModel) = AnalyzeCode(code);

        // Act
        var duckType = semanticModel?.GetDuckType("obj");

        // Assert
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("update"));
        Assert.IsTrue(duckType.RequiredProperties.ContainsKey("name"));
        Assert.IsTrue(duckType.RequiredProperties.ContainsKey("position"));
    }

    [TestMethod]
    public void DuckType_TypedVariable_StillCollectsDuckType()
    {
        // Arrange
        // Note: Currently duck type is collected even for typed parameters because
        // the duck type collector runs after scope validation and doesn't have
        // access to parameter scopes. This is acceptable behavior - the type info
        // is still available via the declared type.
        var code = @"
func process(obj: Node2D):
    obj.position = Vector2.ZERO
";
        var (_, semanticModel) = AnalyzeCode(code);

        // Act
        var duckType = semanticModel?.GetDuckType("obj");

        // Assert
        // Currently duck type IS collected even for typed params
        // The effective type will still be Node2D from the declared type
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.RequiredProperties.ContainsKey("position"));
    }

    [TestMethod]
    public void DuckType_ChainedAccess_CollectsRootVariable()
    {
        // Arrange
        var code = @"
func process(entity):
    entity.stats.health = 100
";
        var (_, semanticModel) = AnalyzeCode(code);

        // Act
        var duckType = semanticModel?.GetDuckType("entity");

        // Assert
        Assert.IsNotNull(duckType);
        // The root variable 'entity' should have 'stats' as required property
        Assert.IsTrue(duckType.RequiredProperties.ContainsKey("stats"));
    }

    #endregion

    #region Duck Type Compatibility Tests

    [TestMethod]
    public void DuckType_IsCompatibleWith_EmptyType_ReturnsTrue()
    {
        // Arrange
        var duckType = new GDDuckType();
        duckType.RequireMethod("move");

        var provider = new GDDefaultRuntimeProvider();
        var resolver = new GDDuckTypeResolver(provider);

        // Act - empty type string means unknown type
        var result = resolver.IsCompatibleWith(duckType, "");

        // Assert
        Assert.IsTrue(result, "Duck type should be compatible with unknown type");
    }

    [TestMethod]
    public void DuckType_IsCompatibleWith_NullType_ReturnsTrue()
    {
        // Arrange
        var duckType = new GDDuckType();
        duckType.RequireMethod("attack");

        var provider = new GDDefaultRuntimeProvider();
        var resolver = new GDDuckTypeResolver(provider);

        // Act
        var result = resolver.IsCompatibleWith(duckType, null!);

        // Assert
        Assert.IsTrue(result, "Duck type should be compatible with null type");
    }

    [TestMethod]
    public void DuckType_ExcludedTypes_BlocksCompatibility()
    {
        // Arrange
        var duckType = new GDDuckType();
        duckType.ExcludeTypeName("Enemy");

        var provider = new GDDefaultRuntimeProvider();
        var resolver = new GDDuckTypeResolver(provider);

        // Act
        var result = resolver.IsCompatibleWith(duckType, "Enemy");

        // Assert
        Assert.IsFalse(result, "Excluded type should not be compatible");
    }

    [TestMethod]
    public void DuckType_PossibleTypes_OnlyAllowsListed()
    {
        // Arrange
        var duckType = new GDDuckType();
        duckType.AddPossibleTypeName("Player");
        duckType.AddPossibleTypeName("Enemy");

        var provider = new GDDefaultRuntimeProvider();
        var resolver = new GDDuckTypeResolver(provider);

        // Act & Assert
        // Types in PossibleTypes should be compatible
        // Note: This requires a provider that knows about type inheritance
        // For simplicity, exact match is checked
        Assert.IsFalse(resolver.IsCompatibleWith(duckType, "NPC"), "Type not in PossibleTypes should not be compatible");
    }

    #endregion

    #region Duck Type Merge Tests

    [TestMethod]
    public void DuckType_MergeWith_CombinesRequirements()
    {
        // Arrange
        var duckType1 = new GDDuckType();
        duckType1.RequireMethod("move");
        duckType1.RequireProperty("health");

        var duckType2 = new GDDuckType();
        duckType2.RequireMethod("attack");
        duckType2.RequireProperty("damage");

        // Act
        duckType1.MergeWith(duckType2);

        // Assert
        Assert.IsTrue(duckType1.RequiredMethods.ContainsKey("move"));
        Assert.IsTrue(duckType1.RequiredMethods.ContainsKey("attack"));
        Assert.IsTrue(duckType1.RequiredProperties.ContainsKey("health"));
        Assert.IsTrue(duckType1.RequiredProperties.ContainsKey("damage"));
    }

    [TestMethod]
    public void DuckType_IntersectWith_KeepsCommonRequirements()
    {
        // Arrange
        var duckType1 = new GDDuckType();
        duckType1.RequireMethod("move");
        duckType1.RequireMethod("common");

        var duckType2 = new GDDuckType();
        duckType2.RequireMethod("attack");
        duckType2.RequireMethod("common");

        // Act
        var intersection = duckType1.IntersectWith(duckType2);

        // Assert
        // Intersection merges all requirements (both must have all methods)
        Assert.IsTrue(intersection.RequiredMethods.ContainsKey("move"));
        Assert.IsTrue(intersection.RequiredMethods.ContainsKey("attack"));
        Assert.IsTrue(intersection.RequiredMethods.ContainsKey("common"));
    }

    #endregion

    #region Effective Type Tests

    [TestMethod]
    public void GetEffectiveType_UntypedVariable_ReturnsDuckTypeString()
    {
        // Arrange
        var code = @"
func process(obj):
    obj.move()
    obj.attack()
";
        var (_, semanticModel) = AnalyzeCode(code);

        // Act
        var effectiveType = semanticModel?.GetEffectiveType("obj");

        // Assert
        Assert.IsNotNull(effectiveType);
        Assert.IsTrue(effectiveType.Contains("DuckType"), "Effective type should contain DuckType representation");
        Assert.IsTrue(effectiveType.Contains("move") || effectiveType.Contains("methods"), "Should contain method info");
    }

    [TestMethod]
    public void GetEffectiveType_TypedVariable_ReturnsType()
    {
        // Arrange
        var code = @"
var player: Player

func _ready():
    player = Player.new()
";
        var (_, semanticModel) = AnalyzeCode(code);

        // Act
        var effectiveType = semanticModel?.GetEffectiveType("player");

        // Assert
        // Typed variable should return the declared type
        Assert.AreEqual("Player", effectiveType);
    }

    #endregion

    #region Duck Type Inference and Confidence Tests

    /// <summary>
    /// Tests that slice() method usage infers Array type for parameter.
    /// </summary>
    [TestMethod]
    public void InferParameterType_WithSliceMethod_InfersArray()
    {
        // Arrange
        var code = @"
func process(data):
    data.slice(1)
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(model);

        var method = classDecl.Methods
            .FirstOrDefault(m => m.Identifier?.Sequence == "process");
        Assert.IsNotNull(method);

        var param = method.Parameters?.FirstOrDefault();
        Assert.IsNotNull(param);

        // Act - infer type from usage
        var inferredType = model.InferParameterType(param);

        // Assert - should infer Array (or union containing Array) because slice() is Array-specific
        // Note: slice() exists on Array and all Packed*Array types, so we may get a union
        Assert.IsTrue(inferredType.TypeName.DisplayName.Contains("Array"),
            $"Parameter using slice() should infer type containing Array. Got: {inferredType.TypeName.DisplayName}");
    }

    /// <summary>
    /// Tests that is_empty() + slice() usage infers Array type.
    /// </summary>
    [TestMethod]
    public void InferParameterType_WithIsEmptyAndSlice_InfersArray()
    {
        // Arrange - accumulate_left pattern from cyclic_inference.gd
        var code = @"
func accumulate_left(list, func_ref, initial):
    if list.is_empty():
        return initial
    var head = list[0]
    var tail = list.slice(1)
    return accumulate_left(tail, func_ref, func_ref.call(initial, head))
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(model);

        var method = classDecl.Methods
            .FirstOrDefault(m => m.Identifier?.Sequence == "accumulate_left");
        Assert.IsNotNull(method);

        var listParam = method.Parameters?.FirstOrDefault(p => p.Identifier?.Sequence == "list");
        Assert.IsNotNull(listParam);

        // Act
        var inferredType = model.InferParameterType(listParam);

        // Assert - is_empty + slice + indexable = Array (or union containing Array)
        // Note: slice() + is_empty() exist on Array and Packed*Array types
        Assert.IsTrue(inferredType.TypeName.DisplayName.Contains("Array"),
            $"Parameter with is_empty() + slice() should infer type containing Array. Got: {inferredType.TypeName.DisplayName}");
    }

    /// <summary>
    /// Tests that GetMemberAccessConfidence returns Potential for duck-typed parameters.
    /// </summary>
    [TestMethod]
    public void GetMemberAccessConfidence_DuckTypedParameter_ReturnsPotential()
    {
        // Arrange
        var code = @"
func process(data):
    data.is_empty()
    data.slice(1)
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(model);

        // Find the is_empty() member access
        var isEmptyCall = FindMemberAccess(classDecl, "is_empty");
        Assert.IsNotNull(isEmptyCall, "Could not find is_empty member access");

        // Act
        var confidence = model.GetMemberAccessConfidence(isEmptyCall);

        // Assert - should be Potential because duck-type constraints exist
        Assert.AreEqual(GDReferenceConfidence.Potential, confidence,
            "Duck-typed parameter should have Potential confidence, not NameMatch");
    }

    private static GDMemberOperatorExpression? FindMemberAccess(GDClassDeclaration classDecl, string memberName)
    {
        GDMemberOperatorExpression? found = null;
        var visitor = new MemberAccessFinder(memberName, m => found = m);
        classDecl.WalkIn(visitor);
        return found;
    }

    private class MemberAccessFinder : GDVisitor
    {
        private readonly string _memberName;
        private readonly System.Action<GDMemberOperatorExpression> _onFound;

        public MemberAccessFinder(string memberName, System.Action<GDMemberOperatorExpression> onFound)
        {
            _memberName = memberName;
            _onFound = onFound;
        }

        public override void Visit(GDMemberOperatorExpression e)
        {
            if (e.Identifier?.Sequence == _memberName)
                _onFound(e);
        }
    }

    #endregion

    #region Unknown Method Tests - Should Produce Warning

    /// <summary>
    /// Tests that calling unknown method (not in TypesMap) returns NameMatch confidence.
    /// This should produce GD7003 warning.
    /// </summary>
    [TestMethod]
    public void GetMemberAccessConfidence_UnknownMethod_ReturnsNameMatch()
    {
        // Arrange - some_unknown_method does NOT exist in any Godot type
        var code = @"
func process(data):
    data.some_unknown_method()
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(model);

        var unknownCall = FindMemberAccess(classDecl, "some_unknown_method");
        Assert.IsNotNull(unknownCall, "Could not find some_unknown_method member access");

        // Act
        var confidence = model.GetMemberAccessConfidence(unknownCall);

        // Assert - should be NameMatch because method is unknown
        Assert.AreEqual(GDReferenceConfidence.NameMatch, confidence,
            "Unknown method should have NameMatch confidence (produces warning)");
    }

    /// <summary>
    /// Tests that accessing unknown property (not in TypesMap) returns NameMatch confidence.
    /// </summary>
    [TestMethod]
    public void GetMemberAccessConfidence_UnknownProperty_ReturnsNameMatch()
    {
        // Arrange - xyz_nonexistent does NOT exist in any Godot type
        var code = @"
func process(data):
    var x = data.xyz_nonexistent
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(model);

        var unknownAccess = FindMemberAccess(classDecl, "xyz_nonexistent");
        Assert.IsNotNull(unknownAccess, "Could not find xyz_nonexistent member access");

        // Act
        var confidence = model.GetMemberAccessConfidence(unknownAccess);

        // Assert - should be NameMatch because property is unknown
        Assert.AreEqual(GDReferenceConfidence.NameMatch, confidence,
            "Unknown property should have NameMatch confidence (produces warning)");
    }

    #endregion

    #region Known Godot Method Tests - Should NOT Produce Warning

    /// <summary>
    /// Tests that calling known Godot method (queue_free) returns Potential confidence.
    /// </summary>
    [TestMethod]
    public void GetMemberAccessConfidence_KnownGodotMethod_ReturnsPotential()
    {
        // Arrange - queue_free exists on Node and subclasses
        var code = @"
func process(node):
    node.queue_free()
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(model);

        var queueFreeCall = FindMemberAccess(classDecl, "queue_free");
        Assert.IsNotNull(queueFreeCall, "Could not find queue_free member access");

        // Act
        var confidence = model.GetMemberAccessConfidence(queueFreeCall);

        // Assert - should be Potential because queue_free exists in TypesMap
        Assert.AreEqual(GDReferenceConfidence.Potential, confidence,
            "Known Godot method should have Potential confidence (no warning)");
    }

    /// <summary>
    /// Tests that accessing known Vector2 property (x, y) returns Potential confidence.
    /// </summary>
    [TestMethod]
    public void GetMemberAccessConfidence_KnownVectorProperty_ReturnsPotential()
    {
        // Arrange - 'x' property exists on Vector2, Vector3, etc.
        var code = @"
func process(vec):
    var x = vec.x
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(model);

        var xAccess = FindMemberAccess(classDecl, "x");
        Assert.IsNotNull(xAccess, "Could not find 'x' member access");

        // Act
        var confidence = model.GetMemberAccessConfidence(xAccess);

        // Assert - should be Potential because 'x' property exists in TypesMap
        Assert.AreEqual(GDReferenceConfidence.Potential, confidence,
            "Known vector property should have Potential confidence (no warning)");
    }

    #endregion

    #region Multiple Methods - Union Type Inference

    /// <summary>
    /// Tests that using multiple Godot methods infers a union of types.
    /// </summary>
    [TestMethod]
    public void InferParameterType_MultipleGodotMethods_InfersUnionType()
    {
        // Arrange - queue_free and get_parent are both Node methods
        var code = @"
func process(node):
    node.queue_free()
    var parent = node.get_parent()
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(model);

        var method = classDecl.Methods.FirstOrDefault(m => m.Identifier?.Sequence == "process");
        Assert.IsNotNull(method);
        var param = method.Parameters?.FirstOrDefault();
        Assert.IsNotNull(param);

        // Act
        var inferredType = model.InferParameterType(param);

        // Assert - should infer Node (or union containing Node)
        Assert.IsTrue(inferredType.TypeName.DisplayName.Contains("Node"),
            $"Multiple Node methods should infer type containing Node. Got: {inferredType.TypeName.DisplayName}");
    }

    #endregion

    #region Explicit Type Annotation - No Duck Typing

    /// <summary>
    /// Tests that parameter with explicit type annotation returns Strict confidence.
    /// </summary>
    [TestMethod]
    public void GetMemberAccessConfidence_ExplicitType_ReturnsStrict()
    {
        // Arrange - data has explicit Array type
        var code = @"
func process(data: Array):
    data.append(1)
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(model);

        var appendCall = FindMemberAccess(classDecl, "append");
        Assert.IsNotNull(appendCall, "Could not find append member access");

        // Act
        var confidence = model.GetMemberAccessConfidence(appendCall);

        // Assert - should be Strict because parameter has explicit type
        Assert.AreEqual(GDReferenceConfidence.Strict, confidence,
            "Explicit type annotation should have Strict confidence");
    }

    #endregion

    #region Object Member Filtering Tests

    /// <summary>
    /// Tests that has_method (Object method) is NOT included in RequiredMethods.
    /// </summary>
    [TestMethod]
    public void DuckType_FilterObjectMethods_HasMethodNotIncluded()
    {
        // has_method is Object method - should NOT be in RequiredMethods
        var code = @"
func process(obj):
    if obj.has_method(""attack""):
        obj.attack()
";
        var (_, semanticModel) = AnalyzeCode(code);
        var duckType = semanticModel?.GetDuckType("obj");

        Assert.IsNotNull(duckType, "Duck type should be collected");
        // attack should be required (actual method being called)
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("attack"),
            "attack should be in RequiredMethods");
        // has_method should NOT be required (Object method)
        Assert.IsFalse(duckType.RequiredMethods.ContainsKey("has_method"),
            "has_method should be filtered as Object method");
    }

    /// <summary>
    /// Tests that get_class (Object method) is NOT included in RequiredMethods.
    /// </summary>
    [TestMethod]
    public void DuckType_FilterObjectMethods_GetClassNotIncluded()
    {
        // get_class is Object method - should NOT be in RequiredMethods
        var code = @"
func identify(obj):
    var class_name = obj.get_class()
    obj.custom_method()
";
        var (_, semanticModel) = AnalyzeCode(code);
        var duckType = semanticModel?.GetDuckType("obj");

        Assert.IsNotNull(duckType, "Duck type should be collected");
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("custom_method"),
            "custom_method should be in RequiredMethods");
        Assert.IsFalse(duckType.RequiredMethods.ContainsKey("get_class"),
            "get_class should be filtered as Object method");
    }

    /// <summary>
    /// Tests that connect (Object method) is NOT included in RequiredMethods.
    /// </summary>
    [TestMethod]
    public void DuckType_FilterObjectMethods_ConnectNotIncluded()
    {
        // connect is Object method for signal connections
        var code = @"
func setup(obj):
    obj.connect(""pressed"", _on_pressed)
    obj.custom_signal_handler()
";
        var (_, semanticModel) = AnalyzeCode(code);
        var duckType = semanticModel?.GetDuckType("obj");

        Assert.IsNotNull(duckType, "Duck type should be collected");
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("custom_signal_handler"),
            "custom_signal_handler should be in RequiredMethods");
        Assert.IsFalse(duckType.RequiredMethods.ContainsKey("connect"),
            "connect should be filtered as Object method");
    }

    /// <summary>
    /// Tests that non-Object methods are still included normally.
    /// </summary>
    [TestMethod]
    public void DuckType_NonObjectMethods_StillIncluded()
    {
        // Regular methods should still be included
        var code = @"
func process(obj):
    obj.foo()
    obj.bar()
    obj.baz()
";
        var (_, semanticModel) = AnalyzeCode(code);
        var duckType = semanticModel?.GetDuckType("obj");

        Assert.IsNotNull(duckType, "Duck type should be collected");
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("foo"), "foo should be in RequiredMethods");
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("bar"), "bar should be in RequiredMethods");
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("baz"), "baz should be in RequiredMethods");
        Assert.AreEqual(3, duckType.RequiredMethods.Count, "Should have exactly 3 required methods");
    }

    /// <summary>
    /// Tests that multiple Object methods are all filtered.
    /// </summary>
    [TestMethod]
    public void DuckType_FilterObjectMethods_MultipleFiltered()
    {
        // Multiple Object methods should all be filtered
        var code = @"
func check(obj):
    if obj.has_method(""process""):
        if obj.has_signal(""done""):
            var name = obj.get_class()
            obj.call(""process"")
            obj.actual_method()
";
        var (_, semanticModel) = AnalyzeCode(code);
        var duckType = semanticModel?.GetDuckType("obj");

        Assert.IsNotNull(duckType, "Duck type should be collected");
        // Only actual_method should be in RequiredMethods
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("actual_method"),
            "actual_method should be in RequiredMethods");
        Assert.IsFalse(duckType.RequiredMethods.ContainsKey("has_method"),
            "has_method should be filtered");
        Assert.IsFalse(duckType.RequiredMethods.ContainsKey("has_signal"),
            "has_signal should be filtered");
        Assert.IsFalse(duckType.RequiredMethods.ContainsKey("get_class"),
            "get_class should be filtered");
        Assert.IsFalse(duckType.RequiredMethods.ContainsKey("call"),
            "call should be filtered");
    }

    #endregion

    #region Helper Methods

    private static (GDClassDeclaration?, GDSemanticModel?) AnalyzeCode(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        if (classDecl == null)
            return (null, null);

        // Use GDSemanticReferenceCollector to build semantic model
        // Create a virtual script file for testing
        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code); // Parse the code

        // Use composite provider with Godot types for duck-type inference
        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null,  // projectTypesProvider
            null,  // autoloadsProvider
            null); // sceneTypesProvider

        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        var semanticModel = collector.BuildSemanticModel();

        return (classDecl, semanticModel);
    }

    #endregion
}
