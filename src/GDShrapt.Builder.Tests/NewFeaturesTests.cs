using GDShrapt.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Building
{
    /// <summary>
    /// Tests for new Builder features: keywords, abstract support, line continuation, etc.
    /// </summary>
    [TestClass]
    public class NewFeaturesTests
    {
        #region Keywords Tests

        [TestMethod]
        public void Keyword_Array_ReturnsCorrectKeyword()
        {
            var keyword = GD.Keyword.Array;
            Assert.AreEqual("Array", keyword.Sequence);
        }

        [TestMethod]
        public void Keyword_Await_ReturnsCorrectKeyword()
        {
            var keyword = GD.Keyword.Await;
            Assert.AreEqual("await", keyword.Sequence);
        }

        [TestMethod]
        public void Keyword_Dictionary_ReturnsCorrectKeyword()
        {
            var keyword = GD.Keyword.Dictionary;
            Assert.AreEqual("Dictionary", keyword.Sequence);
        }

        [TestMethod]
        public void Keyword_Get_ReturnsCorrectKeyword()
        {
            var keyword = GD.Keyword.Get;
            Assert.AreEqual("get", keyword.Sequence);
        }

        [TestMethod]
        public void Keyword_Set_ReturnsCorrectKeyword()
        {
            var keyword = GD.Keyword.Set;
            Assert.AreEqual("set", keyword.Sequence);
        }

        [TestMethod]
        public void Keyword_ReturnType_ReturnsCorrectKeyword()
        {
            var keyword = GD.Keyword.ReturnType;
            Assert.AreEqual("->", keyword.Sequence);
        }

        [TestMethod]
        public void Keyword_When_ReturnsCorrectKeyword()
        {
            var keyword = GD.Keyword.When;
            Assert.AreEqual("when", keyword.Sequence);
        }

        #endregion

        #region Abstract Support Tests

        [TestMethod]
        public void Attribute_Abstract_ReturnsCorrectAttribute()
        {
            var attr = GD.Atribute.Abstract();
            var code = attr.ToString();
            Assert.AreEqual("@abstract", code);
        }

        [TestMethod]
        public void Declaration_AbstractMethod_GeneratesCorrectCode()
        {
            var method = GD.Declaration.AbstractMethod("process");
            var code = method.ToString();
            Assert.IsTrue(code.Contains("func"));
            Assert.IsTrue(code.Contains("process"));
            Assert.IsTrue(code.Contains("()"));
            // Abstract methods have no colon or body
            Assert.IsFalse(code.Contains(":"));
        }

        [TestMethod]
        public void Declaration_AbstractMethodWithParameters_GeneratesCorrectCode()
        {
            var method = GD.Declaration.AbstractMethod("process", GD.List.Parameters(
                GD.Declaration.Parameter("delta", GD.Type.Single("float"), null)
            ));
            var code = method.ToString();
            Assert.IsTrue(code.Contains("func"));
            Assert.IsTrue(code.Contains("process"));
            Assert.IsTrue(code.Contains("delta"));
            Assert.IsFalse(code.EndsWith(":"));
        }

        [TestMethod]
        public void Declaration_AbstractMethodWithReturnType_GeneratesCorrectCode()
        {
            var method = GD.Declaration.AbstractMethod("get_value", GD.Type.Single("int"));
            var code = method.ToString();
            Assert.IsTrue(code.Contains("func"));
            Assert.IsTrue(code.Contains("get_value"));
            Assert.IsTrue(code.Contains("->"));
            Assert.IsTrue(code.Contains("int"));
        }

        #endregion

        #region Line Continuation Tests

        [TestMethod]
        public void Syntax_LineContinuation_GeneratesBackslashNewline()
        {
            var lineCont = GD.Syntax.LineContinuation();
            Assert.AreEqual("\\\n", lineCont.Sequence);
        }

        [TestMethod]
        public void Syntax_LineContinuation_WithTrailing_GeneratesCorrectSequence()
        {
            var lineCont = GD.Syntax.LineContinuation(" ");
            Assert.AreEqual("\\ \n", lineCont.Sequence);
        }

        #endregion

        #region Rest Expression Tests

        [TestMethod]
        public void Expression_Rest_GeneratesDoubleDot()
        {
            var rest = GD.Expression.Rest();
            var code = rest.ToString();
            Assert.AreEqual("..", code);
        }

        #endregion

        #region Multiline String Tests

        [TestMethod]
        public void Expression_MultilineString_UsesTripleDoubleQuotes()
        {
            var str = GD.Expression.MultilineString("line1\nline2");
            var code = str.ToString();
            Assert.IsTrue(code.StartsWith("\"\"\""));
            Assert.IsTrue(code.EndsWith("\"\"\""));
            Assert.IsTrue(code.Contains("line1\nline2"));
        }

        [TestMethod]
        public void Expression_MultilineStringSingleQuote_UsesTripleSingleQuotes()
        {
            var str = GD.Expression.MultilineStringSingleQuote("content");
            var code = str.ToString();
            Assert.IsTrue(code.StartsWith("'''"));
            Assert.IsTrue(code.EndsWith("'''"));
        }

        [TestMethod]
        public void Syntax_MultilineString_UsesTripleQuotes()
        {
            var str = GD.Syntax.MultilineString("text");
            var code = str.ToString();
            Assert.IsTrue(code.StartsWith("\"\"\""));
            Assert.IsTrue(code.EndsWith("\"\"\""));
        }

        #endregion

        #region Property Accessor Body Tests

        [TestMethod]
        public void Declaration_GetAccessorBody_GeneratesCorrectCode()
        {
            var getter = GD.Declaration.GetAccessorBody(
                GD.Statement.Expression(GD.Expression.Return(GD.Expression.Identifier("_value")))
            );
            var code = getter.ToString();
            Assert.IsTrue(code.Contains("get"));
            Assert.IsTrue(code.Contains(":"));
            Assert.IsTrue(code.Contains("return"));
        }

        [TestMethod]
        public void Declaration_GetAccessorBody_WithExpression_GeneratesCorrectCode()
        {
            var getter = GD.Declaration.GetAccessorBody(GD.Expression.Identifier("_value"));
            var code = getter.ToString();
            Assert.IsTrue(code.Contains("get"));
            Assert.IsTrue(code.Contains(":"));
            Assert.IsTrue(code.Contains("_value"));
        }

        [TestMethod]
        public void Declaration_SetAccessorBody_GeneratesCorrectCode()
        {
            var setter = GD.Declaration.SetAccessorBody("value",
                GD.Statement.Expression(
                    GD.Expression.DualOperator(
                        GD.Expression.Identifier("_value"),
                        GD.Syntax.DualOperator(GDDualOperatorType.Assignment),
                        GD.Expression.Identifier("value")
                    )
                )
            );
            var code = setter.ToString();
            Assert.IsTrue(code.Contains("set"));
            Assert.IsTrue(code.Contains("("));
            Assert.IsTrue(code.Contains("value"));
            Assert.IsTrue(code.Contains(")"));
            Assert.IsTrue(code.Contains(":"));
        }

        #endregion

        #region Type Node Tests

        [TestMethod]
        public void Type_Single_GeneratesCorrectCode()
        {
            var type = GD.Type.Single("int");
            var code = type.ToString();
            Assert.AreEqual("int", code);
        }

        [TestMethod]
        public void Type_Array_GeneratesCorrectCode()
        {
            var type = GD.Type.Array("int");
            var code = type.ToString();
            Assert.AreEqual("Array[int]", code);
        }

        [TestMethod]
        public void Type_UntypedArray_GeneratesCorrectCode()
        {
            var type = GD.Type.UntypedArray();
            var code = type.ToString();
            Assert.AreEqual("Array", code);
        }

        [TestMethod]
        public void Type_Dictionary_GeneratesCorrectCode()
        {
            var type = GD.Type.Dictionary("String", "int");
            var code = type.ToString();
            Assert.IsTrue(code.Contains("Dictionary"));
            Assert.IsTrue(code.Contains("String"));
            Assert.IsTrue(code.Contains("int"));
        }

        [TestMethod]
        public void Type_UntypedDictionary_GeneratesCorrectCode()
        {
            var type = GD.Type.UntypedDictionary();
            var code = type.ToString();
            Assert.AreEqual("Dictionary", code);
        }

        [TestMethod]
        public void Type_NestedArray_GeneratesCorrectCode()
        {
            var innerType = GD.Type.Array("int");
            var type = GD.Type.Array(innerType);
            var code = type.ToString();
            Assert.AreEqual("Array[Array[int]]", code);
        }

        #endregion

        #region Extension Method Tests

        [TestMethod]
        public void Extension_AddAbstract_AddsAbstractAttribute()
        {
            // Build a class with @abstract attribute using the members list
            var cls = GD.Declaration.Class();
            cls.Members = GD.List.Members(GD.Atribute.Abstract());
            var code = cls.ToString();
            Assert.IsTrue(code.Contains("@abstract"));
        }

        [TestMethod]
        public void Extension_AddAbstractMethod_AddsAbstractMethod()
        {
            // Build a class with an abstract method using the members list
            var cls = GD.Declaration.Class();
            cls.Members = GD.List.Members(GD.Declaration.AbstractMethod("process"));
            var code = cls.ToString();
            Assert.IsTrue(code.Contains("func"));
            Assert.IsTrue(code.Contains("process"));
        }

        #endregion
    }
}
