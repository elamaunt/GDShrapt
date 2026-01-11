using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Tests for parsing match statements.
    /// </summary>
    [TestClass]
    public class MatchStatementParsingTests
    {
        [TestMethod]
        public void ParseMatchStatement_WithSimpleCases()
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

            Assert.AreEqual("x", matchStatement.Value.ToString());
            Assert.AreEqual(3, matchStatement.Cases.Count);

            Assert.AreEqual("1", matchStatement.Cases[0].Conditions.ToString().Trim());
            Assert.AreEqual("2", matchStatement.Cases[1].Conditions.ToString().Trim());
            Assert.AreEqual("\"test\"", matchStatement.Cases[2].Conditions.ToString().Trim());

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void ParseMatchStatement_WithTypeOfExpression()
        {
            var reader = new GDScriptReader();

            var code = @"match typeof(x):
    TYPE_REAL:
        print(""float"")
    TYPE_STRING:
        print(""string"")
    TYPE_ARRAY:
        print(""array"")";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDMatchStatement));

            var matchStatement = (GDMatchStatement)statement;

            Assert.IsInstanceOfType(matchStatement.Value, typeof(GDCallExpression));
            Assert.AreEqual("typeof(x)", matchStatement.Value.ToString());
            Assert.AreEqual(3, matchStatement.Cases.Count);

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void ParseMatchStatement_WithMultiplePatterns()
        {
            var reader = new GDScriptReader();

            var code = @"match x:
    1, 2, 3:
        print(""It's 1 - 3"")
    ""Sword"", ""Move"":
        print(""Sword or Move"")";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDMatchStatement));

            var matchStatement = (GDMatchStatement)statement;

            var cases = matchStatement.Cases;
            Assert.AreEqual(2, cases.Count);

            Assert.AreEqual("1, 2, 3", cases[0].Conditions.ToString().Trim());
            Assert.AreEqual("\"Sword\", \"Move\"", cases[1].Conditions.ToString().Trim());

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void ParseMatchStatement_WithWildcardPattern()
        {
            var reader = new GDScriptReader();

            var code = @"match x:
    1:
        print(""It's one!"")
    2:
        print(""It's one times two!"")
    _:
        print(""It's not 1 or 2. I don't care tbh."")";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDMatchStatement));

            var matchStatement = (GDMatchStatement)statement;

            var cases = matchStatement.Cases;
            Assert.AreEqual(3, cases.Count);

            Assert.AreEqual("_", cases[2].Conditions.ToString().Trim());

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void ParseMatchStatement_WithArrayPattern()
        {
            var reader = new GDScriptReader();

            var code = @"match x:
    []:
        print(""Empty array"")
    [1, 3, ""test"", null]:
        print(""Very specific array"")
    [var start, _, ""test""]:
        print(""First element is "", start, "", and the last is \""test\"""")
    [42, ..]:
        print(""Open ended array"")";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDMatchStatement));

            var matchStatement = (GDMatchStatement)statement;

            var cases = matchStatement.Cases;
            Assert.AreEqual(4, cases.Count);

            Assert.AreEqual("[]", cases[0].Conditions.ToString().Trim());
            Assert.AreEqual("[1, 3, \"test\", null]", cases[1].Conditions.ToString().Trim());
            Assert.AreEqual("[var start, _, \"test\"]", cases[2].Conditions.ToString().Trim());
            Assert.AreEqual("[42, ..]", cases[3].Conditions.ToString().Trim());

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void ParseMatchStatement_WithDictionaryPattern()
        {
            var reader = new GDScriptReader();

            var code = @"match x:
    {}:
        print(""Empty dict"")
    {""name"": ""Dennis""}:
        print(""The name is Dennis"")
    {""name"": ""Dennis"", ""age"": var age}:
        print(""Dennis is "", age, "" years old."")
    {""name"", ""age""}:
        print(""Has a name and an age, but is not Dennis :("")
    {""key"": ""godotisawesome"", ..}:
        print(""I only checked for one entry."")";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDMatchStatement));

            var matchStatement = (GDMatchStatement)statement;

            var cases = matchStatement.Cases;
            Assert.AreEqual(5, cases.Count);

            Assert.AreEqual("{}", cases[0].Conditions.ToString().Trim());
            Assert.AreEqual("{\"name\": \"Dennis\"}", cases[1].Conditions.ToString().Trim());
            Assert.AreEqual("{\"name\": \"Dennis\", \"age\": var age}", cases[2].Conditions.ToString().Trim());
            Assert.AreEqual("{\"name\", \"age\"}", cases[3].Conditions.ToString().Trim());
            Assert.AreEqual("{\"key\": \"godotisawesome\", ..}", cases[4].Conditions.ToString().Trim());

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void ParseMatchStatement_WithPatternGuard()
        {
            var reader = new GDScriptReader();

            var code = @"match point:
    [0, 0]:
        print(""Origin"")
    [0, var y]:
        print(""Point on Y-axis at %s"" % y)
    [var x, 0]:
        print(""Point on X-axis at %s"" % x)
    [var x, var y] when y == x:
        print(""Point on diagonal (%s, %s)"" % [x, y])
    [var x, var y] when y == -x:
        print(""Point on inverse diagonal (%s, %s)"" % [x, y])
    [var x, var y]:
        print(""Point (%s, %s)"" % [x, y])";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDMatchStatement));

            var matchStatement = (GDMatchStatement)statement;

            var cases = matchStatement.Cases;

            Assert.AreEqual(6, cases.Count);

            var one = cases[3];

            Assert.IsNotNull(one.When);
            Assert.IsNotNull(one.GuardCondition);
            Assert.AreEqual("y == x", one.GuardCondition.ToString());
            Assert.AreEqual("[var x, var y] ", one.Conditions.ToString());

            var two = cases[4];

            Assert.IsNotNull(two.When);
            Assert.IsNotNull(two.GuardCondition);
            Assert.AreEqual("y == -x", two.GuardCondition.ToString());
            Assert.AreEqual("[var x, var y] ", two.Conditions.ToString());

            var three = cases[5];
            Assert.AreEqual("[var x, var y]", three.Conditions.ToString());

            Assert.AreEqual(1, three.Statements.Count);
            Assert.AreEqual("print(\"Point (%s, %s)\" % [x, y])", three.Statements[0].ToString());

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void ParseMatchStatement_WithDefaultOperator()
        {
            var reader = new GDScriptReader();

            var code = @"match point:
    [0, 0]:
        print(""Origin"")
    [_, var b]:
        print(""_"")
    _:
        print(""Default"")";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDMatchStatement));

            var matchStatement = (GDMatchStatement)statement;

            Assert.AreEqual(3, matchStatement.Cases.Count);

            Assert.AreEqual("[0, 0]", matchStatement.Cases[0].Conditions.ToString().Trim());
            Assert.AreEqual("[_, var b]", matchStatement.Cases[1].Conditions.ToString().Trim());
            Assert.AreEqual("_", matchStatement.Cases[2].Conditions.ToString().Trim());

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void ParseMatchStatement_InMethod()
        {
            var reader = new GDScriptReader();

            var code = @"func get_velocity() -> Vector2:
	match type:
		Type.BOUNCE:
			return Vector2(1, 0)
		Type.HOMING:
			return Vector2.ZERO
		_:
			return direction.normalized() * speed";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }
    }
}
