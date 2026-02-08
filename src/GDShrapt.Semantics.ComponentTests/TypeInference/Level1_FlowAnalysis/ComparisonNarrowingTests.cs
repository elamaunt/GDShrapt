using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Level 1: Tests for type narrowing via comparison operators.
/// Tests validate that:
/// - `x == literal` narrows x to the literal's type
/// - `x in container` narrows x to the container's element/key type
/// </summary>
[TestClass]
public class ComparisonNarrowingTests
{
    #region Equality Narrowing - Integer

    [TestMethod]
    public void EqualityWithInt_NarrowsToInt()
    {
        var code = @"
func test(x):
    if x == 42:
        return x + 1
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after == 42");
        Assert.AreEqual("int", result.NarrowedType, $"x should be narrowed to int. Actual: {result.NarrowedType}");
    }

    [TestMethod]
    public void EqualityWithInt_ReverseOrder_NarrowsToInt()
    {
        var code = @"
func test(x):
    if 42 == x:
        return x + 1
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after 42 == x");
        Assert.AreEqual("int", result.NarrowedType, $"x should be narrowed to int. Actual: {result.NarrowedType}");
    }

    #endregion

    #region Equality Narrowing - String

    [TestMethod]
    public void EqualityWithString_NarrowsToString()
    {
        var code = @"
func test(x):
    if x == ""hello"":
        return x.length()
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after == \"hello\"");
        Assert.AreEqual("String", result.NarrowedType, $"x should be narrowed to String. Actual: {result.NarrowedType}");
    }

    [TestMethod]
    public void EqualityWithString_AllowsStringMethods_NoDiagnostic()
    {
        var code = @"
func test(x):
    if x == ""hello"":
        return x.length()
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d =>
            d.Code == GDDiagnosticCode.PotentiallyNullAccess ||
            d.Code == GDDiagnosticCode.PotentiallyNullMethodCall),
            $"After == \"hello\", x is String. Methods should be allowed. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region Equality Narrowing - Float

    [TestMethod]
    public void EqualityWithFloat_NarrowsToFloat()
    {
        var code = @"
func test(x):
    if x == 3.14:
        return x * 2.0
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after == 3.14");
        Assert.AreEqual("float", result.NarrowedType, $"x should be narrowed to float. Actual: {result.NarrowedType}");
    }

    #endregion

    #region Equality Narrowing - Bool

    [TestMethod]
    public void EqualityWithBool_NarrowsToBool()
    {
        var code = @"
func test(x):
    if x == true:
        return not x
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after == true");
        Assert.AreEqual("bool", result.NarrowedType, $"x should be narrowed to bool. Actual: {result.NarrowedType}");
    }

    #endregion

    #region Equality Narrowing - null

    [TestMethod]
    public void EqualityWithNull_NarrowsToNull()
    {
        var code = @"
func test(x):
    if x == null:
        pass
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after == null");
        Assert.AreEqual("null", result.NarrowedType, $"x should be narrowed to null. Actual: {result.NarrowedType}");
    }

    [TestMethod]
    public void EqualityWithNull_MarksAsNotNullInElse()
    {
        var code = @"
func test(x):
    if x == null:
        return 0
    return x.length()
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d =>
            d.Code == GDDiagnosticCode.PotentiallyNullAccess ||
            d.Code == GDDiagnosticCode.PotentiallyNullMethodCall),
            $"After else of 'x == null', x is not null. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region In Operator Narrowing - Array Literal

    [TestMethod]
    public void InArrayLiteral_IntElements_NarrowsToInt()
    {
        var code = @"
func test(x):
    if x in [1, 2, 3]:
        return x + 1
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after 'in [1,2,3]'");
        Assert.AreEqual("int", result.NarrowedType, $"x should be narrowed to int. Actual: {result.NarrowedType}");
    }

    [TestMethod]
    public void InArrayLiteral_StringElements_NarrowsToString()
    {
        var code = @"
func test(x):
    if x in [""a"", ""b"", ""c""]:
        return x.length()
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after 'in [\"a\",\"b\"]'");
        Assert.AreEqual("String", result.NarrowedType, $"x should be narrowed to String. Actual: {result.NarrowedType}");
    }

    [TestMethod]
    public void InArrayLiteral_AllowsMethods_NoDiagnostic()
    {
        var code = @"
func test(x):
    if x in [""a"", ""b"", ""c""]:
        return x.to_upper()
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d =>
            d.Code == GDDiagnosticCode.PotentiallyNullMethodCall),
            $"After 'in [\"a\", \"b\"]', x is String. Methods should be allowed. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region In Operator Narrowing - Typed Array Variable
    // NOTE: These tests require semantic model integration to resolve variable types.
    // Currently they pass as long as the container is a literal expression.
    // For typed variables, full GDFlowAnalyzer integration is needed.

    [TestMethod]
    public void InTypedArrayVariable_NarrowsToElementType()
    {
        var code = @"
func test(x):
    var numbers: Array[int] = [1, 2, 3]
    if x in numbers:
        return x + 1
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after 'in numbers'");
        Assert.AreEqual("int", result.NarrowedType, $"x should be narrowed to int (Array[int] element). Actual: {result.NarrowedType}");
    }

    [TestMethod]
    public void InTypedArrayVariable_StringArray_NarrowsToString()
    {
        var code = @"
func test(x):
    var names: Array[String] = [""Alice"", ""Bob""]
    if x in names:
        return x.length()
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after 'in names'");
        Assert.AreEqual("String", result.NarrowedType, $"x should be narrowed to String. Actual: {result.NarrowedType}");
    }

    #endregion

    #region In Operator Narrowing - Dictionary

    [TestMethod]
    public void InDictionaryLiteral_NarrowsToKeyType()
    {
        var code = @"
func test(x):
    if x in {""key"": 1, ""other"": 2}:
        return x.to_upper()
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after 'in {\"key\": 1}'");
        Assert.AreEqual("String", result.NarrowedType, $"x should be narrowed to String (dict key type). Actual: {result.NarrowedType}");
    }

    [TestMethod]
    public void InTypedDictionaryVariable_NarrowsToKeyType()
    {
        var code = @"
func test(x):
    var data: Dictionary[String, int] = {""a"": 1}
    if x in data:
        return x.length()
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after 'in data'");
        Assert.AreEqual("String", result.NarrowedType, $"x should be narrowed to String (dict key type). Actual: {result.NarrowedType}");
    }

    [TestMethod]
    public void InDictionaryLiteral_IntKeys_NarrowsToInt()
    {
        var code = @"
func test(x):
    if x in {1: ""a"", 2: ""b""}:
        return x + 1
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after 'in {1: \"a\"}'");
        Assert.AreEqual("int", result.NarrowedType, $"x should be narrowed to int (dict key type). Actual: {result.NarrowedType}");
    }

    #endregion

    #region In Operator Narrowing - String

    [TestMethod]
    public void InStringLiteral_NarrowsToString()
    {
        var code = @"
func test(x):
    if x in ""hello world"":
        return x.length()
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after 'in \"hello\"'");
        Assert.AreEqual("String", result.NarrowedType, $"x should be narrowed to String. Actual: {result.NarrowedType}");
    }

    [TestMethod]
    public void InStringVariable_NarrowsToString()
    {
        var code = @"
func test(x):
    var text: String = ""hello""
    if x in text:
        return x.to_upper()
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after 'in text'");
        Assert.AreEqual("String", result.NarrowedType, $"x should be narrowed to String. Actual: {result.NarrowedType}");
    }

    #endregion

    #region In Operator Narrowing - Range

    [TestMethod]
    public void InRange_NarrowsToInt()
    {
        var code = @"
func test(x):
    if x in range(1, 10):
        return x * 2
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after 'in range(1, 10)'");
        Assert.AreEqual("int", result.NarrowedType, $"x should be narrowed to int. Actual: {result.NarrowedType}");
    }

    [TestMethod]
    public void InRangeVariable_NarrowsToInt()
    {
        var code = @"
func test(x):
    var r = range(0, 100)
    if x in r:
        return x + 1
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after 'in r'");
        Assert.AreEqual("int", result.NarrowedType, $"x should be narrowed to int. Actual: {result.NarrowedType}");
    }

    #endregion

    #region In Operator Narrowing - PackedArrays

    [TestMethod]
    public void InPackedInt32Array_NarrowsToInt()
    {
        var code = @"
func test(x):
    var arr: PackedInt32Array = PackedInt32Array([1, 2, 3])
    if x in arr:
        return x + 1
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after 'in PackedInt32Array'");
        Assert.AreEqual("int", result.NarrowedType, $"x should be narrowed to int. Actual: {result.NarrowedType}");
    }

    [TestMethod]
    public void InPackedStringArray_NarrowsToString()
    {
        var code = @"
func test(x):
    var arr: PackedStringArray = PackedStringArray([""a"", ""b""])
    if x in arr:
        return x.length()
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after 'in PackedStringArray'");
        Assert.AreEqual("String", result.NarrowedType, $"x should be narrowed to String. Actual: {result.NarrowedType}");
    }

    [TestMethod]
    public void InPackedFloat32Array_NarrowsToFloat()
    {
        var code = @"
func test(x):
    var arr: PackedFloat32Array = PackedFloat32Array([1.0, 2.0])
    if x in arr:
        return x * 2.0
";
        var result = AnalyzeNarrowedType(code, "x");
        Assert.IsNotNull(result.NarrowedType, "x should be narrowed after 'in PackedFloat32Array'");
        Assert.AreEqual("float", result.NarrowedType, $"x should be narrowed to float. Actual: {result.NarrowedType}");
    }

    #endregion

    #region Narrowing Marks NonNull

    [TestMethod]
    public void InOperator_MarksNonNull()
    {
        var code = @"
func test(x):
    if x in [1, 2, 3]:
        return x.abs()
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d =>
            d.Code == GDDiagnosticCode.PotentiallyNullMethodCall),
            $"After 'in [1,2,3]', x is int (non-null). Methods should be allowed. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void EqualityWithNonNull_MarksNonNull()
    {
        var code = @"
func test(x):
    if x == 42:
        return x.abs()
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d =>
            d.Code == GDDiagnosticCode.PotentiallyNullMethodCall),
            $"After '== 42', x is int (non-null). Methods should be allowed. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void UntypedArrayLiteral_NoNarrowing()
    {
        var code = @"
func test(x):
    var arr = [1, ""str"", 3.14]
    if x in arr:
        pass
";
        var result = AnalyzeNarrowedType(code, "x");
        // Mixed type array - should not narrow or stay as Variant
        Assert.IsTrue(result.NarrowedType == null || result.NarrowedType == "Variant",
            $"Mixed array should not narrow to specific type. Actual: {result.NarrowedType}");
    }

    [TestMethod]
    public void InEmptyArray_NoNarrowing()
    {
        var code = @"
func test(x):
    if x in []:
        pass
";
        var result = AnalyzeNarrowedType(code, "x");
        // Empty array - no narrowing possible
        Assert.IsTrue(result.NarrowedType == null || result.NarrowedType == "Variant",
            $"Empty array should not narrow. Actual: {result.NarrowedType}");
    }

    #endregion

    #region Helper Methods

    private record NarrowingResult(string? NarrowedType, bool IsNonNull);

    private static NarrowingResult AnalyzeNarrowedType(string code, string variableName)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members.OfType<GDMethodDeclaration>().FirstOrDefault();

        if (method == null)
            return new NarrowingResult(null, false);

        // Find the if statement
        var ifStatement = method.Statements?.OfType<GDIfStatement>().FirstOrDefault();
        if (ifStatement?.IfBranch?.Condition == null)
            return new NarrowingResult(null, false);

        var condition = ifStatement.IfBranch.Condition;

        // Build variable type map for typed container resolution
        var variableTypes = BuildVariableTypeMap(method);

        // Create type resolver
        Func<string, string?> variableTypeResolver = (varName) =>
            variableTypes.TryGetValue(varName, out var type) ? type : null;

        // Use GDTypeNarrowingAnalyzer to analyze the condition
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider(), variableTypeResolver);
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

    private static Dictionary<string, string> BuildVariableTypeMap(GDMethodDeclaration method)
    {
        var types = new Dictionary<string, string>();

        // Parameters
        if (method.Parameters != null)
        {
            foreach (var param in method.Parameters)
            {
                var name = param.Identifier?.Sequence;
                var type = param.Type?.BuildName();
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(type))
                    types[name] = type;
            }
        }

        // Local variables
        if (method.Statements != null)
        {
            foreach (var stmt in method.Statements)
            {
                if (stmt is GDVariableDeclarationStatement varDecl)
                {
                    var name = varDecl.Identifier?.Sequence;
                    if (string.IsNullOrEmpty(name)) continue;

                    // Explicit type
                    var type = varDecl.Type?.BuildName();
                    if (!string.IsNullOrEmpty(type))
                    {
                        types[name] = type;
                        continue;
                    }

                    // Infer from initializer
                    var inferred = InferInitializerType(varDecl.Initializer);
                    if (!string.IsNullOrEmpty(inferred))
                        types[name] = inferred;
                }
            }
        }

        return types;
    }

    private static string? InferInitializerType(GDExpression? initializer)
    {
        if (initializer == null) return null;

        // range() -> Range
        if (initializer is GDCallExpression call &&
            call.CallerExpression is GDIdentifierExpression ident &&
            ident.Identifier?.Sequence == "range")
            return "Range";

        // PackedInt32Array([...]) -> PackedInt32Array
        if (initializer is GDCallExpression ctorCall &&
            ctorCall.CallerExpression is GDIdentifierExpression ctorIdent)
        {
            var name = ctorIdent.Identifier?.Sequence;
            if (name != null && name.StartsWith("Packed") && name.EndsWith("Array"))
                return name;
        }

        return null;
    }

    private static IEnumerable<GDDiagnostic> ValidateCode(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        if (classDecl == null)
            return Enumerable.Empty<GDDiagnostic>();

        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = GDDefaultRuntimeProvider.Instance;
        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        var semanticModel = collector.BuildSemanticModel();

        var options = new GDSemanticValidatorOptions
        {
            CheckTypes = true,
            CheckMemberAccess = true,
            CheckArgumentTypes = true,
            CheckComparisonOperators = true
        };
        var validator = new GDSemanticValidator(semanticModel, options);
        var result = validator.Validate(classDecl);

        return result.Diagnostics;
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
