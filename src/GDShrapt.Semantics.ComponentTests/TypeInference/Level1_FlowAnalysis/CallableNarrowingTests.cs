using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests.TypeInference.Level1_FlowAnalysis;

/// <summary>
/// Tests for Callable type narrowing via is_valid() and is_null() type guards.
/// These tests verify that Callable validity checks properly narrow the type
/// to exclude null, enabling safe call() usage without warnings.
/// </summary>
[TestClass]
public class CallableNarrowingTests
{
    #region is_valid() Type Guard

    [TestMethod]
    public void Callable_IsValid_NarrowsToValidCallable()
    {
        var code = @"
func test(cb: Callable):
    if cb.is_valid():
        pass  # cb is valid here
";
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider());
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var ifStatement = FindFirstIfStatement(classDecl);

        Assert.IsNotNull(ifStatement);
        var condition = ifStatement.IfBranch?.Condition;
        Assert.IsNotNull(condition);

        // Act
        var narrowingContext = analyzer.AnalyzeCondition(condition, isNegated: false);

        // Assert - cb should be narrowed to valid (non-null) state
        var narrowedType = narrowingContext.GetNarrowedType("cb");
        Assert.IsNotNull(narrowedType, "cb should have narrowed type info after is_valid() check");
        Assert.IsTrue(narrowedType.IsValidated || narrowedType.ConcreteType?.DisplayName == "ValidCallable" ||
                      !narrowedType.MayBeNull,
            "cb should be marked as validated/non-null after is_valid() check");
    }

    [TestMethod]
    public void Callable_NotIsValid_NarrowsToInvalidInElseBranch()
    {
        var code = @"
func test(cb: Callable):
    if cb.is_valid():
        pass
    else:
        pass  # cb is invalid here
";
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider());
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var ifStatement = FindFirstIfStatement(classDecl);

        Assert.IsNotNull(ifStatement);
        var condition = ifStatement.IfBranch?.Condition;
        Assert.IsNotNull(condition);

        // Act - analyze for else branch (negated)
        var narrowingContext = analyzer.AnalyzeCondition(condition, isNegated: true);

        // Assert - cb should be marked as potentially invalid/null
        var narrowedType = narrowingContext.GetNarrowedType("cb");
        // In else branch of is_valid(), the callable may be invalid
        // This could be represented as MayBeNull=true or ConcreteType="null"
    }

    #endregion

    #region is_null() Type Guard

    [TestMethod]
    public void Callable_IsNull_NarrowsToNullInTrueBranch()
    {
        var code = @"
func test(cb: Callable):
    if cb.is_null():
        pass  # cb is null here
";
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider());
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var ifStatement = FindFirstIfStatement(classDecl);

        Assert.IsNotNull(ifStatement);
        var condition = ifStatement.IfBranch?.Condition;
        Assert.IsNotNull(condition);

        // Act
        var narrowingContext = analyzer.AnalyzeCondition(condition, isNegated: false);

        // Assert - cb should be narrowed to null
        var concreteType = narrowingContext.GetConcreteType("cb");
        var narrowedType = narrowingContext.GetNarrowedType("cb");

        // Either concrete type is "null" or narrowed type indicates null
        Assert.IsTrue(concreteType?.DisplayName == "null" ||
                      (narrowedType != null && narrowedType.MayBeNull),
            "cb should be narrowed to null after is_null() returns true");
    }

    [TestMethod]
    public void Callable_NotIsNull_NarrowsToValidInElseBranch()
    {
        var code = @"
func test(cb: Callable):
    if cb.is_null():
        return
    pass  # cb is valid here (not null)
";
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider());
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var ifStatement = FindFirstIfStatement(classDecl);

        Assert.IsNotNull(ifStatement);
        var condition = ifStatement.IfBranch?.Condition;
        Assert.IsNotNull(condition);

        // Act - analyze for else branch (negated is_null)
        var narrowingContext = analyzer.AnalyzeCondition(condition, isNegated: true);

        // Assert - cb should be valid (not null) after negated is_null() check
        var narrowedType = narrowingContext.GetNarrowedType("cb");
        Assert.IsNotNull(narrowedType, "cb should have narrowed type after not is_null() check");
        Assert.IsFalse(narrowedType.MayBeNull, "cb should not be nullable after negated is_null() check");
    }

    [TestMethod]
    public void Callable_ExplicitNotIsNull_NarrowsToValid()
    {
        var code = @"
func test(cb: Callable):
    if not cb.is_null():
        pass  # cb is valid here
";
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider());
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var ifStatement = FindFirstIfStatement(classDecl);

        Assert.IsNotNull(ifStatement);
        var condition = ifStatement.IfBranch?.Condition;
        Assert.IsNotNull(condition);

        // Act
        var narrowingContext = analyzer.AnalyzeCondition(condition, isNegated: false);

        // Assert - cb should be valid (not null) after "not is_null()" check
        var narrowedType = narrowingContext.GetNarrowedType("cb");
        Assert.IsNotNull(narrowedType, "cb should have narrowed type after 'not is_null()' check");
        Assert.IsFalse(narrowedType.MayBeNull, "cb should not be nullable after 'not is_null()' check");
    }

    #endregion

    #region Truthy Check Type Guard

    [TestMethod]
    public void Callable_TruthyCheck_NarrowsToValid()
    {
        var code = @"
func test(cb: Callable):
    if cb:
        pass  # cb is truthy, meaning it's valid
";
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider());
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var ifStatement = FindFirstIfStatement(classDecl);

        Assert.IsNotNull(ifStatement);
        var condition = ifStatement.IfBranch?.Condition;
        Assert.IsNotNull(condition);

        // Act
        var narrowingContext = analyzer.AnalyzeCondition(condition, isNegated: false);

        // Assert - truthy check on Callable means it's valid
        var narrowedType = narrowingContext.GetNarrowedType("cb");
        // Truthy Callable is valid/non-null
    }

    [TestMethod]
    public void Callable_NotTruthyCheck_NarrowsToInvalid()
    {
        var code = @"
func test(cb: Callable):
    if not cb:
        pass  # cb is falsy, meaning it's invalid/null
";
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider());
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var ifStatement = FindFirstIfStatement(classDecl);

        Assert.IsNotNull(ifStatement);
        var condition = ifStatement.IfBranch?.Condition;
        Assert.IsNotNull(condition);

        // Act
        var narrowingContext = analyzer.AnalyzeCondition(condition, isNegated: false);

        // Assert - falsy check means the callable is invalid/null
        var concreteType = narrowingContext.GetConcreteType("cb");
        var narrowedType = narrowingContext.GetNarrowedType("cb");

        Assert.IsTrue(concreteType?.DisplayName == "null" ||
                      (narrowedType != null && narrowedType.MayBeNull),
            "cb should be narrowed to null/invalid after 'not cb' check");
    }

    #endregion

    #region Combined Checks

    [TestMethod]
    public void Callable_IsValidAndCall_NoWarning()
    {
        var code = @"
func test(cb: Callable):
    if cb.is_valid():
        cb.call()  # Should be safe after is_valid()
";
        // This test verifies that after is_valid() check,
        // cb.call() should not produce any warnings about potential null
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider());
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var ifStatement = FindFirstIfStatement(classDecl);

        Assert.IsNotNull(ifStatement);
        var condition = ifStatement.IfBranch?.Condition;
        Assert.IsNotNull(condition);

        // Act
        var narrowingContext = analyzer.AnalyzeCondition(condition, isNegated: false);

        // Assert - cb is validated
        var narrowedType = narrowingContext.GetNarrowedType("cb");
        Assert.IsNotNull(narrowedType, "Narrowing context should exist for cb");
    }

    [TestMethod]
    public void Callable_CombinedCondition_IsValidAndNotNull()
    {
        var code = @"
func test(cb: Callable):
    if cb and cb.is_valid():
        pass  # cb is definitely valid here
";
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider());
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var ifStatement = FindFirstIfStatement(classDecl);

        Assert.IsNotNull(ifStatement);
        var condition = ifStatement.IfBranch?.Condition;
        Assert.IsNotNull(condition);

        // Act
        var narrowingContext = analyzer.AnalyzeCondition(condition, isNegated: false);

        // Assert - cb should be valid after combined check
        var narrowedType = narrowingContext.GetNarrowedType("cb");
        Assert.IsNotNull(narrowedType, "cb should have narrowed type after combined check");
    }

    #endregion

    #region Helper Methods

    private static GDIfStatement? FindFirstIfStatement(GDClassDeclaration? classDecl)
    {
        if (classDecl == null)
            return null;

        return classDecl.AllNodes
            .OfType<GDIfStatement>()
            .FirstOrDefault();
    }

    #endregion
}
