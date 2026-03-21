using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level5_CrossFile;

/// <summary>
/// Tests for false positives when accessing members on explicitly typed class variables.
/// A variable with explicit type annotation (e.g., var x: MyType) should NOT be flagged
/// as "untyped" even when the type is a project-defined class_name.
/// </summary>
[TestClass]
public class CrossFileTypedMemberAccessTests
{
    #region GD7002 — No false positive on typed class member property chain

    [TestMethod]
    public void TypedClassMember_PropertyChain_NoGD7002()
    {
        var providerCode = @"
class_name GameboardProperties
extends Resource

var cell_size: Vector2i = Vector2i(64, 64)
var half_cell_size: Vector2 = Vector2(32, 32)
";
        var consumerCode = @"
extends Node

var properties: GameboardProperties = null

func cell_to_pixel(cell: Vector2i) -> Vector2:
    return Vector2(cell * properties.cell_size) + properties.half_cell_size
";
        var diagnostics = ValidateCrossFile(providerCode, consumerCode);
        var gd7002 = diagnostics.Where(d => d.Code == GDDiagnosticCode.UnguardedPropertyAccess).ToList();

        Assert.AreEqual(0, gd7002.Count,
            $"'properties' has explicit type GameboardProperties — should not be 'untyped'. Found: {FormatDiagnostics(gd7002)}");
    }

    #endregion

    #region GD7002 — No false positive on typed class member with setter

    [TestMethod]
    public void TypedClassMember_WithSetter_PropertyChain_NoGD7002()
    {
        var providerCode = @"
@tool
class_name GameboardProperties
extends Resource

var cell_size: Vector2i = Vector2i(64, 64)
var half_cell_size: Vector2 = Vector2(32, 32)
";
        var consumerCode = @"
extends Node

signal properties_set

var properties: GameboardProperties = null:
    set(value):
        if value != properties:
            properties = value
            properties_set.emit()

func cell_to_pixel(cell: Vector2i) -> Vector2:
    return Vector2(cell * properties.cell_size) + properties.half_cell_size
";
        var diagnostics = ValidateCrossFile(providerCode, consumerCode);
        var gd7002 = diagnostics.Where(d => d.Code == GDDiagnosticCode.UnguardedPropertyAccess).ToList();

        Assert.AreEqual(0, gd7002.Count,
            $"'properties' has explicit type GameboardProperties with setter — should not be 'untyped'. Found: {FormatDiagnostics(gd7002)}");
    }

    #endregion

    #region GD7003 — No false positive on typed class member method call

    [TestMethod]
    public void TypedClassMember_MethodCall_NoGD7003()
    {
        var providerCode = @"
class_name GameboardProperties
extends Resource

var extents: Rect2i = Rect2i()

func has_point(p: Vector2i) -> bool:
    return extents.has_point(p)
";
        var consumerCode = @"
extends Node

var properties: GameboardProperties = null

func check(cell: Vector2i) -> bool:
    return properties.has_point(cell)
";
        var diagnostics = ValidateCrossFile(providerCode, consumerCode);
        var gd7003 = diagnostics.Where(d => d.Code == GDDiagnosticCode.UnguardedMethodCall).ToList();

        Assert.AreEqual(0, gd7003.Count,
            $"'properties' has explicit type — method call should not be 'unguarded'. Found: {FormatDiagnostics(gd7003)}");
    }

    #endregion

    #region Deep chain — no false positive on chained property access

    [TestMethod]
    public void TypedClassMember_DeepPropertyChain_NoGD7002()
    {
        var providerCode = @"
class_name GameboardProperties
extends Resource

var extents: Rect2i = Rect2i()
";
        var consumerCode = @"
extends Node

var properties: GameboardProperties = null

func get_width() -> int:
    return properties.extents.size.x
";
        var diagnostics = ValidateCrossFile(providerCode, consumerCode);
        var gd7002 = diagnostics.Where(d => d.Code == GDDiagnosticCode.UnguardedPropertyAccess).ToList();

        Assert.AreEqual(0, gd7002.Count,
            $"Deep chain 'properties.extents.size.x' should not trigger GD7002. Found: {FormatDiagnostics(gd7002)}");
    }

    #endregion

    #region Diagnostic — trace the resolution chain for typed class member

    [TestMethod]
    public void TypedClassMember_DiagnosticTrace_SymbolAndExpressionType()
    {
        var providerCode = @"
@tool
class_name GameboardProperties
extends Resource

var cell_size: Vector2i = Vector2i(64, 64)
var half_cell_size: Vector2 = Vector2(32, 32)
var extents: Rect2i = Rect2i()
";
        var consumerCode = @"
extends Node

signal properties_set

var properties: GameboardProperties = null:
    set(value):
        if value != properties:
            properties = value
            properties_set.emit()

func cell_to_pixel(cell: Vector2i) -> Vector2:
    return Vector2(cell * properties.cell_size) + properties.half_cell_size

func is_in_bounds(cell: Vector2i) -> bool:
    return properties.extents.has_point(cell)

func get_cell_x(cell: Vector2i) -> int:
    return properties.extents.position.x + properties.extents.size.x
";
        var (project, consumerScript) = LoadCrossFileProject(providerCode, consumerCode);

        try
        {
            var model = consumerScript.SemanticModel;
            Assert.IsNotNull(model, "SemanticModel should not be null");

            // 1. Check FindSymbol
            var symbol = model.FindSymbol("properties");
            Assert.IsNotNull(symbol, "FindSymbol('properties') should return a symbol");
            Assert.AreEqual("GameboardProperties", symbol.TypeName,
                $"symbol.TypeName should be 'GameboardProperties', got '{symbol.TypeName}'");

            // 2. Check GetExpressionType for identifier "properties"
            var allIdents = consumerScript.Class!.AllNodes
                .OfType<GDIdentifierExpression>()
                .Where(e => e.Identifier?.Sequence == "properties")
                .ToList();
            Assert.IsTrue(allIdents.Count > 0, "Should find 'properties' identifier expressions");

            var firstIdent = allIdents[0];
            var exprType = model.GetExpressionType(firstIdent)?.DisplayName;
            Assert.AreEqual("GameboardProperties", exprType,
                $"GetExpressionType('properties' identifier) should return 'GameboardProperties', got '{exprType}'");

            // 3. Check GetExpressionType for member access "properties.cell_size"
            var memberAccesses = consumerScript.Class!.AllNodes
                .OfType<GDMemberOperatorExpression>()
                .Where(e => e.Identifier?.Sequence == "cell_size"
                    && e.CallerExpression is GDIdentifierExpression id
                    && id.Identifier?.Sequence == "properties")
                .ToList();
            Assert.IsTrue(memberAccesses.Count > 0, "Should find 'properties.cell_size' member access");

            var cellSizeAccess = memberAccesses[0];
            var cellSizeType = model.GetExpressionType(cellSizeAccess)?.DisplayName;
            Assert.IsNotNull(cellSizeType,
                $"GetExpressionType('properties.cell_size') should not be null, got '{cellSizeType}'");

            // 4. Check confidence
            var confidence = model.GetMemberAccessConfidence(cellSizeAccess);
            Assert.AreNotEqual(GDReferenceConfidence.NameMatch, confidence,
                $"Confidence for 'properties.cell_size' should NOT be NameMatch (which triggers GD7002), got {confidence}");
        }
        finally
        {
            project?.Dispose();
        }
    }

    #endregion

    #region Regression — truly untyped variable still triggers GD7002

    [TestMethod]
    public void UntypedParameter_PropertyAccess_StillGD7002()
    {
        var providerCode = @"
class_name SomeType
extends Resource
";
        var consumerCode = @"
extends Node

func test(obj):
    var x = obj.some_property
";
        var diagnostics = ValidateCrossFile(providerCode, consumerCode);
        var gd7002 = diagnostics.Where(d => d.Code == GDDiagnosticCode.UnguardedPropertyAccess).ToList();

        Assert.IsTrue(gd7002.Count > 0,
            $"Untyped parameter 'obj' (no type annotation) should still trigger GD7002. All diagnostics: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region Helpers

    private static List<GDDiagnostic> ValidateCrossFile(string providerCode, string consumerCode)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GDShrapt_Test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "provider.gd"), providerCode);
            File.WriteAllText(Path.Combine(tempDir, "consumer.gd"), consumerCode);
            File.WriteAllText(Path.Combine(tempDir, "project.godot"), "[gd_resource]\n");

            var context = new GDDefaultProjectContext(tempDir);
            var project = new GDScriptProject(context);
            project.LoadScripts();
            project.AnalyzeAll();

            var consumerScript = project.ScriptFiles.FirstOrDefault(s =>
                s.FullPath != null &&
                Path.GetFileName(s.FullPath).Equals("consumer.gd", StringComparison.OrdinalIgnoreCase));

            Assert.IsNotNull(consumerScript, "consumer.gd script not found in project");
            Assert.IsNotNull(consumerScript.Class, "consumer.gd should have a class declaration");
            Assert.IsNotNull(consumerScript.SemanticModel, "consumer.gd should have a semantic model");

            var options = new GDSemanticValidatorOptions
            {
                CheckTypes = true,
                CheckMemberAccess = true,
                CheckArgumentTypes = true
            };
            var validator = new GDSemanticValidator(consumerScript.SemanticModel, options);
            var result = validator.Validate(consumerScript.Class);
            return result.Diagnostics.ToList();
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    private static (GDScriptProject project, GDScriptFile consumerScript) LoadCrossFileProject(
        string providerCode, string consumerCode)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GDShrapt_Test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        File.WriteAllText(Path.Combine(tempDir, "provider.gd"), providerCode);
        File.WriteAllText(Path.Combine(tempDir, "consumer.gd"), consumerCode);
        File.WriteAllText(Path.Combine(tempDir, "project.godot"), "[gd_resource]\n");

        var context = new GDDefaultProjectContext(tempDir);
        var project = new GDScriptProject(context);
        project.LoadScripts();
        project.AnalyzeAll();

        var consumerScript = project.ScriptFiles.FirstOrDefault(s =>
            s.FullPath != null &&
            Path.GetFileName(s.FullPath).Equals("consumer.gd", StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(consumerScript, "consumer.gd script not found in project");
        Assert.IsNotNull(consumerScript.Class, "consumer.gd should have a class declaration");
        Assert.IsNotNull(consumerScript.SemanticModel, "consumer.gd should have a semantic model");

        return (project, consumerScript);
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
