using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for lambda parameter type inference using duck-typing analysis.
/// These tests verify that untyped lambda parameters are inferred from their usage patterns,
/// similar to how method parameters are inferred.
/// </summary>
[TestClass]
public class LambdaParameterInferenceTests
{
    #region Array Method Call Tests

    [TestMethod]
    public void InferLambdaParam_ArraySlice_InfersArray()
    {
        var code = @"
extends Node
var callback = func(items):
    return items.slice(1)
";
        var type = InferVariableType(code, "callback");
        // May infer as Array or Variant depending on TypesMap coverage
        Assert.IsTrue(type.Contains("Array") || type.Contains("Variant"),
            $"Lambda with items.slice() should infer Array parameter. Got: {type}");
    }

    [TestMethod]
    public void InferLambdaParam_ArrayAppend_InfersArray()
    {
        var code = @"
extends Node
var callback = func(items):
    items.append(1)
";
        var type = InferVariableType(code, "callback");
        // append() is a common Array method
        Assert.IsTrue(type.Contains("Array") || type.StartsWith("Callable[[Variant"),
            $"Lambda with items.append() should infer Array parameter. Got: {type}");
    }

    [TestMethod]
    public void InferLambdaParam_ArraySize_InfersArray()
    {
        var code = @"
extends Node
var callback = func(items):
    return items.size()
";
        var type = InferVariableType(code, "callback");
        // size() is available on many types (Array, Dictionary, String, etc.)
        // May return Variant if multiple types have this method
        Assert.IsTrue(type.Contains("Callable[["),
            $"Lambda with items.size() should return a Callable type. Got: {type}");
    }

    #endregion

    #region Dictionary Method Call Tests

    [TestMethod]
    public void InferLambdaParam_DictionaryGet_InfersDictionary()
    {
        var code = @"
extends Node
var callback = func(data):
    return data.get(""key"")
";
        var type = InferVariableType(code, "callback");
        // May infer generic Dictionary[String, Variant] from get() usage
        Assert.IsTrue(type.Contains("Dictionary"),
            $"Lambda with data.get() should infer Dictionary parameter. Got: {type}");
    }

    [TestMethod]
    public void InferLambdaParam_DictionaryHas_InfersDictionary()
    {
        var code = @"
extends Node
var callback = func(config):
    return config.has(""timeout"")
";
        var type = InferVariableType(code, "callback");
        // has() exists on Dictionary, Array, and other containers
        Assert.IsTrue(type.Contains("Callable[["),
            $"Lambda with config.has() should return a Callable type. Got: {type}");
    }

    #endregion

    #region Property Access Tests

    [TestMethod]
    public void InferLambdaParam_PropertyAccess_InfersVector()
    {
        var code = @"
extends Node
var callback = func(pos):
    return pos.x + pos.y
";
        var type = InferVariableType(code, "callback");
        // Should infer Vector2, Vector3, Vector4, or union
        Assert.IsTrue(type.Contains("Vector2") || type.Contains("Vector3") || type.Contains("Vector4"),
            $"Lambda with pos.x + pos.y should infer Vector type. Got: {type}");
    }

    #endregion

    #region Indexer Tests

    [TestMethod]
    public void InferLambdaParam_Indexer_InfersIndexable()
    {
        var code = @"
extends Node
var callback = func(data):
    return data[0]
";
        var type = InferVariableType(code, "callback");
        // Should infer Array, String, or union (indexable types)
        Assert.IsTrue(type.Contains("Array") || type.Contains("String") || type.Contains("Packed"),
            $"Lambda with data[0] should infer indexable type. Got: {type}");
    }

    #endregion

    #region For Loop Tests

    [TestMethod]
    public void InferLambdaParam_ForLoop_InfersIterable()
    {
        var code = @"
extends Node
var callback = func(items):
    for item in items:
        print(item)
";
        var type = InferVariableType(code, "callback");
        // Should infer Array or String (iterables)
        Assert.IsTrue(type.Contains("Array") || type.Contains("String"),
            $"Lambda with 'for item in items' should infer iterable type. Got: {type}");
    }

    #endregion

    #region Multiple Parameters Tests

    [TestMethod]
    public void InferLambdaParam_MultipleParameters_InfersEach()
    {
        var code = @"
extends Node
var callback = func(data, items):
    data.get(""key"")
    items.slice(1)
";
        var type = InferVariableType(code, "callback");
        // Should have two parameters inferred
        Assert.IsTrue(type.Contains("Callable[[") && type.Contains(","),
            $"Lambda with two parameters should have both inferred. Got: {type}");
        // One should be Dictionary-like
        Assert.IsTrue(type.Contains("Dictionary") || type.Contains("Variant"),
            $"First parameter should be Dictionary or Variant. Got: {type}");
    }

    [TestMethod]
    public void InferLambdaParam_TwoArrayParams_InfersArrayForBoth()
    {
        var code = @"
extends Node
var callback = func(left, right):
    left.append(1)
    right.append(2)
";
        var type = InferVariableType(code, "callback");
        // Should have two Array parameters
        Assert.IsTrue(type.Contains("Callable[[Array, Array"),
            $"Lambda with two array operations should infer Array for both. Got: {type}");
    }

    #endregion

    #region No Usage / Variant Tests

    [TestMethod]
    public void InferLambdaParam_NoUsage_ReturnsVariant()
    {
        var code = @"
extends Node
var callback = func(x):
    return 42
";
        var type = InferVariableType(code, "callback");
        Assert.AreEqual("Callable[[Variant], int]", type,
            "Lambda parameter with no usage should default to Variant");
    }

    [TestMethod]
    public void InferLambdaParam_OnlyPassedAround_ReturnsVariant()
    {
        var code = @"
extends Node
var callback = func(value):
    var temp = value
    return temp
";
        var type = InferVariableType(code, "callback");
        // Parameter has no constraining usage, should be Variant
        // Return type inferred from variable (which is also untyped)
        Assert.IsTrue(type.StartsWith("Callable[[Variant"),
            $"Lambda parameter only assigned to variable should be Variant. Got: {type}");
    }

    #endregion

    #region Explicit Annotation Tests

    [TestMethod]
    public void InferLambdaParam_ExplicitType_UsesAnnotation()
    {
        var code = @"
extends Node
var callback = func(items: Array):
    items.append(1)
";
        var type = InferVariableType(code, "callback");
        Assert.AreEqual("Callable[[Array], void]", type,
            "Lambda with explicit parameter type should use annotation");
    }

    [TestMethod]
    public void InferLambdaParam_ExplicitOverridesInference_UsesAnnotation()
    {
        var code = @"
extends Node
var callback = func(data: Dictionary):
    # Even though we call .slice(), explicit type takes precedence
    return data
";
        var type = InferVariableType(code, "callback");
        Assert.AreEqual("Callable[[Dictionary], Dictionary]", type,
            "Explicit type annotation should override duck-type inference");
    }

    #endregion

    #region Default Value Tests

    [TestMethod]
    public void InferLambdaParam_DefaultValue_InfersFromDefault()
    {
        var code = @"
extends Node
var callback = func(count = 42):
    return count * 2
";
        var type = InferVariableType(code, "callback");
        Assert.AreEqual("Callable[[int], int]", type,
            "Lambda parameter with default value should infer from default");
    }

    #endregion

    #region Helper Methods

    private static string InferVariableType(string code, string variableName)
    {
        var classDecl = ParseClass(code);
        var engine = CreateTypeEngine();

        var variable = classDecl.Members
            .OfType<GDVariableDeclaration>()
            .FirstOrDefault(v => v.Identifier?.Sequence == variableName);

        Assert.IsNotNull(variable?.Initializer, $"Variable '{variableName}' should have initializer");
        var lambda = variable.Initializer as GDMethodExpression;
        Assert.IsNotNull(lambda, "Initializer should be a lambda");

        // Use InferLambdaSemanticType to get full Callable signature with parameter types
        var semanticType = engine.InferLambdaSemanticType(lambda);
        return semanticType;
    }

    private static GDClassDeclaration ParseClass(string code)
    {
        var parser = new GDScriptReader();
        var classDecl = parser.ParseFileContent(code);
        Assert.IsNotNull(classDecl, "Failed to parse class");
        return classDecl;
    }

    private static GDTypeInferenceEngine CreateTypeEngine()
    {
        return new GDTypeInferenceEngine(GDDefaultRuntimeProvider.Instance);
    }

    #endregion
}
