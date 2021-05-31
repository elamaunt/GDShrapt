using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    [TestClass]
    public class ParsingTests
    {
        [TestMethod]
        public void ParseClassTest()
        {
            var reader = new GDScriptReader();

            var code = @"
tool
class_name HTerrainDataSaver
extends ResourceFormatSaver

const HTerrainData = preload(""./ hterrain_data.gd"")


func get_recognized_extensions(res):
	if res != null and res is HTerrainData:
		return PoolStringArray([HTerrainData.META_EXTENSION])
	return PoolStringArray()


func recognize(res):
	return res is HTerrainData


func save(path, resource, flags):
	resource.save_data(path.get_base_dir())
";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);
            Assert.AreEqual("ResourceFormatSaver", @class.ExtendsClass?.Sequence);
            Assert.AreEqual("HTerrainDataSaver", @class.Name?.Sequence);
            Assert.AreEqual(true, @class.IsTool);

            Assert.AreEqual(7, @class.Members.Count);
            Assert.AreEqual(3, @class.Methods.Count());
            Assert.AreEqual(2, @class.Methods.ElementAt(0).Statements.Count);
            Assert.AreEqual(1, @class.Methods.ElementAt(1).Statements.Count);
            Assert.AreEqual(1, @class.Methods.ElementAt(2).Statements.Count);
        }

        [TestMethod]
        public void ParseLogicalExpressionTest()
        {
            var reader = new GDScriptReader();

            var code = @"a > b and c > d";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExression));

            var @dualOperator = (GDDualOperatorExression)expression;
            Assert.AreEqual(GDDualOperatorType.And, @dualOperator.OperatorType);

            var leftExpression = @dualOperator.LeftExpression;

            Assert.IsNotNull(leftExpression);
            Assert.IsInstanceOfType(leftExpression, typeof(GDDualOperatorExression));
            
            var rightExpression = @dualOperator.RightExpression;

            Assert.IsNotNull(rightExpression);
            Assert.IsInstanceOfType(rightExpression, typeof(GDDualOperatorExression));

            var @leftDualOperator = (GDDualOperatorExression)leftExpression;

            Assert.IsInstanceOfType(@leftDualOperator.LeftExpression, typeof(GDIdentifierExpression));
            Assert.IsNotNull(@leftDualOperator.LeftExpression);
            Assert.IsInstanceOfType(@leftDualOperator.RightExpression, typeof(GDIdentifierExpression));
            Assert.IsNotNull(@leftDualOperator.RightExpression);

            Assert.AreEqual("a", ((GDIdentifierExpression)@leftDualOperator.LeftExpression).Identifier.Sequence);
            Assert.AreEqual("b", ((GDIdentifierExpression)@leftDualOperator.RightExpression).Identifier.Sequence);

            var @rightDualOperator = (GDDualOperatorExression)rightExpression;

            Assert.IsInstanceOfType(@rightDualOperator.LeftExpression, typeof(GDIdentifierExpression));
            Assert.IsNotNull(@rightDualOperator.LeftExpression);
            Assert.IsInstanceOfType(@rightDualOperator.RightExpression, typeof(GDIdentifierExpression));
            Assert.IsNotNull(@rightDualOperator.RightExpression);

            Assert.AreEqual("c", ((GDIdentifierExpression)@rightDualOperator.LeftExpression).Identifier.Sequence);
            Assert.AreEqual("d", ((GDIdentifierExpression)@rightDualOperator.RightExpression).Identifier.Sequence);
        }

        [TestMethod]
        public void ExpressionsPriorityTest()
        {
            var reader = new GDScriptReader();

            var code = @"a > b > c = d = e > f > g";

            var expression = reader.ParseExpression(code);

            var printedTree = expression.ToString();

            Assert.IsNotNull(expression);
            Assert.AreEqual(code, printedTree);

            Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExression));

            var @dualOperator = (GDDualOperatorExression)expression;

            Assert.AreEqual(GDDualOperatorType.Assignment, @dualOperator.OperatorType);
            Assert.AreEqual("a > b > c", @dualOperator.LeftExpression.ToString());
        }

        [TestMethod]
        public void IfStatementTest()
        {
            var reader = new GDScriptReader();

            var code = @"
if a != null and a is A:
	return";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDIfStatement));

            var ifStatement = (GDIfStatement)statement;

            Assert.AreEqual("a != null and a is A", ifStatement.Condition.ToString());
            Assert.AreEqual(1, ifStatement.TrueStatements.Count);
            Assert.AreEqual(0, ifStatement.FalseStatements.Count);

            Assert.IsInstanceOfType(ifStatement.TrueStatements[0], typeof(GDReturnStatement));
        }

        [TestMethod]
        public void IfElseStatementTest()
        {
            var reader = new GDScriptReader();

            var code = @"
if a != null || a is A:
	return
else:
	a = b";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDIfStatement));

            var ifStatement = (GDIfStatement)statement;

            Assert.AreEqual(1, ifStatement.TrueStatements.Count);
            Assert.AreEqual(1, ifStatement.FalseStatements.Count);

            Assert.IsInstanceOfType(ifStatement.TrueStatements[0], typeof(GDReturnStatement));
            Assert.IsInstanceOfType(ifStatement.FalseStatements[0], typeof(GDExpressionStatement));
        }

        [TestMethod]
        public void IfStatementTest2()
        {
            var reader = new GDScriptReader();

            var code = @"
if a: return";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDIfStatement));

            var ifStatement = (GDIfStatement)statement;

            Assert.AreEqual(1, ifStatement.TrueStatements.Count);
            Assert.AreEqual(0, ifStatement.FalseStatements.Count);

            Assert.IsInstanceOfType(ifStatement.TrueStatements[0], typeof(GDExpressionStatement));
        }

        [TestMethod]
        public void IfElseStatementTest2()
        {
            var reader = new GDScriptReader();

            var code = @"
if 1 + 1 == 2: return 2 + 2
else:
    var x = 3 + 3
    return x";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDIfStatement));

            var ifStatement = (GDIfStatement)statement;

            Assert.AreEqual(1, ifStatement.TrueStatements.Count);
            Assert.AreEqual(2, ifStatement.FalseStatements.Count);

            Assert.IsInstanceOfType(ifStatement.TrueStatements[0], typeof(GDExpressionStatement));

            Assert.IsInstanceOfType(((GDExpressionStatement)ifStatement.TrueStatements[0]).Expression, typeof(GDReturnExpression));

            Assert.IsInstanceOfType(ifStatement.FalseStatements[0], typeof(GDVariableDeclarationStatement));
            Assert.IsInstanceOfType(ifStatement.FalseStatements[1], typeof(GDReturnStatement));
        }

        [TestMethod]
        public void ElifStatementTest()
        {
            var reader = new GDScriptReader();

            var code = @"
if a == b:
	return 0
elif a > 0:
	a = -b
	return 1
else:
	a += b
	return 2";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDIfStatement));

            var ifStatement = (GDIfStatement)statement;

            Assert.AreEqual(1, ifStatement.TrueStatements.Count);
            Assert.AreEqual(1, ifStatement.FalseStatements.Count);

            Assert.IsInstanceOfType(ifStatement.TrueStatements[0], typeof(GDReturnStatement));
            Assert.IsInstanceOfType(ifStatement.FalseStatements[0], typeof(GDIfStatement));

            ifStatement = (GDIfStatement)ifStatement.FalseStatements[0];

            Assert.AreEqual(2, ifStatement.TrueStatements.Count);
            Assert.AreEqual(2, ifStatement.FalseStatements.Count);

            Assert.IsInstanceOfType(ifStatement.TrueStatements[0], typeof(GDExpressionStatement));
            Assert.IsInstanceOfType(ifStatement.TrueStatements[1], typeof(GDReturnStatement));

            Assert.IsInstanceOfType(ifStatement.FalseStatements[0], typeof(GDExpressionStatement));
            Assert.IsInstanceOfType(ifStatement.FalseStatements[1], typeof(GDReturnStatement));
        }

        [TestMethod]
        public void FunctionTypeTest()
        {
            var reader = new GDScriptReader();

            var code = @"
static func my_int_function() -> int:
    return 0";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Methods.Count());

            var method = declaration.Methods.ElementAt(0);

            Assert.IsNotNull(method);
            Assert.AreEqual(1, method.Statements.Count);
            Assert.IsInstanceOfType(method.Statements[0], typeof(GDReturnStatement));

            Assert.IsNotNull(method);
            Assert.AreEqual("int", method.ReturnType?.Sequence);
            Assert.AreEqual("my_int_function", method.Identifier?.Sequence);
            Assert.AreEqual(true, method.IsStatic);
        }

        [TestMethod]
        public void ForStatementTest()
        {
            var reader = new GDScriptReader();

            var code = @"
for x in [5, 7, 11]:
    print(x)";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDForStatement));

            var forStatement = (GDForStatement)statement;

            Assert.AreEqual("x", forStatement.Variable?.Sequence);
            Assert.IsInstanceOfType(forStatement.Collection, typeof(GDArrayInitializerExpression));
            Assert.AreEqual("[5, 7, 11]", forStatement.Collection.ToString());

            Assert.AreEqual(1, forStatement.Statements.Count);
            Assert.IsInstanceOfType(forStatement.Statements[0], typeof(GDExpressionStatement));

        }

        [TestMethod]
        public void ForStatementTest2()
        {
            var reader = new GDScriptReader();

            var code = @"
for i in range(2, 8, 2):
    print(i)";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDForStatement));

            var forStatement = (GDForStatement)statement;

            Assert.AreEqual("i", forStatement.Variable?.Sequence);
            Assert.IsInstanceOfType(forStatement.Collection, typeof(GDCallExression));
            Assert.AreEqual("range(2, 8, 2)", forStatement.Collection.ToString());

            Assert.AreEqual(1, forStatement.Statements.Count);
            Assert.IsInstanceOfType(forStatement.Statements[0], typeof(GDExpressionStatement));

        }

        [TestMethod]
        public void WhileStatementTest()
        {
            var reader = new GDScriptReader();

            var code = @"
while true:
    print(""Hello world"")";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDWhileStatement));

            var whileStatement = (GDWhileStatement)statement;

            Assert.IsInstanceOfType(whileStatement.Condition, typeof(GDIdentifierExpression));
            Assert.AreEqual("true", ((GDIdentifierExpression)whileStatement.Condition).Identifier?.Sequence);

            Assert.AreEqual(1, whileStatement.Statements.Count);
            Assert.IsInstanceOfType(whileStatement.Statements[0], typeof(GDExpressionStatement));
        }

        [TestMethod]
        public void WhileStatementTest2()
        {
            var reader = new GDScriptReader();

            var code = @"
while a > b:
    print(""Hello world"")";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDWhileStatement));

            var whileStatement = (GDWhileStatement)statement;

            Assert.IsInstanceOfType(whileStatement.Condition, typeof(GDDualOperatorExression));
            Assert.AreEqual("a > b", whileStatement.Condition.ToString());

            Assert.AreEqual(1, whileStatement.Statements.Count);
            Assert.IsInstanceOfType(whileStatement.Statements[0], typeof(GDExpressionStatement));
        }
    }
}
