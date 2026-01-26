using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for method return type inference when methods return local variables.
/// These tests verify that local variable types are tracked during return type inference.
/// </summary>
[TestClass]
public class LocalVariableReturnTypeTests
{
    private static GDScriptFile? _script;

    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        _script = TestProjectFixture.GetScript("union_types_complex.gd");
        Assert.IsNotNull(_script, "Script 'union_types_complex.gd' not found in test project");

        if (_script.SemanticModel == null)
        {
            _script.Analyze();
        }
    }

    #region Local Variable Return Tests (Problems 1-2)

    /// <summary>
    /// Tests that map_with_fallback returns Array, not void.
    /// The method initializes 'results = []' and returns it.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_MapWithFallback_ShouldReturnArray()
    {
        // Arrange
        var method = FindMethod("map_with_fallback");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreNotEqual("void", returnType,
            $"map_with_fallback returns 'results' which is initialized as []. Type should NOT be 'void'. Got: {returnType}");
        Assert.AreEqual("Array", returnType,
            $"map_with_fallback should return 'Array' (from 'var results = []'). Got: {returnType}");
    }

    /// <summary>
    /// Tests that filter_map returns Array, not void.
    /// The method initializes 'results = []' and returns it.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_FilterMap_ShouldReturnArray()
    {
        // Arrange
        var method = FindMethod("filter_map");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreNotEqual("void", returnType,
            $"filter_map returns 'results' which is initialized as []. Type should NOT be 'void'. Got: {returnType}");
        Assert.AreEqual("Array", returnType,
            $"filter_map should return 'Array' (from 'var results = []'). Got: {returnType}");
    }

    /// <summary>
    /// Tests that reduce_or_default correctly infers return type.
    /// Returns either default_value (Variant) or acc (Variant from array element).
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_ReduceOrDefault_ShouldNotReturnVoid()
    {
        // Arrange
        var method = FindMethod("reduce_or_default");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreNotEqual("void", returnType,
            $"reduce_or_default has return statements with values. Type should NOT be 'void'. Got: {returnType}");
    }

    #endregion

    #region Union Type Tests (Problem 4)

    /// <summary>
    /// Tests that complex_conditional returns a Union type, not just Variant.
    /// The method returns int, String, Array, or Dictionary depending on conditions.
    /// Expected: "Array | Dictionary | String | int" (alphabetically sorted)
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_ComplexConditional_ShouldReturnUnion()
    {
        // Arrange
        var method = FindMethod("complex_conditional");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreNotEqual("void", returnType,
            $"complex_conditional has multiple return types. Type should NOT be 'void'. Got: {returnType}");
        Assert.AreNotEqual("Variant", returnType,
            $"complex_conditional should return Union type, not 'Variant'. Got: {returnType}");

        // Should contain all return types
        Assert.IsTrue(returnType.Contains("int"), $"Union should contain 'int'. Got: {returnType}");
        Assert.IsTrue(returnType.Contains("String"), $"Union should contain 'String'. Got: {returnType}");
        Assert.IsTrue(returnType.Contains("Array"), $"Union should contain 'Array'. Got: {returnType}");
        Assert.IsTrue(returnType.Contains("Dictionary"), $"Union should contain 'Dictionary'. Got: {returnType}");
    }

    /// <summary>
    /// Tests that match_return returns a Union type.
    /// The method returns String, float, Array, Dictionary, or Variant (for _ case).
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_MatchReturn_ShouldReturnUnion()
    {
        // Arrange
        var method = FindMethod("match_return");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreNotEqual("void", returnType,
            $"match_return has multiple return types in match cases. Type should NOT be 'void'. Got: {returnType}");
        Assert.AreNotEqual("Variant", returnType,
            $"match_return should return Union type, not just 'Variant'. Got: {returnType}");

        // Should contain at least some return types
        Assert.IsTrue(returnType.Contains("String") || returnType.Contains("float") ||
                      returnType.Contains("Array") || returnType.Contains("Dictionary"),
            $"Union should contain match case types. Got: {returnType}");
    }

    #endregion

    #region Pattern Variable Tests (Problem 3)

    /// <summary>
    /// Tests that match_with_patterns does not return just null.
    /// The method has pattern variables (var h, var first, var x) and returns them.
    /// At minimum, should return a Union with null and other types.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_MatchWithPatterns_ShouldNotReturnJustNull()
    {
        // Arrange
        var method = FindMethod("match_with_patterns");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreNotEqual("void", returnType,
            $"match_with_patterns has return statements. Type should NOT be 'void'. Got: {returnType}");

        // Should not be just "null" - there are other return types
        Assert.AreNotEqual("null", returnType,
            $"match_with_patterns should not return just 'null'. Has other returns. Got: {returnType}");

        // Should contain at least int (from 'return x * 2' where x is int after guard)
        // or Variant (for pattern variables) or null
        Assert.IsTrue(returnType.Contains("int") || returnType.Contains("Variant") || returnType.Contains("null"),
            $"match_with_patterns should include int or Variant. Got: {returnType}");
    }

    #endregion

    #region Dictionary Value Type Tests (Problems 5-6)

    /// <summary>
    /// Tests that process_mixed_array returns Array.
    /// The method initializes 'results = []' and returns it after processing.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_ProcessMixedArray_ShouldReturnArray()
    {
        // Arrange
        var method = FindMethod("process_mixed_array");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreNotEqual("void", returnType,
            $"process_mixed_array returns 'results' which is initialized as []. Got: {returnType}");
        Assert.AreEqual("Array", returnType,
            $"process_mixed_array should return 'Array'. Got: {returnType}");
    }

    /// <summary>
    /// Tests that _process_mixed_item returns Union type.
    /// Returns int, String, int, int, Array (keys), or item (Variant).
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_ProcessMixedItem_ShouldReturnUnion()
    {
        // Arrange
        var method = FindMethod("_process_mixed_item");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreNotEqual("void", returnType,
            $"_process_mixed_item has multiple typed returns. Got: {returnType}");

        // Should have int at least (from item * 10, item.length(), int(item), item.size())
        Assert.IsTrue(returnType.Contains("int") || returnType.Contains("Variant"),
            $"_process_mixed_item should contain 'int'. Got: {returnType}");
    }

    #endregion

    #region Dictionary Value Type Tests (Problems 5-6)

    /// <summary>
    /// Tests that get_config returns Union type of dictionary values.
    /// The config dictionary has values: String, int, float, bool, Array, Dictionary.
    /// Expected: "Array | Dictionary | String | bool | float | int" or similar union.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_GetConfig_ShouldReturnUnionOfDictionaryValues()
    {
        // Arrange
        var method = FindMethod("get_config");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreNotEqual("void", returnType,
            $"get_config returns config.get(key). Should NOT be 'void'. Got: {returnType}");

        // Should be Union or Variant (Variant is acceptable if we can't infer dictionary value types)
        // If Union, should contain the dictionary value types
        if (returnType != "Variant")
        {
            Assert.IsTrue(returnType.Contains("|"),
                $"get_config should return Union type or Variant. Got: {returnType}");
        }
    }

    #endregion

    #region Phase 2 Tests - Dictionary[Union], Variant for params, methods on Variant

    /// <summary>
    /// Tests that config variable returns Dictionary[Union] with all value types.
    /// The config dictionary has values: String, int, float, bool, Array, Dictionary.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_Config_ShouldReturnDictionaryWithUnion()
    {
        // Arrange
        var variable = FindVariable("config");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var type = analyzer.GetTypeForNode(variable) ?? "unknown";

        // Assert
        Assert.IsTrue(type.StartsWith("Dictionary["),
            $"config should return 'Dictionary[Union]'. Got: {type}");
        Assert.IsTrue(type.Contains("String") && type.Contains("int"),
            $"config Union should contain String and int. Got: {type}");
    }

    /// <summary>
    /// Tests that safe_get_nested returns Variant | null, not just null.
    /// The method has 'return current' where current is initialized from parameter.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_SafeGetNested_ShouldReturnVariantOrNull()
    {
        // Arrange
        var method = FindMethod("safe_get_nested");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreNotEqual("null", returnType,
            $"safe_get_nested should NOT return just 'null'. Got: {returnType}");
        Assert.IsTrue(returnType.Contains("Variant") || returnType.Contains("null"),
            $"safe_get_nested should include Variant or null. Got: {returnType}");
    }

    /// <summary>
    /// Tests that _process_mixed_item includes Array (from item.keys()) and String (from item.to_upper()).
    /// When methods are called on Variant, we should still infer their return types.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_ProcessMixedItem_ShouldIncludeArrayAndString()
    {
        // Arrange
        var method = FindMethod("_process_mixed_item");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert - Should include Array (from item.keys()) and String (from item.to_upper())
        Assert.IsTrue(returnType.Contains("Array"),
            $"_process_mixed_item should contain 'Array' from item.keys(). Got: {returnType}");
        Assert.IsTrue(returnType.Contains("String"),
            $"_process_mixed_item should contain 'String' from item.to_upper(). Got: {returnType}");
    }

    /// <summary>
    /// Tests that handle_result returns Union with String and null.
    /// The method has match cases returning String, Variant, and null.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_HandleResult_ShouldReturnUnionWithNull()
    {
        // Arrange
        var method = FindMethod("handle_result");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.IsTrue(returnType.Contains("String"),
            $"handle_result should contain 'String'. Got: {returnType}");
        Assert.IsTrue(returnType.Contains("null"),
            $"handle_result should contain 'null'. Got: {returnType}");
    }

    /// <summary>
    /// Tests that safe_chain_example returns Array.
    /// The method returns [name, missing] which is an array literal.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_SafeChainExample_ShouldReturnArray()
    {
        // Arrange
        var method = FindMethod("safe_chain_example");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert - Array[StringName] is more precise than just Array
        Assert.IsTrue(returnType == "Array" || returnType == "Array[StringName]",
            $"safe_chain_example should return 'Array' or 'Array[StringName]'. Got: {returnType}");
    }

    #endregion

    #region Phase 3 Tests - Array[Union], Signal types

    /// <summary>
    /// Tests that mixed_array returns Array with Union element type.
    /// The array [1, "two", 3.0, [4], {"five": 5}] contains int, String, float, Array, Dictionary.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_MixedArray_ShouldReturnArrayWithUnion()
    {
        // Arrange
        var variable = FindVariable("mixed_array");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var type = analyzer.GetTypeForNode(variable) ?? "unknown";

        // Assert
        Assert.IsTrue(type.StartsWith("Array["),
            $"mixed_array should return 'Array[Union]'. Got: {type}");
        Assert.IsTrue(type.Contains("int") && type.Contains("String") && type.Contains("float"),
            $"Array Union should contain int, String, float. Got: {type}");
    }

    /// <summary>
    /// Tests that result_ready signal shows parameter type.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_SignalWithParam_ShouldShowParameterType()
    {
        // Arrange
        var signal = FindSignal("result_ready");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var type = analyzer.GetTypeForNode(signal) ?? "unknown";

        // Assert
        Assert.IsTrue(type.Contains("result"),
            $"Signal type should show parameter name 'result'. Got: {type}");
    }

    #endregion

    #region Helper Methods

    private static GDMethodDeclaration FindMethod(string name)
    {
        Assert.IsNotNull(_script?.Class, "Script not loaded or has no class");

        var method = _script.Class.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == name);

        Assert.IsNotNull(method, $"Method '{name}' not found in union_types_complex.gd");
        return method;
    }

    private static GDVariableDeclaration FindVariable(string name)
    {
        Assert.IsNotNull(_script?.Class, "Script not loaded or has no class");

        var variable = _script.Class.Members
            .OfType<GDVariableDeclaration>()
            .FirstOrDefault(v => v.Identifier?.Sequence == name);

        Assert.IsNotNull(variable, $"Variable '{name}' not found in union_types_complex.gd");
        return variable;
    }

    private static GDSignalDeclaration FindSignal(string name)
    {
        Assert.IsNotNull(_script?.Class, "Script not loaded or has no class");

        var signal = _script.Class.Members
            .OfType<GDSignalDeclaration>()
            .FirstOrDefault(s => s.Identifier?.Sequence == name);

        Assert.IsNotNull(signal, $"Signal '{name}' not found in union_types_complex.gd");
        return signal;
    }

    #endregion
}
