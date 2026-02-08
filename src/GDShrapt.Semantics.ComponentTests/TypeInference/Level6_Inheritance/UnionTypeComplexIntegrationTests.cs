using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Integration tests for complex Union type inference using union_types_complex.gd from test project.
/// Tests verify that the type inference engine correctly handles methods returning union types
/// across various patterns: Result types, multiple branches, match statements, tagged unions.
/// </summary>
[TestClass]
public class UnionTypeComplexIntegrationTests
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

    #region Result Type Pattern Tests

    /// <summary>
    /// Tests try_operation which returns int on success, String on error.
    /// Expected union: int | String
    ///
    /// NOTE: Type narrowing after 'is' check should work - expressions like
    /// 'input * 2' after 'if input is int' should be inferred as int.
    /// </summary>
    [TestMethod]
    public void TryOperation_ReturnsUnion_IntOrString()
    {
        // Arrange
        var method = FindMethod("try_operation");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Debug: Print actual inferred types for diagnostics
        var actualTypes = string.Join(", ", union.Types);

        // Assert
        Assert.IsTrue(union.IsUnion,
            $"Expected union type. Actual types: [{actualTypes}]");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("int")),
            $"Union should contain 'int'. Actual types: [{actualTypes}]");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("String")),
            $"Union should contain 'String'. Actual types: [{actualTypes}]");
    }

    /// <summary>
    /// Tests that try_operation return types do NOT include Variant - type narrowing should work.
    /// After 'if input is int:', 'input * 2' should be inferred as int, not Variant.
    /// </summary>
    [TestMethod]
    public void TryOperation_NoVariantType_NarrowingWorks()
    {
        // Arrange
        var method = FindMethod("try_operation");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Debug: Print actual inferred types for diagnostics
        var actualTypes = string.Join(", ", union.Types);
        var returnInfos = string.Join("\n", collector.Returns.Select(r =>
            $"  Line {r.Line}: {r.InferredType?.DisplayName ?? "null"} (expr: {r.ExpressionText ?? "implicit"})"));

        // Assert - Variant should NOT be in the union when narrowing works correctly
        Assert.IsFalse(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("Variant")),
            $"Union should NOT contain 'Variant' - type narrowing should work. Actual types: [{actualTypes}]\nReturns:\n{returnInfos}");
    }

    /// <summary>
    /// Verifies that try_operation has the expected number of return statements.
    /// The method has 5 explicit return statements.
    /// </summary>
    [TestMethod]
    public void TryOperation_HasCorrectReturnCount()
    {
        // Arrange
        var method = FindMethod("try_operation");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();

        // Assert: 5 explicit returns + possible implicit return
        Assert.IsTrue(collector.Returns.Count >= 5,
            $"Expected at least 5 return statements, got {collector.Returns.Count}");
    }

    #endregion

    #region Multiple Type Branches Tests

    /// <summary>
    /// Tests process_by_type which returns different types based on input type.
    /// Expected union: int | float | String | Array | null
    ///
    /// NOTE: Type narrowing after 'is' check should work correctly.
    /// </summary>
    [TestMethod]
    public void ProcessByType_ReturnsUnion_MultipleTypes()
    {
        // Arrange
        var method = FindMethod("process_by_type");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Debug info
        var actualTypes = string.Join(", ", union.Types);
        var returnInfos = string.Join("\n", collector.Returns.Select(r =>
            $"  Line {r.Line}: {r.InferredType?.DisplayName ?? "null"} (expr: {r.ExpressionText ?? "implicit"})"));

        // Assert
        Assert.IsTrue(union.IsUnion,
            $"Expected union type. Actual types: [{actualTypes}]\nReturns:\n{returnInfos}");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("int")), $"Union should contain 'int'. Actual: [{actualTypes}]");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("float")), $"Union should contain 'float'. Actual: [{actualTypes}]");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("String")), $"Union should contain 'String'. Actual: [{actualTypes}]");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("Array")), $"Union should contain 'Array'. Actual: [{actualTypes}]");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("null")), $"Union should contain 'null'. Actual: [{actualTypes}]");
    }

    /// <summary>
    /// Verifies that process_by_type captures branch context for returns.
    /// </summary>
    [TestMethod]
    public void ProcessByType_HasBranchContext()
    {
        // Arrange
        var method = FindMethod("process_by_type");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();

        // Assert: At least one return should have "if" or "elif" context
        var hasIfContext = collector.Returns.Any(r =>
            r.BranchContext?.Contains("if") == true ||
            r.BranchContext?.Contains("elif") == true);
        Assert.IsTrue(hasIfContext, "Expected at least one return with if/elif branch context");
    }

    #endregion

    #region Complex Conditional Tests

    /// <summary>
    /// Tests complex_conditional which returns 4 completely different types.
    /// Expected union: int | String | Array | Dictionary
    /// </summary>
    [TestMethod]
    public void ComplexConditional_ReturnsUnion_FourTypes()
    {
        // Arrange
        var method = FindMethod("complex_conditional");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Assert
        Assert.IsTrue(union.IsUnion, "Expected union type");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("int")), "Union should contain 'int'");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("String")), "Union should contain 'String'");
        // Array types now include element type, e.g., "Array[int]"
        Assert.IsTrue(union.Types.Any(t => t.DisplayName.StartsWith("Array")), "Union should contain 'Array' or 'Array[...]'");
        Assert.IsTrue(union.Types.Any(t => t.DisplayName.StartsWith("Dictionary")), "Union should contain 'Dictionary' or 'Dictionary[...]'");
    }

    /// <summary>
    /// Tests conditional_return which returns based on ternary-like pattern.
    /// Return type depends on the types of true_value and false_value parameters.
    /// </summary>
    [TestMethod]
    public void ConditionalReturn_ReturnsVariantForUnknownParams()
    {
        // Arrange
        var method = FindMethod("conditional_return");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Assert: Since parameters are untyped, we expect Variant-like behavior
        // The return values depend on runtime parameters, so inference might be limited
        Assert.IsNotNull(union, "Union should not be null");
        Assert.IsTrue(collector.Returns.Count >= 2, "Should have at least 2 return statements");
    }

    #endregion

    #region Match Statement Tests

    /// <summary>
    /// Tests match_return which uses match statement with different return types per case.
    /// Expected union: String | float | Array | Dictionary (+ Variant for _ case)
    /// </summary>
    [TestMethod]
    public void MatchReturn_ReturnsUnion_MultipleTypes()
    {
        // Arrange
        var method = FindMethod("match_return");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Assert
        Assert.IsTrue(union.IsUnion, "Expected union type");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("String")), "Union should contain 'String'");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("float")), "Union should contain 'float'");
        // Array types now include element type, e.g., "Array[int]"
        Assert.IsTrue(union.Types.Any(t => t.DisplayName.StartsWith("Array")), "Union should contain 'Array' or 'Array[...]'");
        Assert.IsTrue(union.Types.Any(t => t.DisplayName.StartsWith("Dictionary")), "Union should contain 'Dictionary' or 'Dictionary[...]'");
    }

    /// <summary>
    /// Verifies that match_return captures match case context for returns.
    /// </summary>
    [TestMethod]
    public void MatchReturn_HasMatchCaseContext()
    {
        // Arrange
        var method = FindMethod("match_return");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();

        // Assert: At least 4 returns should have "match case" context
        var matchCaseReturns = collector.Returns.Count(r =>
            r.BranchContext?.Contains("match case") == true);
        Assert.IsTrue(matchCaseReturns >= 4,
            $"Expected at least 4 match case returns, got {matchCaseReturns}");
    }

    /// <summary>
    /// Tests match_with_patterns which uses pattern matching with extraction.
    /// Return types depend on matched patterns and extracted values.
    /// </summary>
    [TestMethod]
    public void MatchWithPatterns_ReturnsUnion_WithPatternExtraction()
    {
        // Arrange
        var method = FindMethod("match_with_patterns");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Assert: Pattern matching extracts Variants, plus explicit int and null
        Assert.IsNotNull(union, "Union should not be null");
        Assert.IsTrue(collector.Returns.Count >= 4,
            $"Expected at least 4 return statements, got {collector.Returns.Count}");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("null")), "Union should contain 'null' from default case");
    }

    #endregion

    #region Tagged Union Pattern Tests

    /// <summary>
    /// Tests handle_result which implements a discriminated/tagged union pattern.
    /// Expected union: Variant | String | null
    /// </summary>
    [TestMethod]
    public void HandleResult_ReturnsUnion_TaggedPattern()
    {
        // Arrange
        var method = FindMethod("handle_result");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Assert
        Assert.IsNotNull(union, "Union should not be null");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("String")), "Union should contain 'String'");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("null")), "Union should contain 'null'");
    }

    /// <summary>
    /// Tests the tagged union creation functions for consistency.
    /// </summary>
    [TestMethod]
    public void CreateSuccess_ReturnsDictionary()
    {
        // Arrange
        var method = FindMethod("create_success");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Assert: create_success returns a Dictionary with tag and value
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("Dictionary")), "Union should contain 'Dictionary'");
    }

    #endregion

    #region Input-Dependent Return Tests

    /// <summary>
    /// Tests _compute_result which returns different types based on input type.
    /// Expected union: String | int | Variant
    ///
    /// NOTE: Type narrowing should work - params.sha256_text() should be String,
    /// params * params should be int after 'is' check.
    /// </summary>
    [TestMethod]
    public void ComputeResult_ReturnsUnion_InputDependent()
    {
        // Arrange
        var method = FindMethod("_compute_result");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Debug info
        var actualTypes = string.Join(", ", union.Types);

        // Assert
        Assert.IsNotNull(union, "Union should not be null");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("String")),
            $"Union should contain 'String'. Actual types: [{actualTypes}]");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("int")),
            $"Union should contain 'int'. Actual types: [{actualTypes}]");
    }

    /// <summary>
    /// Tests get_result which returns Variant|null pattern.
    /// </summary>
    [TestMethod]
    public void GetResult_ReturnsVariantOrNull()
    {
        // Arrange
        var method = FindMethod("get_result");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Assert: Dictionary.get() returns Variant or null if key not found
        Assert.IsNotNull(union, "Union should not be null");
    }

    #endregion

    #region Type Guard Tests

    /// <summary>
    /// Tests process_with_guards which uses custom type guard functions.
    /// </summary>
    [TestMethod]
    public void ProcessWithGuards_ReturnsUnion_AfterGuards()
    {
        // Arrange
        var method = FindMethod("process_with_guards");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Assert: Returns int from numeric, int from text.length(), int from Array.size(), and 0
        Assert.IsNotNull(union, "Union should not be null");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("int")), "Union should contain 'int'");
    }

    /// <summary>
    /// Tests validate_and_process which validates data and returns success/error.
    ///
    /// NOTE: create_error() and create_success() both return Dictionary,
    /// so the union type should be Dictionary.
    /// </summary>
    [TestMethod]
    public void ValidateAndProcess_ReturnsUnion_SuccessOrError()
    {
        // Arrange
        var method = FindMethod("validate_and_process");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Debug info
        var actualTypes = string.Join(", ", union.Types);

        // Assert: Returns create_error (Dictionary) or create_success (Dictionary)
        Assert.IsNotNull(union, "Union should not be null");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("Dictionary")),
            $"Union should contain 'Dictionary'. Actual types: [{actualTypes}]");
    }

    #endregion

    #region Higher-Order Function Tests

    /// <summary>
    /// Tests map_with_fallback which uses callable transform.
    ///
    /// NOTE: Local variable 'results' is initialized as [] (Array literal),
    /// and the function returns results, so type should be Array.
    /// </summary>
    [TestMethod]
    public void MapWithFallback_ReturnsArray()
    {
        // Arrange
        var method = FindMethod("map_with_fallback");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Debug info
        var actualTypes = string.Join(", ", union.Types);

        // Assert: Returns results array
        Assert.IsNotNull(union, "Union should not be null");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("Array")),
            $"Union should contain 'Array'. Actual types: [{actualTypes}]");
    }

    /// <summary>
    /// Tests reduce_or_default which returns accumulated value or default.
    /// </summary>
    [TestMethod]
    public void ReduceOrDefault_ReturnsUnion_AccOrDefault()
    {
        // Arrange
        var method = FindMethod("reduce_or_default");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Assert: Returns default_value (Variant) or accumulated result (Variant)
        Assert.IsNotNull(union, "Union should not be null");
        Assert.IsTrue(collector.Returns.Count >= 2,
            $"Expected at least 2 return statements, got {collector.Returns.Count}");
    }

    #endregion

    #region Safe Navigation Tests

    /// <summary>
    /// Tests safe_get_nested which navigates nested data with null safety.
    /// Expected union: Variant | null
    /// </summary>
    [TestMethod]
    public void SafeGetNested_ReturnsVariantOrNull()
    {
        // Arrange
        var method = FindMethod("safe_get_nested");
        var collector = new GDReturnTypeCollector(method, GetRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Assert
        Assert.IsNotNull(union, "Union should not be null");
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("null")), "Union should contain 'null'");
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

    private static IGDRuntimeProvider GetRuntimeProvider()
    {
        return _script?.SemanticModel?.RuntimeProvider ?? GDDefaultRuntimeProvider.Instance;
    }

    #endregion

    #region Semantic Model GetUnionType Tests

    /// <summary>
    /// Tests that SemanticModel.GetUnionType returns a union type for methods with multiple return types.
    /// try_operation returns int | String, so IsUnion should be true.
    /// </summary>
    [TestMethod]
    public void SemanticModel_GetUnionType_ForMethod_ReturnsUnionFromReturnStatements()
    {
        // Arrange
        var semanticModel = _script?.SemanticModel;
        Assert.IsNotNull(semanticModel, "Semantic model not available");

        // Act
        var unionType = semanticModel.GetUnionType("try_operation");

        // Assert
        Assert.IsNotNull(unionType, "GetUnionType should return a union for method 'try_operation'");
        Assert.IsTrue(unionType.IsUnion,
            $"IsUnion should be true for try_operation. Actual types: [{string.Join(", ", unionType.Types)}]");
        Assert.IsTrue(unionType.Types.Contains(GDSemanticType.FromRuntimeTypeName("int")),
            $"Union should contain 'int'. Actual types: [{string.Join(", ", unionType.Types)}]");
        Assert.IsTrue(unionType.Types.Contains(GDSemanticType.FromRuntimeTypeName("String")),
            $"Union should contain 'String'. Actual types: [{string.Join(", ", unionType.Types)}]");
    }

    /// <summary>
    /// Tests that SemanticModel.GetUnionType returns a union type for parameters with null checks.
    /// The 'input' parameter in try_operation has 'if input == null:' check, so union should include null.
    /// </summary>
    [TestMethod]
    public void SemanticModel_GetUnionType_ForParameter_IncludesNullFromNullCheck()
    {
        // Arrange
        var semanticModel = _script?.SemanticModel;
        Assert.IsNotNull(semanticModel, "Semantic model not available");

        // Act
        var unionType = semanticModel.GetUnionType("input");

        // Assert
        Assert.IsNotNull(unionType, "GetUnionType should return a union for parameter 'input'");
        Assert.IsTrue(unionType.Types.Contains(GDSemanticType.FromRuntimeTypeName("null")),
            $"Union should contain 'null' from null check. Actual types: [{string.Join(", ", unionType.Types)}]");
    }

    /// <summary>
    /// Tests that SemanticModel.GetUnionType returns types from 'is' checks for parameters.
    /// The 'input' parameter in try_operation has 'if input is int:' and 'if input is String:' checks.
    /// </summary>
    [TestMethod]
    public void SemanticModel_GetUnionType_ForParameter_IncludesTypesFromIsChecks()
    {
        // Arrange
        var semanticModel = _script?.SemanticModel;
        Assert.IsNotNull(semanticModel, "Semantic model not available");

        // Act
        var unionType = semanticModel.GetUnionType("input");

        // Assert
        Assert.IsNotNull(unionType, "GetUnionType should return a union for parameter 'input'");
        var actualTypes = string.Join(", ", unionType.Types);

        Assert.IsTrue(unionType.Types.Contains(GDSemanticType.FromRuntimeTypeName("int")),
            $"Union should contain 'int' from is check. Actual types: [{actualTypes}]");
        Assert.IsTrue(unionType.Types.Contains(GDSemanticType.FromRuntimeTypeName("String")),
            $"Union should contain 'String' from is check. Actual types: [{actualTypes}]");
    }

    #endregion
}
