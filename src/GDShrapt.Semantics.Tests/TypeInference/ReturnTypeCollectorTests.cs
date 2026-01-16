using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Unit tests for GDReturnTypeCollector class.
/// Tests collection of return statements and return Union type computation.
/// </summary>
[TestClass]
public class ReturnTypeCollectorTests
{
    #region Single Return Tests

    [TestMethod]
    public void Collect_SingleReturn_ReturnsSingleType()
    {
        // Arrange
        var code = @"
func test():
    return 42
";
        var method = ParseMethod(code);
        var collector = new GDReturnTypeCollector(method, GDDefaultRuntimeProvider.Instance);

        // Act
        collector.Collect();

        // Assert
        Assert.AreEqual(1, collector.Returns.Count);
        Assert.AreEqual("int", collector.Returns[0].InferredType);
        Assert.IsTrue(collector.Returns[0].IsHighConfidence);
    }

    [TestMethod]
    public void Collect_ReturnString_ReturnsStringType()
    {
        // Arrange
        var code = @"
func test():
    return ""hello""
";
        var method = ParseMethod(code);
        var collector = new GDReturnTypeCollector(method, GDDefaultRuntimeProvider.Instance);

        // Act
        collector.Collect();

        // Assert
        Assert.AreEqual(1, collector.Returns.Count);
        Assert.AreEqual("String", collector.Returns[0].InferredType);
    }

    #endregion

    #region Multiple Returns Tests

    [TestMethod]
    public void Collect_MultipleReturns_ReturnsAll()
    {
        // Arrange
        var code = @"
func test(condition):
    if condition:
        return 42
    else:
        return ""hello""
";
        var method = ParseMethod(code);
        var collector = new GDReturnTypeCollector(method, GDDefaultRuntimeProvider.Instance);

        // Act
        collector.Collect();

        // Assert
        Assert.AreEqual(2, collector.Returns.Count);
        var types = collector.Returns.Select(r => r.InferredType).ToList();
        Assert.IsTrue(types.Contains("int"));
        Assert.IsTrue(types.Contains("String"));
    }

    [TestMethod]
    public void Collect_ConditionalReturns_IncludesBranchContext()
    {
        // Arrange
        var code = @"
func test(condition):
    if condition:
        return 1
    return 2
";
        var method = ParseMethod(code);
        var collector = new GDReturnTypeCollector(method, GDDefaultRuntimeProvider.Instance);

        // Act
        collector.Collect();

        // Assert
        Assert.AreEqual(2, collector.Returns.Count);
        Assert.IsTrue(collector.Returns[0].BranchContext?.Contains("if") ?? false);
        Assert.IsNull(collector.Returns[1].BranchContext);
    }

    #endregion

    #region Implicit Return Tests

    [TestMethod]
    public void Collect_NoReturn_ReturnsImplicit()
    {
        // Arrange
        var code = @"
func test():
    var x = 42
";
        var method = ParseMethod(code);
        var collector = new GDReturnTypeCollector(method, GDDefaultRuntimeProvider.Instance);

        // Act
        collector.Collect();

        // Assert
        Assert.AreEqual(1, collector.Returns.Count);
        Assert.IsTrue(collector.Returns[0].IsImplicit);
        Assert.IsNull(collector.Returns[0].InferredType);
    }

    [TestMethod]
    public void Collect_EmptyReturn_ReturnsNull()
    {
        // Arrange
        var code = @"
func test():
    return
";
        var method = ParseMethod(code);
        var collector = new GDReturnTypeCollector(method, GDDefaultRuntimeProvider.Instance);

        // Act
        collector.Collect();

        // Assert
        Assert.AreEqual(1, collector.Returns.Count);
        Assert.IsFalse(collector.Returns[0].IsImplicit);
        Assert.IsNull(collector.Returns[0].InferredType);
    }

    [TestMethod]
    public void Collect_ImplicitReturn_ReturnsNull()
    {
        // Arrange
        var code = @"
func test():
    if true:
        return 42
    # No else - implicit return
";
        var method = ParseMethod(code);
        var collector = new GDReturnTypeCollector(method, GDDefaultRuntimeProvider.Instance);

        // Act
        collector.Collect();

        // Assert
        Assert.IsTrue(collector.Returns.Count >= 2);
        var implicitReturn = collector.Returns.FirstOrDefault(r => r.IsImplicit);
        Assert.IsNotNull(implicitReturn);
    }

    #endregion

    #region Union Type Computation Tests

    [TestMethod]
    public void ComputeReturnUnionType_SingleType_ReturnsSingleType()
    {
        // Arrange
        var code = @"
func test():
    return 42
";
        var method = ParseMethod(code);
        var collector = new GDReturnTypeCollector(method, GDDefaultRuntimeProvider.Instance);
        collector.Collect();

        // Act
        var union = collector.ComputeReturnUnionType();

        // Assert
        Assert.IsTrue(union.IsSingleType);
        Assert.AreEqual("int", union.EffectiveType);
    }

    [TestMethod]
    public void ComputeReturnUnionType_MultipleTypes_ReturnsUnion()
    {
        // Arrange
        var code = @"
func test(condition):
    if condition:
        return 42
    else:
        return ""hello""
";
        var method = ParseMethod(code);
        var collector = new GDReturnTypeCollector(method, GDDefaultRuntimeProvider.Instance);
        collector.Collect();

        // Act
        var union = collector.ComputeReturnUnionType();

        // Assert
        Assert.IsTrue(union.IsUnion);
        Assert.IsTrue(union.Types.Contains("int"));
        Assert.IsTrue(union.Types.Contains("String"));
    }

    [TestMethod]
    public void ComputeReturnUnionType_SameTypeMultipleTimes_ReturnsSingleType()
    {
        // Arrange
        var code = @"
func test(x):
    if x > 0:
        return 1
    elif x < 0:
        return -1
    else:
        return 0
";
        var method = ParseMethod(code);
        var collector = new GDReturnTypeCollector(method, GDDefaultRuntimeProvider.Instance);
        collector.Collect();

        // Act
        var union = collector.ComputeReturnUnionType();

        // Assert
        Assert.IsTrue(union.IsSingleType);
        Assert.AreEqual("int", union.EffectiveType);
    }

    [TestMethod]
    public void ComputeReturnUnionType_ImplicitReturn_IncludesNull()
    {
        // Arrange
        var code = @"
func test():
    var x = 42
";
        var method = ParseMethod(code);
        var collector = new GDReturnTypeCollector(method, GDDefaultRuntimeProvider.Instance);
        collector.Collect();

        // Act
        var union = collector.ComputeReturnUnionType();

        // Assert
        Assert.IsTrue(union.Types.Contains("null"));
    }

    #endregion

    #region Loop Returns Tests

    [TestMethod]
    public void Collect_ReturnInLoop_IncludesLoopContext()
    {
        // Arrange
        var code = @"
func test(items):
    for item in items:
        if item == null:
            return false
    return true
";
        var method = ParseMethod(code);
        var collector = new GDReturnTypeCollector(method, GDDefaultRuntimeProvider.Instance);

        // Act
        collector.Collect();

        // Assert
        Assert.AreEqual(2, collector.Returns.Count);
        var loopReturn = collector.Returns.FirstOrDefault(r => r.BranchContext?.Contains("for loop") ?? false);
        Assert.IsNotNull(loopReturn);
    }

    [TestMethod]
    public void Collect_ReturnInWhileLoop_IncludesLoopContext()
    {
        // Arrange
        var code = @"
func test():
    var i = 0
    while i < 10:
        if i == 5:
            return i
        i += 1
    return -1
";
        var method = ParseMethod(code);
        var collector = new GDReturnTypeCollector(method, GDDefaultRuntimeProvider.Instance);

        // Act
        collector.Collect();

        // Assert
        Assert.AreEqual(2, collector.Returns.Count);
        var whileReturn = collector.Returns.FirstOrDefault(r => r.BranchContext?.Contains("while loop") ?? false);
        Assert.IsNotNull(whileReturn);
    }

    #endregion

    #region Match Statement Tests

    [TestMethod]
    public void Collect_ReturnInMatch_IncludesMatchContext()
    {
        // Arrange
        var code = @"
func test(value):
    match value:
        1:
            return ""one""
        2:
            return ""two""
        _:
            return ""other""
";
        var method = ParseMethod(code);
        var collector = new GDReturnTypeCollector(method, GDDefaultRuntimeProvider.Instance);

        // Act
        collector.Collect();

        // Assert
        // Match statements don't guarantee exhaustive coverage, so implicit return is added
        // 3 explicit returns + 1 implicit return = 4
        var matchReturns = collector.Returns.Where(r => r.BranchContext?.Contains("match case") ?? false).ToList();
        Assert.AreEqual(3, matchReturns.Count);
        Assert.IsTrue(matchReturns.All(r => r.InferredType == "String"));
    }

    #endregion

    #region Explicit Return Type Tests

    [TestMethod]
    public void HasExplicitReturnType_WithType_ReturnsTrue()
    {
        // Arrange
        var code = @"
func test() -> int:
    return 42
";
        var method = ParseMethod(code);

        // Act
        var hasExplicit = GDReturnTypeCollector.HasExplicitReturnType(method);

        // Assert
        Assert.IsTrue(hasExplicit);
    }

    [TestMethod]
    public void HasExplicitReturnType_WithoutType_ReturnsFalse()
    {
        // Arrange
        var code = @"
func test():
    return 42
";
        var method = ParseMethod(code);

        // Act
        var hasExplicit = GDReturnTypeCollector.HasExplicitReturnType(method);

        // Assert
        Assert.IsFalse(hasExplicit);
    }

    [TestMethod]
    public void GetExplicitReturnType_WithType_ReturnsTypeName()
    {
        // Arrange
        var code = @"
func test() -> String:
    return ""hello""
";
        var method = ParseMethod(code);

        // Act
        var explicitType = GDReturnTypeCollector.GetExplicitReturnType(method);

        // Assert
        Assert.AreEqual("String", explicitType);
    }

    #endregion

    #region Early Return Tests

    [TestMethod]
    public void Collect_EarlyReturn_IncludedInUnion()
    {
        // Arrange
        var code = @"
func test(value):
    if value == null:
        return  # Early return
    return value.to_string()
";
        var method = ParseMethod(code);
        var collector = new GDReturnTypeCollector(method, GDDefaultRuntimeProvider.Instance);

        // Act
        collector.Collect();

        // Assert
        Assert.AreEqual(2, collector.Returns.Count);
        // First return should be null (empty return)
        Assert.IsNull(collector.Returns[0].InferredType);
    }

    #endregion

    #region Return Info Properties Tests

    [TestMethod]
    public void ReturnInfo_HasCorrectLineAndColumn()
    {
        // Arrange
        var code = @"
func test():
    return 42
";
        var method = ParseMethod(code);
        var collector = new GDReturnTypeCollector(method, GDDefaultRuntimeProvider.Instance);
        collector.Collect();

        // Assert
        Assert.AreEqual(1, collector.Returns.Count);
        Assert.IsTrue(collector.Returns[0].Line > 0);
    }

    [TestMethod]
    public void ReturnInfo_HasExpressionText()
    {
        // Arrange
        var code = @"
func test():
    return 42 + 10
";
        var method = ParseMethod(code);
        var collector = new GDReturnTypeCollector(method, GDDefaultRuntimeProvider.Instance);
        collector.Collect();

        // Assert
        Assert.AreEqual(1, collector.Returns.Count);
        Assert.IsNotNull(collector.Returns[0].ExpressionText);
        Assert.IsTrue(collector.Returns[0].ExpressionText.Contains("42"));
    }

    #endregion

    #region Helper Methods

    private static GDMethodDeclaration ParseMethod(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        Assert.IsNotNull(classDecl, "Failed to parse code");

        var method = classDecl.Members.OfType<GDMethodDeclaration>().FirstOrDefault();
        Assert.IsNotNull(method, "No method found in code");

        return method;
    }

    #endregion
}
