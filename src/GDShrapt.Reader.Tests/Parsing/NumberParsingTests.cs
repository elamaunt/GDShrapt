using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Parsing
{
    /// <summary>
    /// Tests for parsing numbers and operators.
    /// </summary>
    [TestClass]
    public class NumberParsingTests
    {
        [TestMethod]
        [DataRow("1234")]
        [DataRow("1_2_3_4")]
        public void ParseNumber_Decimal(string code)
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
        public void ParseNumber_Hexadecimal(string code)
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
        public void ParseNumber_Binary(string code)
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
        public void ParseNumber_Double(string code)
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
                "-",
                "+",
                "%",
                "^",
                "&",
                "|",
                "==",
                "!=",
                ">=",
                "<=",
                "<<",
                ">>",
                "&&",
                "||",
                "**",
                "+=",
                "-=",
                "*=",
                "/=",
                "%=",
                "<<=",
                ">>=",
                "&=",
                "|=",
                "^=",
                "in")]
        public void ParseOperator_Dual(params string[] operators)
        {
            var reader = new GDScriptReader();

            foreach (var op in operators)
            {
                var code = $"a {op} b";
                var expression = reader.ParseExpression(code);

                Assert.IsNotNull(expression);
                Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExpression));

                AssertHelper.CompareCodeStrings(code, expression.ToString());
                AssertHelper.NoInvalidTokens(expression);
            }
        }

        [TestMethod]
        [DataRow("not",
                "-",
                "!",
                "~")]
        public void ParseOperator_Single(params string[] operators)
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
    }
}
