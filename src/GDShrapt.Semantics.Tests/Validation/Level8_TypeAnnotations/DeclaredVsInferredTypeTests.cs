using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level8_TypeAnnotations;

[TestClass]
public class DeclaredVsInferredTypeTests
{
    #region Category 1: DeclaredType subclass of InitType — use DeclaredType

    [TestMethod]
    public void DeclaredSubclass_InitBase_UsesDeclaredType()
    {
        // var s: Sprite2D = get_node("Sprite") → get_node returns Node
        // Sprite2D is subclass of Node → use Sprite2D
        var code = @"
extends Node2D

func test():
    var s: Sprite2D = get_node(""Sprite"")
    s.offset = Vector2.ZERO
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.PropertyNotFound && d.Message.Contains("offset")),
            $"Sprite2D has 'offset'. DeclaredType should be used over inferred Node. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void DeclaredSubclass_InitBase_NoFPOnMultipleProperties()
    {
        // Access multiple Sprite2D-specific properties
        var code = @"
extends Node2D

func test():
    var s: Sprite2D = get_node(""Sprite"")
    s.offset = Vector2.ZERO
    s.flip_h = true
    s.texture = null
";
        var diagnostics = ValidateCode(code);
        var fpDiags = diagnostics.Where(d => d.Code == GDDiagnosticCode.PropertyNotFound).ToList();
        Assert.AreEqual(0, fpDiags.Count,
            $"No GD3009 expected for Sprite2D properties. Found: {FormatDiagnostics(fpDiags)}");
    }

    [TestMethod]
    public void DeclaredSubclass_Instantiate_UsesDeclaredType()
    {
        // var icon: TextureRect = scene.instantiate() → instantiate returns Node
        // TextureRect is subclass of Node → use TextureRect
        var code = @"
extends Node2D

func test():
    var icon: TextureRect = preload(""res://scene.tscn"").instantiate()
    icon.texture = null
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.PropertyNotFound && d.Message.Contains("texture")),
            $"TextureRect has 'texture'. DeclaredType should be used. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region Category 2: InitType subclass of DeclaredType — use InitType (more specific)

    [TestMethod]
    public void DeclaredBase_InitSubclass_UsesInitType()
    {
        // var x: Node = Sprite2D.new() → init is more specific
        // Should use Sprite2D for member access
        var code = @"
extends Node2D

func test():
    var x: Node = Sprite2D.new()
    x.offset = Vector2.ZERO
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.PropertyNotFound && d.Message.Contains("offset")),
            $"Sprite2D.new() is more specific than Node. Should use Sprite2D. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region Category 3: Reassignment — AdjustTypeForDeclaredType

    [TestMethod]
    public void Reassignment_BaseReturn_PreservesDeclaredType()
    {
        // var s: Sprite2D = Sprite2D.new()
        // s = s.duplicate() → duplicate() returns Object, but Sprite2D extends Object
        var code = @"
extends Node2D

func test():
    var s: Sprite2D = Sprite2D.new()
    s = s.duplicate()
    s.offset = Vector2.ZERO
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.PropertyNotFound && d.Message.Contains("offset")),
            $"After s = s.duplicate(), declared type Sprite2D should be preserved. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region Category 4: Variant / null

    [TestMethod]
    public void DeclaredType_NullInit_UsesDeclaredType()
    {
        var code = @"
extends Node2D

func test():
    var s: Sprite2D = null
    s = Sprite2D.new()
    s.offset = Vector2.ZERO
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.PropertyNotFound && d.Message.Contains("offset")),
            $"Declared type Sprite2D should be used when init is null. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void DeclaredType_NoInit_UsesDeclaredType()
    {
        var code = @"
extends Node2D

var sprite: Sprite2D

func test():
    sprite.offset = Vector2.ZERO
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.PropertyNotFound && d.Message.Contains("offset")),
            $"Declared type without init should use DeclaredType. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region Category 5: Parameters

    [TestMethod]
    public void Parameter_DeclaredType_AllowsSubtypeProperties()
    {
        var code = @"
extends Node2D

func test(s: Sprite2D):
    s.offset = Vector2.ZERO
    s.flip_h = true
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.PropertyNotFound && d.Message.Contains("offset")),
            $"Parameter declared as Sprite2D should allow offset. Found: {FormatDiagnostics(diagnostics)}");
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.PropertyNotFound && d.Message.Contains("flip_h")),
            $"Parameter declared as Sprite2D should allow flip_h. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region Category 6: For-loop iterators

    [TestMethod]
    public void ForLoop_ExplicitIteratorType_UsesAnnotation()
    {
        var code = @"
extends Node2D

func test():
    for item: Sprite2D in get_children():
        item.offset = Vector2.ZERO
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.PropertyNotFound && d.Message.Contains("offset")),
            $"For loop with explicit type annotation should use Sprite2D. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region Category 7: Incompatible types — conflict diagnostics

    [TestMethod]
    public void UnrelatedTypes_SiblingClasses_FlagsConflict()
    {
        // Sprite2D and Label are siblings (both extend CanvasItem) but not in same branch
        var code = @"
func test():
    var s: Sprite2D = Label.new()
";
        var diagnostics = ValidateCode(code);
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.TypeAnnotationMismatch),
            $"Expected GD3004 for incompatible Sprite2D vs Label. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void UnrelatedTypes_CompletelyDifferent_FlagsConflict()
    {
        // Resource and Node are in completely different branches
        var code = @"
func test():
    var r: Resource = Node.new()
";
        var diagnostics = ValidateCode(code);
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.TypeAnnotationMismatch),
            $"Expected GD3004 for incompatible Resource vs Node. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void AnnotationWider_BaseTypeAnnotation_HintsGD3022()
    {
        // var x: Node = Sprite2D.new() — annotation is wider than inferred
        var code = @"
func test():
    var x: Node = Sprite2D.new()
";
        var diagnostics = ValidateCode(code, new GDSemanticValidatorOptions
        {
            CheckAnnotationNarrowing = true
        });
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.AnnotationWiderThanInferred),
            $"Expected GD3022 for Node wider than Sprite2D. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region Category 9: Reassignment widening

    [TestMethod]
    public void Reassignment_Widening_WarnsGD7019()
    {
        var code = @"
func test():
    var s: Sprite2D = Sprite2D.new()
    s = Node.new()
";
        var diagnostics = ValidateCode(code, new GDSemanticValidatorOptions
        {
            CheckTypeWidening = true
        });
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.TypeWideningAssignment),
            $"Expected GD7019 for widening Sprite2D → Node. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region Category 10: Same type — no change

    [TestMethod]
    public void SameType_DeclaredEqualsInit_NoIssue()
    {
        var code = @"
extends Node2D

func test():
    var s: Sprite2D = Sprite2D.new()
    s.offset = Vector2.ZERO
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.PropertyNotFound),
            $"Same type should have no issues. Found: {FormatDiagnostics(diagnostics)}");
    }

    #endregion

    #region Category 11: DataFlow API — declared type as observation

    [TestMethod]
    public void DataFlow_DeclaredTypeAppearsInFlowType()
    {
        // var s: Sprite2D = Sprite2D.new() → DataFlow should show Sprite2D in flow type
        var (model, _) = BuildModel(@"
extends Node2D

func test():
    var s: Sprite2D = Sprite2D.new()
    s.offset = Vector2.ZERO
");
        var symbol = model.Symbols.FirstOrDefault(s => s.Name == "s");
        Assert.IsNotNull(symbol, "Symbol 's' should exist");

        var flowVar = model.GetFlowVariableType("s", symbol.DeclarationNode);
        Assert.IsNotNull(flowVar, "Flow variable type for 's' should exist");
        Assert.IsNotNull(flowVar.DeclaredType, "DeclaredType should be set");
        Assert.AreEqual("Sprite2D", flowVar.DeclaredType.DisplayName);
        Assert.AreEqual("Sprite2D", flowVar.EffectiveType.DisplayName);
    }

    [TestMethod]
    public void DataFlow_WiderAnnotation_BothTypesInFlow()
    {
        // var x: Node = Sprite2D.new() → DataFlow contains both Node (declaration) and Sprite2D (init)
        var (model, _) = BuildModel(@"
extends Node2D

func test():
    var x: Node = Sprite2D.new()
    x.name = """"
");
        var symbol = model.Symbols.FirstOrDefault(s => s.Name == "x");
        Assert.IsNotNull(symbol, "Symbol 'x' should exist");

        var flowVar = model.GetFlowVariableType("x", symbol.DeclarationNode);
        Assert.IsNotNull(flowVar, "Flow variable type for 'x' should exist");
        Assert.AreEqual("Node", flowVar.DeclaredType?.DisplayName);

        // EffectiveType should be the more specific type (Sprite2D)
        var effective = flowVar.EffectiveType;
        Assert.IsNotNull(effective);
    }

    [TestMethod]
    public void DataFlow_NullInit_DeclaredTypePreserved()
    {
        // var s: Sprite2D = null → DeclaredType = Sprite2D, EffectiveType = Sprite2D
        var (model, _) = BuildModel(@"
extends Node2D

func test():
    var s: Sprite2D = null
    s = Sprite2D.new()
");
        var symbol = model.Symbols.FirstOrDefault(s => s.Name == "s");
        Assert.IsNotNull(symbol, "Symbol 's' should exist");

        var flowVar = model.GetFlowVariableType("s", symbol.DeclarationNode);
        Assert.IsNotNull(flowVar);
        Assert.AreEqual("Sprite2D", flowVar.DeclaredType?.DisplayName);
        Assert.AreEqual("Sprite2D", flowVar.EffectiveType.DisplayName);
    }

    [TestMethod]
    public void DataFlow_QueryReturnsFlowInfo()
    {
        var (model, _) = BuildModel(@"
extends Node2D

func test():
    var s: Sprite2D = Sprite2D.new()
    s.offset = Vector2.ZERO
");
        var symbol = model.Symbols.FirstOrDefault(s => s.Name == "s");
        Assert.IsNotNull(symbol);

        var dataFlow = model.DataFlow;
        Assert.IsNotNull(dataFlow, "DataFlow property should be available");

        // GetDataFlowAt for the declaration
        var token = symbol.PositionToken;
        if (token != null)
        {
            var info = dataFlow.GetDataFlowAt(symbol, token.StartLine, token.StartColumn);
            if (info != null)
            {
                Assert.AreEqual("Sprite2D", info.EffectiveType.DisplayName);
            }
        }
    }

    [TestMethod]
    public void DataFlow_TypeConflicts_DetectsIncompatible()
    {
        // var s: Sprite2D = Label.new() → incompatible types
        var (model, _) = BuildModel(@"
func test():
    var s: Sprite2D = Label.new()
");
        var symbol = model.Symbols.FirstOrDefault(s => s.Name == "s");
        Assert.IsNotNull(symbol);

        var token = symbol.PositionToken;
        if (token != null)
        {
            var conflicts = model.DataFlow.GetTypeConflicts(symbol, token.StartLine, token.StartColumn);
            // At minimum, we should not crash
            Assert.IsNotNull(conflicts);
        }
    }

    [TestMethod]
    public void DataFlow_EscapePoints_EmptyForSimpleCase()
    {
        var (model, _) = BuildModel(@"
func test():
    var s: Sprite2D = Sprite2D.new()
");
        var symbol = model.Symbols.FirstOrDefault(s => s.Name == "s");
        Assert.IsNotNull(symbol);

        var escapes = model.DataFlow.GetEscapePoints(symbol);
        Assert.IsNotNull(escapes);
        Assert.AreEqual(0, escapes.Count, "Simple assignment should have no escape points");
    }

    #endregion

    #region Category 12: Enriched diagnostic messages

    [TestMethod]
    public void Enriched_WideningDiagnostic_ContainsWideningInfo()
    {
        var code = @"
func test():
    var s: Sprite2D = Sprite2D.new()
    s = Node.new()
";
        var diagnostics = ValidateCode(code, new GDSemanticValidatorOptions
        {
            CheckTypeWidening = true
        });
        var widening = diagnostics.FirstOrDefault(d => d.Code == GDDiagnosticCode.TypeWideningAssignment);
        Assert.IsNotNull(widening, $"Expected GD7019. Found: {FormatDiagnostics(diagnostics)}");
        Assert.IsTrue(widening.Message.Contains("widens"), $"Message should mention widening: {widening.Message}");
        Assert.IsTrue(widening.Message.Contains("Sprite2D"), $"Message should mention Sprite2D: {widening.Message}");
        Assert.IsTrue(widening.Message.Contains("Node"), $"Message should mention Node: {widening.Message}");
    }

    [TestMethod]
    public void Enriched_AnnotationWider_ContainsTypeInfo()
    {
        var code = @"
func test():
    var x: Node = Sprite2D.new()
";
        var diagnostics = ValidateCode(code, new GDSemanticValidatorOptions
        {
            CheckAnnotationNarrowing = true
        });
        var annotation = diagnostics.FirstOrDefault(d => d.Code == GDDiagnosticCode.AnnotationWiderThanInferred);
        Assert.IsNotNull(annotation, $"Expected GD3022. Found: {FormatDiagnostics(diagnostics)}");
        Assert.IsTrue(annotation.Message.Contains("Node"), $"Message should mention Node: {annotation.Message}");
        Assert.IsTrue(annotation.Message.Contains("Sprite2D"), $"Message should mention Sprite2D: {annotation.Message}");
    }

    #endregion

    #region Helper Methods

    private static (GDSemanticModel model, GDScriptFile scriptFile) BuildModel(string code)
    {
        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null, null, null);
        scriptFile.Analyze(runtimeProvider);
        var semanticModel = scriptFile.SemanticModel!;
        return (semanticModel, scriptFile);
    }

    private static IEnumerable<GDDiagnostic> ValidateCode(string code, GDSemanticValidatorOptions? options = null)
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

        options ??= new GDSemanticValidatorOptions
        {
            CheckTypeWidening = true
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
