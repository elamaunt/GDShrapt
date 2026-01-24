using GDShrapt.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Building
{
    /// <summary>
    /// Tests for building function parameters with types and default values.
    /// </summary>
    [TestClass]
    public class ParameterBuildingTests
    {
        [TestMethod]
        public void BuildParameter_UntypedWithoutDefault()
        {
            var param = GD.Declaration.Parameter("value");
            var code = param.ToString();

            Assert.AreEqual("value", code);
            AssertHelper.NoInvalidTokens(param);
        }

        [TestMethod]
        public void BuildParameter_TypedWithoutDefault()
        {
            var param = GD.Declaration.Parameter("count", GD.Type.Single("int"));
            var code = param.ToString();

            Assert.IsTrue(code.Contains("count"));
            Assert.IsTrue(code.Contains("int"));
            AssertHelper.NoInvalidTokens(param);
        }

        [TestMethod]
        public void BuildParameter_TypedWithDefault_Int()
        {
            var param = GD.Declaration.Parameter("health", GD.Type.Single("int"), GD.Expression.Number(100));
            var code = param.ToString();

            Assert.IsTrue(code.Contains("health"));
            Assert.IsTrue(code.Contains("int"));
            Assert.IsTrue(code.Contains("100"));
            AssertHelper.NoInvalidTokens(param);
        }

        [TestMethod]
        public void BuildParameter_TypedWithDefault_String()
        {
            var param = GD.Declaration.Parameter("name", GD.Type.Single("String"), GD.Expression.String("Player"));
            var code = param.ToString();

            Assert.IsTrue(code.Contains("name"));
            Assert.IsTrue(code.Contains("String"));
            Assert.IsTrue(code.Contains("\"Player\""));
            AssertHelper.NoInvalidTokens(param);
        }

        [TestMethod]
        public void BuildParameter_TypedWithDefault_Bool()
        {
            var param = GD.Declaration.Parameter("enabled", GD.Type.Single("bool"), GD.Expression.Bool(true));
            var code = param.ToString();

            Assert.IsTrue(code.Contains("enabled"));
            Assert.IsTrue(code.Contains("bool"));
            Assert.IsTrue(code.Contains("true"));
            AssertHelper.NoInvalidTokens(param);
        }

        [TestMethod]
        public void BuildParameter_TypedWithDefault_Float()
        {
            var param = GD.Declaration.Parameter("speed", GD.Type.Single("float"), GD.Expression.Number(1.5));
            var code = param.ToString();

            Assert.IsTrue(code.Contains("speed"));
            Assert.IsTrue(code.Contains("float"));
            // Float may be formatted as "1.5" or "1,5" depending on locale
            Assert.IsTrue(code.Contains("1.5") || code.Contains("1,5"));
            AssertHelper.NoInvalidTokens(param);
        }

        [TestMethod]
        public void BuildParameter_ArrayType()
        {
            var param = GD.Declaration.Parameter("items", GD.Type.Array("String"));
            var code = param.ToString();

            Assert.IsTrue(code.Contains("items"));
            Assert.IsTrue(code.Contains("Array"));
            Assert.IsTrue(code.Contains("String"));
            AssertHelper.NoInvalidTokens(param);
        }

        [TestMethod]
        public void BuildParameter_ArrayTypeWithDefault()
        {
            // Use typed array initializer with square brackets
            var param = GD.Declaration.Parameter("scores", GD.Type.Array("int"),
                GD.Expression.Array(a => a
                    .AddSquareOpenBracket()
                    .AddSquareCloseBracket()
                ));
            var code = param.ToString();

            Assert.IsTrue(code.Contains("scores"));
            Assert.IsTrue(code.Contains("Array"));
            Assert.IsTrue(code.Contains("int"));
            // Empty array generates "[]"
            Assert.IsTrue(code.Contains("[]"));
            AssertHelper.NoInvalidTokens(param);
        }

        [TestMethod]
        public void BuildParameter_Vector2Type()
        {
            var param = GD.Declaration.Parameter("position", GD.Type.Single("Vector2"));
            var code = param.ToString();

            Assert.IsTrue(code.Contains("position"));
            Assert.IsTrue(code.Contains("Vector2"));
            AssertHelper.NoInvalidTokens(param);
        }

        [TestMethod]
        public void BuildMethod_WithTypedParameters()
        {
            var method = GD.Declaration.Method(m => m
                .AddFuncKeyword()
                .AddSpace()
                .Add(GD.Syntax.Identifier("move"))
                .AddOpenBracket()
                .AddParameters(GD.List.Parameters(
                    GD.Declaration.Parameter("direction", GD.Type.Single("Vector2")),
                    GD.Declaration.Parameter("speed", GD.Type.Single("float"), GD.Expression.Number(1.0))
                ))
                .AddCloseBracket()
                .AddStatements(s => s
                    .AddNewLine()
                    .AddIntendation()
                    .AddPass()
                )
            );

            method.UpdateIntendation();
            var code = method.ToString();

            Assert.IsTrue(code.Contains("func move"));
            Assert.IsTrue(code.Contains("direction"));
            Assert.IsTrue(code.Contains("Vector2"));
            Assert.IsTrue(code.Contains("speed"));
            Assert.IsTrue(code.Contains("float"));
            Assert.IsTrue(code.Contains("1"));
            AssertHelper.NoInvalidTokens(method);
        }
    }
}
