using GDShrapt.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Building
{
    /// <summary>
    /// Tests for string expressions with edge cases: escapes, unicode, multiline.
    /// </summary>
    [TestClass]
    public class StringEdgeCaseTests
    {
        #region Basic Strings

        [TestMethod]
        public void BuildString_Empty()
        {
            var expr = GD.Expression.String("");
            Assert.AreEqual("\"\"", expr.ToString());
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildString_Simple()
        {
            var expr = GD.Expression.String("Hello World");
            Assert.AreEqual("\"Hello World\"", expr.ToString());
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildString_SingleQuotes()
        {
            var expr = GD.Expression.String("Hello", GDStringBoundingChar.SingleQuotas);
            Assert.AreEqual("'Hello'", expr.ToString());
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Escape Sequences

        [TestMethod]
        public void BuildString_WithNewline()
        {
            var expr = GD.Expression.String("Line1\\nLine2");
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("\\n"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildString_WithTab()
        {
            var expr = GD.Expression.String("Col1\\tCol2");
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("\\t"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildString_WithBackslash()
        {
            var expr = GD.Expression.String("C:\\\\path\\\\file");
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("\\\\"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildString_WithQuotes()
        {
            var expr = GD.Expression.String("Say \\\"Hello\\\"");
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("\\\""));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Multiline Strings

        [TestMethod]
        public void BuildString_Multiline_DoubleQuotes()
        {
            var expr = GD.Expression.MultilineString("Line1\nLine2\nLine3");
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("\"\"\""));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildString_Multiline_SingleQuotes()
        {
            var expr = GD.Expression.MultilineStringSingleQuote("Line1\nLine2");
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("'''"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildString_Multiline_Empty()
        {
            var expr = GD.Expression.MultilineString("");
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("\"\"\""));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region StringName

        [TestMethod]
        public void BuildStringName_Simple()
        {
            var expr = GD.Expression.StringName("my_signal");
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("&"));
            Assert.IsTrue(code.Contains("my_signal"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildStringName_NodePath()
        {
            var expr = GD.Expression.StringName("Player/Sprite");
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("&"));
            Assert.IsTrue(code.Contains("Player/Sprite"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Special Characters

        [TestMethod]
        public void BuildString_WithSpaces()
        {
            var expr = GD.Expression.String("  leading and trailing  ");
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("  leading and trailing  "));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildString_WithNumbers()
        {
            var expr = GD.Expression.String("Player 1");
            Assert.AreEqual("\"Player 1\"", expr.ToString());
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildString_PathLike()
        {
            var expr = GD.Expression.String("res://scenes/level_1.tscn");
            Assert.AreEqual("\"res://scenes/level_1.tscn\"", expr.ToString());
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Strings in Context

        [TestMethod]
        public void BuildVariable_WithMultilineString()
        {
            var decl = GD.Declaration.Variable("description", GD.Expression.MultilineString("Long\ntext\nhere"));
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("var description"));
            Assert.IsTrue(code.Contains("\"\"\""));
            AssertHelper.NoInvalidTokens(decl);
        }

        [TestMethod]
        public void BuildConst_WithStringName()
        {
            var decl = GD.Declaration.Const("SIGNAL_NAME", GD.Expression.StringName("health_changed"));
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("const SIGNAL_NAME"));
            Assert.IsTrue(code.Contains("&"));
            AssertHelper.NoInvalidTokens(decl);
        }

        #endregion

        #region String Format/Interpolation

        [TestMethod]
        public void BuildString_FormatOperator_SingleValue()
        {
            // Test: "Hello %s" % name
            var expr = GD.Expression.DualOperator(
                GD.Expression.String("Hello %s"),
                GD.Syntax.DualOperator(GDDualOperatorType.Mod),
                GD.Expression.Identifier("name")
            );

            var code = expr.ToString();
            Assert.IsTrue(code.Contains("\"Hello %s\""));
            Assert.IsTrue(code.Contains("%"));
            Assert.IsTrue(code.Contains("name"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildString_FormatOperator_MultipleValues()
        {
            // Test: "%s scored %d points" % [name, score]
            var expr = GD.Expression.DualOperator(
                GD.Expression.String("%s scored %d points"),
                GD.Syntax.DualOperator(GDDualOperatorType.Mod),
                GD.Expression.Array(
                    GD.Expression.Identifier("name"),
                    GD.Expression.Identifier("score")
                )
            );

            var code = expr.ToString();
            Assert.IsTrue(code.Contains("\"%s scored %d points\""));
            Assert.IsTrue(code.Contains("[name, score]"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Unicode and Special Escapes

        [TestMethod]
        public void BuildString_WithUnicodeEscape()
        {
            // Note: GDScript uses \u for unicode
            var expr = GD.Expression.String("\\u0041\\u0042\\u0043");  // ABC in unicode escapes
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("\\u"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildString_WithHexEscape()
        {
            var expr = GD.Expression.String("\\x41\\x42");  // AB in hex escapes
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("\\x"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildString_WithCarriageReturn()
        {
            var expr = GD.Expression.String("Line1\\r\\nLine2");
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("\\r\\n"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Multiline String Edge Cases

        [TestMethod]
        public void BuildString_Multiline_WithEmbeddedDoubleQuotes()
        {
            // Triple-quoted strings can contain regular double quotes
            var expr = GD.Expression.MultilineString("He said \"Hello\" to her");
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("\"\"\""));
            Assert.IsTrue(code.Contains("said"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildString_Multiline_WithEmbeddedSingleQuotes()
        {
            var expr = GD.Expression.MultilineStringSingleQuote("It's a test");
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("'''"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildString_Multiline_LongText()
        {
            var longText = @"This is a long multiline text
that spans multiple lines
and includes various content
like numbers: 123
and symbols: @#$%";

            var expr = GD.Expression.MultilineString(longText);
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("\"\"\""));
            Assert.IsTrue(code.Contains("multiline"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region StringName Edge Cases

        [TestMethod]
        public void BuildStringName_WithPath()
        {
            var expr = GD.Expression.StringName("../Player/Sprite2D");
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("&"));
            Assert.IsTrue(code.Contains("../Player/Sprite2D"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildStringName_WithSpecialChars()
        {
            var expr = GD.Expression.StringName("signal_with_underscore_123");
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("&"));
            Assert.IsTrue(code.Contains("signal_with_underscore_123"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildStringName_InSignalConnect()
        {
            // Test: signal.connect(&"on_event")
            var expr = GD.Expression.Call(
                GD.Expression.Member(
                    GD.Expression.Identifier("button"),
                    GD.Syntax.Identifier("connect")
                ),
                GD.Expression.StringName("pressed"),
                GD.Expression.Identifier("callback")
            );

            var code = expr.ToString();
            Assert.IsTrue(code.Contains("button.connect"));
            Assert.IsTrue(code.Contains("&"));
            Assert.IsTrue(code.Contains("pressed"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region String in Complex Expressions

        [TestMethod]
        public void BuildString_InDictionaryKey()
        {
            var expr = GD.Expression.Dictionary(
                GD.Expression.KeyValue(GD.Expression.String("name"), GD.Expression.String("Player")),
                GD.Expression.KeyValue(GD.Expression.String("health"), GD.Expression.Number(100)),
                GD.Expression.KeyValue(
                    GD.Expression.String("position"),
                    GD.Expression.Member(GD.Expression.Identifier("Vector2"), GD.Syntax.Identifier("ZERO"))
                )
            );

            var code = expr.ToString();
            Assert.IsTrue(code.Contains("\"name\""));
            Assert.IsTrue(code.Contains("\"Player\""));
            Assert.IsTrue(code.Contains("\"health\""));
            Assert.IsTrue(code.Contains("\"position\""));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildString_InArrayOfStrings()
        {
            var expr = GD.Expression.Array(
                GD.Expression.String("apple"),
                GD.Expression.String("banana"),
                GD.Expression.String("cherry")
            );

            var code = expr.ToString();
            Assert.IsTrue(code.Contains("\"apple\""));
            Assert.IsTrue(code.Contains("\"banana\""));
            Assert.IsTrue(code.Contains("\"cherry\""));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildString_Concatenation()
        {
            // Test: "Hello" + " " + "World"
            var expr = GD.Expression.DualOperator(
                GD.Expression.DualOperator(
                    GD.Expression.String("Hello"),
                    GD.Syntax.DualOperator(GDDualOperatorType.Addition),
                    GD.Expression.String(" ")
                ),
                GD.Syntax.DualOperator(GDDualOperatorType.Addition),
                GD.Expression.String("World")
            );

            var code = expr.ToString();
            Assert.IsTrue(code.Contains("\"Hello\""));
            Assert.IsTrue(code.Contains("\" \""));
            Assert.IsTrue(code.Contains("\"World\""));
            Assert.IsTrue(code.Contains("+"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Round-Trip Tests

        private readonly GDScriptReader _reader = new GDScriptReader();

        [TestMethod]
        public void RoundTrip_StringFormat()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Variable("message", GD.Expression.DualOperator(
                    GD.Expression.String("Score: %d"),
                    GD.Syntax.DualOperator(GDDualOperatorType.Mod),
                    GD.Expression.Number(100)
                ))
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_MultilineString()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Variable("text", GD.Expression.MultilineString("Line1\nLine2\nLine3"))
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_StringName()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Const("SIGNAL", GD.Expression.StringName("value_changed"))
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        #endregion
    }
}
