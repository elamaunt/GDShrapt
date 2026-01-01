using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests that verify parsed nodes are in correct parent containers.
    /// These tests catch bugs where code parses without errors but nodes
    /// end up in wrong locations in the syntax tree.
    /// </summary>
    [TestClass]
    public class StructureValidationTests
    {
        #region Function Structure

        [TestMethod]
        public void Structure_TwoFunctions_BothAtClassLevel()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	pass


func second():
	pass";

            var declaration = reader.ParseFileContent(code);

            // Both functions should be direct children of class
            declaration.Methods.Count().Should().Be(2, "both functions should be at class level");

            var first = declaration.Methods.ElementAt(0);
            var second = declaration.Methods.ElementAt(1);

            first.Identifier?.ToString().Should().Be("first");
            second.Identifier?.ToString().Should().Be("second");

            // Functions should not be nested
            first.AllNodes.OfType<GDMethodDeclaration>().Should().BeEmpty("first should not contain nested functions");
            second.AllNodes.OfType<GDMethodDeclaration>().Should().BeEmpty("second should not contain nested functions");
        }

        [TestMethod]
        public void Structure_ThreeFunctions_AllAtClassLevel()
        {
            var reader = new GDScriptReader();

            var code = @"func a():
	pass


func b():
	pass


func c():
	pass";

            var declaration = reader.ParseFileContent(code);

            declaration.Methods.Count().Should().Be(3, "all three functions should be at class level");

            var names = declaration.Methods.Select(m => m.Identifier?.ToString()).ToList();
            names.Should().ContainInOrder("a", "b", "c");
        }

        [TestMethod]
        public void Structure_FunctionStatements_InsideFunction()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	var x = 1
	print(x)
	return x";

            var declaration = reader.ParseFileContent(code);

            var method = declaration.Methods.First();
            method.Statements.Should().NotBeNull();
            method.Statements.Count.Should().Be(3, "all statements should be inside function");

            // Verify statement types
            method.Statements[0].Should().BeOfType<GDVariableDeclarationStatement>();
            method.Statements[1].Should().BeOfType<GDExpressionStatement>();
            method.Statements[2].Should().BeOfType<GDExpressionStatement>();
        }

        [TestMethod]
        public void Structure_NestedBlocks_CorrectParent()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	if true:
		print(1)
	print(2)";

            var declaration = reader.ParseFileContent(code);

            var method = declaration.Methods.First();
            method.Statements.Count.Should().Be(2, "if and print should be at function level");

            var ifStmt = method.Statements[0] as GDIfStatement;
            ifStmt.Should().NotBeNull();
            ifStmt.IfBranch.Statements.Count.Should().Be(1, "print(1) should be inside if block");
        }

        #endregion

        #region Variable Structure

        [TestMethod]
        public void Structure_ClassVariable_AtClassLevel()
        {
            var reader = new GDScriptReader();

            var code = @"var my_var = 10


func test():
	pass";

            var declaration = reader.ParseFileContent(code);

            declaration.Variables.Count().Should().Be(1, "class variable should be at class level");
            declaration.Methods.Count().Should().Be(1);

            // Variable should not be inside function
            var method = declaration.Methods.First();
            method.AllNodes.OfType<GDVariableDeclaration>().Should().BeEmpty();
        }

        [TestMethod]
        public void Structure_LocalVariable_InsideFunction()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	var local_var = 10
	print(local_var)";

            var declaration = reader.ParseFileContent(code);

            // No class-level variables
            declaration.Variables.Should().BeEmpty("local var should not be at class level");

            // Variable should be inside function as statement
            var method = declaration.Methods.First();
            var varStmt = method.Statements[0] as GDVariableDeclarationStatement;
            varStmt.Should().NotBeNull();
        }

        [TestMethod]
        public void Structure_MultipleClassVariables_AllAtClassLevel()
        {
            var reader = new GDScriptReader();

            var code = @"var a = 1
var b = 2
var c = 3


func test():
	pass";

            var declaration = reader.ParseFileContent(code);

            declaration.Variables.Count().Should().Be(3, "all variables should be at class level");
            declaration.Methods.Count().Should().Be(1);
        }

        #endregion

        #region Signal Structure

        [TestMethod]
        public void Structure_Signal_AtClassLevel()
        {
            var reader = new GDScriptReader();

            var code = @"signal my_signal


func test():
	pass";

            var declaration = reader.ParseFileContent(code);

            declaration.Signals.Count().Should().Be(1, "signal should be at class level");
            declaration.Methods.Count().Should().Be(1);
        }

        [TestMethod]
        public void Structure_SignalWithParams_AtClassLevel()
        {
            var reader = new GDScriptReader();

            var code = @"signal my_signal(value, data)


func test():
	pass";

            var declaration = reader.ParseFileContent(code);

            declaration.Signals.Count().Should().Be(1);
            var signal = declaration.Signals.First();
            signal.Parameters.Should().NotBeNull();
        }

        #endregion

        #region Enum Structure

        [TestMethod]
        public void Structure_Enum_AtClassLevel()
        {
            var reader = new GDScriptReader();

            var code = @"enum State { IDLE, RUNNING, STOPPED }


func test():
	pass";

            var declaration = reader.ParseFileContent(code);

            declaration.Enums.Count().Should().Be(1, "enum should be at class level");
            declaration.Methods.Count().Should().Be(1);
        }

        [TestMethod]
        public void Structure_EnumValues_InsideEnum()
        {
            var reader = new GDScriptReader();

            var code = @"enum State { IDLE, RUNNING, STOPPED }";

            var declaration = reader.ParseFileContent(code);

            var enumDecl = declaration.Enums.First();
            enumDecl.Values.Count.Should().Be(3);
        }

        #endregion

        #region Const Structure

        [TestMethod]
        public void Structure_Const_AtClassLevel()
        {
            var reader = new GDScriptReader();

            var code = @"const MAX_VALUE = 100


func test():
	pass";

            var declaration = reader.ParseFileContent(code);

            // Constants are parsed as variables with const modifier
            declaration.Variables.Count().Should().Be(1, "const should be at class level as variable");
            declaration.Methods.Count().Should().Be(1);

            var constVar = declaration.Variables.First();
            constVar.IsConstant.Should().BeTrue();
        }

        #endregion

        #region If Statement Structure

        [TestMethod]
        public void Structure_IfElse_CorrectBranches()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	if a:
		print(1)
	elif b:
		print(2)
	else:
		print(3)";

            var declaration = reader.ParseFileContent(code);

            var method = declaration.Methods.First();
            method.Statements.Count.Should().Be(1, "if-elif-else is one statement");

            var ifStmt = method.Statements[0] as GDIfStatement;
            ifStmt.Should().NotBeNull();

            ifStmt.IfBranch.Statements.Count.Should().Be(1);
            ifStmt.ElifBranchesList.Count.Should().Be(1);
            ifStmt.ElseBranch.Should().NotBeNull();
            ifStmt.ElseBranch.Statements.Count.Should().Be(1);
        }

        [TestMethod]
        public void Structure_NestedIf_CorrectNesting()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	if a:
		if b:
			print(1)";

            var declaration = reader.ParseFileContent(code);

            var method = declaration.Methods.First();
            method.Statements.Count.Should().Be(1, "outer if is one statement");

            var outerIf = method.Statements[0] as GDIfStatement;
            outerIf.IfBranch.Statements.Count.Should().Be(1, "inner if is one statement inside outer");

            var innerIf = outerIf.IfBranch.Statements[0] as GDIfStatement;
            innerIf.Should().NotBeNull();
            innerIf.IfBranch.Statements.Count.Should().Be(1);
        }

        #endregion

        #region For Loop Structure

        [TestMethod]
        public void Structure_ForLoop_BodyInsideLoop()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	for i in range(10):
		print(i)
	print(""done"")";

            var declaration = reader.ParseFileContent(code);

            var method = declaration.Methods.First();
            method.Statements.Count.Should().Be(2, "for and print should be at function level");

            var forStmt = method.Statements[0] as GDForStatement;
            forStmt.Should().NotBeNull();
            forStmt.Statements.Count.Should().Be(1, "print(i) should be inside for loop");
        }

        [TestMethod]
        public void Structure_NestedForLoops()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	for i in range(3):
		for j in range(3):
			print(i, j)";

            var declaration = reader.ParseFileContent(code);

            var method = declaration.Methods.First();
            var outerFor = method.Statements[0] as GDForStatement;
            outerFor.Statements.Count.Should().Be(1);

            var innerFor = outerFor.Statements[0] as GDForStatement;
            innerFor.Should().NotBeNull();
            innerFor.Statements.Count.Should().Be(1);
        }

        #endregion

        #region While Loop Structure

        [TestMethod]
        public void Structure_WhileLoop_BodyInsideLoop()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	while true:
		print(1)
		break
	print(""done"")";

            var declaration = reader.ParseFileContent(code);

            var method = declaration.Methods.First();
            method.Statements.Count.Should().Be(2);

            var whileStmt = method.Statements[0] as GDWhileStatement;
            whileStmt.Should().NotBeNull();
            whileStmt.Statements.Count.Should().Be(2, "print and break should be inside while");
        }

        #endregion

        #region Match Statement Structure

        [TestMethod]
        public void Structure_Match_CasesInsideMatch()
        {
            var reader = new GDScriptReader();

            var code = @"func test(value):
	match value:
		1:
			print(""one"")
		2:
			print(""two"")
		_:
			print(""other"")";

            var declaration = reader.ParseFileContent(code);

            var method = declaration.Methods.First();
            method.Statements.Count.Should().Be(1, "match is one statement");

            var matchStmt = method.Statements[0] as GDMatchStatement;
            matchStmt.Should().NotBeNull();
            matchStmt.Cases.Count.Should().Be(3, "all cases should be inside match");
        }

        #endregion

        #region Lambda Structure

        [TestMethod]
        public void Structure_InlineLambda_AsExpression()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	var f := func(): pass
	f.call()";

            var declaration = reader.ParseFileContent(code);

            var method = declaration.Methods.First();
            method.Statements.Count.Should().Be(2);

            // Lambda should not create a separate method at class level
            declaration.Methods.Count().Should().Be(1, "lambda should not be at class level");
        }

        [TestMethod]
        public void Structure_MultilineLambda_AsExpression()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	var f := func(x):
		print(x)
	f.call(1)";

            var declaration = reader.ParseFileContent(code);

            // Only one method at class level
            declaration.Methods.Count().Should().Be(1, "lambda should not be at class level");

            var method = declaration.Methods.First();
            method.Statements.Count.Should().Be(2);
        }

        [TestMethod]
        public void Structure_LambdaInArray_CorrectParent()
        {
            var reader = new GDScriptReader();

            var code = @"var operations = [
	func(x): return x + 1,
	func(x): return x * 2,
]";

            var declaration = reader.ParseFileContent(code);

            // Array with lambdas should be a variable at class level
            declaration.Variables.Count().Should().Be(1);
            declaration.Methods.Should().BeEmpty("lambdas in array should not be methods");
        }

        [TestMethod]
        public void Structure_LambdaFollowedByFunction_Separate()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	var f := func(x):
		print(x)
	f.call(1)


func second():
	pass";

            var declaration = reader.ParseFileContent(code);

            declaration.Methods.Count().Should().Be(2, "lambda should not consume next function");

            var first = declaration.Methods.ElementAt(0);
            var second = declaration.Methods.ElementAt(1);

            first.Identifier?.ToString().Should().Be("first");
            second.Identifier?.ToString().Should().Be("second");

            // Second function should have its own statements, not inherit from first
            second.Statements.Count.Should().Be(1);
            second.Statements[0].Should().BeOfType<GDExpressionStatement>();
        }

        #endregion

        #region Inner Class Structure

        [TestMethod]
        public void Structure_InnerClass_AtClassLevel()
        {
            var reader = new GDScriptReader();

            var code = @"class Inner:
	var value = 0

	func get_value():
		return value


func test():
	pass";

            var declaration = reader.ParseFileContent(code);

            declaration.InnerClasses.Count().Should().Be(1, "inner class should be at class level");
            declaration.Methods.Count().Should().Be(1, "test function should be at class level");

            var inner = declaration.InnerClasses.First();
            inner.Variables.Count().Should().Be(1);
            inner.Methods.Count().Should().Be(1);
        }

        #endregion

        #region Mixed Members Order

        [TestMethod]
        public void Structure_MixedMembers_CorrectOrder()
        {
            var reader = new GDScriptReader();

            var code = @"extends Node
class_name MyClass

signal my_signal

const MAX = 100

enum State { IDLE, RUNNING }

var my_var = 0


func _ready():
	pass


func _process(delta):
	pass";

            var declaration = reader.ParseFileContent(code);

            declaration.Signals.Count().Should().Be(1);
            declaration.Enums.Count().Should().Be(1);
            // Variables includes both const and var
            declaration.Variables.Count().Should().Be(2, "should have const and var");
            declaration.Methods.Count().Should().Be(2);

            // Verify methods are not nested
            foreach (var method in declaration.Methods)
            {
                method.AllNodes.OfType<GDMethodDeclaration>().Should().BeEmpty(
                    $"method {method.Identifier} should not contain nested methods");
            }
        }

        #endregion

        #region Export/Onready Structure

        [TestMethod]
        public void Structure_ExportVariable_AtClassLevel()
        {
            var reader = new GDScriptReader();

            var code = @"@export var speed: float = 10.0


func test():
	pass";

            var declaration = reader.ParseFileContent(code);

            declaration.Variables.Count().Should().Be(1);
            declaration.Methods.Count().Should().Be(1);
        }

        [TestMethod]
        public void Structure_OnreadyVariable_AtClassLevel()
        {
            var reader = new GDScriptReader();

            var code = @"@onready var label = $Label


func test():
	pass";

            var declaration = reader.ParseFileContent(code);

            declaration.Variables.Count().Should().Be(1);
            declaration.Methods.Count().Should().Be(1);
        }

        #endregion
    }
}
