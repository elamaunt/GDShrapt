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

            var code = @"tool
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
            Assert.AreEqual("ResourceFormatSaver", (@class.Extends?.Type as GDSingleTypeNode)?.Type?.Sequence);
            Assert.AreEqual("HTerrainDataSaver", @class.ClassName?.Identifier?.Sequence);
            Assert.AreEqual(true, @class.IsTool);

            Assert.AreEqual(3, @class.Atributes.Count);
            Assert.AreEqual(4, @class.Members.Count);
            Assert.AreEqual(1, @class.Variables.Count());
            Assert.AreEqual(3, @class.Methods.Count());
            Assert.AreEqual(2, @class.Methods.ElementAt(0).Statements.Count);
            Assert.AreEqual(1, @class.Methods.ElementAt(1).Statements.Count);
            Assert.AreEqual(1, @class.Methods.ElementAt(2).Statements.Count);

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void ParseLogicalExpressionTest()
        {
            var reader = new GDScriptReader();

            var code = @"a > b and c > d";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExpression));

            var @dualOperator = (GDDualOperatorExpression)expression;
            Assert.AreEqual(GDDualOperatorType.And2, @dualOperator.OperatorType);

            var leftExpression = @dualOperator.LeftExpression;

            Assert.IsNotNull(leftExpression);
            Assert.IsInstanceOfType(leftExpression, typeof(GDDualOperatorExpression));

            var rightExpression = @dualOperator.RightExpression;

            Assert.IsNotNull(rightExpression);
            Assert.IsInstanceOfType(rightExpression, typeof(GDDualOperatorExpression));

            var @leftDualOperator = (GDDualOperatorExpression)leftExpression;

            Assert.IsInstanceOfType(@leftDualOperator.LeftExpression, typeof(GDIdentifierExpression));
            Assert.IsNotNull(@leftDualOperator.LeftExpression);
            Assert.IsInstanceOfType(@leftDualOperator.RightExpression, typeof(GDIdentifierExpression));
            Assert.IsNotNull(@leftDualOperator.RightExpression);

            Assert.AreEqual("a", ((GDIdentifierExpression)@leftDualOperator.LeftExpression).Identifier.Sequence);
            Assert.AreEqual("b", ((GDIdentifierExpression)@leftDualOperator.RightExpression).Identifier.Sequence);

            var @rightDualOperator = (GDDualOperatorExpression)rightExpression;

            Assert.IsInstanceOfType(@rightDualOperator.LeftExpression, typeof(GDIdentifierExpression));
            Assert.IsNotNull(@rightDualOperator.LeftExpression);
            Assert.IsInstanceOfType(@rightDualOperator.RightExpression, typeof(GDIdentifierExpression));
            Assert.IsNotNull(@rightDualOperator.RightExpression);

            Assert.AreEqual("c", ((GDIdentifierExpression)@rightDualOperator.LeftExpression).Identifier.Sequence);
            Assert.AreEqual("d", ((GDIdentifierExpression)@rightDualOperator.RightExpression).Identifier.Sequence);

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void ExpressionsPriorityTest()
        {
            var reader = new GDScriptReader();

            var code = @"a > b > c = d = e > f > g";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);

            Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExpression));

            var @dualOperator = (GDDualOperatorExpression)expression;

            Assert.AreEqual(GDDualOperatorType.Assignment, @dualOperator.OperatorType);
            Assert.AreEqual("a > b > c", @dualOperator.LeftExpression.ToString());

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void IfStatementTest()
        {
            var reader = new GDScriptReader();

            var code = @"if a != null and a is A:
	return";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDIfStatement));

            var ifStatement = (GDIfStatement)statement;

            Assert.AreEqual("a != null and a is A", ifStatement.IfBranch.Condition.ToString());
            Assert.AreEqual(1, ifStatement.IfBranch.Statements.Count);

            Assert.AreEqual(0, ifStatement.ElifBranchesList.Count);
            Assert.AreEqual(1, ifStatement.IfBranch.Statements.Count);

            Assert.IsInstanceOfType(ifStatement.IfBranch.Statements[0], typeof(GDExpressionStatement));
            Assert.IsInstanceOfType(((GDExpressionStatement)ifStatement.IfBranch.Statements[0]).Expression, typeof(GDReturnExpression));

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void IfElseStatementTest()
        {
            var reader = new GDScriptReader();

            var code = @"if a != null || a is A:
	return
else:
	a = b";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDIfStatement));

            var ifStatement = (GDIfStatement)statement;

            Assert.IsNotNull(ifStatement.IfBranch);
            Assert.IsNotNull(ifStatement.ElseBranch);

            Assert.AreEqual(1, ifStatement.IfBranch.Statements.Count);
            Assert.AreEqual(1, ifStatement.ElseBranch.Statements.Count);

            Assert.IsInstanceOfType(ifStatement.IfBranch.Statements[0], typeof(GDExpressionStatement));
            Assert.IsInstanceOfType(ifStatement.ElseBranch.Statements[0], typeof(GDExpressionStatement));

            Assert.IsInstanceOfType(((GDExpressionStatement)ifStatement.IfBranch.Statements[0]).Expression, typeof(GDReturnExpression));
            Assert.IsInstanceOfType(((GDExpressionStatement)ifStatement.ElseBranch.Statements[0]).Expression, typeof(GDDualOperatorExpression));

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void IfStatementTest2()
        {
            var reader = new GDScriptReader();

            var code = @"if a: return";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDIfStatement));

            var ifStatement = (GDIfStatement)statement;

            Assert.AreEqual(0, ifStatement.IfBranch.Statements.Count);

            Assert.IsNotNull(ifStatement.IfBranch.Expression);
            Assert.IsInstanceOfType(ifStatement.IfBranch.Expression, typeof(GDReturnExpression));

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void IfElseStatementTest2()
        {
            var reader = new GDScriptReader();

            var code = @"if 1 + 1 == 2: return 2 + 2
else:
    var x = 3 + 3
    return x";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDIfStatement));

            var ifStatement = (GDIfStatement)statement;

            Assert.IsNotNull(ifStatement.IfBranch);
            Assert.IsNotNull(ifStatement.ElseBranch);

            Assert.AreEqual(0, ifStatement.IfBranch.Statements.Count);
            Assert.AreEqual(2, ifStatement.ElseBranch.Statements.Count);

            Assert.IsInstanceOfType(ifStatement.IfBranch.Expression, typeof(GDReturnExpression));

            Assert.IsInstanceOfType(ifStatement.ElseBranch.Statements[0], typeof(GDVariableDeclarationStatement));
            Assert.IsInstanceOfType(ifStatement.ElseBranch.Statements[1], typeof(GDExpressionStatement));
            Assert.IsInstanceOfType(((GDExpressionStatement)ifStatement.ElseBranch.Statements[1]).Expression, typeof(GDReturnExpression));

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void IfElseStatementTest3()
        {
            var reader = new GDScriptReader();

            var code = @"if a:
	print()
elif b:
	print()
if c:
	print()
elif d:
	print()
elif e:
	print()
else:
    print()";

            var statements = reader.ParseStatements(code);

            Assert.AreEqual(2, statements.Count);

            var first = statements[0];
            var second = statements[1];

            Assert.IsInstanceOfType(first, typeof(GDIfStatement));
            Assert.IsInstanceOfType(second, typeof(GDIfStatement));

            var firstIf = (GDIfStatement)first;
            var secondIf = (GDIfStatement)second;

            Assert.AreEqual(1, firstIf.ElifBranchesList.Count);
            Assert.AreEqual(2, secondIf.ElifBranchesList.Count);
        }

        [TestMethod]
        public void ElifStatementTest()
        {
            var reader = new GDScriptReader();

            var code = @"if a == b:
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

            Assert.IsNotNull(ifStatement.IfBranch);
            Assert.IsNotNull(ifStatement.ElseBranch);

            Assert.AreEqual(1, ifStatement.ElifBranchesList.Count);
            Assert.AreEqual(2, ifStatement.ElifBranchesList[0].Statements.Count);

            Assert.AreEqual(1, ifStatement.IfBranch.Statements.Count);
            Assert.AreEqual(2, ifStatement.ElseBranch.Statements.Count);

            Assert.IsInstanceOfType(ifStatement.IfBranch.Statements[0], typeof(GDExpressionStatement));
            Assert.IsInstanceOfType(((GDExpressionStatement)ifStatement.IfBranch.Statements[0]).Expression, typeof(GDReturnExpression));

            Assert.IsInstanceOfType(ifStatement.ElifBranchesList[0].Statements[0], typeof(GDExpressionStatement));
            Assert.IsInstanceOfType(ifStatement.ElifBranchesList[0].Statements[1], typeof(GDExpressionStatement));
            Assert.IsInstanceOfType(((GDExpressionStatement)ifStatement.ElifBranchesList[0].Statements[1]).Expression, typeof(GDReturnExpression));

            Assert.IsInstanceOfType(ifStatement.ElseBranch.Statements[0], typeof(GDExpressionStatement));
            Assert.IsInstanceOfType(ifStatement.ElseBranch.Statements[1], typeof(GDExpressionStatement));
            Assert.IsInstanceOfType(((GDExpressionStatement)ifStatement.ElseBranch.Statements[1]).Expression, typeof(GDReturnExpression));

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void FunctionTypeTest()
        {
            var reader = new GDScriptReader();

            var code = @"static func my_int_function() -> int:
    return 0";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Methods.Count());

            var method = declaration.Methods.ElementAt(0);

            Assert.IsNotNull(method);
            Assert.AreEqual(1, method.Statements.Count);
            Assert.IsInstanceOfType(method.Statements[0], typeof(GDExpressionStatement));
            Assert.IsInstanceOfType(((GDExpressionStatement)method.Statements[0]).Expression, typeof(GDReturnExpression));

            Assert.IsNotNull(method);
            Assert.AreEqual("int", (method.ReturnType as GDSingleTypeNode)?.Type?.Sequence);
            Assert.AreEqual("my_int_function", method.Identifier?.Sequence);
            Assert.AreEqual(true, method.IsStatic);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ForStatementTest()
        {
            var reader = new GDScriptReader();

            var code = @"for x in [5, 7, 11]:
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

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void ForStatementTest2()
        {
            var reader = new GDScriptReader();

            var code = @"for i in range(2, 8, 2):
    print(i)";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDForStatement));

            var forStatement = (GDForStatement)statement;

            Assert.AreEqual("i", forStatement.Variable?.Sequence);
            Assert.IsInstanceOfType(forStatement.Collection, typeof(GDCallExpression));
            Assert.AreEqual("range(2, 8, 2)", forStatement.Collection.ToString());

            Assert.AreEqual(1, forStatement.Statements.Count);
            Assert.IsInstanceOfType(forStatement.Statements[0], typeof(GDExpressionStatement));

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void WhileStatementTest()
        {
            var reader = new GDScriptReader();

            var code = @"while true:
    print(""Hello world"")";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDWhileStatement));

            var whileStatement = (GDWhileStatement)statement;

            Assert.IsInstanceOfType(whileStatement.Condition, typeof(GDBoolExpression));
            Assert.AreEqual(true, ((GDBoolExpression)whileStatement.Condition).Value);

            Assert.AreEqual(1, whileStatement.Statements.Count);
            Assert.IsInstanceOfType(whileStatement.Statements[0], typeof(GDExpressionStatement));

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void WhileStatementTest2()
        {
            var reader = new GDScriptReader();

            var code = @"while a > b:
    print(""Hello world"")";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDWhileStatement));

            var whileStatement = (GDWhileStatement)statement;

            Assert.IsInstanceOfType(whileStatement.Condition, typeof(GDDualOperatorExpression));
            Assert.AreEqual("a > b", whileStatement.Condition.ToString());

            Assert.AreEqual(1, whileStatement.Statements.Count);
            Assert.IsInstanceOfType(whileStatement.Statements[0], typeof(GDExpressionStatement));

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void MatchStatementTest()
        {
            var reader = new GDScriptReader();

            var code = @"match x:
    1:
        print(""We are number one!"")
    2:
        print(""Two are better than one!"")
    ""test"":
        print(""Oh snap! It's a string!"")";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDMatchStatement));

            var matchStatement = (GDMatchStatement)statement;

            Assert.IsNotNull(matchStatement.Value);
            Assert.AreEqual("x", matchStatement.Value.ToString());
            Assert.AreEqual(3, matchStatement.Cases.Count);

            Assert.AreEqual(1, matchStatement.Cases[0].Conditions.Count);
            Assert.AreEqual("1", matchStatement.Cases[0].Conditions[0].ToString());

            Assert.AreEqual(1, matchStatement.Cases[1].Conditions.Count);
            Assert.AreEqual("2", matchStatement.Cases[1].Conditions[0].ToString());

            Assert.AreEqual(1, matchStatement.Cases[2].Conditions.Count);
            Assert.AreEqual("\"test\"", matchStatement.Cases[2].Conditions[0].ToString());

            Assert.AreEqual(1, matchStatement.Cases[0].Statements.Count);
            Assert.AreEqual(1, matchStatement.Cases[1].Statements.Count);
            Assert.AreEqual(1, matchStatement.Cases[2].Statements.Count);

            Assert.AreEqual("print(\"We are number one!\")", matchStatement.Cases[0].Statements[0].ToString());
            Assert.AreEqual("print(\"Two are better than one!\")", matchStatement.Cases[1].Statements[0].ToString());
            Assert.AreEqual("print(\"Oh snap! It's a string!\")", matchStatement.Cases[2].Statements[0].ToString());

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void MatchStatementTest2()
        {
            var reader = new GDScriptReader();

            var code = @"match x:
    1:
        print(""It's one!"")
    2:
        print(""It's one times two!"")
    var new_var:
        print(""It's not 1 or 2, it's "", new_var)";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDMatchStatement));

            var matchStatement = (GDMatchStatement)statement;

            Assert.IsNotNull(matchStatement.Value);
            Assert.AreEqual("x", matchStatement.Value.ToString());
            Assert.AreEqual(3, matchStatement.Cases.Count);

            Assert.AreEqual(1, matchStatement.Cases[0].Conditions.Count);
            Assert.AreEqual("1", matchStatement.Cases[0].Conditions[0].ToString());

            Assert.AreEqual(1, matchStatement.Cases[1].Conditions.Count);
            Assert.AreEqual("2", matchStatement.Cases[1].Conditions[0].ToString());

            Assert.AreEqual(1, matchStatement.Cases[2].Conditions.Count);
            Assert.IsInstanceOfType(matchStatement.Cases[2].Conditions[0], typeof(GDMatchCaseVariableExpression));
            Assert.AreEqual("var new_var", matchStatement.Cases[2].Conditions[0].ToString());

            Assert.AreEqual(1, matchStatement.Cases[0].Statements.Count);
            Assert.AreEqual(1, matchStatement.Cases[1].Statements.Count);
            Assert.AreEqual(1, matchStatement.Cases[2].Statements.Count);

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void ArrayTest()
        {
            var reader = new GDScriptReader();

            var code = @"var d = [ a, b, 1, ""Hello World"" ]";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDVariableDeclarationStatement));

            var variableDeclaration = (GDVariableDeclarationStatement)statement;

            Assert.IsNotNull(variableDeclaration.Initializer);
            Assert.IsInstanceOfType(variableDeclaration.Initializer, typeof(GDArrayInitializerExpression));

            var arrayInitializer = (GDArrayInitializerExpression)variableDeclaration.Initializer;

            Assert.AreEqual(4, arrayInitializer.Values.Count);

            Assert.AreEqual("a", arrayInitializer.Values[0].ToString());
            Assert.AreEqual("b", arrayInitializer.Values[1].ToString());
            Assert.AreEqual("1", arrayInitializer.Values[2].ToString());
            Assert.AreEqual("\"Hello World\"", arrayInitializer.Values[3].ToString());

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void DictionaryTest()
        {
            var reader = new GDScriptReader();

            var code = @"var d = { a : 1, b : 2, c : ""test"", ""Hello"":""World"" }";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDVariableDeclarationStatement));

            var variableDeclaration = (GDVariableDeclarationStatement)statement;

            Assert.IsNotNull(variableDeclaration.Initializer);
            Assert.IsInstanceOfType(variableDeclaration.Initializer, typeof(GDDictionaryInitializerExpression));

            var dictionaryInitializer = (GDDictionaryInitializerExpression)variableDeclaration.Initializer;

            Assert.AreEqual(4, dictionaryInitializer.KeyValues.Count);

            Assert.AreEqual("a", dictionaryInitializer.KeyValues[0].Key.ToString());
            Assert.AreEqual("b", dictionaryInitializer.KeyValues[1].Key.ToString());
            Assert.AreEqual("c", dictionaryInitializer.KeyValues[2].Key.ToString());
            Assert.AreEqual("\"Hello\"", dictionaryInitializer.KeyValues[3].Key.ToString());

            Assert.AreEqual("1", dictionaryInitializer.KeyValues[0].Value.ToString());
            Assert.AreEqual("2", dictionaryInitializer.KeyValues[1].Value.ToString());
            Assert.AreEqual("\"test\"", dictionaryInitializer.KeyValues[2].Value.ToString());
            Assert.AreEqual("\"World\"", dictionaryInitializer.KeyValues[3].Value.ToString());

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void StringTest()
        {
            var reader = new GDScriptReader();

            var code = @"""test""";

            var statement = reader.ParseExpression(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDStringExpression));

            var stringExpression = (GDStringExpression)statement;

            Assert.IsNotNull(stringExpression.String);
            Assert.AreEqual(GDStringBoundingChar.DoubleQuotas, stringExpression.String.BoundingChar);
            Assert.AreEqual("test", stringExpression.String.Sequence);

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void StringTest2()
        {
            var reader = new GDScriptReader();

            var code = "'te\"\"st'";

            var statement = reader.ParseExpression(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDStringExpression));

            var stringExpression = (GDStringExpression)statement;

            Assert.IsNotNull(stringExpression.String);
            Assert.AreEqual(GDStringBoundingChar.SingleQuotas, stringExpression.String.BoundingChar);
            Assert.AreEqual("te\"\"st", stringExpression.String.Sequence);

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void StringTest3()
        {
            var reader = new GDScriptReader();

            var code = "\"\"\"te\"\"st\"\"\"";

            var statement = reader.ParseExpression(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDStringExpression));

            var stringExpression = (GDStringExpression)statement;

            Assert.IsNotNull(stringExpression.String);
            Assert.AreEqual(GDStringBoundingChar.TripleDoubleQuotas, stringExpression.String.BoundingChar);
            Assert.AreEqual("te\"\"st", stringExpression.String.Sequence);

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void StringSplitTest()
        {
            var reader = new GDScriptReader();

            var code = @"func _ready():
	var s = ""Hello\\
World""

    var s2 = ""Hello\
World""

    var s3 = """"""Hello\
World""""""

    var s4 = """"""Hello\\\
World""""""
	var s5 = """"""Hello\\
World""""""

    print(s)
	print(s2)
	print(s3)
	print(s4)
	print(s5)
	pass # Replace with function body.";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);

            var stringExpressions = @class.AllNodes.OfType<GDStringExpression>().ToArray();

            Assert.AreEqual(5, stringExpressions.Length);

            var s = stringExpressions[0].String;
            var s2 = stringExpressions[1].String;
            var s3 = stringExpressions[2].String;
            var s4 = stringExpressions[3].String;
            var s5 = stringExpressions[4].String;

            Assert.IsNotNull(s);
            Assert.IsNotNull(s2);
            Assert.IsNotNull(s3);
            Assert.IsNotNull(s4);
            Assert.IsNotNull(s5);

            Assert.AreEqual("Hello\\\\\nWorld", s.Sequence);
            Assert.AreEqual("HelloWorld", s2.Sequence);
            Assert.AreEqual("HelloWorld", s3.Sequence);
            Assert.AreEqual("Hello\\\\World", s4.Sequence);
            Assert.AreEqual("Hello\\\\\nWorld", s5.Sequence);

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void StringTest4()
        {
            var reader = new GDScriptReader();

            var code = "\'\'\'te\'\"st\'\'\'";

            var statement = reader.ParseExpression(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDStringExpression));

            var stringExpression = (GDStringExpression)statement;

            Assert.IsNotNull(stringExpression.String);
            Assert.AreEqual(GDStringBoundingChar.TripleSingleQuotas, stringExpression.String.BoundingChar);
            Assert.AreEqual("te'\"st", stringExpression.String.Sequence);

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        /*[TestMethod]
        public void ExportTest()
        {
            var reader = new GDScriptReader();

            var code = "export var a = 123";
            var classDeclaration = reader.ParseFileContent(code);

            Assert.IsNotNull(classDeclaration);
            Assert.AreEqual(1, classDeclaration.Members.Count);
            Assert.IsInstanceOfType(classDeclaration.Members[0], typeof(GDVariableDeclaration));

            var variableDeclaration = (GDVariableDeclaration)classDeclaration.Members[0];

            Assert.IsTrue(variableDeclaration.IsExported);
            Assert.IsNotNull(variableDeclaration.Initializer);
            Assert.IsNotNull(variableDeclaration.Identifier);
            Assert.AreEqual("a", variableDeclaration.Identifier.Sequence);
            Assert.AreEqual("123", variableDeclaration.Initializer.ToString());

            AssertHelper.CompareCodeStrings(code, classDeclaration.ToString());
            AssertHelper.NoInvalidTokens(classDeclaration);
        }*/

        [TestMethod]
        public void ExportDeclarationsTest()
        {
            var reader = new GDScriptReader();

            var code = @"
# If the exported value assigns a constant or constant expression,
# the type will be inferred and used in the editor.

@export var number = 5

# Export can take a basic data type as an argument, which will be
# used in the editor.

@export(int) var number

# Export can also take a resource type to use as a hint.

@export(Texture) var character_face
@export(PackedScene) var scene_file
# There are many resource types that can be used this way, try e.g.
# the following to list them:
@export(Resource) var resource

# Integers and strings hint enumerated values.

# Editor will enumerate as 0, 1 and 2.
@export(int, ""Warrior"", ""Magician"", ""Thief"") var character_class
# Editor will enumerate with string names.
@export(String, ""Rebecca"", ""Mary"", ""Leah"") var character_name

# Named enum values

# Editor will enumerate as THING_1, THING_2, ANOTHER_THING.
enum NamedEnum { THING_1, THING_2, ANOTHER_THING = -1 }
        @export(NamedEnum) var x

# Strings as paths

# String is a path to a file.
@export(String, FILE) var f
# String is a path to a directory.
@export(String, DIR) var f
# String is a path to a file, custom filter provided as hint.
@export(String, FILE, ""*.txt"") var f

# Using paths in the global filesystem is also possible,
# but only in scripts in ""tool"" mode.

# String is a path to a PNG file in the global filesystem.
@export(String, FILE, GLOBAL, ""*.png"") var tool_image
# String is a path to a directory in the global filesystem.
@export(String, DIR, GLOBAL) var tool_dir

# The MULTILINE setting tells the editor to show a large input
# field for editing over multiple lines.
@export(String, MULTILINE) var text

# Limiting editor input ranges

# Allow integer values from 0 to 20.
@export(int, 20) var i
# Allow integer values from -10 to 20.
@export(int, -10, 20) var j
# Allow floats from -10 to 20 and snap the value to multiples of 0.2.
@export(float, -10, 20, 0.2) var k
# Allow values 'y = exp(x)' where 'y' varies between 100 and 1000
# while snapping to steps of 20. The editor will present a
# slider for easily editing the value.
@export(float, EXP, 100, 1000, 20) var l

# Floats with easing hint

# Display a visual representation of the 'ease()' function
# when editing.
@export(float, EASE) var transition_speed

# Colors

# Color given as red-green-blue value (alpha will always be 1).
@export(Color, RGB) var col
# Color given as red-green-blue-alpha value.
@export(Color, RGBA) var col

# Nodes

# Another node in the scene can be exported as a NodePath.
@export(NodePath) var node_path
# Do take note that the node itself isn't being exported -
# there is one more step to call the true node:
var node = get_node(node_path)

# Resources

@export(Resource) var resource
# In the Inspector, you can then drag and drop a resource file
# from the FileSystem dock into the variable slot.

# Opening the inspector dropdown may result in an
# extremely long list of possible classes to create, however.
# Therefore, if you specify an extension of Resource such as:
@export(AnimationNode) var resource
# The drop-down menu will be limited to AnimationNode and all
# its inherited classes.
";
            var classDeclaration = reader.ParseFileContent(code);

            Assert.IsNotNull(classDeclaration);

            var exports = classDeclaration.AllNodes.OfType<GDClassMemberAttributeDeclaration>().ToArray();

            Assert.AreEqual(24, exports.Length);

            Assert.AreEqual(0, exports[0].Attribute.Parameters.Count);
            Assert.AreEqual(1, exports[1].Attribute.Parameters.Count);
            Assert.AreEqual(1, exports[2].Attribute.Parameters.Count);
            Assert.AreEqual(1, exports[3].Attribute.Parameters.Count);
            Assert.AreEqual(1, exports[4].Attribute.Parameters.Count);
            Assert.AreEqual(4, exports[5].Attribute.Parameters.Count);
            Assert.AreEqual(4, exports[6].Attribute.Parameters.Count);
            Assert.AreEqual(1, exports[7].Attribute.Parameters.Count);
            Assert.AreEqual(2, exports[8].Attribute.Parameters.Count);
            Assert.AreEqual(2, exports[9].Attribute.Parameters.Count);
            Assert.AreEqual(3, exports[10].Attribute.Parameters.Count);
            Assert.AreEqual(4, exports[11].Attribute.Parameters.Count);
            Assert.AreEqual(3, exports[12].Attribute.Parameters.Count);
            Assert.AreEqual(2, exports[13].Attribute.Parameters.Count);
            Assert.AreEqual(2, exports[14].Attribute.Parameters.Count);
            Assert.AreEqual(3, exports[15].Attribute.Parameters.Count);
            Assert.AreEqual(4, exports[16].Attribute.Parameters.Count);
            Assert.AreEqual(5, exports[17].Attribute.Parameters.Count);
            Assert.AreEqual(2, exports[18].Attribute.Parameters.Count);
            Assert.AreEqual(2, exports[19].Attribute.Parameters.Count);
            Assert.AreEqual(2, exports[20].Attribute.Parameters.Count);
            Assert.AreEqual(1, exports[21].Attribute.Parameters.Count);
            Assert.AreEqual(1, exports[22].Attribute.Parameters.Count);
            Assert.AreEqual(1, exports[23].Attribute.Parameters.Count);

            AssertHelper.CompareCodeStrings(code, classDeclaration.ToString());
            AssertHelper.NoInvalidTokens(classDeclaration);
        }

        [TestMethod]
        public void EnumTest()
        {
            var reader = new GDScriptReader();

            var code = @"enum test { a : 1, b, c : 3}";

            var classDeclaration = reader.ParseFileContent(code);

            Assert.IsNotNull(classDeclaration);
            Assert.AreEqual(1, classDeclaration.Members.Count);
            Assert.IsInstanceOfType(classDeclaration.Members[0], typeof(GDEnumDeclaration));

            var enumDeclaration = (GDEnumDeclaration)classDeclaration.Members[0];

            Assert.AreEqual("test", enumDeclaration.Identifier.Sequence);
            Assert.AreEqual(3, enumDeclaration.Values.Count);

            Assert.AreEqual("a", enumDeclaration.Values[0].Identifier?.ToString());
            Assert.AreEqual("b", enumDeclaration.Values[1].Identifier?.ToString());
            Assert.AreEqual("c", enumDeclaration.Values[2].Identifier?.ToString());

            Assert.AreEqual("1", enumDeclaration.Values[0].Value?.ToString());
            Assert.IsNull(enumDeclaration.Values[1].Value);
            Assert.AreEqual("3", enumDeclaration.Values[2].Value?.ToString());

            AssertHelper.CompareCodeStrings(code, classDeclaration.ToString());
            AssertHelper.NoInvalidTokens(classDeclaration);
        }

        [TestMethod]
        public void EnumTest2()
        {
            var reader = new GDScriptReader();

            var code = @"enum {a,b,c = 10}";

            var classDeclaration = reader.ParseFileContent(code);

            Assert.IsNotNull(classDeclaration);
            Assert.AreEqual(1, classDeclaration.Members.Count);
            Assert.IsInstanceOfType(classDeclaration.Members[0], typeof(GDEnumDeclaration));

            var enumDeclaration = (GDEnumDeclaration)classDeclaration.Members[0];

            Assert.IsNull(enumDeclaration.Identifier);
            Assert.AreEqual(3, enumDeclaration.Values.Count);

            Assert.AreEqual("a", enumDeclaration.Values[0].Identifier?.ToString());
            Assert.AreEqual("b", enumDeclaration.Values[1].Identifier?.ToString());
            Assert.AreEqual("c", enumDeclaration.Values[2].Identifier?.ToString());

            Assert.IsNull(enumDeclaration.Values[0].Value);
            Assert.IsNull(enumDeclaration.Values[1].Value);
            Assert.IsNotNull(enumDeclaration.Values[2].Value);

            AssertHelper.CompareCodeStrings(code, classDeclaration.ToString());
            AssertHelper.NoInvalidTokens(classDeclaration);
        }

        [TestMethod]
        public void ClassNameTest()
        {
            var reader = new GDScriptReader();

            var code = "class_name Test, \"res://interface/icons/item.png\"";
            var classDeclaration = reader.ParseFileContent(code);

            Assert.IsNotNull(classDeclaration);
            Assert.IsNotNull(classDeclaration.ClassName);
            Assert.IsNotNull(classDeclaration.ClassName.Identifier);
            Assert.AreEqual(1, classDeclaration.Atributes.Count);
            Assert.AreEqual("Test", classDeclaration.ClassName.Identifier.Sequence);

            Assert.IsNotNull(classDeclaration.ClassName.Icon);
            Assert.AreEqual("res://interface/icons/item.png", classDeclaration.ClassName.Icon.Sequence);

            AssertHelper.CompareCodeStrings(code, classDeclaration.ToString());
            AssertHelper.NoInvalidTokens(classDeclaration);
        }

        [TestMethod]
        public void ExtendsTest()
        {
            var reader = new GDScriptReader();

            var code = "extends Test";
            var classDeclaration = reader.ParseFileContent(code);

            Assert.IsNotNull(classDeclaration);
            Assert.IsNotNull(classDeclaration.Extends);
            Assert.IsNotNull(classDeclaration.Extends.Type);
            Assert.AreEqual(1, classDeclaration.Atributes.Count);
            Assert.AreEqual("Test", (classDeclaration.Extends.Type as GDSingleTypeNode).Type.Sequence);

            AssertHelper.CompareCodeStrings(code, classDeclaration.ToString());
            AssertHelper.NoInvalidTokens(classDeclaration);
        }

        [TestMethod]
        public void ExtendsTest2()
        {
            var reader = new GDScriptReader();

            var code = "extends \"res://path/to/character.gd\"";
            var classDeclaration = reader.ParseFileContent(code);

            Assert.IsNotNull(classDeclaration);
            Assert.IsNotNull(classDeclaration.Extends);
            Assert.IsNotNull(classDeclaration.Extends.Type is GDStringTypeNode);
            Assert.AreEqual(1, classDeclaration.Atributes.Count);
            Assert.AreEqual("res://path/to/character.gd", (classDeclaration.Extends.Type as GDStringTypeNode).Path.Sequence);

            AssertHelper.CompareCodeStrings(code, classDeclaration.ToString());
            AssertHelper.NoInvalidTokens(classDeclaration);
        }

        [TestMethod]
        [DataRow("1234")]
        [DataRow("1_2_3_4")]
        public void NumberTest(string code)
        {
            var reader = new GDScriptReader();

            var statement = reader.ParseExpression(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDNumberExpression));

            var numberExpression = (GDNumberExpression)statement;

            Assert.IsNotNull(numberExpression.Number);
            Assert.AreEqual(GDNumberType.LongDecimal, numberExpression.Number.ResolveNumberType());
            Assert.AreEqual(1234, numberExpression.Number.ValueInt64);

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        [DataRow("0x8f51")]
        [DataRow("0x_8f_51")]
        public void NumberTest2(string code)
        {
            var reader = new GDScriptReader();

            var statement = reader.ParseExpression(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDNumberExpression));

            var numberExpression = (GDNumberExpression)statement;

            Assert.IsNotNull(numberExpression.Number);
            Assert.AreEqual(GDNumberType.LongHexadecimal, numberExpression.Number.ResolveNumberType());
            Assert.AreEqual(36689, numberExpression.Number.ValueInt64);

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        [DataRow("0b101010")]
        [DataRow("0b10_10_10")]
        public void NumberTest3(string code)
        {
            var reader = new GDScriptReader();

            var statement = reader.ParseExpression(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDNumberExpression));

            var numberExpression = (GDNumberExpression)statement;

            Assert.IsNotNull(numberExpression.Number);
            Assert.AreEqual(GDNumberType.LongBinary, numberExpression.Number.ResolveNumberType());
            Assert.AreEqual(42, numberExpression.Number.ValueInt64);

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        [DataRow("3.14")]
        [DataRow("58.1e-10")]
        public void NumberTest4(string code)
        {
            var reader = new GDScriptReader();

            var statement = reader.ParseExpression(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDNumberExpression));

            var numberExpression = (GDNumberExpression)statement;

            var value = double.Parse(code.Replace("_", ""), System.Globalization.CultureInfo.InvariantCulture);

            Assert.IsNotNull(numberExpression.Number);
            Assert.AreEqual(GDNumberType.Double, numberExpression.Number.ResolveNumberType());
            Assert.AreEqual(value, numberExpression.Number.ValueDouble);

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        [DataRow("and",
                "or",
                "as",
                "is",
                "=",
                "<",
                ">",
                "/",
                "*",
                "+",
                "-",
                ">=",
                "<=",
                "==",
                "/=",
                "!=",
                "*=",
                "-=",
                "+=",
                "&&",
                "||",
                "%=",
                "<<",
                ">>",
                "%",
                "^",
                "|",
                "&",
                "in",
                "&=",
                "|=",
                "**",
                "**=",
                "<<=",
                ">>=", 
                "^=",
                "%=")]
        public void DualOperatorsTest(params string[] operators)
        {
            var reader = new GDScriptReader();

            foreach (var op in operators)
            {
                var code = $"a {op} b";

                var expression = reader.ParseExpression(code);
                Assert.IsNotNull(expression);
                Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExpression));

                var dualOperatorExpression = (GDDualOperatorExpression)expression;

                Assert.AreEqual("a", dualOperatorExpression.LeftExpression.ToString());
                Assert.AreEqual("b", dualOperatorExpression.RightExpression.ToString());
                Assert.AreEqual(op, dualOperatorExpression.OperatorType.Print());

                AssertHelper.CompareCodeStrings(code, expression.ToString());
                AssertHelper.NoInvalidTokens(expression);
            }
        }

        [TestMethod]
        [DataRow("not",
                "-",
                "!",
                "~")]
        public void SingleOperatorsTest(params string[] operators)
        {
            var reader = new GDScriptReader();

            foreach (var op in operators)
            {
                var code = $"{op} a";

                var expression = reader.ParseExpression(code);
                Assert.IsNotNull(expression);
                Assert.IsInstanceOfType(expression, typeof(GDSingleOperatorExpression));

                var singleOperatorExpression = (GDSingleOperatorExpression)expression;

                Assert.AreEqual("a", singleOperatorExpression.TargetExpression.ToString());
                Assert.AreEqual(op, singleOperatorExpression.OperatorType.Print());

                AssertHelper.CompareCodeStrings(code, expression.ToString());
                AssertHelper.NoInvalidTokens(expression);
            }
        }

        /*[TestMethod]
        public void PropertyTest()
        {
            var reader = new GDScriptReader();

            var code = "var speed = 1 setget set_speed, get_speed";

            var classDeclaration = reader.ParseFileContent(code);

            Assert.IsNotNull(classDeclaration);
            Assert.AreEqual(1, classDeclaration.Members.Count);

            Assert.IsNotNull(classDeclaration.Members[0]);
            Assert.IsInstanceOfType(classDeclaration.Members[0], typeof(GDVariableDeclaration));

            var variableDeclaration = (GDVariableDeclaration)classDeclaration.Members[0];

            Assert.IsNotNull(variableDeclaration.Identifier);
            Assert.AreEqual("speed", variableDeclaration.Identifier.Sequence);

            Assert.AreEqual("1", variableDeclaration.Initializer.ToString());

            Assert.IsFalse(variableDeclaration.IsConstant);
            Assert.IsFalse(variableDeclaration.IsExported);
            Assert.IsFalse(variableDeclaration.HasOnReadyInitialization);

            Assert.IsNotNull(variableDeclaration.GetMethodIdentifier);
            Assert.IsNotNull(variableDeclaration.SetMethodIdentifier);

            Assert.AreEqual("set_speed", variableDeclaration.SetMethodIdentifier.Sequence);
            Assert.AreEqual("get_speed", variableDeclaration.GetMethodIdentifier.Sequence);

            AssertHelper.CompareCodeStrings(code, classDeclaration.ToString());
            AssertHelper.NoInvalidTokens(classDeclaration);
        }*/

        /*[TestMethod]
        public void PropertyTest2()
        {
            var reader = new GDScriptReader();

            var code = "export var _height = 100.1e+10 setget set_height";

            var classDeclaration = reader.ParseFileContent(code);

            Assert.IsNotNull(classDeclaration);
            Assert.AreEqual(1, classDeclaration.Members.Count);

            Assert.IsNotNull(classDeclaration.Members[0]);
            Assert.IsInstanceOfType(classDeclaration.Members[0], typeof(GDVariableDeclaration));

            var variableDeclaration = (GDVariableDeclaration)classDeclaration.Members[0];

            Assert.IsNotNull(variableDeclaration.Identifier);
            Assert.AreEqual("_height", variableDeclaration.Identifier.Sequence);

            Assert.IsInstanceOfType(variableDeclaration.Initializer, typeof(GDNumberExpression));

            var number = ((GDNumberExpression)variableDeclaration.Initializer).Number;

            Assert.IsNotNull(number);

            Assert.AreEqual(GDNumberType.Double, number.ResolveNumberType());

            var value = double.Parse("100.1e+10", CultureInfo.InvariantCulture);
            Assert.AreEqual(value, number.ValueDouble);
            Assert.AreEqual("100.1e+10", number.Sequence);

            Assert.IsFalse(variableDeclaration.IsConstant);
            Assert.IsTrue(variableDeclaration.IsExported);
            Assert.IsFalse(variableDeclaration.HasOnReadyInitialization);

            Assert.IsNull(variableDeclaration.GetMethodIdentifier);
            Assert.IsNotNull(variableDeclaration.SetMethodIdentifier);

            Assert.AreEqual("set_height", variableDeclaration.SetMethodIdentifier.Sequence);

            AssertHelper.CompareCodeStrings(code, classDeclaration.ToString());
            AssertHelper.NoInvalidTokens(classDeclaration);
        }*/

        [TestMethod]
        public void SignalTest()
        {
            var reader = new GDScriptReader();
            var code = "signal my_signal(value, other_value)";

            var classDeclaration = reader.ParseFileContent(code);

            Assert.IsNotNull(classDeclaration);
            Assert.AreEqual(1, classDeclaration.Members.Count);

            Assert.IsNotNull(classDeclaration.Members[0]);
            Assert.IsInstanceOfType(classDeclaration.Members[0], typeof(GDSignalDeclaration));

            var signalDeclaration = (GDSignalDeclaration)classDeclaration.Members[0];

            Assert.IsNotNull(signalDeclaration.Identifier);
            Assert.AreEqual("my_signal", signalDeclaration.Identifier.Sequence);

            Assert.IsNotNull(signalDeclaration.Parameters);
            Assert.AreEqual(2, signalDeclaration.Parameters.Count);

            Assert.AreEqual("value", signalDeclaration.Parameters[0]?.Identifier?.Sequence);
            Assert.AreEqual("other_value", signalDeclaration.Parameters[1]?.Identifier?.Sequence);

            AssertHelper.CompareCodeStrings(code, classDeclaration.ToString());
            AssertHelper.NoInvalidTokens(classDeclaration);
        }

        [TestMethod]
        public void IfExpressionTest()
        {
            var reader = new GDScriptReader();

            var code = "3 if y < 10 else -1";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDIfExpression));

            var ifExpression = (GDIfExpression)expression;

            Assert.IsNotNull(ifExpression.TrueExpression);
            Assert.IsNotNull(ifExpression.Condition);
            Assert.IsNotNull(ifExpression.FalseExpression);

            Assert.AreEqual("3", ifExpression.TrueExpression.ToString());
            Assert.AreEqual("y < 10", ifExpression.Condition.ToString());
            Assert.AreEqual("-1", ifExpression.FalseExpression.ToString());

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void IfExpressionTest2()
        {
            var reader = new GDScriptReader();

            var code = "var x = 3 + 4 if -y != 10 else n";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Members.Count);
            Assert.IsInstanceOfType(declaration.Members[0], typeof(GDVariableDeclaration));

            var variableDeclaration = (GDVariableDeclaration)declaration.Members[0];

            Assert.IsInstanceOfType(variableDeclaration.Initializer, typeof(GDIfExpression));

            var ifExpression = (GDIfExpression)variableDeclaration.Initializer;

            Assert.IsNotNull(ifExpression.TrueExpression);
            Assert.IsNotNull(ifExpression.Condition);
            Assert.IsNotNull(ifExpression.FalseExpression);

            Assert.IsInstanceOfType(ifExpression.Condition, typeof(GDDualOperatorExpression));

            Assert.AreEqual("3 + 4", ifExpression.TrueExpression.ToString());
            Assert.AreEqual("-y != 10", ifExpression.Condition.ToString());
            Assert.AreEqual("n", ifExpression.FalseExpression.ToString());

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void NegativeNumberTest()
        {
            var reader = new GDScriptReader();

            var code = "-10";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDNumberExpression));

            var numberExpression = (GDNumberExpression)expression;

            Assert.IsNotNull(numberExpression.Number);
            Assert.AreEqual(GDNumberType.LongDecimal, numberExpression.Number.ResolveNumberType());
            Assert.AreEqual(-10, numberExpression.Number.ValueInt64);

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void BracketsTest()
        {
            var reader = new GDScriptReader();

            var code = "13 + -2 * -(10-20) / 3.0";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExpression));

            var dualOperator = (GDDualOperatorExpression)expression;

            Assert.AreEqual("13", dualOperator.LeftExpression.ToString());

            Assert.AreEqual(GDDualOperatorType.Addition, dualOperator.OperatorType);

            Assert.IsInstanceOfType(dualOperator.RightExpression, typeof(GDDualOperatorExpression));

            var dualOperator2 = (GDDualOperatorExpression)dualOperator.RightExpression;

            Assert.AreEqual("3.0", dualOperator2.RightExpression.ToString());

            Assert.IsInstanceOfType(dualOperator2.LeftExpression, typeof(GDDualOperatorExpression));

            var dualOperator3 = (GDDualOperatorExpression)dualOperator2.LeftExpression;

            Assert.AreEqual("-2", dualOperator3.LeftExpression.ToString());
            Assert.IsInstanceOfType(dualOperator3.RightExpression, typeof(GDSingleOperatorExpression));

            var singleOperator = (GDSingleOperatorExpression)dualOperator3.RightExpression;

            Assert.AreEqual(GDSingleOperatorType.Negate, singleOperator.OperatorType);
            Assert.IsInstanceOfType(singleOperator.TargetExpression, typeof(GDBracketExpression));

            Assert.AreEqual("(10-20)", singleOperator.TargetExpression.ToString());

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void MatchDefaultOperatorTest()
        {
            var reader = new GDScriptReader();

            var code = @"match x:
    1:
        return true
    _:
        return false";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDMatchStatement));

            var matchStatement = (GDMatchStatement)statement;

            Assert.AreEqual(2, matchStatement.Cases.Count);
            Assert.AreEqual(1, matchStatement.Cases[1].Conditions.Count);
            Assert.IsInstanceOfType(matchStatement.Cases[1].Conditions[0], typeof(GDMatchDefaultOperatorExpression));

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void MethodsChainTest()
        {
            var reader = new GDScriptReader();

            var code = @"A(1 + 2)()(-3).B().C() + D() / E()(""test"")";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExpression));

            var dualOperator = (GDDualOperatorExpression)expression;

            Assert.AreEqual(GDDualOperatorType.Addition, dualOperator.OperatorType);

            Assert.IsInstanceOfType(dualOperator.LeftExpression, typeof(GDCallExpression));
            Assert.IsInstanceOfType(dualOperator.RightExpression, typeof(GDDualOperatorExpression));

            var rightDualOperator = (GDDualOperatorExpression)dualOperator.RightExpression;

            Assert.AreEqual(GDDualOperatorType.Division, rightDualOperator.OperatorType);
            Assert.AreEqual("D()", rightDualOperator.LeftExpression.ToString());

            Assert.IsInstanceOfType(rightDualOperator.RightExpression, typeof(GDCallExpression));

            var rightCallExpression = (GDCallExpression)rightDualOperator.RightExpression;

            Assert.AreEqual("E()(\"test\")", rightCallExpression.ToString());

            var callExpression = (GDCallExpression)dualOperator.LeftExpression;
            Assert.AreEqual(0, callExpression.Parameters.Count);
            var memberOperatorExpression = callExpression.CallerExpression.CastOrAssert<GDMemberOperatorExpression>();
            Assert.AreEqual("C", memberOperatorExpression.Identifier?.Sequence);

            callExpression = memberOperatorExpression.CallerExpression.CastOrAssert<GDCallExpression>();
            Assert.AreEqual(0, callExpression.Parameters.Count);
            memberOperatorExpression = callExpression.CallerExpression.CastOrAssert<GDMemberOperatorExpression>();
            Assert.AreEqual("B", memberOperatorExpression.Identifier?.Sequence);

            callExpression = memberOperatorExpression.CallerExpression.CastOrAssert<GDCallExpression>();
            Assert.AreEqual(1, callExpression.Parameters.Count);
            Assert.AreEqual("-3", callExpression.Parameters.ToString());

            callExpression = callExpression.CallerExpression.CastOrAssert<GDCallExpression>();
            Assert.AreEqual(0, callExpression.Parameters.Count);

            callExpression = callExpression.CallerExpression.CastOrAssert<GDCallExpression>();
            Assert.AreEqual(1, callExpression.Parameters.Count);
            Assert.AreEqual("1 + 2", callExpression.Parameters.ToString());

            var identifierExpression = callExpression.CallerExpression.CastOrAssert<GDIdentifierExpression>();
            Assert.AreEqual("A", identifierExpression.Identifier?.Sequence);

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void BaseMethodCallTest()
        {
            var reader = new GDScriptReader();

            var code = @"func _init(res : string = ""Hello world"").(res) -> void:
	._init(""1234"");
    pass";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);
            Assert.AreEqual(1, @class.Methods.Count());

            var method = @class.Methods.First();
            Assert.IsNotNull(method);

            Assert.IsTrue((method.ReturnType as GDSingleTypeNode).Type.IsVoid);
            Assert.AreEqual("_init", method.Identifier.Sequence);
            Assert.AreEqual(1, method.Parameters.Count);
            Assert.AreEqual(1, method.BaseCallParameters.Count);

            var parameter = method.Parameters[0];
            Assert.IsNotNull(parameter);

            Assert.AreEqual("res", parameter.Identifier.Sequence);
            Assert.AreEqual("string", (parameter.Type as GDSingleTypeNode)?.Type?.Sequence);
            Assert.AreEqual("\"Hello world\"", parameter.DefaultValue.ToString());

            var baseCallParameter = method.BaseCallParameters[0];

            Assert.AreEqual("res", baseCallParameter.ToString());

            Assert.AreEqual(2, method.Statements.Count);

            var callStatement = method.Statements[0].CastOrAssert<GDExpressionStatement>();

            Assert.IsInstanceOfType(callStatement.Tokens.Last(), typeof(GDSemiColon));

            var callExpression = callStatement.Expression.CastOrAssert<GDCallExpression>();

            Assert.AreEqual("._init", callExpression.CallerExpression.ToString());
            Assert.AreEqual("\"1234\"", callExpression.Parameters.ToString());

            var passStatement = method.Statements[1].CastOrAssert<GDExpressionStatement>().Expression.CastOrAssert<GDPassExpression>();

            Assert.IsNotNull(passStatement);
            Assert.AreEqual(1, passStatement.Tokens.Count());

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void StringEscapingTest()
        {
            var reader = new GDScriptReader();

            var code = "\"Hello \\\" World\"";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDStringExpression));
            Assert.AreEqual("Hello \\\" World", ((GDStringExpression)expression).String.Sequence);

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void GetNodeTest()
        {
            var reader = new GDScriptReader();

            var code = @"$Animation/Root/ _345/ end .CallMethod()";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);

            var getNodeExpression = expression.CastOrAssert<GDGetNodeExpression>();
            Assert.AreEqual("Animation/Root/ _345/ end .CallMethod()", getNodeExpression.Path.ToString());

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void NodePathTest()
        {
            var reader = new GDScriptReader();

            var code = @"^""/root/MyAutoload"".get_name(0)";

            var expression = reader.ParseExpression(code);

            var call = expression.CastOrAssert<GDCallExpression>();
            Assert.AreEqual("0", call.Parameters.ToString());

            var memberOperator = call.CallerExpression.CastOrAssert<GDMemberOperatorExpression>();
            Assert.AreEqual("get_name", memberOperator.Identifier.Sequence);

            var nodePathExpression = memberOperator.CallerExpression.CastOrAssert<GDNodePathExpression>();

            Assert.AreEqual("\"/root/MyAutoload\"", nodePathExpression.Path.ToString());

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void NewLineParsingTest1()
        {
            var reader = new GDScriptReader();

            var code = @"var a = (b
+
c)";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);

            var declaration = statement.CastOrAssert<GDVariableDeclarationStatement>();
            var brackets = declaration.Initializer.CastOrAssert<GDBracketExpression>();
            var dualExpression = brackets.InnerExpression.CastOrAssert<GDDualOperatorExpression>();

            Assert.AreEqual("b", dualExpression.LeftExpression.ToString());
            Assert.AreEqual("c", dualExpression.RightExpression.ToString());

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void UnspecifiedContentParsingTest()
        {
            var reader = new GDScriptReader();

            var code1 = "tool";
            var code2 = "var a = b + c";
            var code3 = "for a in [0,1,2]: print(\"Hello\")";

            var nodes1 = reader.ParseUnspecifiedContent(code1);
            var nodes2 = reader.ParseUnspecifiedContent(code2);
            var nodes3 = reader.ParseUnspecifiedContent(code3);

            Assert.AreEqual(1, nodes1.Count);
            Assert.AreEqual(1, nodes2.Count);
            Assert.AreEqual(1, nodes3.Count);

            Assert.IsInstanceOfType(nodes1[0], typeof(GDClassDeclaration));
            Assert.IsInstanceOfType(nodes2[0], typeof(GDClassDeclaration));
            Assert.IsInstanceOfType(nodes3[0], typeof(GDStatementsList));

            Assert.IsInstanceOfType(((GDStatementsList)nodes3[0])[0], typeof(GDForStatement));
        }

        [TestMethod]
        public void CallInSameStringTest()
        {
            var reader = new GDScriptReader();

            var code = @"
var hello = ""Hello""

var s = Usage.new()

func t():
	return ""a"";

func _init(
     a = ""please""
            ):
	var b = ""Extract me"" + t()


    var _dict = {
        ""width"": 2,
		""depth"": 2,
		""heights"": PoolRealArray([0, 0, 0, 0]),
		""min_height"": -1,
		""max_height"": 1

    }

        match b:
		0:

            pass
        var t:
			for i in range(10) :

                var c = b + a + i + t

                new_method()

                print(c)


    print(""done"")

    pass

func new_method() :  print(hello)";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);

            var methodDecl = (GDMethodDeclaration)@class.Members.Last();

            Assert.AreEqual("new_method", methodDecl.Identifier?.Sequence);
            Assert.IsNotNull(methodDecl.Colon);
            Assert.IsNotNull(methodDecl.Expression);
            Assert.AreEqual(0, methodDecl.Statements.Count);

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void InnerClassesTest()
        {
            var reader = new GDScriptReader();

            var code = @"extends Node2D

var _type = """"
var _size = Vector2(1,1)

func _ready():

    pass

class fish extends Node2D:

    func _init():
        _randomize()

    func _randomize():
        randomize()
        _size = rand_range(0.75,2.0)

class fishB extends fish:

    func _init():
        pass

    func _randomize():
        randomize()
        _size = rand_range(0.0,20.0)

    class fishC:

        func _init():
            pass";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);

            Assert.AreEqual(2, @class.InnerClasses.Count());

            var innerClass1 = @class.InnerClasses.ElementAt(0);
            var innerClass2 = @class.InnerClasses.ElementAt(1);

            Assert.AreEqual("fish", innerClass1.Identifier?.Sequence);
            Assert.AreEqual("Node2D", (innerClass1.BaseType as GDSingleTypeNode)?.Type?.Sequence);
            Assert.AreEqual("fishB", innerClass2.Identifier?.Sequence);
            Assert.AreEqual("fish", (innerClass2.BaseType as GDSingleTypeNode)?.Type?.Sequence);

            var innerClass3 = innerClass2.InnerClasses.FirstOrDefault();

            Assert.IsNotNull(innerClass3);

            Assert.AreEqual("fishC", innerClass3.Identifier?.Sequence);
            Assert.AreEqual(1, innerClass3.Methods.Count());

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void NodePathTest2()
        {
            var reader = new GDScriptReader();

            var samples = new string[]
            {
                "^\"A\"",
                "^\"A/B\"",
                "^\".\"",
                "^\"..\"",
                "^\"../C\"",
                "^\"/root\"",
                "^\"/root/Main\"",
                "^\"/root/MyAutoload\"",
                "^\"Path2D/PathFollow2D/Sprite\"",
                "^\"Path2D/PathFollow2D/Sprite:texture\"",
                "^\"Path2D/PathFollow2D/Sprite:position\"",
                "^\"Path2D/PathFollow2D/Sprite:position:x\"",
                "^\"/root/Level/Path2D\"",
                "^\"Path2D/PathFollow2D/Sprite:texture:load_path\""
            };


            for (int i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];

                var expression = reader.ParseExpression(sample);

                Assert.IsNotNull(expression);
                Assert.IsInstanceOfType(expression, typeof(GDNodePathExpression));

                var nodePathExpression = (GDNodePathExpression)expression;

                AssertHelper.CompareCodeStrings(sample, nodePathExpression.ToString());
                AssertHelper.NoInvalidTokens(nodePathExpression);
            }
        }

        [TestMethod]
        public void ExpressionSplittingTest()
        {
            var reader = new GDScriptReader();

            var code = @"func _ready():
    if (2 == 2
        and 3 == 3
        and 4 == 4
        and 5 == 5
        and 6 == 6):
            print(""The parenthesis way of putting 'if' statements on multiple lines."")

    if 2 == 2 \
        and 3 == 3 \
        and 4 == 4 \
        and 5 == 5 \
        and 6 == 6:
            print(""The backslash way of putting 'if' statements on multiple lines."")";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);

            Assert.AreEqual(1, @class.Methods.Count());

            var method = @class.Methods.First();

            Assert.AreEqual(2, method.Statements.Count);

            var statement = method.Statements[1];

            Assert.IsInstanceOfType(statement, typeof(GDIfStatement));

            var ifStatement = (GDIfStatement)statement;

            Assert.IsNotNull(ifStatement.IfBranch);

            Assert.IsNotNull(ifStatement.IfBranch.Condition);
            AssertHelper.CompareCodeStrings(@"2 == 2 \
        and 3 == 3 \
        and 4 == 4 \
        and 5 == 5 \
        and 6 == 6", ifStatement.IfBranch.Condition.ToString());

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void ParseSameLineStatements()
        {
            var reader = new GDScriptReader();

            var code = @"func _draw_block_face(surface_tool, verts, uvs):
	surface_tool.add_uv(uvs[1]); surface_tool.add_vertex(verts[1])";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);

            Assert.AreEqual(1, @class.Methods.Count());

            var method = @class.Methods.First();

            Assert.AreEqual(2, method.Statements.Count);
            ;
            AssertHelper.CompareCodeStrings("surface_tool.add_uv(uvs[1]);", method.Statements[0].ToString());
            AssertHelper.CompareCodeStrings("surface_tool.add_vertex(verts[1])", method.Statements[1].ToString());

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void ParseFirstClassFunctions()
        {
            var reader = new GDScriptReader();

            var code = @"var test_var = func is_positive(x):
		return x >= 0

print(test_var.call(8)) # true

test_var = func is_negative(x):
	return x < 0

print(test_var.call(-4)) # true";

            var @statements = reader.ParseStatements(code);

            Assert.AreEqual(2, @statements.SelectMany(x => x.AllNodes).Count(x => x is GDMethodExpression));
        }

        [TestMethod]
        public void ParseFirstClassLambda()
        {
            var reader = new GDScriptReader();

            var code = @"var arr = [2,1,4,5,3]
		
arr.sort_custom(func(x, y): return x > y)

print(arr) # [5, 4, 3, 2, 1]

var dicts = [{a=1, b=6}, {a=5, b=5}, {a=3, b=2}, {a=4, b=1}]

dicts.sort_custom(func(x, y): return x.a > y.a)

print(dicts)
# [{""a"":5, ""b"":5}, {""a"":4, ""b"":1}, {""a"":3, ""b"":2}, {""a"":1, ""b"":6}]

var greet = func(): return ""Hello, World!""
var add_one = func(x): return x + 1
print(greet.call())
print(add_one.call(2))";

            var @statements = reader.ParseStatements(code);

            Assert.AreEqual(10, @statements.Count);
            Assert.AreEqual(4, @statements.SelectMany(x => x.AllNodes).Count(x => x is GDMethodExpression));
        }

        [TestMethod]
        public void ParseLambdaWithClosureTest()
        {
            var reader = new GDScriptReader();

            var code = @"var get_counter = func():
	var count = 0
	return func():
		count += 1
		return count

var counter = get_counter.call()
print(counter.call()) # 1
print(counter.call()) # 1 Didn't work :(
print(counter.call()) # 1";

            var @statements = reader.ParseStatements(code);

            Assert.AreEqual(5, @statements.Count);
            Assert.AreEqual(2, @statements.SelectMany(x => x.AllNodes).Count(x => x is GDMethodExpression));
        }

        [TestMethod]
        public void ParseCurryingTest()
        {
            var reader = new GDScriptReader();

            var code = @"var sum_all = func(x, y, z):
	return x + y + z
	
var curry = func(f):
	return func(x):
		return func(y):
			return func(z):
				return f.call(x, y, z)
	
var curried_sum = curry.call(sum_all)
var partial_sum_x = curried_sum.call(1)
var partial_sum_y = partial_sum_x.call(2)
print(partial_sum_y.call(3)) # 6
print(curried_sum.call(1).call(2).call(3)) # 6";

            var @statements = reader.ParseStatements(code);

            Assert.AreEqual(7, @statements.Count);
            Assert.AreEqual(5, @statements.SelectMany(x => x.AllNodes).Count(x => x is GDMethodExpression));
            Assert.AreEqual(8, @statements.SelectMany(x => x.AllNodes).OfType<GDMemberOperatorExpression>().Count(x => x.Identifier == "call"));
        }

        [TestMethod]
        public void ParseNewProperiesSyntaxTest1()
        {
            var reader = new GDScriptReader();

            var code = @"var my_var: set = _setter, get = _getter

func _setter(new_value):
	my_var = new_value


func _getter():
	return my_var";

            var @class = reader.ParseFileContent(code);

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        public void ParseNewProperiesSyntaxTest2()
        {
            var reader = new GDScriptReader();

            var code = @"var _score: int
var score: int:
    get:
        return _score
    set(value: int):
        _score = value
        _update_score_display()

func _update_score_display():
	pass # Do something to update the displayed score";

            var @class = reader.ParseFileContent(code);

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        public void ParseNewProperiesSyntaxTest3()
        {
            var reader = new GDScriptReader();

            var code = @"var score: int:
    get:
        return score
    set(value: int):
        score = value
        update_score_display()

func update_score_display():
	pass # Do something to update the displayed score";

            var @class = reader.ParseFileContent(code);

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        public void ParseNewExportsTest()
        {
            var reader = new GDScriptReader();

            var code = @"@export_category(""Main Category"")
@export var number = 3
@export var string = """"

@export_category(""Extra Category"")
@export var flag = false
@export var number2: int";


            var @class = reader.ParseFileContent(code);

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void ParseTypedArrayTest()
        {
            var reader = new GDScriptReader();

            var code = @"
var array1: Array[int] = [3]
var array2: Array = [3]
var array3: [int] = [3]";

            var @class = reader.ParseFileContent(code);

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void ParseMultilineIfTest()
        {
            var reader = new GDScriptReader();

            var code = @"var angle_degrees = 135
var quadrant = (
		""northeast"" if angle_degrees <= 90
		else ""southeast"" if angle_degrees <= 180
		else ""southwest"" if angle_degrees <= 270
		else ""northwest""
)

var position = Vector2(250, 350)
if (
		position.x > 200 and position.x < 400
		and position.y > 300 and position.y < 400
):
	pass";
            var @statementsList = reader.ParseStatementsList(code);

            Assert.AreEqual(4, @statementsList.Count);

            AssertHelper.CompareCodeStrings(code, @statementsList.ToString());
            AssertHelper.NoInvalidTokens(@statementsList);
        }

        [TestMethod]
        public void ParseSameLineIfTest()
        {
            var reader = new GDScriptReader();

            var code = @"var angle_degrees = 135
var quadrant = ""northeast"" if angle_degrees <= 90 else ""southeast"" if angle_degrees <= 180 else ""southwest"" if angle_degrees <= 270 else ""northwest""

var position = Vector2(250, 350)
if position.x > 200 and position.x < 400 and position.y > 300 and position.y < 400:
	pass";

            var @statements = reader.ParseStatements(code);

            Assert.AreEqual(4, @statements.Count);
            Assert.AreEqual(0, @statements.SelectMany(x => x.AllInvalidTokens).Count());
        }

        [TestMethod]
        public void ParseInlinedLambda()
        {
            var reader = new GDScriptReader();

            var code = @"var x = func(y): return y == 1 if true else func(y): return y == 2";

            var @statementsList = reader.ParseStatementsList(code);

            Assert.AreEqual(1, @statementsList.Count);

            var statement = @statementsList[0];

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDVariableDeclarationStatement));

            var declaration = (GDVariableDeclarationStatement)statement;
            var initializer = declaration.Initializer;

            Assert.IsNotNull(initializer);
            Assert.IsInstanceOfType(initializer, typeof(GDMethodExpression));

            var methodExpr = (GDMethodExpression)initializer;

            var expr = methodExpr.Expression;

            Assert.IsNotNull(expr);
            Assert.IsInstanceOfType(expr, typeof(GDReturnExpression));

            var returnInnerExpr = ((GDReturnExpression)expr).Expression;

            Assert.IsNotNull(returnInnerExpr);
            Assert.IsInstanceOfType(returnInnerExpr, typeof(GDIfExpression));

            var ifExpression = (GDIfExpression)returnInnerExpr;

            var falseExpression = ifExpression.FalseExpression;

            Assert.IsNotNull(falseExpression);
            Assert.IsInstanceOfType(falseExpression, typeof(GDMethodExpression));

            var inlinedMethodExpression = (GDMethodExpression)falseExpression;

            var inlinedMethodReturnExpression = inlinedMethodExpression.Expression;

            Assert.IsNotNull(inlinedMethodReturnExpression);
            Assert.IsInstanceOfType(inlinedMethodReturnExpression, typeof(GDReturnExpression));

            Assert.AreEqual("return y == 2", inlinedMethodReturnExpression.ToString());

            AssertHelper.CompareCodeStrings(code, @statementsList.ToString());
            AssertHelper.NoInvalidTokens(@statementsList);
        }
    }
}