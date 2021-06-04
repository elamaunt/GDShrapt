using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
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
            Assert.AreEqual("ResourceFormatSaver", @class.Extends?.Type?.Sequence);
            Assert.AreEqual("HTerrainDataSaver", @class.ClassName?.Identifier?.Sequence);
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
            Assert.AreEqual(GDDualOperatorType.And2, @dualOperator.OperatorType);

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

            Assert.IsNotNull(expression);
            Assert.AreEqual(code, expression.ToString());

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

        [TestMethod]
        public void MatchStatementTest()
        {
            var reader = new GDScriptReader();

            var code = @"
match x:
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
            Assert.IsInstanceOfType(matchStatement.Cases[2].Conditions[0], typeof(GDVariableDeclarationExpression));
            Assert.AreEqual("var new_var", matchStatement.Cases[2].Conditions[0].ToString());

            Assert.AreEqual(1, matchStatement.Cases[0].Statements.Count);
            Assert.AreEqual(1, matchStatement.Cases[1].Statements.Count);
            Assert.AreEqual(1, matchStatement.Cases[2].Statements.Count);
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
            Assert.IsFalse(stringExpression.String.Multiline);
            Assert.AreEqual(GDStringBoundingChar.DoubleQuotas, stringExpression.String.BoundingChar);
            Assert.AreEqual("test", stringExpression.String.Value);
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
            Assert.IsFalse(stringExpression.String.Multiline);
            Assert.AreEqual(GDStringBoundingChar.SingleQuotas, stringExpression.String.BoundingChar);
            Assert.AreEqual("te\"\"st", stringExpression.String.Value);
        }

        [TestMethod]
        public void MultilineStringTest()
        {
            var reader = new GDScriptReader();

            var code = "\"\"\"te\"\"st\"\"\"";

            var statement = reader.ParseExpression(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDStringExpression));

            var stringExpression = (GDStringExpression)statement;

            Assert.IsNotNull(stringExpression.String);
            Assert.IsTrue(stringExpression.String.Multiline);
            Assert.AreEqual(GDStringBoundingChar.DoubleQuotas, stringExpression.String.BoundingChar);
            Assert.AreEqual("te\"\"st", stringExpression.String.Value);
        }

        [TestMethod]
        public void MultilineStringTest2()
        {
            var reader = new GDScriptReader();
            
            var code = "\'\'\'te\'\"st\'\'\'";

            var statement = reader.ParseExpression(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDStringExpression));

            var stringExpression = (GDStringExpression)statement;

            Assert.IsNotNull(stringExpression.String);
            Assert.IsTrue(stringExpression.String.Multiline);
            Assert.AreEqual(GDStringBoundingChar.SingleQuotas, stringExpression.String.BoundingChar);
            Assert.AreEqual("te'\"st", stringExpression.String.Value);
        }

        [TestMethod]
        public void VariableExportTest()
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
            Assert.AreEqual(1, classDeclaration.Members.Count);
            Assert.AreEqual("Test", classDeclaration.ClassName.Identifier.Sequence);

            Assert.IsNotNull(classDeclaration.ClassName.Icon);
            Assert.AreEqual("res://interface/icons/item.png", classDeclaration.ClassName.Icon.Value);
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
            Assert.AreEqual(1, classDeclaration.Members.Count);
            Assert.AreEqual("Test", classDeclaration.Extends.Type.Sequence);
        }

        [TestMethod]
        public void ExtendsTest2()
        {
            var reader = new GDScriptReader();

            var code = "extends \"res://path/to/character.gd\"";
            var classDeclaration = reader.ParseFileContent(code);

            Assert.IsNotNull(classDeclaration);
            Assert.IsNotNull(classDeclaration.Extends);
            Assert.IsNotNull(classDeclaration.Extends.Path);
            Assert.AreEqual(1, classDeclaration.Members.Count);
            Assert.AreEqual("res://path/to/character.gd", classDeclaration.Extends.Path.Value);
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
                "|=")]
        public void DualOperatorsTest(params string[] operators)
        {
            var reader = new GDScriptReader();

            foreach (var op in operators)
            {
                var code = $"a {op} b";

                var expression = reader.ParseExpression(code);
                Assert.IsNotNull(expression);
                Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExression));

                var dualOperatorExpression = (GDDualOperatorExression)expression;

                Assert.AreEqual("a", dualOperatorExpression.LeftExpression.ToString());
                Assert.AreEqual("b", dualOperatorExpression.RightExpression.ToString());
                Assert.AreEqual(op, dualOperatorExpression.OperatorType.Print());
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
            }
        }

        [TestMethod]
        public void PropertyTest()
        {
            var reader = new GDScriptReader();

            var code = "var speed = 1 setget set_speed get_speed";

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

            Assert.IsNotNull(variableDeclaration.SetMethodIdentifier);
            Assert.IsNotNull(variableDeclaration.GetMethodIdentifier);

            Assert.AreEqual("set_speed", variableDeclaration.SetMethodIdentifier.Sequence);
            Assert.AreEqual("get_speed", variableDeclaration.GetMethodIdentifier.Sequence);
        }

        [TestMethod]
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
            Assert.AreEqual("100.1e+10", number.ValueAsString);

            Assert.IsFalse(variableDeclaration.IsConstant);
            Assert.IsTrue(variableDeclaration.IsExported);
            Assert.IsFalse(variableDeclaration.HasOnReadyInitialization);

            Assert.IsNotNull(variableDeclaration.SetMethodIdentifier);
            Assert.IsNull(variableDeclaration.GetMethodIdentifier);

            Assert.AreEqual("set_height", variableDeclaration.SetMethodIdentifier.Sequence);
        }

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
            Assert.IsNotNull(signalDeclaration.Parameters.Parameters);
            Assert.AreEqual(2, signalDeclaration.Parameters.Parameters.Count);

            Assert.AreEqual("value", signalDeclaration.Parameters.Parameters[0]?.Identifier?.Sequence);
            Assert.AreEqual("other_value", signalDeclaration.Parameters.Parameters[1]?.Identifier?.Sequence);
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

            Assert.IsInstanceOfType(ifExpression.Condition, typeof(GDDualOperatorExression));

            Assert.AreEqual("3 + 4", ifExpression.TrueExpression.ToString());
            Assert.AreEqual("-y != 10", ifExpression.Condition.ToString());
            Assert.AreEqual("n", ifExpression.FalseExpression.ToString());
        }

        [TestMethod]
        public void NegativeNumberTest()
        {
            var reader = new GDScriptReader();

            var code = "- 10";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDNumberExpression));

            var numberExpression = (GDNumberExpression)expression;

            Assert.IsNotNull(numberExpression.Number);
            Assert.AreEqual(GDNumberType.LongDecimal, numberExpression.Number.ResolveNumberType());
            Assert.AreEqual(-10, numberExpression.Number.ValueInt64);
        }

        [TestMethod]
        public void BracketsTest()
        {
            var reader = new GDScriptReader();

            var code = "13 + -2 * -(10-20) / 3.0";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExression));

            var dualOperator = (GDDualOperatorExression)expression;

            Assert.AreEqual("13", dualOperator.LeftExpression.ToString());

            Assert.AreEqual(GDDualOperatorType.Addition, dualOperator.OperatorType);

            Assert.IsInstanceOfType(dualOperator.RightExpression, typeof(GDDualOperatorExression));

            var dualOperator2 = (GDDualOperatorExression)dualOperator.RightExpression;

            Assert.AreEqual("3.0", dualOperator2.RightExpression.ToString());

            Assert.IsInstanceOfType(dualOperator2.LeftExpression, typeof(GDDualOperatorExression));

            var dualOperator3 = (GDDualOperatorExression)dualOperator2.LeftExpression;

            Assert.AreEqual("-2", dualOperator3.LeftExpression.ToString());
            Assert.IsInstanceOfType(dualOperator3.RightExpression, typeof(GDSingleOperatorExpression));

            var singleOperator = (GDSingleOperatorExpression)dualOperator3.RightExpression;

            Assert.AreEqual(GDSingleOperatorType.Negate, singleOperator.OperatorType);
            Assert.IsInstanceOfType(singleOperator.TargetExpression, typeof(GDBracketExpression));

            Assert.AreEqual("(10 - 20)", singleOperator.TargetExpression.ToString());
        }
    }
}
