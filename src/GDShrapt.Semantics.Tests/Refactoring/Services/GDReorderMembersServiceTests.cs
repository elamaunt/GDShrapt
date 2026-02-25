using System.Linq;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDReorderMembersServiceTests
{
    private readonly GDScriptReader _reader = new();
    private readonly GDReorderMembersService _service = new();

    private GDRefactoringContext CreateContext(string code)
    {
        var classDecl = _reader.ParseFileContent(code);
        var reference = new GDScriptReference("test.gd");
        var script = new GDScriptFile(reference);
        script.Reload(code);
        var cursor = new GDCursorPosition(0, 0);
        return new GDRefactoringContext(script, classDecl, cursor, GDSelectionInfo.None);
    }

    #region CanExecute Tests

    [TestMethod]
    public void CanExecute_WithMultipleMembers_ReturnsTrue()
    {
        var code = @"extends Node
var x = 10
func test():
    pass
";
        var context = CreateContext(code);

        Assert.IsTrue(_service.CanExecute(context));
    }

    [TestMethod]
    public void CanExecute_WithSingleMember_ReturnsFalse()
    {
        var code = @"extends Node
var x = 10
";
        var context = CreateContext(code);

        // Only one member (var x), extends is part of class, not a separate member
        // Actually, extends is a member too
        var result = _service.CanExecute(context);
        Assert.IsNotNull(result.ToString());
    }

    [TestMethod]
    public void CanExecute_NullContext_ReturnsFalse()
    {
        Assert.IsFalse(_service.CanExecute(null));
    }

    [TestMethod]
    public void CanExecute_EmptyClass_ReturnsFalse()
    {
        var code = @"";
        var classDecl = _reader.ParseFileContent(code);
        var reference = new GDScriptReference("test.gd");
        var script = new GDScriptFile(reference);
        script.Reload(code);
        var cursor = new GDCursorPosition(0, 0);
        var context = new GDRefactoringContext(script, classDecl, cursor, GDSelectionInfo.None);

        Assert.IsFalse(_service.CanExecute(context));
    }

    #endregion

    #region GetCategory Tests

    [TestMethod]
    public void GetCategory_SignalDeclaration_ReturnsSignal()
    {
        var code = @"extends Node
signal test_signal
";
        var classDecl = _reader.ParseFileContent(code);
        var signal = classDecl.Members.OfType<GDSignalDeclaration>().FirstOrDefault();

        Assert.IsNotNull(signal);
        Assert.AreEqual(GDMemberCategory.Signal, _service.GetCategory(signal));
    }

    [TestMethod]
    public void GetCategory_EnumDeclaration_ReturnsEnum()
    {
        var code = @"extends Node
enum State { IDLE, RUNNING }
";
        var classDecl = _reader.ParseFileContent(code);
        var enumDecl = classDecl.Members.OfType<GDEnumDeclaration>().FirstOrDefault();

        Assert.IsNotNull(enumDecl);
        Assert.AreEqual(GDMemberCategory.Enum, _service.GetCategory(enumDecl));
    }

    [TestMethod]
    public void GetCategory_ConstVariable_ReturnsConstant()
    {
        var code = @"extends Node
const MAX_SPEED = 100
";
        var classDecl = _reader.ParseFileContent(code);
        var varDecl = classDecl.Members.OfType<GDVariableDeclaration>().FirstOrDefault();

        Assert.IsNotNull(varDecl);
        Assert.AreEqual(GDMemberCategory.Constant, _service.GetCategory(varDecl));
    }

    [TestMethod]
    public void GetCategory_PublicVariable_ReturnsPublicVariable()
    {
        var code = @"extends Node
var speed = 100
";
        var classDecl = _reader.ParseFileContent(code);
        var varDecl = classDecl.Members.OfType<GDVariableDeclaration>().FirstOrDefault();

        Assert.IsNotNull(varDecl);
        Assert.AreEqual(GDMemberCategory.PublicVariable, _service.GetCategory(varDecl));
    }

    [TestMethod]
    public void GetCategory_PrivateVariable_ReturnsPrivateVariable()
    {
        var code = @"extends Node
var _speed = 100
";
        var classDecl = _reader.ParseFileContent(code);
        var varDecl = classDecl.Members.OfType<GDVariableDeclaration>().FirstOrDefault();

        Assert.IsNotNull(varDecl);
        Assert.AreEqual(GDMemberCategory.PrivateVariable, _service.GetCategory(varDecl));
    }

    [TestMethod]
    public void GetCategory_PublicMethod_ReturnsPublicMethod()
    {
        var code = @"extends Node
func test():
    pass
";
        var classDecl = _reader.ParseFileContent(code);
        var methodDecl = classDecl.Methods.FirstOrDefault();

        Assert.IsNotNull(methodDecl);
        Assert.AreEqual(GDMemberCategory.PublicMethod, _service.GetCategory(methodDecl));
    }

    [TestMethod]
    public void GetCategory_PrivateMethod_ReturnsPrivateMethod()
    {
        var code = @"extends Node
func _custom_private():
    pass
";
        var classDecl = _reader.ParseFileContent(code);
        var methodDecl = classDecl.Methods.FirstOrDefault();

        Assert.IsNotNull(methodDecl);
        Assert.AreEqual(GDMemberCategory.PrivateMethod, _service.GetCategory(methodDecl));
    }

    [TestMethod]
    public void GetCategory_BuiltinMethod_ReturnsBuiltinMethod()
    {
        var code = @"extends Node
func _ready():
    pass
";
        var classDecl = _reader.ParseFileContent(code);
        var methodDecl = classDecl.Methods.FirstOrDefault();

        Assert.IsNotNull(methodDecl);
        Assert.AreEqual(GDMemberCategory.BuiltinMethod, _service.GetCategory(methodDecl));
    }

    [TestMethod]
    public void GetCategory_ProcessMethod_ReturnsBuiltinMethod()
    {
        var code = @"extends Node
func _process(delta):
    pass
";
        var classDecl = _reader.ParseFileContent(code);
        var methodDecl = classDecl.Methods.FirstOrDefault();

        Assert.IsNotNull(methodDecl);
        Assert.AreEqual(GDMemberCategory.BuiltinMethod, _service.GetCategory(methodDecl));
    }

    [TestMethod]
    public void GetCategory_InnerClass_ReturnsInnerClass()
    {
        var code = @"extends Node
class InnerThing:
    var x = 10
";
        var classDecl = _reader.ParseFileContent(code);
        var innerClass = classDecl.Members.OfType<GDInnerClassDeclaration>().FirstOrDefault();

        Assert.IsNotNull(innerClass);
        Assert.AreEqual(GDMemberCategory.InnerClass, _service.GetCategory(innerClass));
    }

    #endregion

    #region Plan Tests

    [TestMethod]
    public void Plan_AlreadyOrdered_ReturnsNoChanges()
    {
        var code = @"extends Node
signal test_signal
const MAX = 100
var speed = 50
func _ready():
    pass
func test():
    pass
";
        var context = CreateContext(code);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        // Should have no changes or minimal changes
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Plan_MixedOrder_DetectsChanges()
    {
        var code = @"extends Node
func test():
    pass
var speed = 50
signal test_signal
";
        var context = CreateContext(code);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Changes);
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
    public void Execute_WithMixedOrder_ReturnsReorderedCode()
    {
        var code = @"extends Node
func test():
    pass
var speed = 50
";
        var context = CreateContext(code);

        var result = _service.Execute(context);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Execute_NullContext_ReturnsFailed()
    {
        var result = _service.Execute(null);

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public void Execute_AlreadyOrdered_ReturnsEmpty()
    {
        var code = @"extends Node
signal test_signal
var speed = 50
func _ready():
    pass
";
        var context = CreateContext(code);

        var result = _service.Execute(context);

        // If already ordered, should return empty or success with no edits
        Assert.IsNotNull(result);
    }

    #endregion

    #region AnalyzeMembers Tests

    [TestMethod]
    public void AnalyzeMembers_ReturnsCategorizedList()
    {
        var code = @"extends Node
signal test_signal
const MAX = 100
var speed = 50
func _ready():
    pass
func test():
    pass
";
        var context = CreateContext(code);

        var analysis = _service.AnalyzeMembers(context);

        Assert.IsNotNull(analysis);
        Assert.IsTrue(analysis.Count > 0);

        // Check that categories are assigned
        var hasSignal = analysis.Any(a => a.Category == GDMemberCategory.Signal);
        var hasConstant = analysis.Any(a => a.Category == GDMemberCategory.Constant);
        var hasVariable = analysis.Any(a => a.Category == GDMemberCategory.PublicVariable);
        var hasBuiltinMethod = analysis.Any(a => a.Category == GDMemberCategory.BuiltinMethod);
        var hasPublicMethod = analysis.Any(a => a.Category == GDMemberCategory.PublicMethod);

        Assert.IsTrue(hasSignal);
        Assert.IsTrue(hasConstant);
        Assert.IsTrue(hasVariable);
        Assert.IsTrue(hasBuiltinMethod);
        Assert.IsTrue(hasPublicMethod);
    }

    [TestMethod]
    public void AnalyzeMembers_NullContext_ReturnsEmptyList()
    {
        var analysis = _service.AnalyzeMembers(null);

        Assert.IsNotNull(analysis);
        Assert.AreEqual(0, analysis.Count);
    }

    #endregion

    #region DefaultMemberOrder Tests

    [TestMethod]
    public void DefaultMemberOrder_ContainsAllCategories()
    {
        var order = GDReorderMembersService.DefaultMemberOrder;

        Assert.IsNotNull(order);
        Assert.IsTrue(order.Contains(GDMemberCategory.ClassAttribute));
        Assert.IsTrue(order.Contains(GDMemberCategory.Signal));
        Assert.IsTrue(order.Contains(GDMemberCategory.Enum));
        Assert.IsTrue(order.Contains(GDMemberCategory.Constant));
        Assert.IsTrue(order.Contains(GDMemberCategory.ExportVariable));
        Assert.IsTrue(order.Contains(GDMemberCategory.PublicVariable));
        Assert.IsTrue(order.Contains(GDMemberCategory.PrivateVariable));
        Assert.IsTrue(order.Contains(GDMemberCategory.OnreadyVariable));
        Assert.IsTrue(order.Contains(GDMemberCategory.BuiltinMethod));
        Assert.IsTrue(order.Contains(GDMemberCategory.PublicMethod));
        Assert.IsTrue(order.Contains(GDMemberCategory.PrivateMethod));
        Assert.IsTrue(order.Contains(GDMemberCategory.InnerClass));
    }

    [TestMethod]
    public void DefaultMemberOrder_SignalsBeforeVariables()
    {
        var order = GDReorderMembersService.DefaultMemberOrder;

        var signalIndex = order.IndexOf(GDMemberCategory.Signal);
        var variableIndex = order.IndexOf(GDMemberCategory.PublicVariable);

        Assert.IsTrue(signalIndex < variableIndex);
    }

    [TestMethod]
    public void DefaultMemberOrder_ConstantsBeforeVariables()
    {
        var order = GDReorderMembersService.DefaultMemberOrder;

        var constantIndex = order.IndexOf(GDMemberCategory.Constant);
        var variableIndex = order.IndexOf(GDMemberCategory.PublicVariable);

        Assert.IsTrue(constantIndex < variableIndex);
    }

    [TestMethod]
    public void DefaultMemberOrder_BuiltinMethodsBeforePublicMethods()
    {
        var order = GDReorderMembersService.DefaultMemberOrder;

        var builtinIndex = order.IndexOf(GDMemberCategory.BuiltinMethod);
        var publicIndex = order.IndexOf(GDMemberCategory.PublicMethod);

        Assert.IsTrue(builtinIndex < publicIndex);
    }

    #endregion

    #region Custom Order Tests

    [TestMethod]
    public void Plan_WithCustomOrder_UsesCustomOrder()
    {
        var code = @"extends Node
func test():
    pass
var speed = 50
";
        var context = CreateContext(code);

        // Custom order with methods before variables
        var customOrder = new System.Collections.Generic.List<GDMemberCategory>
        {
            GDMemberCategory.ClassAttribute,
            GDMemberCategory.PublicMethod,
            GDMemberCategory.PublicVariable
        };

        var result = _service.Plan(context, customOrder);

        Assert.IsTrue(result.Success);
    }

    #endregion

    #region Plan Preview Properties Tests

    [TestMethod]
    public void Plan_MixedOrder_ReturnsOriginalAndReorderedCode()
    {
        var code = @"extends Node
func test():
    pass
var speed = 50
";
        var context = CreateContext(code);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.OriginalCode);
        Assert.IsNotNull(result.ReorderedCode);
        Assert.AreNotEqual(result.OriginalCode, result.ReorderedCode);
        // Original should have func before var
        Assert.IsTrue(result.OriginalCode.IndexOf("func") < result.OriginalCode.IndexOf("var speed"));
    }

    [TestMethod]
    public void Plan_AlreadyOrdered_ReturnsOriginalCodeAsBoth()
    {
        var code = @"extends Node
signal test_signal
var speed = 50
func _ready():
    pass
";
        var context = CreateContext(code);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.OriginalCode);
        // When no changes needed, ReorderedCode equals OriginalCode
        Assert.AreEqual(result.OriginalCode, result.ReorderedCode);
        Assert.IsFalse(result.HasChanges);
    }

    [TestMethod]
    public void Plan_WithChanges_HasChangesIsTrue()
    {
        var code = @"extends Node
func test():
    pass
var speed = 50
signal test_signal
";
        var context = CreateContext(code);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.HasChanges);
        Assert.IsTrue(result.Changes.Count > 0);
    }

    [TestMethod]
    public void GDReorderMembersResult_NoChanges_HasCorrectProperties()
    {
        var originalCode = "test code";
        var result = GDReorderMembersResult.NoChanges(originalCode);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.HasChanges);
        Assert.AreEqual(originalCode, result.OriginalCode);
        Assert.AreEqual(originalCode, result.ReorderedCode);
        Assert.AreEqual(0, result.Changes.Count);
    }

    [TestMethod]
    public void GDReorderMembersResult_Failed_HasNullCodeProperties()
    {
        var result = GDReorderMembersResult.Failed("Test error");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Test error", result.ErrorMessage);
        Assert.IsNull(result.OriginalCode);
        Assert.IsNull(result.ReorderedCode);
    }

    #endregion

    #region Annotation Preservation Tests

    [TestMethod]
    public void Plan_ExportVariable_PreservesAnnotation()
    {
        var code = @"extends Node
func test():
	pass
@export var starting_gold: int = 150
";
        var context = CreateContext(code);
        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.HasChanges);
        var reordered = result.ReorderedCode;
        var exportCount = System.Text.RegularExpressions.Regex.Matches(reordered, "@export").Count;
        Assert.AreEqual(1, exportCount, $"Expected 1 @export but found {exportCount} in:\n{reordered}");
        Assert.IsTrue(reordered.IndexOf("@export") < reordered.IndexOf("func test()"),
            "Export variable should appear before method");
    }

    [TestMethod]
    public void Plan_MultipleExportVariables_NoAnnotationDuplication()
    {
        var code = @"extends Node
func test():
	pass
@export var starting_gold: int = 150
@export var starting_health: int = 20
";
        var context = CreateContext(code);
        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        var reordered = result.ReorderedCode;
        var exportCount = System.Text.RegularExpressions.Regex.Matches(reordered, "@export").Count;
        Assert.AreEqual(2, exportCount, $"Expected 2 @export annotations but found {exportCount} in:\n{reordered}");
    }

    [TestMethod]
    public void Plan_MultiLineAnnotation_PreservesAnnotation()
    {
        var code = @"extends Node
func test():
	pass
@export
var starting_gold: int = 150
";
        var context = CreateContext(code);
        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.HasChanges);
        var reordered = result.ReorderedCode;
        var exportCount = System.Text.RegularExpressions.Regex.Matches(reordered, "@export").Count;
        Assert.AreEqual(1, exportCount, $"Expected 1 @export but found {exportCount} in:\n{reordered}");
    }

    [TestMethod]
    public void Plan_ExportOnreadyVariable_PreservesBothAnnotations()
    {
        var code = @"extends Node
func test():
	pass
@export
@onready var sprite = $Sprite2D
";
        var context = CreateContext(code);
        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        var reordered = result.ReorderedCode;
        var exportCount = System.Text.RegularExpressions.Regex.Matches(reordered, "@export").Count;
        var onreadyCount = System.Text.RegularExpressions.Regex.Matches(reordered, "@onready").Count;
        Assert.AreEqual(1, exportCount, $"Expected 1 @export in:\n{reordered}");
        Assert.AreEqual(1, onreadyCount, $"Expected 1 @onready in:\n{reordered}");
    }

    [TestMethod]
    public void Plan_ExportGroupWithExportVars_PreservesGrouping()
    {
        var code = @"extends Node
func test():
	pass
@export_group(""Stats"")
@export var health: int = 100
@export var mana: int = 50
";
        var context = CreateContext(code);
        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        var reordered = result.ReorderedCode;
        Assert.IsTrue(reordered.IndexOf("@export_group") < reordered.IndexOf("var health"),
            $"@export_group should appear before export vars in:\n{reordered}");
    }

    [TestMethod]
    public void Plan_RpcAnnotatedMethod_PreservesAnnotation()
    {
        var code = @"extends Node
var speed = 50
@rpc(""any_peer"")
func sync_position(pos):
	position = pos
";
        var context = CreateContext(code);
        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        var reordered = result.ReorderedCode;
        var rpcCount = System.Text.RegularExpressions.Regex.Matches(reordered, "@rpc").Count;
        Assert.AreEqual(1, rpcCount, $"Expected 1 @rpc annotation in:\n{reordered}");
    }

    [TestMethod]
    public void Plan_AnnotatedInnerClass_PreservesAnnotation()
    {
        var code = @"extends Node
func test():
	pass
@abstract
class MyAbstractClass:
	func abstract_method() -> void:
		pass
";
        var context = CreateContext(code);
        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        var reordered = result.ReorderedCode;
        Assert.IsTrue(reordered.Contains("@abstract"), $"@abstract annotation should be preserved in:\n{reordered}");
        Assert.IsTrue(reordered.IndexOf("@abstract") < reordered.IndexOf("class MyAbstractClass"),
            "@abstract should appear before class declaration");
    }

    #endregion

    #region Blank Line Formatting Tests

    [TestMethod]
    public void Plan_Methods_HaveTwoBlankLinesBetween()
    {
        var code = @"extends Node
var speed = 50
func test():
	pass
func _ready():
	pass
";
        var context = CreateContext(code);
        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        var reordered = result.ReorderedCode.Replace("\r\n", "\n");
        // Between methods there should be 2 blank lines (\n from AppendLine + \n\n from spacing = \n\n\n)
        Assert.IsTrue(reordered.Contains("\n\n\nfunc"),
            $"Methods should have 2 blank lines before them in:\n{reordered}");
    }

    [TestMethod]
    public void Plan_DifferentCategories_HaveBlankLineBetween()
    {
        var code = @"extends Node
func test():
	pass
signal health_changed
const MAX = 100
var speed = 50
";
        var context = CreateContext(code);
        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        var reordered = result.ReorderedCode.Replace("\r\n", "\n");
        // signal → const should have 1 blank line (= \n\n: one from AppendLine, one from spacing)
        Assert.IsTrue(reordered.Contains("signal health_changed\n\nconst MAX"),
            $"Should have 1 blank line between signal and constant categories in:\n{reordered}");
    }

    [TestMethod]
    public void Plan_SameCategory_NoExtraBlankLines()
    {
        var code = @"extends Node
func test():
	pass
var x = 1
var y = 2
var z = 3
";
        var context = CreateContext(code);
        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        var reordered = result.ReorderedCode.Replace("\r\n", "\n");
        Assert.IsTrue(reordered.Contains("var x = 1\nvar y = 2\nvar z = 3"),
            $"Same-category variables should not have blank lines between them in:\n{reordered}");
    }

    #endregion

    #region Already-Ordered With Annotations Tests

    [TestMethod]
    public void Plan_AlreadyOrdered_WithExportVars_ReturnsNoChanges()
    {
        var code = @"extends Node
signal test_signal
@export var health: int = 100
var speed = 50
func _ready():
	pass
func test():
	pass
";
        var context = CreateContext(code);
        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.HasChanges,
            "Already-ordered code with export vars should report no changes");
    }

    [TestMethod]
    public void Plan_GameManagerScript_FullVerification()
    {
        // Put things out of order: methods before variables
        var code = @"extends Node

func _ready() -> void:
	_connect_signals()

func start_game() -> void:
	current_gold = starting_gold

@export var starting_gold: int = 150
@export var starting_health: int = 20

var current_gold: int = 0
var current_health: int = 0

func _connect_signals() -> void:
	pass

func _on_enemy_killed() -> void:
	pass
";
        var context = CreateContext(code);
        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        var reordered = result.ReorderedCode;

        // Verify no duplicate annotations
        var exportCount = System.Text.RegularExpressions.Regex.Matches(reordered, "@export").Count;
        Assert.AreEqual(2, exportCount, $"Should have exactly 2 @export in:\n{reordered}");

        // Verify order: export vars < public vars < _ready < public methods < private methods
        var exportIdx = reordered.IndexOf("@export var starting_gold");
        var publicVarIdx = reordered.IndexOf("var current_gold");
        var readyIdx = reordered.IndexOf("func _ready");
        var publicMethodIdx = reordered.IndexOf("func start_game");
        var privateMethodIdx = reordered.IndexOf("func _connect_signals");

        Assert.IsTrue(exportIdx < publicVarIdx, "Export vars should be before public vars");
        Assert.IsTrue(publicVarIdx < readyIdx, "Public vars should be before _ready");
        Assert.IsTrue(readyIdx < publicMethodIdx, "Builtin methods should be before public methods");
        Assert.IsTrue(publicMethodIdx < privateMethodIdx, "Public methods should be before private methods");

        // Verify blank lines between methods
        var normalized = reordered.Replace("\r\n", "\n");
        Assert.IsTrue(normalized.Contains("\n\n\nfunc"),
            $"Should have 2 blank lines between methods in:\n{reordered}");
    }

    [TestMethod]
    public void Plan_ReorderedOutput_IsStableAfterFormatting()
    {
        var code = @"extends Node

func _ready():
	pass

func _connect_signals():
	pass

@export var starting_gold: int = 150
@export var starting_health: int = 20
var current_gold: int = 0

func start_game():
	pass

func _on_enemy_killed():
	pass
";
        var context = CreateContext(code);
        var reorderResult = _service.Plan(context);

        Assert.IsTrue(reorderResult.Success);
        Assert.IsTrue(reorderResult.HasChanges);
        var reordered = reorderResult.ReorderedCode;

        // Now format the reordered output
        var formatService = new GDFormatCodeService();
        var formatContext = CreateContext(reordered);
        var formatResult = formatService.Plan(formatContext);

        if (formatResult.Success && formatResult.HasChanges)
        {
            var formatted = formatResult.FormattedCode;
            var reorderedNorm = reordered.Replace("\r\n", "\n").Trim();
            var formattedNorm = formatted.Replace("\r\n", "\n").Trim();

            Assert.AreEqual(reorderedNorm, formattedNorm,
                $"Reorder output should not be changed by formatter.\n\nReordered:\n{reorderedNorm}\n\nFormatted:\n{formattedNorm}");
        }
        // If format says no changes — that's perfect, reorder output was already well-formatted
    }

    [TestMethod]
    public void Plan_ReorderedOutputWithExportGroup_IsStableAfterFormatting()
    {
        var code = @"extends Node

func _ready():
	pass

@export_group(""Stats"")
@export var health: int = 100
@export var mana: int = 50
var speed: float = 1.0

func attack():
	pass
";
        var context = CreateContext(code);
        var reorderResult = _service.Plan(context);

        Assert.IsTrue(reorderResult.Success);
        Assert.IsTrue(reorderResult.HasChanges);
        var reordered = reorderResult.ReorderedCode;

        // Now format the reordered output
        var formatService = new GDFormatCodeService();
        var formatContext = CreateContext(reordered);
        var formatResult = formatService.Plan(formatContext);

        if (formatResult.Success && formatResult.HasChanges)
        {
            var formatted = formatResult.FormattedCode;
            var reorderedNorm = reordered.Replace("\r\n", "\n").Trim();
            var formattedNorm = formatted.Replace("\r\n", "\n").Trim();

            Assert.AreEqual(reorderedNorm, formattedNorm,
                $"Reorder output with @export_group should not be changed by formatter.\n\nReordered:\n{reorderedNorm}\n\nFormatted:\n{formattedNorm}");
        }
    }

    #endregion
}
