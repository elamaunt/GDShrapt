using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using GDShrapt.TypesMap;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level0;

/// <summary>
/// Level 0: Basic assignment validation tests.
/// Single file, single method, no inheritance or cross-file.
/// Tests validate that type mismatches in assignments are detected correctly.
/// </summary>
[TestClass]
public class BasicAssignmentValidationTests
{
    #region Correct Assignments - No Diagnostics Expected

    [TestMethod]
    public void Assignment_IntToInt_NoDiagnostic()
    {
        var code = @"
func test():
    var x: int = 42
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"No type errors expected. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Assignment_StringToString_NoDiagnostic()
    {
        var code = @"
func test():
    var s: String = ""hello""
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"No type errors expected. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Assignment_IntToFloat_NoDiagnostic()
    {
        // int is assignable to float (widening conversion)
        var code = @"
func test():
    var f: float = 42
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"int->float should be compatible. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Assignment_NullToReference_NoDiagnostic()
    {
        var code = @"
func test():
    var node: Node = null
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"null should be assignable to reference types. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Assignment_ArrayLiteralToArray_NoDiagnostic()
    {
        var code = @"
func test():
    var arr: Array = [1, 2, 3]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Array literal should be assignable to Array. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Assignment_DictionaryLiteralToDictionary_NoDiagnostic()
    {
        var code = @"
func test():
    var dict: Dictionary = {""a"": 1}
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Dictionary literal should be assignable to Dictionary. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Assignment_VariantAcceptsAnything_NoDiagnostic()
    {
        var code = @"
func test():
    var x = 42
    x = ""hello""
    x = [1, 2, 3]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Variant should accept any type. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Assignment_BaseTypeToSubtype_NoDiagnostic()
    {
        // GDScript allows implicit downcasts: var x: Sprite2D = some_node
        // This is common with load(), get_node(), get_collider() etc.
        var code = @"
extends Node
func test():
    var node: Node = Node.new()
    var sprite: Sprite2D = node
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);

        Assert.AreEqual(0, typeDiagnostics.Count,
            $"GDScript allows implicit downcasts (Node -> Sprite2D). Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Assignment_ObjectToSubtype_NoDiagnostic()
    {
        // GDScript allows implicit downcast from Object to any subclass
        var code = @"
extends Node
func test():
    var obj: Object = Object.new()
    var node: Node = obj
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"GDScript allows implicit downcasts (Object -> Node). Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ClassVar_BaseTypeToSubtype_NoDiagnostic()
    {
        // Class-level variable with base type initializer should allow downcast
        var code = @"
extends Node
var sprite: Sprite2D = $Sprite
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"Class var with downcast should be valid. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Assignment_UnrelatedTypes_StillReports()
    {
        // Unrelated types should still report: String cannot be a Node
        var code = @"
extends Node
func test():
    var node: Node = ""hello""
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.IsTrue(typeDiagnostics.Count > 0,
            "Unrelated types (String -> Node) should still report");
    }

    [TestMethod]
    public void Reassignment_BaseToSubtype_NoDiagnostic()
    {
        // Reassigning base type to subtype variable (implicit downcast)
        var code = @"
extends Node
func test():
    var pawn: Sprite2D = Sprite2D.new()
    var node: Node = Node.new()
    pawn = node
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"GDScript allows implicit downcasts in reassignment. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Reassignment_UnrelatedTypes_StillReports()
    {
        // Reassigning unrelated types should still report
        var code = @"
extends Node
func test():
    var node: Node = Node.new()
    node = ""hello""
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.IsTrue(typeDiagnostics.Count > 0,
            "Unrelated types (String -> Node) in reassignment should still report");
    }

    #endregion

    #region Incorrect Assignments - Diagnostics Expected

    [TestMethod]
    public void Assignment_StringToInt_ReportsMismatch()
    {
        var code = @"
func test():
    var x: int = ""hello""
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);

        // Should detect type mismatch: String cannot be assigned to int
        Assert.IsTrue(typeDiagnostics.Count > 0,
            "Expected type mismatch for String->int assignment");
        Assert.IsTrue(typeDiagnostics.Any(d =>
            d.Message.Contains("String") || d.Message.Contains("int")),
            $"Message should mention String and int. Got: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Assignment_ArrayToInt_ReportsMismatch()
    {
        var code = @"
func test():
    var x: int = [1, 2, 3]
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);

        Assert.IsTrue(typeDiagnostics.Count > 0,
            "Expected type mismatch for Array->int assignment");
    }

    [TestMethod]
    public void Assignment_FloatToInt_NoDiagnostic()
    {
        // In GDScript, float→int is a valid implicit narrowing conversion (truncation)
        // Godot silently truncates: var x: int = 3.14 → x = 3
        var code = @"
func test():
    var x: int = 3.14
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);

        Assert.AreEqual(0, typeDiagnostics.Count,
            $"GDScript allows float->int (narrowing). Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void Assignment_BoolToInt_ReportsMismatch()
    {
        var code = @"
func test():
    var x: int = true
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);

        Assert.IsTrue(typeDiagnostics.Count > 0,
            "Expected type mismatch for bool->int assignment");
    }

    [TestMethod]
    public void Reassignment_StringToTypedInt_ReportsMismatch()
    {
        var code = @"
func test():
    var x: int = 10
    x = ""wrong""
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);

        Assert.IsTrue(typeDiagnostics.Count > 0,
            "Expected type mismatch when reassigning String to typed int variable");
    }

    #endregion

    #region Class Variable Assignments

    [TestMethod]
    public void ClassVariable_CorrectType_NoDiagnostic()
    {
        var code = @"
var score: int = 100
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);
        Assert.AreEqual(0, typeDiagnostics.Count,
            $"No type errors expected for class variable. Found: {FormatDiagnostics(typeDiagnostics)}");
    }

    [TestMethod]
    public void ClassVariable_WrongType_ReportsMismatch()
    {
        var code = @"
var score: int = ""not a number""
";
        var diagnostics = ValidateCode(code);
        var typeDiagnostics = FilterTypeDiagnostics(diagnostics);

        Assert.IsTrue(typeDiagnostics.Count > 0,
            "Expected type mismatch for class variable with wrong type");
    }

    #endregion

    #region Helper Methods

    private static IEnumerable<GDDiagnostic> ValidateCode(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        if (classDecl == null)
            return Enumerable.Empty<GDDiagnostic>();

        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null, null, null);
        scriptFile.Analyze(runtimeProvider);
        var semanticModel = scriptFile.SemanticModel!;

        var options = new GDSemanticValidatorOptions
        {
            CheckTypes = true,
            CheckMemberAccess = true,
            CheckArgumentTypes = true
        };
        var validator = new GDSemanticValidator(semanticModel, options);
        var result = validator.Validate(classDecl);

        return result.Diagnostics;
    }

    private static List<GDDiagnostic> FilterTypeDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.TypeMismatch ||
            d.Code == GDDiagnosticCode.InvalidAssignment ||
            d.Code == GDDiagnosticCode.TypeAnnotationMismatch ||
            d.Code == GDDiagnosticCode.InvalidOperandType).ToList();
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
