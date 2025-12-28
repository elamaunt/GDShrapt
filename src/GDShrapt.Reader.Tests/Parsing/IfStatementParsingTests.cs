using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Parsing
{
    /// <summary>
    /// Tests for parsing if/elif/else statements.
    /// </summary>
    [TestClass]
    public class IfStatementParsingTests
    {
        [TestMethod]
        public void ParseIfStatement_Simple()
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
        public void ParseIfStatement_WithElse()
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
        public void ParseIfStatement_InlineReturn()
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
        public void ParseIfStatement_InlineWithElseBlock()
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
        public void ParseIfStatement_MultipleIfStatements()
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
        public void ParseIfStatement_WithElif()
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
        public void ParseIfExpression_Ternary()
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
        public void ParseIfExpression_InVariable()
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
        public void ParseIfStatement_Multiline()
        {
            var reader = new GDScriptReader();

            var code = @"func f():
	if \
	a:
		pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseIfStatement_SameLinePass()
        {
            var reader = new GDScriptReader();

            var code = @"func f():
	if a: pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }
    }
}
