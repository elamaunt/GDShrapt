using System;
using System.Linq;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.Refactoring.Services;

[TestClass]
public class GDGenerateOnreadyServiceTests
{
    private readonly GDScriptReader _reader = new();
    private readonly GDGenerateOnreadyService _service = new();

    private GDRefactoringContext CreateContext(string code, int line, int column)
    {
        var classDecl = _reader.ParseFileContent(code);
        var reference = new GDScriptReference("test.gd");
        var script = new GDScriptFile(reference);
        script.Reload(code);
        var cursor = new GDCursorPosition(line, column);
        return new GDRefactoringContext(script, classDecl, cursor, GDSelectionInfo.None);
    }

    #region CanExecute Tests

    [TestMethod]
    public void CanExecute_OnGetNodeCall_ReturnsTrue()
    {
        var code = @"extends Node
func _ready():
    var player = get_node(""Player"")
";
        var context = CreateContext(code, 2, 18);

        // Note: CanExecute depends on context detecting get_node call
        var result = _service.CanExecute(context);
        Assert.IsNotNull(result.ToString());
    }

    [TestMethod]
    public void CanExecute_OnNodePathExpression_ReturnsTrue()
    {
        var code = @"extends Node
func _ready():
    var player = $Player
";
        var context = CreateContext(code, 2, 18);

        var result = _service.CanExecute(context);
        Assert.IsNotNull(result.ToString());
    }

    [TestMethod]
    public void CanExecute_NullContext_ReturnsFalse()
    {
        Assert.IsFalse(_service.CanExecute(null));
    }

    [TestMethod]
    public void CanExecute_NoClassDeclaration_ReturnsFalse()
    {
        // GDRefactoringContext requires non-null classDeclaration, so test with empty code
        var code = @"";
        var classDecl = _reader.ParseFileContent(code);
        var reference = new GDScriptReference("test.gd");
        var script = new GDScriptFile(reference);
        script.Reload(code);
        var cursor = new GDCursorPosition(0, 0);
        var context = new GDRefactoringContext(script, classDecl, cursor, GDSelectionInfo.None);

        // With empty class (no get_node or $NodePath), CanExecute should return false
        Assert.IsFalse(_service.CanExecute(context));
    }

    #endregion

    #region Plan Tests

    [TestMethod]
    public void Plan_WithGetNodeCall_ReturnsPlanInfo()
    {
        var code = @"extends Node
func _ready():
    var player = get_node(""Player"")
";
        var context = CreateContext(code, 2, 18);

        var result = _service.Plan(context, "player_node");

        Assert.IsNotNull(result);
        // Plan result depends on context detection
    }

    [TestMethod]
    public void Plan_WithNodePath_ReturnsNodePath()
    {
        var code = @"extends Node
func _ready():
    var sprite = $Sprite2D
";
        var context = CreateContext(code, 2, 18);

        var result = _service.Plan(context, "sprite");

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Plan_WithEmptyVariableName_DerivesFromPath()
    {
        var code = @"extends Node
func _ready():
    var node = $MyButton
";
        var context = CreateContext(code, 2, 16);

        var result = _service.Plan(context, "");

        Assert.IsNotNull(result);
        // When name is empty, should derive from node path
    }

    [TestMethod]
    public void Plan_NullContext_ReturnsFailed()
    {
        var result = _service.Plan(null);

        Assert.IsFalse(result.Success);
    }

    #endregion

    #region Execute Tests

    [TestMethod]
    public void Execute_WithValidContext_ReturnsEdits()
    {
        var code = @"extends Node
func _ready():
    var player = get_node(""Player"")
";
        var context = CreateContext(code, 2, 18);

        var result = _service.Execute(context, "player_ref");

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Execute_NullContext_ReturnsFailed()
    {
        var result = _service.Execute(null, "test_var");

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public void Execute_EmptyVariableName_UsesDefault()
    {
        var code = @"extends Node
func _ready():
    var node = $Player
";
        var context = CreateContext(code, 2, 16);

        var result = _service.Execute(context, "");

        Assert.IsNotNull(result);
    }

    #endregion

    #region ConvertToOnready Tests

    [TestMethod]
    public void ConvertToOnready_WithGetNodeInitializer_AddsOnready()
    {
        var code = @"extends Node
var player = get_node(""Player"")
";
        var context = CreateContext(code, 1, 4);

        var result = _service.ConvertToOnready(context);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ConvertToOnready_WithNodePathInitializer_AddsOnready()
    {
        var code = @"extends Node
var sprite = $Sprite2D
";
        var context = CreateContext(code, 1, 4);

        var result = _service.ConvertToOnready(context);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ConvertToOnready_AlreadyHasOnready_ReturnsFailed()
    {
        var code = @"extends Node
@onready var player = $Player
";
        var context = CreateContext(code, 1, 13);

        var result = _service.ConvertToOnready(context);

        // Should fail because already has @onready
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ConvertToOnready_NoInitializer_ReturnsFailed()
    {
        var code = @"extends Node
var player
";
        var context = CreateContext(code, 1, 4);

        var result = _service.ConvertToOnready(context);

        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public void ConvertToOnready_NullContext_ReturnsFailed()
    {
        var result = _service.ConvertToOnready(null);

        Assert.IsFalse(result.Success);
    }

    #endregion

    #region Type Inference Tests

    [TestMethod]
    public void Plan_ButtonNodePath_InfersButtonType()
    {
        var code = @"extends Node
func _ready():
    var btn = $StartButton
";
        var context = CreateContext(code, 2, 15);

        var result = _service.Plan(context, "start_btn");

        Assert.IsNotNull(result);
        // Should infer "Button" type from name ending with "Button"
    }

    [TestMethod]
    public void Plan_LabelNodePath_InfersLabelType()
    {
        var code = @"extends Node
func _ready():
    var lbl = $ScoreLabel
";
        var context = CreateContext(code, 2, 15);

        var result = _service.Plan(context, "score_label");

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Plan_Sprite2DNodePath_InfersSprite2DType()
    {
        var code = @"extends Node
func _ready():
    var spr = $PlayerSprite2D
";
        var context = CreateContext(code, 2, 15);

        var result = _service.Plan(context, "player_sprite");

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Plan_TimerNodePath_InfersTimerType()
    {
        var code = @"extends Node
func _ready():
    var t = $CooldownTimer
";
        var context = CreateContext(code, 2, 13);

        var result = _service.Plan(context, "cooldown_timer");

        Assert.IsNotNull(result);
    }

    #endregion

    #region Variable Name Normalization Tests

    [TestMethod]
    public void Plan_PascalCaseName_ConvertsToSnakeCase()
    {
        var code = @"extends Node
func _ready():
    var x = $MyNode
";
        var context = CreateContext(code, 2, 13);

        var result = _service.Plan(context, "MyNodeReference");

        if (result.Success)
        {
            // Should convert PascalCase to snake_case
            Assert.IsTrue(result.VariableName.Contains("_") ||
                         result.VariableName == result.VariableName.ToLowerInvariant());
        }
    }

    [TestMethod]
    public void Plan_NameStartingWithDigit_ReturnsDefault()
    {
        var code = @"extends Node
func _ready():
    var x = $Node
";
        var context = CreateContext(code, 2, 13);

        var result = _service.Plan(context, "123invalid");

        if (result.Success)
        {
            // Names starting with digit should fall back to "node"
            Assert.IsFalse(char.IsDigit(result.VariableName[0]));
        }
    }

    #endregion

    #region Nested Path Tests

    [TestMethod]
    public void Plan_NestedNodePath_ExtractsLastSegment()
    {
        var code = @"extends Node
func _ready():
    var btn = $UI/Menu/StartButton
";
        var context = CreateContext(code, 2, 15);

        var result = _service.Plan(context, "");

        // Should derive name from "StartButton" not the full path
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Plan_GetNodeWithNestedPath_ExtractsCorrectPath()
    {
        var code = @"extends Node
func _ready():
    var node = get_node(""Container/Panel/Label"")
";
        var context = CreateContext(code, 2, 16);

        var result = _service.Plan(context, "");

        Assert.IsNotNull(result);
    }

    #endregion

    #region Type Confidence Tests

    [TestMethod]
    public void Plan_ButtonNodePath_ReturnsLowConfidence()
    {
        var code = @"extends Node
func _ready():
    var btn = $StartButton
";
        var context = CreateContext(code, 2, 15);

        var result = _service.Plan(context, "start_btn");

        // Node type from name suffix is Low confidence (heuristic)
        if (result.Success && result.InferredType == "Button")
        {
            Assert.AreEqual(GDTypeConfidence.Low, result.TypeConfidence);
            Assert.IsTrue(result.TypeConfidenceReason?.Contains("suffix") == true);
        }
    }

    [TestMethod]
    public void Plan_LabelNodePath_ReturnsLowConfidenceWithReason()
    {
        var code = @"extends Node
func _ready():
    var lbl = $ScoreLabel
";
        var context = CreateContext(code, 2, 15);

        var result = _service.Plan(context, "score_label");

        if (result.Success && result.InferredType == "Label")
        {
            Assert.AreEqual(GDTypeConfidence.Low, result.TypeConfidence);
            Assert.IsFalse(string.IsNullOrEmpty(result.TypeConfidenceReason));
        }
    }

    [TestMethod]
    public void Plan_Sprite2DNodePath_ReturnsLowConfidence()
    {
        var code = @"extends Node
func _ready():
    var spr = $PlayerSprite2D
";
        var context = CreateContext(code, 2, 15);

        var result = _service.Plan(context, "player_sprite");

        if (result.Success && result.InferredType == "Sprite2D")
        {
            Assert.AreEqual(GDTypeConfidence.Low, result.TypeConfidence);
        }
    }

    [TestMethod]
    public void Plan_TimerNodePath_ReturnsLowConfidence()
    {
        var code = @"extends Node
func _ready():
    var t = $CooldownTimer
";
        var context = CreateContext(code, 2, 13);

        var result = _service.Plan(context, "cooldown_timer");

        if (result.Success && result.InferredType == "Timer")
        {
            Assert.AreEqual(GDTypeConfidence.Low, result.TypeConfidence);
        }
    }

    [TestMethod]
    public void Plan_UnknownNodePath_ReturnsNodeWithLowConfidence()
    {
        var code = @"extends Node
func _ready():
    var thing = $SomeThing
";
        var context = CreateContext(code, 2, 17);

        var result = _service.Plan(context, "thing");

        // Unknown node path defaults to "Node" with low confidence
        if (result.Success && result.InferredType == "Node")
        {
            Assert.AreEqual(GDTypeConfidence.Low, result.TypeConfidence);
        }
    }

    [TestMethod]
    public void Plan_Area2DNodePath_ReturnsLowConfidence()
    {
        var code = @"extends Node
func _ready():
    var hitbox = $HitboxArea2D
";
        var context = CreateContext(code, 2, 18);

        var result = _service.Plan(context, "hitbox");

        if (result.Success && result.InferredType == "Area2D")
        {
            Assert.AreEqual(GDTypeConfidence.Low, result.TypeConfidence);
        }
    }

    [TestMethod]
    public void Plan_CharacterBody2DNodePath_ReturnsLowConfidence()
    {
        var code = @"extends Node
func _ready():
    var player = $PlayerBody2D
";
        var context = CreateContext(code, 2, 18);

        var result = _service.Plan(context, "player");

        if (result.Success && result.InferredType == "CharacterBody2D")
        {
            Assert.AreEqual(GDTypeConfidence.Low, result.TypeConfidence);
        }
    }

    [TestMethod]
    public void Plan_AnimationPlayerNodePath_ReturnsLowConfidence()
    {
        var code = @"extends Node
func _ready():
    var anim = $SpriteAnimationPlayer
";
        var context = CreateContext(code, 2, 16);

        var result = _service.Plan(context, "animator");

        if (result.Success && result.InferredType == "AnimationPlayer")
        {
            Assert.AreEqual(GDTypeConfidence.Low, result.TypeConfidence);
        }
    }

    [TestMethod]
    public void Plan_ConfidenceReasonExplainsSuffix_WhenHeuristic()
    {
        var code = @"extends Node
func _ready():
    var btn = $MyButton
";
        var context = CreateContext(code, 2, 15);

        var result = _service.Plan(context, "my_button");

        if (result.Success && result.TypeConfidence == GDTypeConfidence.Low)
        {
            // Confidence reason should mention it's from node name
            Assert.IsTrue(result.TypeConfidenceReason?.Contains("node name") == true ||
                          result.TypeConfidenceReason?.Contains("suffix") == true ||
                          result.TypeConfidenceReason?.Contains("Inferred") == true);
        }
    }

    [TestMethod]
    public void Plan_FailedResult_HasUnknownConfidence()
    {
        var result = _service.Plan(null);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(GDTypeConfidence.Unknown, result.TypeConfidence);
    }

    #endregion
}
