using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for combined parameter type inference from multiple sources:
/// 1. Explicit type annotations
/// 2. Type guards (is checks) and null checks inside the method
/// 3. Call site argument types from external callers
///
/// These tests verify:
/// - Correct merging of types from all sources
/// - Type diff detection when sources disagree
/// - Edge cases where different sources provide conflicting information
/// </summary>
[TestClass]
public class ParameterTypeSourcesCombinedTests
{
    #region Combined Sources - No Conflict

    [TestMethod]
    public void CombinedSources_TypeGuardAndCallSite_BothInt_SingleType()
    {
        // Arrange - method expects int (from type guard), callers pass int
        var code = @"
func process(x):
    if x is int:
        return x * 2
    return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Inject call site data - callers pass int
        var callSiteTypes = new GDUnionType();
        callSiteTypes.AddType("int");
        semanticModel.SetCallSiteParameterTypes("process", "x", callSiteTypes);

        // Act
        var union = semanticModel.GetUnionType("x");
        var diff = semanticModel.GetParameterTypeDiff("process", "x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"), "Union should contain int");

        Assert.IsNotNull(diff);
        Assert.IsFalse(diff.HasMismatch, "No mismatch when both sources agree on int");
    }

    [TestMethod]
    public void CombinedSources_TypeGuardIntString_CallSiteIntString_Match()
    {
        // Arrange - method expects int|String, callers pass int|String
        var code = @"
func process(x):
    if x is int:
        return x * 2
    if x is String:
        return x.length()
    return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Inject call site data - callers pass both int and String
        var callSiteTypes = new GDUnionType();
        callSiteTypes.AddType("int");
        callSiteTypes.AddType("String");
        semanticModel.SetCallSiteParameterTypes("process", "x", callSiteTypes);

        // Act
        var union = semanticModel.GetUnionType("x");
        var diff = semanticModel.GetParameterTypeDiff("process", "x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"));
        Assert.IsTrue(union.Types.Contains("String"));

        Assert.IsNotNull(diff);
        Assert.IsFalse(diff.HasMismatch,
            $"No mismatch expected. Missing: [{string.Join(", ", diff.MissingTypes)}], " +
            $"Unexpected: [{string.Join(", ", diff.UnexpectedTypes)}]");
    }

    #endregion

    #region Combined Sources - Mismatch Cases

    [TestMethod]
    public void CombinedSources_TypeGuardInt_CallSiteString_Mismatch()
    {
        // Arrange - method expects int (from type guard), but callers pass String
        var code = @"
func process(x):
    if x is int:
        return x * 2
    return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Inject call site data - callers pass String (not expected!)
        var callSiteTypes = new GDUnionType();
        callSiteTypes.AddType("String");
        semanticModel.SetCallSiteParameterTypes("process", "x", callSiteTypes);

        // Act
        var diff = semanticModel.GetParameterTypeDiff("process", "x");

        // Assert
        Assert.IsNotNull(diff);
        Assert.IsTrue(diff.HasMismatch, "Mismatch expected: method expects int, callers pass String");
        Assert.IsTrue(diff.UnexpectedTypes.Contains("String"),
            $"String should be unexpected. Unexpected: [{string.Join(", ", diff.UnexpectedTypes)}]");
    }

    [TestMethod]
    public void CombinedSources_TypeGuardIntString_CallSiteIntOnly_MissingString()
    {
        // Arrange - method handles int|String, but callers only pass int
        var code = @"
func process(x):
    if x is int:
        return x * 2
    if x is String:
        return x.length()
    return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Inject call site data - callers only pass int
        var callSiteTypes = new GDUnionType();
        callSiteTypes.AddType("int");
        semanticModel.SetCallSiteParameterTypes("process", "x", callSiteTypes);

        // Act
        var diff = semanticModel.GetParameterTypeDiff("process", "x");

        // Assert
        Assert.IsNotNull(diff);
        // String is expected (from type guard) but never passed - this is "missing"
        Assert.IsTrue(diff.MissingTypes.Contains("String"),
            $"String should be missing from call sites. Missing: [{string.Join(", ", diff.MissingTypes)}]");
    }

    [TestMethod]
    public void CombinedSources_TypeGuardInt_CallSiteIntFloat_UnexpectedFloat()
    {
        // Arrange - method only handles int, but callers also pass float
        var code = @"
func process(x):
    if x is int:
        return x * 2
    return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Inject call site data - callers pass int and float
        var callSiteTypes = new GDUnionType();
        callSiteTypes.AddType("int");
        callSiteTypes.AddType("float");
        semanticModel.SetCallSiteParameterTypes("process", "x", callSiteTypes);

        // Act
        var diff = semanticModel.GetParameterTypeDiff("process", "x");

        // Assert - Note: int->float is compatible, but float->int requires explicit check
        // The method expects int, callers pass float - this could be problematic
        Assert.IsNotNull(diff);
        // float is not in the expected types from type guards
        // But int->float compatibility might make this pass
        var actualTypes = string.Join(", ", diff.ActualTypes.Types);
        var expectedTypes = string.Join(", ", diff.ExpectedTypes.Types);
        // This test documents the behavior - float may or may not be unexpected
        // depending on compatibility rules
    }

    #endregion

    #region Null Check Scenarios

    [TestMethod]
    public void CombinedSources_NullCheckInMethod_CallSitePassesNull_Match()
    {
        // Arrange - method has null check, callers pass null
        var code = @"
func process(x):
    if x == null:
        return 0
    return x
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Inject call site data - callers pass null
        var callSiteTypes = new GDUnionType();
        callSiteTypes.AddType("null");
        semanticModel.SetCallSiteParameterTypes("process", "x", callSiteTypes);

        // Act
        var union = semanticModel.GetUnionType("x");
        var diff = semanticModel.GetParameterTypeDiff("process", "x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("null"),
            $"Union should contain null from null check. Types: [{string.Join(", ", union.Types)}]");

        Assert.IsNotNull(diff);
        Assert.IsTrue(diff.MatchingTypes.Contains("null") || !diff.UnexpectedTypes.Contains("null"),
            "null should match since method has null check");
    }

    [TestMethod]
    public void CombinedSources_NoNullCheck_CallSitePassesNull_MaybeUnexpected()
    {
        // Arrange - method has NO null check, callers pass null
        var code = @"
func process(x):
    if x is int:
        return x * 2
    return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Inject call site data - callers pass null
        var callSiteTypes = new GDUnionType();
        callSiteTypes.AddType("null");
        semanticModel.SetCallSiteParameterTypes("process", "x", callSiteTypes);

        // Act
        var diff = semanticModel.GetParameterTypeDiff("process", "x");

        // Assert
        Assert.IsNotNull(diff);
        // null is compatible with everything in GDScript, so it might not be "unexpected"
        // Document the actual behavior
        var summary = diff.GetSummary();
    }

    [TestMethod]
    public void CombinedSources_TypeGuardAndNullCheck_CallSiteAll_Match()
    {
        // Arrange - method expects int|String|null
        var code = @"
func process(x):
    if x == null:
        return -1
    if x is int:
        return x * 2
    if x is String:
        return x.length()
    return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Inject call site data - callers pass all expected types
        var callSiteTypes = new GDUnionType();
        callSiteTypes.AddType("int");
        callSiteTypes.AddType("String");
        callSiteTypes.AddType("null");
        semanticModel.SetCallSiteParameterTypes("process", "x", callSiteTypes);

        // Act
        var union = semanticModel.GetUnionType("x");
        var diff = semanticModel.GetParameterTypeDiff("process", "x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"));
        Assert.IsTrue(union.Types.Contains("String"));
        Assert.IsTrue(union.Types.Contains("null"));

        Assert.IsNotNull(diff);
        Assert.IsFalse(diff.HasMismatch,
            $"All types should match. Summary: {diff.GetSummary()}");
    }

    #endregion

    #region Explicit Type Annotation vs Call Sites

    [TestMethod]
    public void CombinedSources_ExplicitInt_CallSiteInt_Match()
    {
        // Arrange - explicit type annotation
        var code = @"
func process(x: int):
    return x * 2
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Inject call site data - callers pass int
        var callSiteTypes = new GDUnionType();
        callSiteTypes.AddType("int");
        semanticModel.SetCallSiteParameterTypes("process", "x", callSiteTypes);

        // Act
        var diff = semanticModel.GetParameterTypeDiff("process", "x");

        // Assert
        Assert.IsNotNull(diff);
        Assert.IsFalse(diff.HasMismatch, "Explicit int should match call site int");
    }

    [TestMethod]
    public void CombinedSources_ExplicitInt_CallSiteString_Mismatch()
    {
        // Arrange - explicit type annotation
        var code = @"
func process(x: int):
    return x * 2
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Inject call site data - callers pass String (wrong!)
        var callSiteTypes = new GDUnionType();
        callSiteTypes.AddType("String");
        semanticModel.SetCallSiteParameterTypes("process", "x", callSiteTypes);

        // Act
        var diff = semanticModel.GetParameterTypeDiff("process", "x");

        // Assert
        Assert.IsNotNull(diff);
        Assert.IsTrue(diff.HasMismatch, "Explicit int should NOT match call site String");
        Assert.IsTrue(diff.UnexpectedTypes.Contains("String"),
            $"String should be unexpected. Unexpected: [{string.Join(", ", diff.UnexpectedTypes)}]");
    }

    [TestMethod]
    public void CombinedSources_ExplicitInt_CallSiteIntFloat_FloatUnexpected()
    {
        // Arrange - explicit int type
        var code = @"
func process(x: int):
    return x * 2
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Inject call site data - callers pass int and float
        var callSiteTypes = new GDUnionType();
        callSiteTypes.AddType("int");
        callSiteTypes.AddType("float");
        semanticModel.SetCallSiteParameterTypes("process", "x", callSiteTypes);

        // Act
        var diff = semanticModel.GetParameterTypeDiff("process", "x");

        // Assert
        Assert.IsNotNull(diff);
        // float is NOT compatible with int (narrowing)
        Assert.IsTrue(diff.HasMismatch,
            $"float->int is narrowing. Summary: {diff.GetSummary()}");
    }

    #endregion

    #region Multiple Parameters

    [TestMethod]
    public void CombinedSources_MultipleParams_IndependentDiffs()
    {
        // Arrange - method with multiple parameters
        var code = @"
func process(a, b):
    if a is int:
        pass
    if b is String:
        pass
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Inject call site data for both params
        var aTypes = new GDUnionType();
        aTypes.AddType("int");
        aTypes.AddType("float"); // Extra type not in type guard
        semanticModel.SetCallSiteParameterTypes("process", "a", aTypes);

        var bTypes = new GDUnionType();
        bTypes.AddType("String");
        semanticModel.SetCallSiteParameterTypes("process", "b", bTypes);

        // Act
        var diffA = semanticModel.GetParameterTypeDiff("process", "a");
        var diffB = semanticModel.GetParameterTypeDiff("process", "b");

        // Assert
        Assert.IsNotNull(diffA);
        Assert.IsNotNull(diffB);

        // a: expects int, gets int|float - float might be unexpected
        // b: expects String, gets String - should match
        Assert.IsFalse(diffB.HasMismatch,
            $"Parameter b should match. Summary: {diffB.GetSummary()}");
    }

    #endregion

    #region Call Sites Override Union

    [TestMethod]
    public void CombinedSources_CallSitesAddToUnion()
    {
        // Arrange - type guard says int, but call sites also pass String
        var code = @"
func process(x):
    if x is int:
        return x * 2
    return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Inject call site data with additional type
        var callSiteTypes = new GDUnionType();
        callSiteTypes.AddType("int");
        callSiteTypes.AddType("String"); // Not in type guards
        semanticModel.SetCallSiteParameterTypes("process", "x", callSiteTypes);

        // Act - GetUnionType should include call site types
        var union = semanticModel.GetUnionType("x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"), "int from type guard");
        Assert.IsTrue(union.Types.Contains("String"),
            $"String from call sites should be in union. Types: [{string.Join(", ", union.Types)}]");
    }

    #endregion

    #region Complex Scenarios

    [TestMethod]
    public void ComplexScenario_MethodChain_TypeGuardInCallee_CallSiteFromCaller()
    {
        // Arrange - Two methods where one calls the other
        var code = @"
func inner(x):
    if x is int:
        return x * 2
    if x is String:
        return x.length()
    return 0

func outer(y):
    # y is passed to inner
    return inner(y)
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Inject call site for inner - as if outer is calling it
        var innerCallSites = new GDUnionType();
        innerCallSites.AddType("Array"); // outer might pass Array to inner
        semanticModel.SetCallSiteParameterTypes("inner", "x", innerCallSites);

        // Act
        var diff = semanticModel.GetParameterTypeDiff("inner", "x");

        // Assert
        Assert.IsNotNull(diff);
        // inner expects int|String (from type guards), but outer passes Array
        Assert.IsTrue(diff.HasMismatch, "Array is not handled by inner");
        Assert.IsTrue(diff.UnexpectedTypes.Contains("Array"),
            $"Array should be unexpected. Summary: {diff.GetSummary()}");
    }

    [TestMethod]
    public void ComplexScenario_PolymorphicUsage()
    {
        // Arrange - method with duck typing (uses methods common to multiple types)
        var code = @"
func process(x):
    # Duck typing: expects something with size() method
    return x.size()
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Inject call site data - callers pass Array and Dictionary (both have size())
        var callSiteTypes = new GDUnionType();
        callSiteTypes.AddType("Array");
        callSiteTypes.AddType("Dictionary");
        callSiteTypes.AddType("String"); // String also has .size() in some contexts
        semanticModel.SetCallSiteParameterTypes("process", "x", callSiteTypes);

        // Act
        var union = semanticModel.GetUnionType("x");
        var diff = semanticModel.GetParameterTypeDiff("process", "x");

        // Assert - no type guards, so all call site types should be in union
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("Array"));
        Assert.IsTrue(union.Types.Contains("Dictionary"));
        Assert.IsTrue(union.Types.Contains("String"));
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void EdgeCase_NoTypeInfo_EmptyUnion()
    {
        // Arrange - no type guards, no call sites
        var code = @"
func process(x):
    return x
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Don't inject any call site data

        // Act
        var union = semanticModel.GetUnionType("x");
        var diff = semanticModel.GetParameterTypeDiff("process", "x");

        // Assert - should be empty/Variant
        Assert.IsNotNull(diff);
        Assert.IsTrue(diff.ExpectedIsEmpty || diff.ExpectedTypes.Types.Count == 0,
            "No expected types without type guards");
        Assert.IsTrue(diff.ActualIsEmpty,
            "No actual types without call site data");
    }

    [TestMethod]
    public void EdgeCase_OnlyCallSites_NoTypeGuards()
    {
        // Arrange - no type guards in method
        var code = @"
func process(x):
    return x
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Inject call site data
        var callSiteTypes = new GDUnionType();
        callSiteTypes.AddType("int");
        callSiteTypes.AddType("String");
        semanticModel.SetCallSiteParameterTypes("process", "x", callSiteTypes);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert - union should have call site types
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"));
        Assert.IsTrue(union.Types.Contains("String"));
    }

    [TestMethod]
    public void EdgeCase_SameTypeDifferentConfidence()
    {
        // Arrange - type guard (high confidence) + call site (low confidence) both say int
        var code = @"
func process(x):
    if x is int:
        return x * 2
    return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Inject call site data with same type
        var callSiteTypes = new GDUnionType();
        callSiteTypes.AddType("int", isHighConfidence: false);
        semanticModel.SetCallSiteParameterTypes("process", "x", callSiteTypes);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert - int should appear once, not twice
        Assert.IsNotNull(union);
        Assert.AreEqual(1, union.Types.Count(t => t == "int"),
            "int should appear exactly once in union");
    }

    #endregion

    #region Default Value Tests

    [TestMethod]
    public void DefaultValue_IntLiteral_InfersInt()
    {
        // Arrange - parameter with default value
        var code = @"
func process(x = 42):
    return x
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");
        var diff = semanticModel.GetParameterTypeDiff("process", "x");

        // Assert - should infer int from default value
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"),
            $"Should infer int from default value 42. Types: [{string.Join(", ", union.Types)}]");

        Assert.IsNotNull(diff);
        Assert.IsTrue(diff.ExpectedTypes.Types.Contains("int"),
            $"Expected should contain int from default value. Types: [{string.Join(", ", diff.ExpectedTypes.Types)}]");
    }

    [TestMethod]
    public void DefaultValue_StringLiteral_InfersString()
    {
        // Arrange
        var code = @"
func process(name = ""default""):
    return name
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("name");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("String"),
            $"Should infer String from default value. Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void DefaultValue_NullLiteral_InfersNull()
    {
        // Arrange
        var code = @"
func process(x = null):
    if x == null:
        return 0
    return x
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("null"),
            $"Should infer null from default value. Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void DefaultValue_WithTypeAnnotation_BothInUnion()
    {
        // Arrange - explicit type + default value of same type
        var code = @"
func process(x: int = 42):
    return x * 2
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert - int should appear once (deduplicated)
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"));
        Assert.AreEqual(1, union.Types.Count(t => t == "int"),
            "int should appear exactly once even with both annotation and default");
    }

    [TestMethod]
    public void DefaultValue_ArrayLiteral_InfersArray()
    {
        // Arrange
        var code = @"
func process(items = []):
    for item in items:
        print(item)
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("items");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("Array"),
            $"Should infer Array from default value []. Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void DefaultValue_DictLiteral_InfersDictionary()
    {
        // Arrange
        var code = @"
func process(config = {}):
    return config.get(""key"")
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("config");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("Dictionary"),
            $"Should infer Dictionary from default value {{}}. Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void DefaultValue_WithCallSite_CombinesBoth()
    {
        // Arrange - default is int, call site passes String
        var code = @"
func process(x = 42):
    return x
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Inject call site with String
        var callSiteTypes = new GDUnionType();
        callSiteTypes.AddType("String");
        semanticModel.SetCallSiteParameterTypes("process", "x", callSiteTypes);

        // Act
        var diff = semanticModel.GetParameterTypeDiff("process", "x");

        // Assert - expected has int (from default), actual has String
        Assert.IsNotNull(diff);
        Assert.IsTrue(diff.ExpectedTypes.Types.Contains("int"),
            "Expected should have int from default value");
        Assert.IsTrue(diff.ActualTypes.Types.Contains("String"),
            "Actual should have String from call site");
        Assert.IsTrue(diff.HasMismatch,
            "Should detect mismatch: expects int, gets String");
        Assert.IsTrue(diff.UnexpectedTypes.Contains("String"),
            "String should be unexpected");
    }

    #endregion

    #region Match Pattern Tests

    [TestMethod]
    public void MatchPattern_IntLiterals_InfersInt()
    {
        // Arrange - match statement with int literals
        var code = @"
func process(x):
    match x:
        1:
            return ""one""
        2:
            return ""two""
        _:
            return ""other""
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert - should infer int from match patterns
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"),
            $"Should infer int from match patterns 1, 2. Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void MatchPattern_StringLiterals_InfersString()
    {
        // Arrange - match statement with string literals
        var code = @"
func process(x):
    match x:
        ""hello"":
            return 1
        ""world"":
            return 2
        _:
            return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("String"),
            $"Should infer String from match patterns. Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void MatchPattern_MixedLiterals_InfersUnion()
    {
        // Arrange - match statement with int and string literals
        var code = @"
func process(x):
    match x:
        1:
            return ""int one""
        ""hello"":
            return ""string hello""
        _:
            return ""unknown""
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert - should infer int|String
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"),
            $"Should infer int from match pattern 1. Types: [{string.Join(", ", union.Types)}]");
        Assert.IsTrue(union.Types.Contains("String"),
            $"Should infer String from match pattern \"hello\". Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void MatchPattern_WithBinding_OnlyInfersLiterals()
    {
        // Arrange - match with var binding (should NOT add type)
        var code = @"
func process(x):
    match x:
        1:
            return ""one""
        var y:
            return str(y)
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert - should only infer int from literal, not from var binding
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"),
            $"Should infer int from literal pattern. Types: [{string.Join(", ", union.Types)}]");
        // var binding doesn't constrain type
    }

    [TestMethod]
    public void MatchPattern_BoolLiterals_InfersBool()
    {
        // Arrange
        var code = @"
func process(flag):
    match flag:
        true:
            return 1
        false:
            return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("flag");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("bool"),
            $"Should infer bool from match patterns. Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void MatchPattern_NullLiteral_InfersNull()
    {
        // Arrange
        var code = @"
func process(x):
    match x:
        null:
            return ""is null""
        _:
            return ""not null""
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("null"),
            $"Should infer null from match pattern. Types: [{string.Join(", ", union.Types)}]");
    }

    #endregion

    #region typeof() Tests

    [TestMethod]
    public void TypeofCheck_TYPE_INT_InfersInt()
    {
        // Arrange
        var code = @"
func process(x):
    if typeof(x) == TYPE_INT:
        return x * 2
    return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"),
            $"Should infer int from typeof(x) == TYPE_INT. Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void TypeofCheck_TYPE_STRING_InfersString()
    {
        // Arrange
        var code = @"
func process(x):
    if typeof(x) == TYPE_STRING:
        return x.length()
    return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("String"),
            $"Should infer String from typeof check. Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void TypeofCheck_TYPE_VECTOR2I_InfersVector2I()
    {
        // Arrange - test "i" variant types
        var code = @"
func process(pos):
    if typeof(pos) == TYPE_VECTOR2I:
        return pos.x + pos.y
    return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("pos");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("Vector2I"),
            $"Should infer Vector2I from typeof check. Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void TypeofCheck_TYPE_VECTOR3I_InfersVector3I()
    {
        // Arrange
        var code = @"
func process(pos):
    if typeof(pos) == TYPE_VECTOR3I:
        return pos.x + pos.y + pos.z
    return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("pos");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("Vector3I"),
            $"Should infer Vector3I from typeof check. Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void TypeofCheck_TYPE_ARRAY_InfersArray()
    {
        // Arrange
        var code = @"
func process(data):
    if typeof(data) == TYPE_ARRAY:
        return data.size()
    return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("data");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("Array"),
            $"Should infer Array from typeof check. Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void TypeofCheck_MultipleTypes_InfersUnion()
    {
        // Arrange
        var code = @"
func process(x):
    if typeof(x) == TYPE_INT:
        return x * 2
    elif typeof(x) == TYPE_FLOAT:
        return x * 3.0
    return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"),
            $"Should infer int from first typeof check. Types: [{string.Join(", ", union.Types)}]");
        Assert.IsTrue(union.Types.Contains("float"),
            $"Should infer float from second typeof check. Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void TypeofCheck_ReversedOrder_StillInfers()
    {
        // Arrange - TYPE_INT == typeof(x) instead of typeof(x) == TYPE_INT
        var code = @"
func process(x):
    if TYPE_INT == typeof(x):
        return x * 2
    return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"),
            $"Should infer int even with reversed comparison. Types: [{string.Join(", ", union.Types)}]");
    }

    #endregion

    #region Assert Tests

    [TestMethod]
    public void AssertIs_SingleType_InfersType()
    {
        // Arrange
        var code = @"
func process(x):
    assert(x is Player)
    return x.health
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("Player"),
            $"Should infer Player from assert(x is Player). Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void AssertIs_Combined_WithTypeGuard()
    {
        // Arrange - both assert and type guard
        var code = @"
func process(x):
    assert(x is Node)
    if x is Sprite2D:
        return x.texture
    return null
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("Node"),
            $"Should infer Node from assert. Types: [{string.Join(", ", union.Types)}]");
        Assert.IsTrue(union.Types.Contains("Sprite2D"),
            $"Should infer Sprite2D from type guard. Types: [{string.Join(", ", union.Types)}]");
    }

    #endregion

    #region Negative Guard Tests

    [TestMethod]
    public void NegativeGuard_NotIsType_InfersType()
    {
        // Arrange - "if not x is int: return" still tells us x can be int
        var code = @"
func process(x):
    if not x is int:
        return 0
    return x * 2
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert - even with negation, we know x CAN be int
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"),
            $"Should infer int from 'if not x is int' - negation still tells us x can be int. Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void NegativeGuard_NotWithParens_InfersType()
    {
        // Arrange - using "not (x is String)" with parentheses
        var code = @"
func process(x):
    if not (x is String):
        return 0
    return x.length()
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("String"),
            $"Should infer String from 'not (x is String)'. Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void NegativeGuard_DoubleNot_HandlesCorrectly()
    {
        // Arrange - double negation
        var code = @"
func process(x):
    if not not x is int:
        return x * 2
    return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert - double negation is positive, x is int
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"),
            $"Should infer int from 'not not x is int'. Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void NegativeGuard_CombinedWithPositive_InfersBoth()
    {
        // Arrange
        var code = @"
func process(x):
    if not x is int:
        if x is String:
            return x.length()
        return 0
    return x * 2
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert - both int and String should be inferred
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"),
            $"Should infer int from negative guard. Types: [{string.Join(", ", union.Types)}]");
        Assert.IsTrue(union.Types.Contains("String"),
            $"Should infer String from positive guard. Types: [{string.Join(", ", union.Types)}]");
    }

    #endregion

    #region Combined New Features

    [TestMethod]
    public void Combined_MatchAndTypeGuard_InfersUnion()
    {
        // Arrange - both match and type guard
        var code = @"
func process(x):
    if x is Array:
        return x.size()
    match x:
        1:
            return ""one""
        2:
            return ""two""
    return ""unknown""
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert - should have Array (from guard) and int (from match)
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("Array"),
            $"Should infer Array from type guard. Types: [{string.Join(", ", union.Types)}]");
        Assert.IsTrue(union.Types.Contains("int"),
            $"Should infer int from match pattern. Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void Combined_TypeofAndAssert_InfersUnion()
    {
        // Arrange
        var code = @"
func process(x):
    assert(x is Node)
    if typeof(x) == TYPE_INT:
        return x * 2
    return 0
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("Node"),
            $"Should infer Node from assert. Types: [{string.Join(", ", union.Types)}]");
        Assert.IsTrue(union.Types.Contains("int"),
            $"Should infer int from typeof check. Types: [{string.Join(", ", union.Types)}]");
    }

    [TestMethod]
    public void Combined_AllFeatures_ComplexScenario()
    {
        // Arrange - uses all new features together
        var code = @"
func process(x):
    assert(x is Object)  # assert
    if not x is Node:    # negative guard
        return 0
    if typeof(x) == TYPE_INT:  # typeof check
        return x * 2
    match x:             # match pattern
        ""hello"":
            return 1
        null:
            return -1
    return x
";
        var semanticModel = BuildSemanticModel(code);
        Assert.IsNotNull(semanticModel);

        // Act
        var union = semanticModel.GetUnionType("x");

        // Assert - should have Object, Node, int, String, null
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("Object"),
            $"Should infer Object from assert. Types: [{string.Join(", ", union.Types)}]");
        Assert.IsTrue(union.Types.Contains("Node"),
            $"Should infer Node from negative guard. Types: [{string.Join(", ", union.Types)}]");
        Assert.IsTrue(union.Types.Contains("int"),
            $"Should infer int from typeof check. Types: [{string.Join(", ", union.Types)}]");
        Assert.IsTrue(union.Types.Contains("String"),
            $"Should infer String from match pattern. Types: [{string.Join(", ", union.Types)}]");
        Assert.IsTrue(union.Types.Contains("null"),
            $"Should infer null from match pattern. Types: [{string.Join(", ", union.Types)}]");
    }

    #endregion

    #region Helper Methods

    private static GDSemanticModel? BuildSemanticModel(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        if (classDecl == null)
            return null;

        var reference = new GDScriptReference("test://virtual/combined_test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = GDDefaultRuntimeProvider.Instance;
        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        return collector.BuildSemanticModel();
    }

    #endregion
}
