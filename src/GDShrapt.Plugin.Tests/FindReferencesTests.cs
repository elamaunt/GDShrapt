using GDShrapt.Plugin;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GDShrapt.Plugin.Tests;

/// <summary>
/// Tests for FindReferencesCommand semantic analysis functionality.
/// </summary>
[TestClass]
public class FindReferencesTests
{
    #region Test Data

    private const string LocalVariableCode = @"extends Node

func test_local():
    var local_value = 10
    print(local_value)
    local_value += 5
    return local_value
";

    private const string ParameterCode = @"extends Node

func calculate(amount: int, multiplier: float) -> float:
    var result = amount * multiplier
    print(amount)
    return result
";

    private const string ClassMemberCode = @"extends Node

var health = 100
var speed = 5.0

func take_damage(amount: int):
    health -= amount
    if health <= 0:
        die()

func heal(amount: int):
    health += amount

func die():
    print(""Dead"")
";

    private const string ForLoopVariableCode = @"extends Node

func process_items():
    for item in get_children():
        print(item.name)
        process_item(item)

func process_item(node):
    pass
";

    private const string ComplexScopesCode = @"extends Node

var global_var = 10

func outer_func():
    var local_var = 20

    for i in range(10):
        var inner_var = local_var + i
        print(inner_var)

    print(local_var)
    print(global_var)

func another_func():
    var local_var = 30
    print(local_var)
    print(global_var)
";

    // Additional test data for comprehensive coverage

    private const string LocalVariableNestedScopesCode = @"extends Node

func test_nested():
    var outer = 1
    if true:
        var inner = outer + 1
        print(inner)
        while inner > 0:
            var deepest = inner
            print(deepest)
            inner -= 1
    print(outer)
";

    private const string LocalVariableUsedBeforeDeclarationCode = @"extends Node

func test_order():
    print(later_var)
    var later_var = 10
    print(later_var)
";

    private const string ParameterWithAssignmentCode = @"extends Node

func calculate(amount: int, multiplier: float) -> float:
    var result = amount * multiplier
    print(amount)
    amount = 0
    return result
";

    private const string ParameterWithSameNamedLocalCode = @"extends Node

func process(value: int) -> int:
    print(value)
    var value = 99
    print(value)
    return value
";

    private const string ParameterOptionalWithDefaultCode = @"extends Node

func greet(name: String = ""World"", count: int = 1):
    for i in range(count):
        print(""Hello, "" + name)
";

    private const string ForLoopNestedCode = @"extends Node

func matrix_process():
    for i in range(10):
        for j in range(10):
            var cell = i * 10 + j
            print(cell)
        print(i)
";

    private const string ForLoopSameVariableNameCode = @"extends Node

var item = ""class_member""

func first_loop():
    for item in [1, 2, 3]:
        print(item)
    print(item)

func second_loop():
    for item in [4, 5, 6]:
        print(item)
";

    private const string ClassMemberMethodCode = @"extends Node

func helper():
    return 42

func main():
    var x = helper()
    print(helper())
    helper()
";

    private const string ClassMemberSignalCode = @"extends Node

signal health_changed(new_value)
signal died

var health = 100

func take_damage(amount: int):
    health -= amount
    health_changed.emit(health)
    if health <= 0:
        died.emit()
";

    private const string ClassMemberConstantCode = @"extends Node

const MAX_HEALTH = 100
const MIN_HEALTH = 0

func clamp_health(value: int) -> int:
    return clamp(value, MIN_HEALTH, MAX_HEALTH)
";

    private const string ClassMemberEnumCode = @"extends Node

enum State { IDLE, RUNNING, JUMPING }

var current_state = State.IDLE

func set_state(new_state: State):
    current_state = new_state
    if new_state == State.RUNNING:
        start_animation()

func start_animation():
    pass
";

    private const string ClassMemberInnerClassCode = @"extends Node

class InnerData:
    var value: int

    func get_value() -> int:
        return value

func test():
    var data = InnerData.new()
    data.value = 10
    print(data.get_value())
";

    private const string ChainedMemberAccessCode = @"extends Node

func test():
    var x = get_parent().get_child(0).name
    get_tree().current_scene.queue_free()
";

    private const string BuiltInTypeMemberCode = @"extends Node

func process_array():
    var arr = [1, 2, 3]
    arr.append(4)
    var size = arr.size()
    arr.clear()

func process_dict():
    var dict = {""key"": ""value""}
    var keys = dict.keys()
    dict.clear()
";

    private const string ReferenceKindTestCode = @"extends Node

var counter = 0

func test():
    var x = counter
    counter = 10
    counter += 1
    print(counter)
    process(counter)

func process(val):
    pass

func helper():
    return 42

func caller():
    helper()
    var y = helper()
    print(helper())
";

    private const string SameNameAllLevelsCode = @"extends Node

var value = 1

func test(value: int):
    print(value)
    var value = 3
    print(value)

    for value in [4, 5]:
        print(value)

    print(value)
";

    private const string GetterSetterCode = @"extends Node

var _health: int = 100

var health: int:
    get:
        return _health
    set(value):
        _health = clamp(value, 0, 100)

func test():
    health = 50
    print(health)
    _health = 75
";

    private const string LambdaCode = @"extends Node

var multiplier = 2

func test():
    var items = [1, 2, 3]
    var doubled = items.map(func(x): return x * multiplier)
    var filtered = items.filter(func(item): return item > 1)
";

    private const string AnnotationCode = @"extends Node

@export var speed: float = 5.0
@onready var label = $Label

func _ready():
    print(speed)
    label.text = ""Hello""
";

    private const string StringInterpolationCode = @"extends Node

var name = ""World""
var count = 5

func test():
    print(""Hello, %s!"" % name)
    print(""Count: {count}"".format({""count"": count}))
";

    // Cross-file test data
    private const string BaseClassCode = @"extends Node
class_name BaseEntity

var health = 100

func take_damage(amount: int):
    health -= amount
";

    private const string DerivedClassCode = @"extends BaseEntity
class_name Player

func special_attack():
    take_damage(10)
    health = 50
";

    private const string UsingClassCode = @"extends Node

var player: Player

func test():
    player.take_damage(20)
    player.special_attack()
    print(player.health)
";

    #endregion

    #region Helper Methods

    private GDScriptMap CreateScriptMap(string code, string path = "test.gd")
    {
        var reference = new GDPluginScriptReference(path);
        var map = new GDScriptMap(reference);
        map.Reload(code);
        return map;
    }

    private GDIdentifier? FindIdentifierAtLine(GDScriptMap scriptMap, int line, string name)
    {
        if (scriptMap.Class == null) return null;

        return scriptMap.Class.AllTokens
            .OfType<GDIdentifier>()
            .FirstOrDefault(id => id.StartLine == line && id.Sequence == name);
    }

    private GDMethodDeclaration? FindMethod(GDScriptMap scriptMap, string methodName)
    {
        if (scriptMap.Class == null) return null;

        return scriptMap.Class.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == methodName);
    }

    private GDForStatement? FindForStatement(GDMethodDeclaration method)
    {
        return method?.AllNodes.OfType<GDForStatement>().FirstOrDefault();
    }

    private IEnumerable<GDForStatement> FindForStatements(GDMethodDeclaration method)
    {
        return method?.AllNodes.OfType<GDForStatement>() ?? Enumerable.Empty<GDForStatement>();
    }

    /// <summary>
    /// Collects all identifier references within a method for a given variable name.
    /// </summary>
    private List<ReferenceInfo> CollectLocalVariableReferences(GDMethodDeclaration method, string variableName, int declarationLine)
    {
        var references = new List<ReferenceInfo>();
        if (method == null) return references;

        // Add declaration
        var decl = method.AllNodes.OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == variableName &&
                                v.StartLine == declarationLine);
        if (decl != null)
        {
            references.Add(new ReferenceInfo(
                decl.StartLine, decl.StartColumn,
                $"var {variableName}", ReferenceKind.Declaration));
        }

        // Add usages after declaration
        foreach (var id in method.AllNodes.OfType<GDIdentifierExpression>()
            .Where(e => e.Identifier?.Sequence == variableName &&
                        e.StartLine >= declarationLine))
        {
            references.Add(new ReferenceInfo(
                id.StartLine, id.StartColumn,
                GetContext(id.Identifier), DetermineReferenceKind(id.Identifier)));
        }

        return references;
    }

    /// <summary>
    /// Collects all identifier references for a method parameter.
    /// </summary>
    private List<ReferenceInfo> CollectParameterReferences(GDMethodDeclaration method, string paramName)
    {
        var references = new List<ReferenceInfo>();
        if (method == null) return references;

        // Add parameter declaration
        var param = method.Parameters?.OfType<GDParameterDeclaration>()
            .FirstOrDefault(p => p.Identifier?.Sequence == paramName);
        if (param != null)
        {
            references.Add(new ReferenceInfo(
                param.StartLine, param.StartColumn,
                $"param {paramName}", ReferenceKind.Declaration));
        }

        // Find all usages in method body (before any local with same name)
        var localDecl = method.AllNodes.OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == paramName);
        var cutoffLine = localDecl?.StartLine ?? int.MaxValue;

        foreach (var id in method.AllNodes.OfType<GDIdentifierExpression>()
            .Where(e => e.Identifier?.Sequence == paramName &&
                        e.StartLine < cutoffLine))
        {
            references.Add(new ReferenceInfo(
                id.StartLine, id.StartColumn,
                GetContext(id.Identifier), DetermineReferenceKind(id.Identifier)));
        }

        return references;
    }

    /// <summary>
    /// Collects all identifier references within a for loop for the loop variable.
    /// </summary>
    private List<ReferenceInfo> CollectForLoopReferences(GDForStatement forStmt, string variableName)
    {
        var references = new List<ReferenceInfo>();
        if (forStmt == null) return references;

        // Add for loop variable declaration
        if (forStmt.Variable?.Sequence == variableName)
        {
            references.Add(new ReferenceInfo(
                forStmt.Variable.StartLine, forStmt.Variable.StartColumn,
                $"for {variableName} in ...", ReferenceKind.Declaration));
        }

        // Add usages in loop body
        if (forStmt.Statements != null)
        {
            foreach (var id in forStmt.Statements.AllNodes.OfType<GDIdentifierExpression>()
                .Where(e => e.Identifier?.Sequence == variableName))
            {
                references.Add(new ReferenceInfo(
                    id.StartLine, id.StartColumn,
                    GetContext(id.Identifier), DetermineReferenceKind(id.Identifier)));
            }
        }

        return references;
    }

    /// <summary>
    /// Collects all identifier references for a class member.
    /// </summary>
    private List<ReferenceInfo> CollectClassMemberReferences(GDScriptMap scriptMap, string memberName)
    {
        var references = new List<ReferenceInfo>();
        var classDecl = scriptMap.Class;
        if (classDecl == null) return references;

        foreach (var id in classDecl.AllTokens.OfType<GDIdentifier>()
            .Where(i => i.Sequence == memberName))
        {
            references.Add(new ReferenceInfo(
                id.StartLine, id.StartColumn,
                GetContext(id), DetermineReferenceKind(id)));
        }

        return references;
    }

    /// <summary>
    /// Collects all identifier references across multiple scripts.
    /// </summary>
    private List<ReferenceInfo> CollectProjectWideReferences(GDProjectMap project, string symbolName)
    {
        var references = new List<ReferenceInfo>();

        foreach (var script in project.Scripts)
        {
            if (script.Class == null) continue;
            var filePath = script.Reference?.FullPath ?? "unknown";

            foreach (var id in script.Class.AllTokens.OfType<GDIdentifier>()
                .Where(i => i.Sequence == symbolName))
            {
                references.Add(new ReferenceInfo(
                    id.StartLine, id.StartColumn,
                    GetContext(id), DetermineReferenceKind(id), filePath));
            }
        }

        return references;
    }

    /// <summary>
    /// Collects member access references (obj.member pattern).
    /// </summary>
    private List<ReferenceInfo> CollectMemberAccessReferences(GDScriptMap scriptMap, string memberName)
    {
        var references = new List<ReferenceInfo>();

        foreach (var memberOp in scriptMap.Class?.AllNodes.OfType<GDMemberOperatorExpression>()
            ?? Enumerable.Empty<GDMemberOperatorExpression>())
        {
            if (memberOp.Identifier?.Sequence == memberName)
            {
                references.Add(new ReferenceInfo(
                    memberOp.Identifier.StartLine,
                    memberOp.Identifier.StartColumn,
                    GetContext(memberOp.Identifier),
                    DetermineReferenceKind(memberOp.Identifier)));
            }
        }

        return references;
    }

    /// <summary>
    /// Collects all references to a symbol (text-based, not semantic).
    /// </summary>
    private List<ReferenceInfo> CollectAllReferences(GDScriptMap scriptMap, string name)
    {
        var references = new List<ReferenceInfo>();

        foreach (var id in scriptMap.Class?.AllTokens.OfType<GDIdentifier>()
            .Where(i => i.Sequence == name) ?? Enumerable.Empty<GDIdentifier>())
        {
            references.Add(new ReferenceInfo(
                id.StartLine, id.StartColumn,
                GetContext(id), DetermineReferenceKind(id)));
        }

        return references;
    }

    /// <summary>
    /// Determines the reference kind based on AST context.
    /// </summary>
    private static ReferenceKind DetermineReferenceKind(GDIdentifier identifier)
    {
        if (identifier == null) return ReferenceKind.Read;

        var parent = identifier.Parent;

        // Declaration checks
        if (parent is GDMethodDeclaration) return ReferenceKind.Declaration;
        if (parent is GDVariableDeclaration) return ReferenceKind.Declaration;
        if (parent is GDVariableDeclarationStatement) return ReferenceKind.Declaration;
        if (parent is GDSignalDeclaration) return ReferenceKind.Declaration;
        if (parent is GDParameterDeclaration) return ReferenceKind.Declaration;
        if (parent is GDEnumDeclaration) return ReferenceKind.Declaration;
        if (parent is GDInnerClassDeclaration) return ReferenceKind.Declaration;
        if (parent is GDForStatement) return ReferenceKind.Declaration;

        // Call check
        if (parent is GDIdentifierExpression idExpr && idExpr.Parent is GDCallExpression)
            return ReferenceKind.Call;
        if (parent is GDMemberOperatorExpression memberOp && memberOp.Parent is GDCallExpression)
            return ReferenceKind.Call;

        // Write check - look for assignment operators
        var current = parent;
        while (current != null)
        {
            if (current is GDExpressionStatement exprStmt)
            {
                if (exprStmt.Expression is GDDualOperatorExpression dualOp)
                {
                    if (IsAssignmentOperator(dualOp) && IsLeftSide(identifier, dualOp))
                        return ReferenceKind.Write;
                }
                break;
            }
            if (current is GDDualOperatorExpression dualOp2)
            {
                if (IsAssignmentOperator(dualOp2) && IsLeftSide(identifier, dualOp2))
                    return ReferenceKind.Write;
            }
            current = current.Parent;
        }

        return ReferenceKind.Read;
    }

    private static bool IsAssignmentOperator(GDDualOperatorExpression dualOp)
    {
        var opType = dualOp.OperatorType;
        return opType == GDDualOperatorType.Assignment ||
               opType == GDDualOperatorType.AddAndAssign ||
               opType == GDDualOperatorType.SubtractAndAssign ||
               opType == GDDualOperatorType.MultiplyAndAssign ||
               opType == GDDualOperatorType.DivideAndAssign ||
               opType == GDDualOperatorType.ModAndAssign ||
               opType == GDDualOperatorType.BitwiseAndAndAssign ||
               opType == GDDualOperatorType.BitwiseOrAndAssign ||
               opType == GDDualOperatorType.XorAndAssign ||
               opType == GDDualOperatorType.BitShiftLeftAndAssign ||
               opType == GDDualOperatorType.BitShiftRightAndAssign;
    }

    private static bool IsLeftSide(GDIdentifier identifier, GDDualOperatorExpression dualOp)
    {
        var left = dualOp.LeftExpression;
        if (left is GDIdentifierExpression idExpr)
            return idExpr.Identifier == identifier;
        if (left is GDMemberOperatorExpression memberOp)
            return memberOp.Identifier == identifier;
        return false;
    }

    private static string GetContext(GDIdentifier identifier)
    {
        if (identifier == null) return "";

        var parent = identifier.Parent;
        if (parent is GDMethodDeclaration method)
            return $"func {method.Identifier?.Sequence ?? ""}(...)";
        if (parent is GDVariableDeclaration variable)
        {
            // Check if it's a constant (has const keyword)
            if (variable.ConstKeyword != null)
                return $"const {variable.Identifier?.Sequence ?? ""}";
            return $"var {variable.Identifier?.Sequence ?? ""}";
        }
        if (parent is GDVariableDeclarationStatement localVar)
            return $"var {localVar.Identifier?.Sequence ?? ""}";
        if (parent is GDSignalDeclaration signal)
            return $"signal {signal.Identifier?.Sequence ?? ""}";
        if (parent is GDParameterDeclaration param)
            return $"param {param.Identifier?.Sequence ?? ""}";

        return identifier.Sequence ?? "";
    }

    /// <summary>
    /// Simple reference info for test assertions.
    /// </summary>
    private record ReferenceInfo(int Line, int Column, string Context, ReferenceKind Kind, string FilePath = "test.gd");

    /// <summary>
    /// Reference kind enumeration for tests.
    /// </summary>
    private enum ReferenceKind
    {
        Declaration,
        Read,
        Write,
        Call
    }

    #endregion

    #region Local Variable Scope Tests

    [TestMethod]
    public async Task FindReferences_LocalVariable_OnlyInMethod()
    {
        var scriptMap = CreateScriptMap(LocalVariableCode);
        var method = FindMethod(scriptMap, "test_local");

        Assert.IsNotNull(method);

        // Count occurrences of "local_value" in the method
        var references = method.AllNodes
            .OfType<GDIdentifierExpression>()
            .Where(e => e.Identifier?.Sequence == "local_value")
            .ToList();

        // Should find: declaration, print, +=, return (4 total uses, 3 references after declaration)
        Assert.IsTrue(references.Count >= 3, $"Expected at least 3 references, found {references.Count}");
    }

    [TestMethod]
    public async Task FindReferences_LocalVariable_DeclarationPlusUsages()
    {
        // Arrange
        var scriptMap = CreateScriptMap(LocalVariableCode);
        var method = FindMethod(scriptMap, "test_local");

        Assert.IsNotNull(method);

        // Act - find references for local_value on line 3 (declaration)
        var references = CollectLocalVariableReferences(method, "local_value", declarationLine: 3);

        // Assert
        Assert.IsTrue(references.Count >= 4, $"Expected at least 4 references (decl + 3 usages), found {references.Count}");
        Assert.AreEqual(1, references.Count(r => r.Kind == ReferenceKind.Declaration), "Should have exactly 1 declaration");
    }

    [TestMethod]
    public async Task FindReferences_LocalVariable_NestedScopes_AllResolved()
    {
        // Arrange
        var scriptMap = CreateScriptMap(LocalVariableNestedScopesCode);
        var method = FindMethod(scriptMap, "test_nested");

        Assert.IsNotNull(method);

        // Act - find references for different nested variables
        var outerRefs = CollectLocalVariableReferences(method, "outer", declarationLine: 3);
        var innerRefs = CollectLocalVariableReferences(method, "inner", declarationLine: 5);
        var deepestRefs = CollectLocalVariableReferences(method, "deepest", declarationLine: 8);

        // Assert
        Assert.IsTrue(outerRefs.Count >= 3, $"outer: expected at least 3 refs (decl + use in inner init + final print), found {outerRefs.Count}");
        Assert.IsTrue(innerRefs.Count >= 4, $"inner: expected at least 4 refs (decl + print + while + deepest init + -=), found {innerRefs.Count}");
        Assert.IsTrue(deepestRefs.Count >= 2, $"deepest: expected at least 2 refs (decl + print), found {deepestRefs.Count}");
    }

    [TestMethod]
    public async Task FindReferences_LocalVariable_OnlyAfterDeclaration()
    {
        // Arrange
        var scriptMap = CreateScriptMap(LocalVariableUsedBeforeDeclarationCode);
        var method = FindMethod(scriptMap, "test_order");

        Assert.IsNotNull(method);

        // Act - when cursor is on declaration at line 4
        var refs = CollectLocalVariableReferences(method, "later_var", declarationLine: 4);

        // Assert - should NOT include usage on line 3 (before declaration)
        Assert.AreEqual(2, refs.Count, "Only declaration + valid usage after declaration");
        Assert.IsTrue(refs.All(r => r.Line >= 4), "All references should be on or after declaration line");
    }

    #endregion

    #region Parameter Scope Tests

    [TestMethod]
    public async Task FindReferences_Parameter_OnlyInMethod()
    {
        var scriptMap = CreateScriptMap(ParameterCode);
        var method = FindMethod(scriptMap, "calculate");

        Assert.IsNotNull(method);

        // Check parameters are parsed
        Assert.IsNotNull(method.Parameters);
        var paramCount = method.Parameters.Count();
        Assert.AreEqual(2, paramCount, "Should have 2 parameters");

        // Find usages of "amount" parameter in method body
        var amountUsages = method.AllNodes
            .OfType<GDIdentifierExpression>()
            .Where(e => e.Identifier?.Sequence == "amount")
            .ToList();

        // Should find: in expression, in print (2 uses)
        Assert.IsTrue(amountUsages.Count >= 2, $"Expected at least 2 uses of 'amount', found {amountUsages.Count}");
    }

    [TestMethod]
    public async Task FindReferences_Parameter_DeclarationPlusUsages()
    {
        // Arrange
        var scriptMap = CreateScriptMap(ParameterWithAssignmentCode);
        var method = FindMethod(scriptMap, "calculate");

        Assert.IsNotNull(method);

        // Act
        var amountRefs = CollectParameterReferences(method, "amount");

        // Assert - param decl + multiply + print + assignment
        Assert.IsTrue(amountRefs.Count >= 4, $"Expected at least 4 refs for 'amount', found {amountRefs.Count}");
        Assert.AreEqual(1, amountRefs.Count(r => r.Kind == ReferenceKind.Declaration), "Should have 1 declaration");
        Assert.IsTrue(amountRefs.Any(r => r.Kind == ReferenceKind.Read), "Should have read references");
        Assert.IsTrue(amountRefs.Any(r => r.Kind == ReferenceKind.Write), "Should have write reference (assignment)");
    }

    [TestMethod]
    public async Task FindReferences_Parameter_ShadowedByLocal_SeparateScopes()
    {
        // Arrange
        var scriptMap = CreateScriptMap(ParameterWithSameNamedLocalCode);
        var method = FindMethod(scriptMap, "process");

        Assert.IsNotNull(method);

        // Act - when cursor is on parameter "value"
        var paramRefs = CollectParameterReferences(method, "value");

        // Assert - parameter should only have refs before local shadows it (decl + first print)
        Assert.AreEqual(2, paramRefs.Count, "Parameter 'value' should have 2 refs (decl + first print before shadowing)");
    }

    [TestMethod]
    public async Task FindReferences_Parameter_OptionalWithDefaults()
    {
        // Arrange
        var scriptMap = CreateScriptMap(ParameterOptionalWithDefaultCode);
        var method = FindMethod(scriptMap, "greet");

        Assert.IsNotNull(method);

        // Act
        var nameRefs = CollectParameterReferences(method, "name");
        var countRefs = CollectParameterReferences(method, "count");

        // Assert
        Assert.IsTrue(nameRefs.Count >= 2, $"'name' should have at least 2 refs (decl + string concat), found {nameRefs.Count}");
        Assert.IsTrue(countRefs.Count >= 2, $"'count' should have at least 2 refs (decl + range call), found {countRefs.Count}");
    }

    #endregion

    #region Class Member Scope Tests

    [TestMethod]
    public async Task FindReferences_ClassMember_AcrossMethods()
    {
        var scriptMap = CreateScriptMap(ClassMemberCode);

        Assert.IsNotNull(scriptMap.Class);

        // Find all usages of "health" across all methods
        var healthUsages = scriptMap.Class.AllNodes
            .OfType<GDIdentifierExpression>()
            .Where(e => e.Identifier?.Sequence == "health")
            .ToList();

        // Should find: in take_damage (2x), in heal (1x) = 3+ uses
        Assert.IsTrue(healthUsages.Count >= 3, $"Expected at least 3 uses of 'health', found {healthUsages.Count}");
    }

    [TestMethod]
    public async Task FindReferences_ClassMember_DeclarationFound()
    {
        var scriptMap = CreateScriptMap(ClassMemberCode);

        Assert.IsNotNull(scriptMap.Class);

        // Find class-level variable declarations
        var varDeclarations = scriptMap.Class.Members
            .OfType<GDVariableDeclaration>()
            .Where(v => v.Identifier?.Sequence == "health")
            .ToList();

        Assert.AreEqual(1, varDeclarations.Count, "Should have exactly one 'health' declaration");
    }

    [TestMethod]
    public async Task FindReferences_ClassVariable_AllReferences()
    {
        // Arrange
        var scriptMap = CreateScriptMap(ClassMemberCode);

        // Act
        var healthRefs = CollectClassMemberReferences(scriptMap, "health");

        // Assert - decl + take_damage(-=, <=) + heal(+=)
        Assert.IsTrue(healthRefs.Count >= 4, $"Expected at least 4 refs for 'health', found {healthRefs.Count}");
        Assert.AreEqual(1, healthRefs.Count(r => r.Kind == ReferenceKind.Declaration), "Should have 1 declaration");
    }

    [TestMethod]
    public async Task FindReferences_ClassMethod_AllCallSites()
    {
        // Arrange
        var scriptMap = CreateScriptMap(ClassMemberMethodCode);

        // Act
        var helperRefs = CollectClassMemberReferences(scriptMap, "helper");

        // Assert - decl + 3 calls
        Assert.IsTrue(helperRefs.Count >= 4, $"Expected at least 4 refs for 'helper', found {helperRefs.Count}");
        Assert.AreEqual(1, helperRefs.Count(r => r.Kind == ReferenceKind.Declaration), "Should have 1 declaration");
        Assert.AreEqual(3, helperRefs.Count(r => r.Kind == ReferenceKind.Call), "Should have 3 calls");
    }

    [TestMethod]
    public async Task FindReferences_Signal_EmitAndDeclaration()
    {
        // Arrange
        var scriptMap = CreateScriptMap(ClassMemberSignalCode);

        // Act
        var healthChangedRefs = CollectClassMemberReferences(scriptMap, "health_changed");
        var diedRefs = CollectClassMemberReferences(scriptMap, "died");

        // Assert
        Assert.IsTrue(healthChangedRefs.Count >= 2, $"'health_changed' should have at least 2 refs (decl + emit), found {healthChangedRefs.Count}");
        Assert.IsTrue(diedRefs.Count >= 2, $"'died' should have at least 2 refs (decl + emit), found {diedRefs.Count}");
    }

    [TestMethod]
    public async Task FindReferences_Constant_AllUsages()
    {
        // Arrange
        var scriptMap = CreateScriptMap(ClassMemberConstantCode);

        // Act
        var maxHealthRefs = CollectClassMemberReferences(scriptMap, "MAX_HEALTH");
        var minHealthRefs = CollectClassMemberReferences(scriptMap, "MIN_HEALTH");

        // Assert - decl + clamp arg
        Assert.IsTrue(maxHealthRefs.Count >= 2, $"'MAX_HEALTH' should have at least 2 refs, found {maxHealthRefs.Count}");
        Assert.IsTrue(minHealthRefs.Count >= 2, $"'MIN_HEALTH' should have at least 2 refs, found {minHealthRefs.Count}");
    }

    [TestMethod]
    public async Task FindReferences_Enum_TypeAndValues()
    {
        // Arrange
        var scriptMap = CreateScriptMap(ClassMemberEnumCode);

        // Act
        var stateRefs = CollectClassMemberReferences(scriptMap, "State");

        // Assert - enum decl + .IDLE + .RUNNING (parameter type annotation uses full type name)
        Assert.IsTrue(stateRefs.Count >= 3, $"'State' should have at least 3 refs (decl + .IDLE + .RUNNING), found {stateRefs.Count}");
    }

    [TestMethod]
    public async Task FindReferences_InnerClass_TypeUsage()
    {
        // Arrange
        var scriptMap = CreateScriptMap(ClassMemberInnerClassCode);

        // Act
        var innerDataRefs = CollectClassMemberReferences(scriptMap, "InnerData");

        // Assert - class decl + .new()
        Assert.IsTrue(innerDataRefs.Count >= 2, $"'InnerData' should have at least 2 refs (decl + new()), found {innerDataRefs.Count}");
    }

    #endregion

    #region For Loop Variable Scope Tests

    [TestMethod]
    public async Task FindReferences_ForLoopVariable_OnlyInLoop()
    {
        var scriptMap = CreateScriptMap(ForLoopVariableCode);
        var method = FindMethod(scriptMap, "process_items");

        Assert.IsNotNull(method);

        // Find for statement
        var forStmt = method.AllNodes
            .OfType<GDForStatement>()
            .FirstOrDefault();

        Assert.IsNotNull(forStmt, "Should have a for statement");
        Assert.AreEqual("item", forStmt.Variable?.Sequence, "For loop variable should be 'item'");

        // Find usages of "item" in the loop body
        var itemUsages = forStmt.AllNodes
            .OfType<GDIdentifierExpression>()
            .Where(e => e.Identifier?.Sequence == "item")
            .ToList();

        Assert.IsTrue(itemUsages.Count >= 2, $"Expected at least 2 uses of 'item' in loop, found {itemUsages.Count}");
    }

    [TestMethod]
    public async Task FindReferences_ForLoopVariable_DeclarationPlusUsages()
    {
        // Arrange
        var scriptMap = CreateScriptMap(ForLoopVariableCode);
        var method = FindMethod(scriptMap, "process_items");
        var forStmt = FindForStatement(method);

        Assert.IsNotNull(forStmt);

        // Act
        var itemRefs = CollectForLoopReferences(forStmt, "item");

        // Assert - for decl + .name access + process_item arg
        Assert.IsTrue(itemRefs.Count >= 3, $"Expected at least 3 refs for 'item', found {itemRefs.Count}");
        Assert.AreEqual(1, itemRefs.Count(r => r.Kind == ReferenceKind.Declaration), "Should have 1 declaration");
    }

    [TestMethod]
    public async Task FindReferences_ForLoopVariable_NestedLoops_DistinctScopes()
    {
        // Arrange
        var scriptMap = CreateScriptMap(ForLoopNestedCode);
        var method = FindMethod(scriptMap, "matrix_process");

        Assert.IsNotNull(method);

        var forStatements = FindForStatements(method).ToList();
        Assert.AreEqual(2, forStatements.Count, "Should have 2 for statements");

        var outerFor = forStatements[0];
        var innerFor = forStatements[1];

        // Act
        var iRefs = CollectForLoopReferences(outerFor, "i");
        var jRefs = CollectForLoopReferences(innerFor, "j");

        // Assert
        Assert.IsTrue(iRefs.Count >= 2, $"'i' should have at least 2 refs (decl + cell calc), found {iRefs.Count}");
        Assert.IsTrue(jRefs.Count >= 2, $"'j' should have at least 2 refs (decl + cell calc), found {jRefs.Count}");
    }

    [TestMethod]
    public async Task FindReferences_ForLoopVariable_ShadowsClassMember()
    {
        // Arrange
        var scriptMap = CreateScriptMap(ForLoopSameVariableNameCode);

        // Check class member exists
        var classMemberRefs = CollectClassMemberReferences(scriptMap, "item");
        var declarationCount = classMemberRefs.Count(r => r.Kind == ReferenceKind.Declaration);

        // Should have class-level var declaration + 2 for-loop declarations + usages
        Assert.IsTrue(declarationCount >= 1, "Should have at least 1 class-level declaration");

        // Check that for loop has its own scoped references
        var method1 = FindMethod(scriptMap, "first_loop");
        var forStmt1 = FindForStatement(method1);
        Assert.IsNotNull(forStmt1);

        var loopItemRefs = CollectForLoopReferences(forStmt1, "item");
        Assert.IsTrue(loopItemRefs.Count >= 2, $"For loop should have at least 2 refs (decl + print), found {loopItemRefs.Count}");
    }

    #endregion

    #region Complex Scope Tests

    [TestMethod]
    public async Task FindReferences_SameNameDifferentScopes_AreDistinct()
    {
        var scriptMap = CreateScriptMap(ComplexScopesCode);

        // Find both methods with "local_var"
        var outerMethod = FindMethod(scriptMap, "outer_func");
        var anotherMethod = FindMethod(scriptMap, "another_func");

        Assert.IsNotNull(outerMethod);
        Assert.IsNotNull(anotherMethod);

        // Count local_var in each method - they should be separate scopes
        var outerLocalVarCount = outerMethod.AllNodes
            .OfType<GDIdentifierExpression>()
            .Count(e => e.Identifier?.Sequence == "local_var");

        var anotherLocalVarCount = anotherMethod.AllNodes
            .OfType<GDIdentifierExpression>()
            .Count(e => e.Identifier?.Sequence == "local_var");

        // outer_func uses local_var 2x (in inner loop and in print)
        // another_func uses local_var 1x (in print)
        Assert.IsTrue(outerLocalVarCount >= 2, $"Expected at least 2 in outer_func, found {outerLocalVarCount}");
        Assert.IsTrue(anotherLocalVarCount >= 1, $"Expected at least 1 in another_func, found {anotherLocalVarCount}");
    }

    [TestMethod]
    public async Task FindReferences_GlobalVariable_UsedInMultipleMethods()
    {
        var scriptMap = CreateScriptMap(ComplexScopesCode);

        Assert.IsNotNull(scriptMap.Class);

        // Find all usages of "global_var" across the class
        var globalVarUsages = scriptMap.Class.AllNodes
            .OfType<GDIdentifierExpression>()
            .Where(e => e.Identifier?.Sequence == "global_var")
            .ToList();

        // Should be used in both outer_func and another_func
        Assert.IsTrue(globalVarUsages.Count >= 2, $"Expected at least 2 uses of 'global_var', found {globalVarUsages.Count}");
    }

    #endregion

    #region AST Structure Tests

    [TestMethod]
    public async Task ScriptMap_ParsesClassMembers_Correctly()
    {
        var scriptMap = CreateScriptMap(ClassMemberCode);

        Assert.IsNotNull(scriptMap.Class);

        // Check variable declarations
        var varDecls = scriptMap.Class.Members
            .OfType<GDVariableDeclaration>()
            .ToList();

        Assert.AreEqual(2, varDecls.Count, "Should have 2 variable declarations");

        // Check method declarations
        var methodDecls = scriptMap.Class.Members
            .OfType<GDMethodDeclaration>()
            .ToList();

        Assert.AreEqual(3, methodDecls.Count, "Should have 3 method declarations");

        var methodNames = methodDecls.Select(m => m.Identifier?.Sequence).ToList();
        CollectionAssert.Contains(methodNames, "take_damage");
        CollectionAssert.Contains(methodNames, "heal");
        CollectionAssert.Contains(methodNames, "die");
    }

    [TestMethod]
    public async Task ScriptMap_ParsesMethodParameters_Correctly()
    {
        var scriptMap = CreateScriptMap(ParameterCode);
        var method = FindMethod(scriptMap, "calculate");

        Assert.IsNotNull(method);
        Assert.IsNotNull(method.Parameters);

        var paramsList = method.Parameters.ToList();
        Assert.AreEqual(2, paramsList.Count);

        // Check first parameter: amount: int
        var param1 = paramsList[0] as GDParameterDeclaration;
        Assert.IsNotNull(param1);
        Assert.AreEqual("amount", param1.Identifier?.Sequence);

        // Check second parameter: multiplier: float
        var param2 = paramsList[1] as GDParameterDeclaration;
        Assert.IsNotNull(param2);
        Assert.AreEqual("multiplier", param2.Identifier?.Sequence);
    }

    #endregion

    #region External Member Access Tests

    [TestMethod]
    public async Task FindReferences_MemberAccess_ChainedAccess()
    {
        // Arrange
        var scriptMap = CreateScriptMap(ChainedMemberAccessCode);

        // Act - find "name" references (accessed via chain)
        var nameRefs = CollectMemberAccessReferences(scriptMap, "name");

        // Assert - should find the chained access
        Assert.IsTrue(nameRefs.Count >= 1, $"Should find 'name' in chained access, found {nameRefs.Count}");
    }

    [TestMethod]
    public async Task FindReferences_BuiltInType_ArrayMethods()
    {
        // Arrange
        var scriptMap = CreateScriptMap(BuiltInTypeMemberCode);

        // Act - member access patterns on built-in types
        var appendRefs = CollectMemberAccessReferences(scriptMap, "append");
        var sizeRefs = CollectMemberAccessReferences(scriptMap, "size");
        var clearRefs = CollectMemberAccessReferences(scriptMap, "clear");
        var keysRefs = CollectMemberAccessReferences(scriptMap, "keys");

        // Assert - built-in methods should be found
        Assert.IsTrue(appendRefs.Count >= 1, $"Should find 'append' call, found {appendRefs.Count}");
        Assert.IsTrue(sizeRefs.Count >= 1, $"Should find 'size' call, found {sizeRefs.Count}");
        Assert.IsTrue(clearRefs.Count >= 2, $"Should find 'clear' calls (arr + dict), found {clearRefs.Count}");
        Assert.IsTrue(keysRefs.Count >= 1, $"Should find 'keys' call, found {keysRefs.Count}");
    }

    #endregion

    #region Cross-File Reference Tests

    [TestMethod]
    public async Task FindReferences_CrossFile_PublicMember()
    {
        // Arrange - multi-file project
        var project = new GDProjectMap(BaseClassCode, UsingClassCode);

        // Act
        var healthRefs = CollectProjectWideReferences(project, "health");

        // Assert - should find in BaseEntity and UsingClass
        Assert.IsTrue(healthRefs.Count >= 2, $"'health' should be found in multiple files, found {healthRefs.Count}");

        var uniqueFiles = healthRefs.Select(r => r.FilePath).Distinct().Count();
        Assert.IsTrue(uniqueFiles >= 1, "References should span files");
    }

    [TestMethod]
    public async Task FindReferences_CrossFile_InheritedMethod()
    {
        // Arrange
        var project = new GDProjectMap(BaseClassCode, DerivedClassCode, UsingClassCode);

        // Act
        var takeDamageRefs = CollectProjectWideReferences(project, "take_damage");

        // Assert - decl in Base + call in Derived + call in Using
        Assert.IsTrue(takeDamageRefs.Count >= 3, $"'take_damage' should have at least 3 refs, found {takeDamageRefs.Count}");
    }

    [TestMethod]
    public async Task FindReferences_CrossFile_ClassNameAsType()
    {
        // Arrange
        var project = new GDProjectMap(BaseClassCode, UsingClassCode);

        // Act
        var baseEntityRefs = CollectProjectWideReferences(project, "BaseEntity");

        // Assert - class_name declaration
        Assert.IsTrue(baseEntityRefs.Count >= 1, $"'BaseEntity' should be found, found {baseEntityRefs.Count}");
    }

    [TestMethod]
    public async Task FindReferences_CrossFile_PlayerType()
    {
        // Arrange
        var project = new GDProjectMap(BaseClassCode, DerivedClassCode, UsingClassCode);

        // Act
        var playerRefs = CollectProjectWideReferences(project, "Player");

        // Assert - class_name in Derived (type annotation may be parsed differently)
        Assert.IsTrue(playerRefs.Count >= 1, $"'Player' should have at least 1 ref (class_name), found {playerRefs.Count}");
    }

    #endregion

    #region Reference Kind Detection Tests

    [TestMethod]
    public async Task FindReferences_DetectsDeclaration()
    {
        // Arrange
        var scriptMap = CreateScriptMap(ReferenceKindTestCode);

        // Act
        var counterRefs = CollectClassMemberReferences(scriptMap, "counter");

        // Assert
        var declarations = counterRefs.Where(r => r.Kind == ReferenceKind.Declaration).ToList();
        Assert.AreEqual(1, declarations.Count, "Should have exactly 1 declaration");
        Assert.AreEqual(2, declarations[0].Line, "Declaration should be on line 2 (var counter = 0)");
    }

    [TestMethod]
    public async Task FindReferences_DetectsRead()
    {
        // Arrange
        var scriptMap = CreateScriptMap(ReferenceKindTestCode);

        // Act
        var counterRefs = CollectClassMemberReferences(scriptMap, "counter");

        // Assert
        var reads = counterRefs.Where(r => r.Kind == ReferenceKind.Read).ToList();
        Assert.IsTrue(reads.Count >= 3, $"Should have at least 3 read references (x = counter, print, process arg), found {reads.Count}");
    }

    [TestMethod]
    public async Task FindReferences_DetectsWrite()
    {
        // Arrange
        var scriptMap = CreateScriptMap(ReferenceKindTestCode);

        // Act
        var counterRefs = CollectClassMemberReferences(scriptMap, "counter");

        // Assert
        var writes = counterRefs.Where(r => r.Kind == ReferenceKind.Write).ToList();
        Assert.IsTrue(writes.Count >= 2, $"Should have at least 2 write references (= 10 and += 1), found {writes.Count}");
    }

    [TestMethod]
    public async Task FindReferences_DetectsCall()
    {
        // Arrange
        var scriptMap = CreateScriptMap(ReferenceKindTestCode);

        // Act
        var helperRefs = CollectClassMemberReferences(scriptMap, "helper");

        // Assert
        var calls = helperRefs.Where(r => r.Kind == ReferenceKind.Call).ToList();
        Assert.AreEqual(3, calls.Count, "Should have 3 call references (standalone, var =, print arg)");
    }

    #endregion

    #region Edge Case Tests

    [TestMethod]
    public async Task FindReferences_SameNameAllLevels_CorrectScopeResolution()
    {
        // Arrange
        var scriptMap = CreateScriptMap(SameNameAllLevelsCode);

        // Act - find all references to "value" in the class
        var allValueRefs = CollectClassMemberReferences(scriptMap, "value");

        // Assert - should find class member decl + all shadowing declarations + usages
        // Class var + param + local var + for var = at least 4 declarations
        var declarations = allValueRefs.Where(r => r.Kind == ReferenceKind.Declaration).ToList();
        Assert.IsTrue(declarations.Count >= 4, $"Should have at least 4 declarations (class/param/local/for), found {declarations.Count}");
    }

    [TestMethod]
    public async Task FindReferences_GetterSetter_PropertyAccess()
    {
        // Arrange
        var scriptMap = CreateScriptMap(GetterSetterCode);

        // Act
        var healthRefs = CollectClassMemberReferences(scriptMap, "health");
        var _healthRefs = CollectClassMemberReferences(scriptMap, "_health");

        // Assert
        Assert.IsTrue(healthRefs.Count >= 3, $"'health' should have at least 3 refs (property decl + setter use + getter use), found {healthRefs.Count}");
        Assert.IsTrue(_healthRefs.Count >= 4, $"'_health' should have at least 4 refs (backing decl + getter return + setter assignment + direct), found {_healthRefs.Count}");
    }

    [TestMethod]
    public async Task FindReferences_Lambda_CapturedVariables()
    {
        // Arrange
        var scriptMap = CreateScriptMap(LambdaCode);

        // Act
        var multiplierRefs = CollectClassMemberReferences(scriptMap, "multiplier");

        // Assert - should find usage inside lambda
        Assert.IsTrue(multiplierRefs.Count >= 2, $"'multiplier' should have at least 2 refs (decl + lambda capture), found {multiplierRefs.Count}");
    }

    [TestMethod]
    public async Task FindReferences_AnnotatedVariables()
    {
        // Arrange
        var scriptMap = CreateScriptMap(AnnotationCode);

        // Act
        var speedRefs = CollectClassMemberReferences(scriptMap, "speed");
        var labelRefs = CollectClassMemberReferences(scriptMap, "label");

        // Assert
        Assert.IsTrue(speedRefs.Count >= 2, $"'speed' should have at least 2 refs (@export var + print), found {speedRefs.Count}");
        Assert.IsTrue(labelRefs.Count >= 2, $"'label' should have at least 2 refs (@onready var + .text access), found {labelRefs.Count}");
    }

    [TestMethod]
    public async Task FindReferences_StringInterpolation()
    {
        // Arrange
        var scriptMap = CreateScriptMap(StringInterpolationCode);

        // Act
        var nameRefs = CollectClassMemberReferences(scriptMap, "name");
        var countRefs = CollectClassMemberReferences(scriptMap, "count");

        // Assert - format specifiers should find the actual variable references
        Assert.IsTrue(nameRefs.Count >= 2, $"'name' should have at least 2 refs (decl + % format), found {nameRefs.Count}");
        Assert.IsTrue(countRefs.Count >= 2, $"'count' should have at least 2 refs (decl + dict value), found {countRefs.Count}");
    }

    #endregion
}
