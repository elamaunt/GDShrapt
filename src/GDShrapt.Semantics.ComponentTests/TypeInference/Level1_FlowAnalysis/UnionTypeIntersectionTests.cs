using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Level 1: Tests for Union type intersection.
/// Tests validate that:
/// - GDUnionType.IntersectWithType() correctly computes type intersections
/// - Type narrowing for 'in' operator uses intersection when variable has Union type
/// </summary>
[TestClass]
public class UnionTypeIntersectionTests
{
    #region Basic Intersection - GDUnionType.IntersectWithType()

    [TestMethod]
    public void IntersectWithType_IntStringNull_WithInt_ReturnsInt()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddTypeName("int");
        union.AddTypeName("String");
        union.AddTypeName("null");

        // Act
        var result = union.IntersectWithType(GDSemanticType.FromRuntimeTypeName("int"), null);

        // Assert
        Assert.IsTrue(result.IsSingleType, "Result should be single type");
        Assert.AreEqual("int", result.EffectiveType.DisplayName, "Intersection of int|String|null with int should be int");
    }

    [TestMethod]
    public void IntersectWithType_IntString_WithString_ReturnsString()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddTypeName("int");
        union.AddTypeName("String");

        // Act
        var result = union.IntersectWithType(GDSemanticType.FromRuntimeTypeName("String"), null);

        // Assert
        Assert.IsTrue(result.IsSingleType, "Result should be single type");
        Assert.AreEqual("String", result.EffectiveType.DisplayName, "Intersection of int|String with String should be String");
    }

    [TestMethod]
    public void IntersectWithType_StringOnly_WithInt_ReturnsEmpty()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddTypeName("String");

        // Act
        var result = union.IntersectWithType(GDSemanticType.FromRuntimeTypeName("int"), null);

        // Assert
        Assert.IsTrue(result.IsEmpty, "Intersection of String with int should be empty (incompatible types)");
    }

    [TestMethod]
    public void IntersectWithType_EmptyUnion_WithInt_ReturnsInt()
    {
        // Arrange
        var union = new GDUnionType();

        // Act
        var result = union.IntersectWithType(GDSemanticType.FromRuntimeTypeName("int"), null);

        // Assert
        Assert.IsTrue(result.IsSingleType, "Result should be single type");
        Assert.AreEqual("int", result.EffectiveType.DisplayName, "Intersection of empty union with int should be int");
    }

    [TestMethod]
    public void IntersectWithType_ExactMatch_ReturnsSameType()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddTypeName("Node");

        // Act
        var result = union.IntersectWithType(GDSemanticType.FromRuntimeTypeName("Node"), null);

        // Assert
        Assert.IsTrue(result.IsSingleType, "Result should be single type");
        Assert.AreEqual("Node", result.EffectiveType.DisplayName, "Intersection of Node with Node should be Node");
    }

    #endregion

    #region Numeric Compatibility

    [TestMethod]
    public void IntersectWithType_IntString_WithFloat_ReturnsFloat()
    {
        // Arrange: int is compatible with float (numeric)
        var union = new GDUnionType();
        union.AddTypeName("int");
        union.AddTypeName("String");

        // Act
        var result = union.IntersectWithType(GDSemanticType.FromRuntimeTypeName("float"), null);

        // Assert: int is compatible with float, so intersection should not be empty
        Assert.IsFalse(result.IsEmpty, "int is compatible with float (numeric), intersection should not be empty");
        Assert.AreEqual("float", result.EffectiveType.DisplayName, "Result should be float (target type preferred)");
    }

    [TestMethod]
    public void IntersectWithType_Float_WithInt_ReturnsInt()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddTypeName("float");

        // Act
        var result = union.IntersectWithType(GDSemanticType.FromRuntimeTypeName("int"), null);

        // Assert: float is compatible with int (numeric)
        Assert.IsFalse(result.IsEmpty, "float is compatible with int (numeric)");
        Assert.AreEqual("int", result.EffectiveType.DisplayName);
    }

    [TestMethod]
    public void IntersectWithType_IntFloat_WithInt_ReturnsInt()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddTypeName("int");
        union.AddTypeName("float");

        // Act
        var result = union.IntersectWithType(GDSemanticType.FromRuntimeTypeName("int"), null);

        // Assert
        Assert.IsFalse(result.IsEmpty);
        // Both int and float are compatible with int
    }

    #endregion

    #region Inheritance - With RuntimeProvider

    [TestMethod]
    public void IntersectWithType_NodeRefCounted_WithNode_ReturnsNode()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddTypeName("Node");
        union.AddTypeName("RefCounted");

        var provider = GDDefaultRuntimeProvider.Instance;

        // Act
        var result = union.IntersectWithType(GDSemanticType.FromRuntimeTypeName("Node"), provider);

        // Assert: Only Node is compatible with Node (RefCounted is not a subclass)
        Assert.IsFalse(result.IsEmpty, "Node should be compatible with Node");
        Assert.AreEqual("Node", result.EffectiveType.DisplayName);
    }

    [TestMethod]
    public void IntersectWithType_Node2DSprite2D_WithNode2D_IncludesBoth()
    {
        // Arrange: Sprite2D extends Node2D
        var union = new GDUnionType();
        union.AddTypeName("Node2D");
        union.AddTypeName("Sprite2D");

        var provider = GDDefaultRuntimeProvider.Instance;

        // Act
        var result = union.IntersectWithType(GDSemanticType.FromRuntimeTypeName("Node2D"), provider);

        // Assert: Both types are compatible with Node2D (Sprite2D is subclass)
        Assert.IsFalse(result.IsEmpty);
    }

    [TestMethod]
    public void IntersectWithType_Control_WithNode_ReturnsControl()
    {
        // Arrange: Control inherits from Node (in TypesMap: Control -> Node)
        var union = new GDUnionType();
        union.AddTypeName("Control");

        var provider = GDDefaultRuntimeProvider.Instance;

        // Act
        var result = union.IntersectWithType(GDSemanticType.FromRuntimeTypeName("Node"), provider);

        // Assert: Control is assignable to Node, so intersection is valid
        Assert.IsFalse(result.IsEmpty,
            $"Control should be assignable to Node. IsAssignableTo={provider.IsAssignableTo("Control", "Node")}, BaseType={provider.GetBaseType("Control")}");
        Assert.AreEqual("Control", result.EffectiveType.DisplayName, "Result should be Control (more specific type)");
    }

    [TestMethod]
    public void IntersectWithType_NodeString_WithControl_ReturnsControl()
    {
        // Arrange: Node and String, intersect with Control (subclass of Node)
        var union = new GDUnionType();
        union.AddTypeName("Node");
        union.AddTypeName("String");

        var provider = GDDefaultRuntimeProvider.Instance;

        // Act
        var result = union.IntersectWithType(GDSemanticType.FromRuntimeTypeName("Control"), provider);

        // Assert: Control is a subclass of Node, so Node ∩ Control = Control (more specific)
        Assert.IsFalse(result.IsEmpty);
    }

    #endregion

    #region In Operator with Union Type - Integration

    [TestMethod]
    public void InOperator_VariableHasUnion_NarrowsToIntersection()
    {
        var code = @"
func test():
    var x
    if some_condition():
        x = 42
    else:
        x = ""str""
    if x in [1, 2, 3]:
        return x + 1
";
        // This test verifies that after the 'in' check, x is narrowed from int|String to int
        // The actual flow analysis integration will be tested when GDFlowAnalyzer is updated
        var result = AnalyzeNarrowedTypeInIfBranch(code, "x");

        // After 'if x in [1,2,3]:', x should be narrowed to int
        Assert.AreEqual("int", result.NarrowedType,
            $"Variable x should be narrowed to int after 'in [1,2,3]'. Actual: {result.NarrowedType}");
    }

    [TestMethod]
    public void InOperator_VariableIsVariant_NarrowsToElementType()
    {
        var code = @"
func test(x):
    if x in [""a"", ""b""]:
        return x.length()
";
        var result = AnalyzeNarrowedTypeInIfBranch(code, "x");
        Assert.AreEqual("String", result.NarrowedType,
            $"Variant x should be narrowed to String after 'in [\"a\", \"b\"]'. Actual: {result.NarrowedType}");
    }

    [TestMethod]
    public void InOperator_UnionIntFloat_WithIntArray_NarrowsToInt()
    {
        var code = @"
func test():
    var x
    if rand_bool():
        x = 42
    else:
        x = 3.14
    if x in [1, 2, 3]:
        return x
";
        var result = AnalyzeNarrowedTypeInIfBranch(code, "x");
        // int|float intersected with int should give int
        Assert.AreEqual("int", result.NarrowedType,
            $"int|float should narrow to int after 'in [1,2,3]'. Actual: {result.NarrowedType}");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void IntersectWithType_NullType_WithInt_ReturnsEmpty()
    {
        // Arrange: null cannot intersect with int
        var union = new GDUnionType();
        union.AddTypeName("null");

        // Act
        var result = union.IntersectWithType(GDSemanticType.FromRuntimeTypeName("int"), null);

        // Assert
        Assert.IsTrue(result.IsEmpty, "null cannot intersect with int");
    }

    [TestMethod]
    public void IntersectWithType_Variant_WithInt_ReturnsInt()
    {
        // Arrange: Variant is compatible with anything
        var union = new GDUnionType();
        union.AddTypeName("Variant");

        // Act
        var result = union.IntersectWithType(GDSemanticType.FromRuntimeTypeName("int"), null);

        // Assert: Variant is filtered by AddType, so union might be empty
        // or Variant is treated specially
        // For now, we skip Variant in AddType, so union is empty
        // If empty, intersectWithType returns the target type
        Assert.AreEqual("int", result.EffectiveType.DisplayName);
    }

    #endregion

    #region Helper Methods

    private record NarrowingResult(string? NarrowedType, bool IsNonNull);

    private static NarrowingResult AnalyzeNarrowedTypeInIfBranch(string code, string variableName)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members.OfType<GDMethodDeclaration>().FirstOrDefault();

        if (method == null)
            return new NarrowingResult(null, false);

        // Find the last if statement (the one with 'in' operator)
        var ifStatement = method.Statements?.OfType<GDIfStatement>().LastOrDefault();
        if (ifStatement?.IfBranch?.Condition == null)
            return new NarrowingResult(null, false);

        var condition = ifStatement.IfBranch.Condition;

        // Use GDTypeNarrowingAnalyzer to analyze the condition
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider());
        var context = analyzer.AnalyzeCondition(condition, isNegated: false);

        // Check for concrete type first
        var concreteType = context.GetConcreteType(variableName);
        if (concreteType != null)
            return new NarrowingResult(concreteType.DisplayName, concreteType.DisplayName != "null");

        // Check for duck type narrowing
        var duckType = context.GetNarrowedType(variableName);
        if (duckType != null && duckType.PossibleTypes.Count > 0)
        {
            var narrowedType = duckType.PossibleTypes.First().DisplayName;
            return new NarrowingResult(narrowedType, true);
        }

        return new NarrowingResult(null, false);
    }

    #endregion
}
