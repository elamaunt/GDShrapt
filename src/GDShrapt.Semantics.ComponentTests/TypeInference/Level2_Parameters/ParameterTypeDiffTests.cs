using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Tests for GDParameterTypeDiff and the GetParameterTypeDiff() API.
/// Verifies that expected vs actual type comparison works correctly.
/// </summary>
[TestClass]
public class ParameterTypeDiffTests
{
    #region GDParameterTypeDiff Unit Tests

    [TestMethod]
    public void ParameterTypeDiff_Create_ExactMatch_NoMismatch()
    {
        // Arrange
        var expected = new GDUnionType();
        expected.AddType("int");

        var actual = new GDUnionType();
        actual.AddType("int");

        // Act
        var diff = GDParameterTypeDiff.Create("param", expected, actual);

        // Assert
        Assert.IsFalse(diff.HasMismatch, "Exact type match should have no mismatch");
        Assert.AreEqual(0, diff.MissingTypes.Count);
        Assert.AreEqual(0, diff.UnexpectedTypes.Count);
        Assert.AreEqual(1, diff.MatchingTypes.Count);
        Assert.AreEqual("int", diff.MatchingTypes[0]);
    }

    [TestMethod]
    public void ParameterTypeDiff_Create_UnionMatch_NoMismatch()
    {
        // Arrange
        var expected = new GDUnionType();
        expected.AddType("int");
        expected.AddType("String");

        var actual = new GDUnionType();
        actual.AddType("int");
        actual.AddType("String");

        // Act
        var diff = GDParameterTypeDiff.Create("param", expected, actual);

        // Assert
        Assert.IsFalse(diff.HasMismatch, "Union type match should have no mismatch");
        Assert.AreEqual(0, diff.MissingTypes.Count);
        Assert.AreEqual(0, diff.UnexpectedTypes.Count);
        Assert.AreEqual(2, diff.MatchingTypes.Count);
    }

    [TestMethod]
    public void ParameterTypeDiff_Create_SubsetMatch_ReportsMissing()
    {
        // Arrange
        var expected = new GDUnionType();
        expected.AddType("int");
        expected.AddType("String");
        expected.AddType("Array");  // Use non-numeric type instead of float

        var actual = new GDUnionType();
        actual.AddType("int");

        // Act
        var diff = GDParameterTypeDiff.Create("param", expected, actual);

        // Assert
        Assert.IsTrue(diff.HasMismatch, "Missing types should report mismatch");
        Assert.IsTrue(diff.MissingTypes.Contains("String"), "String should be missing");
        Assert.IsTrue(diff.MissingTypes.Contains("Array"), "Array should be missing");
        Assert.AreEqual(0, diff.UnexpectedTypes.Count);
    }

    [TestMethod]
    public void ParameterTypeDiff_Create_SupersetMatch_ReportsUnexpected()
    {
        // Arrange
        var expected = new GDUnionType();
        expected.AddType("int");

        var actual = new GDUnionType();
        actual.AddType("int");
        actual.AddType("String");
        actual.AddType("float");

        // Act
        var diff = GDParameterTypeDiff.Create("param", expected, actual);

        // Assert
        Assert.IsTrue(diff.HasMismatch, "Unexpected types should report mismatch");
        Assert.AreEqual(0, diff.MissingTypes.Count);
        Assert.IsTrue(diff.UnexpectedTypes.Contains("String"), "String should be unexpected");
        Assert.IsTrue(diff.UnexpectedTypes.Contains("float"), "float should be unexpected");
    }

    [TestMethod]
    public void ParameterTypeDiff_Create_NullIsCompatible()
    {
        // Arrange
        var expected = new GDUnionType();
        expected.AddType("int");
        expected.AddType("null");

        var actual = new GDUnionType();
        actual.AddType("int");
        actual.AddType("null");

        // Act
        var diff = GDParameterTypeDiff.Create("param", expected, actual);

        // Assert
        Assert.IsFalse(diff.HasMismatch, "null should be compatible");
    }

    [TestMethod]
    public void ParameterTypeDiff_Create_NumericCompatibility()
    {
        // Arrange - int is compatible with float
        var expected = new GDUnionType();
        expected.AddType("float");

        var actual = new GDUnionType();
        actual.AddType("int");

        // Act
        var diff = GDParameterTypeDiff.Create("param", expected, actual);

        // Assert - int -> float is allowed, so no unexpected
        Assert.AreEqual(0, diff.UnexpectedTypes.Count, "int should be compatible with float");
    }

    [TestMethod]
    public void ParameterTypeDiff_Create_EmptyExpected_NoMissingTypes()
    {
        // Arrange
        var expected = new GDUnionType();

        var actual = new GDUnionType();
        actual.AddType("int");
        actual.AddType("String");

        // Act
        var diff = GDParameterTypeDiff.Create("param", expected, actual);

        // Assert
        Assert.IsTrue(diff.ExpectedIsEmpty);
        Assert.AreEqual(0, diff.MissingTypes.Count, "No expected means no missing");
        // Unexpected are still reported because even with empty expected, we might want to know what's passed
        Assert.AreEqual(2, diff.UnexpectedTypes.Count);
    }

    [TestMethod]
    public void ParameterTypeDiff_Create_EmptyActual_AllMissing()
    {
        // Arrange
        var expected = new GDUnionType();
        expected.AddType("int");
        expected.AddType("String");

        var actual = new GDUnionType();

        // Act
        var diff = GDParameterTypeDiff.Create("param", expected, actual);

        // Assert
        Assert.IsTrue(diff.ActualIsEmpty);
        Assert.AreEqual(2, diff.MissingTypes.Count, "All expected should be missing");
        Assert.AreEqual(0, diff.UnexpectedTypes.Count);
    }

    [TestMethod]
    public void ParameterTypeDiff_GetSummary_NoMismatch()
    {
        // Arrange
        var expected = new GDUnionType();
        expected.AddType("int");

        var actual = new GDUnionType();
        actual.AddType("int");

        // Act
        var diff = GDParameterTypeDiff.Create("myParam", expected, actual);

        // Assert
        var summary = diff.GetSummary();
        Assert.IsTrue(summary.Contains("myParam"), "Summary should contain parameter name");
        Assert.IsTrue(summary.Contains("match"), "Summary should mention match");
    }

    [TestMethod]
    public void ParameterTypeDiff_GetSummary_WithMismatch()
    {
        // Arrange
        var expected = new GDUnionType();
        expected.AddType("int");

        var actual = new GDUnionType();
        actual.AddType("String");

        // Act
        var diff = GDParameterTypeDiff.Create("myParam", expected, actual);

        // Assert
        var summary = diff.GetSummary();
        Assert.IsTrue(summary.Contains("myParam"), "Summary should contain parameter name");
        Assert.IsTrue(summary.Contains("unexpected") || summary.Contains("missing"),
            "Summary should mention unexpected or missing types");
    }

    #endregion

    #region SemanticModel Integration Tests

    [TestMethod]
    public void SemanticModel_GetParameterTypeDiff_ReturnsNullForNonexistentMethod()
    {
        // Arrange
        var code = @"
func test(x):
    pass
";
        var semanticModel = BuildSemanticModel(code);

        // Act
        var diff = semanticModel?.GetParameterTypeDiff("nonexistent", "x");

        // Assert
        Assert.IsNull(diff, "Should return null for nonexistent method");
    }

    [TestMethod]
    public void SemanticModel_GetParameterTypeDiff_ReturnsNullForNonexistentParam()
    {
        // Arrange
        var code = @"
func test(x):
    pass
";
        var semanticModel = BuildSemanticModel(code);

        // Act
        var diff = semanticModel?.GetParameterTypeDiff("test", "nonexistent");

        // Assert
        Assert.IsNull(diff, "Should return null for nonexistent parameter");
    }

    [TestMethod]
    public void SemanticModel_GetParameterTypeDiff_WithExplicitType()
    {
        // Arrange
        var code = @"
func test(x: int):
    pass
";
        var semanticModel = BuildSemanticModel(code);

        // Act
        var diff = semanticModel?.GetParameterTypeDiff("test", "x");

        // Assert
        Assert.IsNotNull(diff, "Should return diff for typed parameter");
        Assert.IsFalse(diff.ExpectedTypes.IsEmpty, "Expected types should include explicit annotation");
        Assert.IsTrue(diff.ExpectedTypes.Types.Contains("int"), "Expected should contain int");
    }

    [TestMethod]
    public void SemanticModel_GetParameterTypeDiff_WithTypeGuard()
    {
        // Arrange
        var code = @"
func test(x):
    if x is int:
        return x * 2
    if x is String:
        return x.length()
";
        var semanticModel = BuildSemanticModel(code);

        // Act
        var diff = semanticModel?.GetParameterTypeDiff("test", "x");

        // Assert
        Assert.IsNotNull(diff, "Should return diff for parameter with type guards");
        Assert.IsTrue(diff.ExpectedTypes.Types.Contains("int"),
            $"Expected should contain int from type guard. Actual: [{string.Join(", ", diff.ExpectedTypes.Types)}]");
        Assert.IsTrue(diff.ExpectedTypes.Types.Contains("String"),
            $"Expected should contain String from type guard. Actual: [{string.Join(", ", diff.ExpectedTypes.Types)}]");
    }

    [TestMethod]
    public void SemanticModel_GetParameterTypeDiff_WithNullCheck()
    {
        // Arrange
        var code = @"
func test(x):
    if x == null:
        return 0
    return x
";
        var semanticModel = BuildSemanticModel(code);

        // Act
        var diff = semanticModel?.GetParameterTypeDiff("test", "x");

        // Assert
        Assert.IsNotNull(diff, "Should return diff for parameter with null check");
        Assert.IsTrue(diff.ExpectedTypes.Types.Contains("null"),
            $"Expected should contain null from null check. Actual: [{string.Join(", ", diff.ExpectedTypes.Types)}]");
    }

    [TestMethod]
    public void SemanticModel_GetCallSiteTypes_ReturnsNullWithoutCallSiteData()
    {
        // Arrange - file-level semantic model without project-level call site data
        var code = @"
func test(x):
    pass
";
        var semanticModel = BuildSemanticModel(code);

        // Act
        var callSiteTypes = semanticModel?.GetCallSiteTypes("test", "x");

        // Assert
        Assert.IsNull(callSiteTypes, "Should return null without injected call site data");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void ParameterTypeDiff_Create_WithVariant_ExpectedIsEmpty()
    {
        // Arrange - Variant is intentionally NOT added to unions (it means "any type")
        // So AddType("Variant") results in an empty union
        var expected = new GDUnionType();
        expected.AddType("Variant");

        var actual = new GDUnionType();
        actual.AddType("int");
        actual.AddType("String");
        actual.AddType("Array");

        // Act
        var diff = GDParameterTypeDiff.Create("param", expected, actual);

        // Assert - Variant not stored in union, so expected is empty
        Assert.IsTrue(diff.ExpectedIsEmpty, "Variant should not be stored in union");
        // When expected is empty, we can't compare, so all actual are unexpected
        Assert.AreEqual(3, diff.UnexpectedTypes.Count,
            "With empty expected, actual types have nothing to match against");
    }

    [TestMethod]
    public void ParameterTypeDiff_Create_Disjoint_ReportsBothMissingAndUnexpected()
    {
        // Arrange
        var expected = new GDUnionType();
        expected.AddType("int");
        expected.AddType("float");

        var actual = new GDUnionType();
        actual.AddType("String");
        actual.AddType("Array");

        // Act
        var diff = GDParameterTypeDiff.Create("param", expected, actual);

        // Assert
        Assert.IsTrue(diff.HasMismatch);
        Assert.AreEqual(2, diff.MissingTypes.Count, "All expected should be missing");
        Assert.AreEqual(2, diff.UnexpectedTypes.Count, "All actual should be unexpected");
        Assert.AreEqual(0, diff.MatchingTypes.Count, "No matching types");
    }

    #endregion

    #region Helper Methods

    private static GDSemanticModel? BuildSemanticModel(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        if (classDecl == null)
            return null;

        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = GDDefaultRuntimeProvider.Instance;
        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        return collector.BuildSemanticModel();
    }

    #endregion
}
