using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Parsing
{
    /// <summary>
    /// Tests for parsing for and while loops.
    /// </summary>
    [TestClass]
    public class LoopStatementParsingTests
    {
        [TestMethod]
        public void ParseForStatement_WithArrayCollection()
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
        public void ParseForStatement_WithRangeCollection()
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
        public void ParseWhileStatement_WithBoolCondition()
        {
            var reader = new GDScriptReader();

            var code = @"while true:
    print(a)";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDWhileStatement));

            var whileStatement = (GDWhileStatement)statement;

            Assert.IsInstanceOfType(whileStatement.Condition, typeof(GDBoolExpression));
            Assert.AreEqual("true", whileStatement.Condition.ToString());

            Assert.AreEqual(1, whileStatement.Statements.Count);
            Assert.IsInstanceOfType(whileStatement.Statements[0], typeof(GDExpressionStatement));

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void ParseWhileStatement_WithComplexCondition()
        {
            var reader = new GDScriptReader();

            var code = @"while a.b == null and b.c == null:
    print(a)
    print(b)";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDWhileStatement));

            var whileStatement = (GDWhileStatement)statement;

            Assert.IsInstanceOfType(whileStatement.Condition, typeof(GDDualOperatorExpression));
            Assert.AreEqual("a.b == null and b.c == null", whileStatement.Condition.ToString());

            Assert.AreEqual(2, whileStatement.Statements.Count);
            Assert.IsInstanceOfType(whileStatement.Statements[0], typeof(GDExpressionStatement));
            Assert.IsInstanceOfType(whileStatement.Statements[1], typeof(GDExpressionStatement));

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }
    }
}
