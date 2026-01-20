using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Tests for parsing literals: arrays, dictionaries, strings.
    /// </summary>
    [TestClass]
    public class LiteralParsingTests
    {
        [TestMethod]
        public void ParseArray_WithMixedElements()
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
        public void ParseDictionary_WithColonSyntax()
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
        public void ParseDictionary_WithEqualsSyntax()
        {
            var reader = new GDScriptReader();

            var code = @"var d = { ""Test"" = 123 }";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDVariableDeclarationStatement));

            var variableDeclaration = (GDVariableDeclarationStatement)statement;

            Assert.IsNotNull(variableDeclaration.Initializer);
            Assert.IsInstanceOfType(variableDeclaration.Initializer, typeof(GDDictionaryInitializerExpression));

            var dictionaryInitializer = (GDDictionaryInitializerExpression)variableDeclaration.Initializer;

            Assert.AreEqual(1, dictionaryInitializer.KeyValues.Count);

            Assert.AreEqual("\"Test\"", dictionaryInitializer.KeyValues[0].Key.ToString());
            Assert.AreEqual("123", dictionaryInitializer.KeyValues[0].Value.ToString());

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void ParseDictionary_WithIdentifierKey()
        {
            var reader = new GDScriptReader();

            var code = @"var d = { test = 123 }";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDVariableDeclarationStatement));

            var variableDeclaration = (GDVariableDeclarationStatement)statement;

            Assert.IsNotNull(variableDeclaration.Initializer);
            Assert.IsInstanceOfType(variableDeclaration.Initializer, typeof(GDDictionaryInitializerExpression));

            var dictionaryInitializer = (GDDictionaryInitializerExpression)variableDeclaration.Initializer;

            Assert.AreEqual(1, dictionaryInitializer.KeyValues.Count);

            Assert.AreEqual("test", dictionaryInitializer.KeyValues[0].Key.ToString());
            Assert.AreEqual("123", dictionaryInitializer.KeyValues[0].Value.ToString());

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void ParseString_WithDoubleQuotes()
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
        public void ParseString_WithSingleQuotes()
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
        public void ParseString_WithTripleDoubleQuotes()
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
        public void ParseString_WithLineContinuation()
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
        public void ParseString_WithTripleSingleQuotes()
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

        [TestMethod]
        public void ParseString_WithEscapeSequences()
        {
            var reader = new GDScriptReader();

            var code = @"print(""test\n\r\""\'\t\\"")";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        #region StringName Tests

        [TestMethod]
        public void ParseStringName_DoubleQuotes()
        {
            var reader = new GDScriptReader();

            var code = "&\"signal_name\"";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDStringNameExpression));

            var stringNameExpr = (GDStringNameExpression)expression;

            Assert.IsNotNull(stringNameExpr.Ampersand);
            Assert.IsNotNull(stringNameExpr.String);
            Assert.AreEqual("signal_name", stringNameExpr.String.Sequence);
            Assert.AreEqual("signal_name", stringNameExpr.Sequence);

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void ParseStringName_SingleQuotes()
        {
            var reader = new GDScriptReader();

            var code = "&'property_name'";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDStringNameExpression));

            var stringNameExpr = (GDStringNameExpression)expression;

            Assert.IsNotNull(stringNameExpr.Ampersand);
            Assert.IsNotNull(stringNameExpr.String);
            Assert.AreEqual("property_name", stringNameExpr.String.Sequence);

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void ParseStringName_InVariableDeclaration()
        {
            var reader = new GDScriptReader();

            var code = "var sig = &\"my_signal\"";

            var statement = reader.ParseStatement(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDVariableDeclarationStatement));

            var varDecl = (GDVariableDeclarationStatement)statement;
            Assert.IsInstanceOfType(varDecl.Initializer, typeof(GDStringNameExpression));

            var stringNameExpr = (GDStringNameExpression)varDecl.Initializer;
            Assert.AreEqual("my_signal", stringNameExpr.Sequence);

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void ParseStringName_InCallExpression()
        {
            var reader = new GDScriptReader();

            var code = "connect(&\"pressed\", _on_pressed)";

            var statement = reader.ParseExpression(code);

            Assert.IsNotNull(statement);
            Assert.IsInstanceOfType(statement, typeof(GDCallExpression));

            var callExpr = (GDCallExpression)statement;
            var firstArg = callExpr.Parameters?.FirstOrDefault();

            Assert.IsNotNull(firstArg);
            Assert.IsInstanceOfType(firstArg, typeof(GDStringNameExpression));

            var stringNameExpr = (GDStringNameExpression)firstArg;
            Assert.AreEqual("pressed", stringNameExpr.Sequence);

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }

        [TestMethod]
        public void ParseStringName_WithSpaces()
        {
            var reader = new GDScriptReader();

            var code = "& \"value_with_space\"";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDStringNameExpression));

            var stringNameExpr = (GDStringNameExpression)expression;
            Assert.AreEqual("value_with_space", stringNameExpr.Sequence);

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void ParseStringName_EmptyString()
        {
            var reader = new GDScriptReader();

            var code = "&\"\"";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDStringNameExpression));

            var stringNameExpr = (GDStringNameExpression)expression;

            Assert.IsNotNull(stringNameExpr.Ampersand);
            Assert.IsNotNull(stringNameExpr.String);
            Assert.AreEqual("", stringNameExpr.String.Sequence);
            Assert.AreEqual("", stringNameExpr.Sequence);

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        #endregion
    }
}
