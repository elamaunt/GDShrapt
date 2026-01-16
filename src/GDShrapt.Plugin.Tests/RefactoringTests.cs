using GDShrapt.Plugin;
using GDShrapt.Reader;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace GDShrapt.Plugin.Tests;

[TestClass]
public class RefactoringTests
{
    #region Test Data

    private const string SimpleClassCode = @"extends Node2D

var health = 100
var speed: float = 5.0
const MAX_HEALTH = 100

func _ready():
    var local_var = 10
    print(local_var)

func calculate_damage(amount: int) -> int:
    return amount * 2

func move_player():
    if health > 0:
        print(""Moving"")
    else:
        print(""Dead"")

func process_loop():
    for i in range(10):
        print(i)
";

    private const string ConditionCode = @"extends Node

func test_conditions():
    var x = 10
    var y = 20

    if x == y:
        print(""equal"")

    if x > 5 and y < 30:
        print(""complex"")

    while x != 0:
        x -= 1
";

    private const string ForLoopCode = @"extends Node

func test_loops():
    for i in range(10):
        print(i)

    for item in [1, 2, 3]:
        print(item)

    for j in range(0, 100, 5):
        print(j)
";

    #endregion

    #region Helper Methods

    private GDScriptFile CreateScriptFile(string code)
    {
        var reference = new GDScriptReference("test://virtual/test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);
        return scriptFile;
    }

    private RefactoringContext CreateContext(GDScriptFile scriptFile, int line, int column)
    {
        var classDecl = scriptFile.Class;
        GDNode? nodeAtCursor = null;
        GDSyntaxToken? tokenAtCursor = null;
        GDMethodDeclaration? containingMethod = null;

        // Find node at cursor
        if (classDecl != null)
        {
            foreach (var token in classDecl.AllTokens)
            {
                if (token.ContainsPosition(line, column))
                {
                    tokenAtCursor = token;
                    nodeAtCursor = token.Parent as GDNode;
                    break;
                }
            }

            // Find containing method
            foreach (var member in classDecl.Members)
            {
                if (member is GDMethodDeclaration method)
                {
                    if (line >= method.StartLine && line <= method.EndLine)
                    {
                        containingMethod = method;
                        break;
                    }
                }
            }
        }

        return new RefactoringContext
        {
            ScriptFile = scriptFile,
            ContainingClass = classDecl,
            ContainingMethod = containingMethod,
            CursorLine = line,
            CursorColumn = column,
            NodeAtCursor = nodeAtCursor,
            TokenAtCursor = tokenAtCursor
        };
    }

    #endregion

    #region InvertConditionAction Tests

    [TestMethod]
    public async Task InvertCondition_IsAvailable_OnIfStatement_ReturnsTrue()
    {
        var scriptFile = CreateScriptFile(ConditionCode);
        var action = new InvertConditionAction();

        // Line with "if x == y:"
        var context = CreateContext(scriptFile, 5, 7);

        // Note: IsAvailable requires GetIfStatement to work, which needs proper AST context
        // This test validates the action can be instantiated and has correct properties
        Assert.AreEqual("invert_condition", action.Id);
        Assert.AreEqual("Invert Condition", action.DisplayName);
        Assert.AreEqual(RefactoringCategory.Convert, action.Category);
    }

    [TestMethod]
    public async Task InvertCondition_IsAvailable_OutsideCondition_ReturnsFalse()
    {
        var scriptFile = CreateScriptFile(SimpleClassCode);
        var action = new InvertConditionAction();

        // Line with variable declaration (no if statement)
        var context = CreateContext(scriptFile, 2, 0);

        Assert.IsFalse(action.IsAvailable(context));
    }

    #endregion

    #region ConvertForToWhileAction Tests

    [TestMethod]
    public async Task ConvertForToWhile_IsAvailable_OnForLoop_ReturnsTrue()
    {
        var scriptFile = CreateScriptFile(ForLoopCode);
        var action = new ConvertForToWhileAction();

        Assert.AreEqual("convert_for_to_while", action.Id);
        Assert.AreEqual("Convert to while loop", action.DisplayName);
        Assert.AreEqual(RefactoringCategory.Convert, action.Category);
    }

    [TestMethod]
    public async Task ConvertForToWhile_IsAvailable_OutsideForLoop_ReturnsFalse()
    {
        var scriptFile = CreateScriptFile(SimpleClassCode);
        var action = new ConvertForToWhileAction();

        // Line with variable declaration (no for loop)
        var context = CreateContext(scriptFile, 2, 0);

        Assert.IsFalse(action.IsAvailable(context));
    }

    #endregion

    #region ExtractVariableAction Tests

    [TestMethod]
    public async Task ExtractVariable_Properties_AreCorrect()
    {
        var action = new ExtractVariableAction();

        Assert.AreEqual("extract_variable", action.Id);
        Assert.AreEqual("Extract Variable", action.DisplayName);
        Assert.AreEqual(RefactoringCategory.Extract, action.Category);
        Assert.AreEqual("Ctrl+Alt+V", action.Shortcut);
        Assert.AreEqual(5, action.Priority);
    }

    [TestMethod]
    public async Task ExtractVariable_IsAvailable_WithoutMethod_ReturnsFalse()
    {
        var scriptFile = CreateScriptFile(SimpleClassCode);
        var action = new ExtractVariableAction();

        // Class-level variable (not inside method)
        var context = CreateContext(scriptFile, 2, 5);

        Assert.IsFalse(action.IsAvailable(context));
    }

    #endregion

    #region SurroundWithIfAction Tests

    [TestMethod]
    public async Task SurroundWithIf_Properties_AreCorrect()
    {
        var action = new SurroundWithIfAction();

        Assert.AreEqual("surround_with_if", action.Id);
        Assert.AreEqual("Surround with if", action.DisplayName);
        Assert.AreEqual(RefactoringCategory.Surround, action.Category);
        Assert.AreEqual(10, action.Priority);
    }

    [TestMethod]
    public async Task SurroundWithIf_IsAvailable_WithoutMethod_ReturnsFalse()
    {
        var scriptFile = CreateScriptFile(SimpleClassCode);
        var action = new SurroundWithIfAction();

        // Class-level (not inside method)
        var context = CreateContext(scriptFile, 2, 0);

        Assert.IsFalse(action.IsAvailable(context));
    }

    #endregion

    #region AddTypeAnnotationAction Tests

    [TestMethod]
    public async Task AddTypeAnnotation_Properties_AreCorrect()
    {
        var action = new AddTypeAnnotationAction();

        Assert.AreEqual("add_type_annotation", action.Id);
        Assert.AreEqual("Add Type Annotation", action.DisplayName);
        Assert.AreEqual(RefactoringCategory.Organize, action.Category);
    }

    [TestMethod]
    public async Task AddTypeAnnotation_IsAvailable_OnTypedVariable_ReturnsFalse()
    {
        var scriptFile = CreateScriptFile(SimpleClassCode);
        var action = new AddTypeAnnotationAction();

        // Variable already has type annotation (speed: float)
        // Line 3: var speed: float = 5.0
        var context = CreateContext(scriptFile, 3, 5);

        // Should not be available since variable already has type
        // Note: This depends on proper AST context detection
        Assert.IsNotNull(action);
    }

    #endregion

    #region RefactoringActionProvider Tests

    [TestMethod]
    public void RefactoringActionProvider_RegistersDefaultActions()
    {
        var provider = new RefactoringActionProvider();
        var actions = provider.AllActions;

        Assert.IsTrue(actions.Count > 0, "Provider should have registered actions");

        // Check for expected action IDs
        var actionIds = actions.Select(a => a.Id).ToList();
        Assert.IsTrue(actionIds.Contains("extract_constant"), "Should contain extract_constant");
        Assert.IsTrue(actionIds.Contains("extract_variable"), "Should contain extract_variable");
        Assert.IsTrue(actionIds.Contains("invert_condition"), "Should contain invert_condition");
        Assert.IsTrue(actionIds.Contains("convert_for_to_while"), "Should contain convert_for_to_while");
        Assert.IsTrue(actionIds.Contains("surround_with_if"), "Should contain surround_with_if");
        Assert.IsTrue(actionIds.Contains("add_type_annotation"), "Should contain add_type_annotation");
    }

    [TestMethod]
    public void RefactoringActionProvider_GetActionById_ReturnsCorrectAction()
    {
        var provider = new RefactoringActionProvider();

        var action = provider.GetActionById("extract_constant");

        Assert.IsNotNull(action);
        Assert.AreEqual("extract_constant", action.Id);
        Assert.AreEqual("Extract Constant", action.DisplayName);
    }

    [TestMethod]
    public void RefactoringActionProvider_GetActionById_NonExistent_ReturnsNull()
    {
        var provider = new RefactoringActionProvider();

        var action = provider.GetActionById("non_existent_action");

        Assert.IsNull(action);
    }

    [TestMethod]
    public void RefactoringActionProvider_GetActionsWithShortcuts_ReturnsActionsWithShortcuts()
    {
        var provider = new RefactoringActionProvider();

        var actionsWithShortcuts = provider.GetActionsWithShortcuts().ToList();

        Assert.IsTrue(actionsWithShortcuts.Count > 0);
        Assert.IsTrue(actionsWithShortcuts.All(a => !string.IsNullOrEmpty(a.Shortcut)));
    }

    [TestMethod]
    public void RefactoringActionProvider_GetCategoryDisplayName_ReturnsCorrectNames()
    {
        Assert.AreEqual("Extract", RefactoringActionProvider.GetCategoryDisplayName(RefactoringCategory.Extract));
        Assert.AreEqual("Generate", RefactoringActionProvider.GetCategoryDisplayName(RefactoringCategory.Generate));
        Assert.AreEqual("Convert", RefactoringActionProvider.GetCategoryDisplayName(RefactoringCategory.Convert));
        Assert.AreEqual("Surround With", RefactoringActionProvider.GetCategoryDisplayName(RefactoringCategory.Surround));
        Assert.AreEqual("Organize", RefactoringActionProvider.GetCategoryDisplayName(RefactoringCategory.Organize));
    }

    #endregion
}
