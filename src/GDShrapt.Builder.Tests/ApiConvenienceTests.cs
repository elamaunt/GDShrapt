using GDShrapt.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Building
{
    /// <summary>
    /// Tests for API convenience methods - GDTypeNode overloads for Variable/Const.
    /// </summary>
    [TestClass]
    public class ApiConvenienceTests
    {
        private readonly GDScriptReader _reader = new GDScriptReader();

        #region Variable with GDTypeNode Tests

        [TestMethod]
        public void Variable_WithTypedArray_DirectOverload()
        {
            var decl = GD.Declaration.Variable("items", GD.Type.Array("Item"));
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("var items"));
            Assert.IsTrue(code.Contains("Array[Item]"));
            AssertHelper.NoInvalidTokens(decl);
        }

        [TestMethod]
        public void Variable_WithTypedDictionary_DirectOverload()
        {
            var decl = GD.Declaration.Variable("scores", GD.Type.Dictionary("String", "int"));
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("var scores"));
            Assert.IsTrue(code.Contains("Dictionary[String, int]"));
            AssertHelper.NoInvalidTokens(decl);
        }

        [TestMethod]
        public void Variable_WithTypedArray_AndInitializer()
        {
            var decl = GD.Declaration.Variable("items", GD.Type.Array("Item"), GD.Expression.Array());
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("var items"));
            Assert.IsTrue(code.Contains("Array[Item]"));
            Assert.IsTrue(code.Contains("[]"));
            AssertHelper.NoInvalidTokens(decl);
        }

        [TestMethod]
        public void Variable_WithTypedDictionary_AndInitializer()
        {
            var decl = GD.Declaration.Variable("data", GD.Type.Dictionary("String", "Variant"), GD.Expression.Dictionary());
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("var data"));
            Assert.IsTrue(code.Contains("Dictionary[String, Variant]"));
            Assert.IsTrue(code.Contains("{}"));
            AssertHelper.NoInvalidTokens(decl);
        }

        [TestMethod]
        public void Variable_WithNestedTypedArray()
        {
            var decl = GD.Declaration.Variable("matrix", GD.Type.Array(GD.Type.Array("int")));
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("var matrix"));
            Assert.IsTrue(code.Contains("Array[Array[int]]"));
            AssertHelper.NoInvalidTokens(decl);
        }

        [TestMethod]
        public void Variable_WithSingleType()
        {
            var decl = GD.Declaration.Variable("player", GD.Type.Single("Node2D"));
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("var player"));
            Assert.IsTrue(code.Contains("Node2D"));
            AssertHelper.NoInvalidTokens(decl);
        }

        #endregion

        #region Const with GDTypeNode Tests

        [TestMethod]
        public void Const_WithTypedArray_DirectOverload()
        {
            var decl = GD.Declaration.Const("ITEMS", GD.Type.Array("Item"), GD.Expression.Array());
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("const ITEMS"));
            Assert.IsTrue(code.Contains("Array[Item]"));
            Assert.IsTrue(code.Contains("[]"));
            AssertHelper.NoInvalidTokens(decl);
        }

        [TestMethod]
        public void Const_WithTypedDictionary_DirectOverload()
        {
            var decl = GD.Declaration.Const("CONFIG", GD.Type.Dictionary("String", "Variant"), GD.Expression.Dictionary());
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("const CONFIG"));
            Assert.IsTrue(code.Contains("Dictionary[String, Variant]"));
            Assert.IsTrue(code.Contains("{}"));
            AssertHelper.NoInvalidTokens(decl);
        }

        [TestMethod]
        public void Const_WithSingleType()
        {
            var decl = GD.Declaration.Const("MAX_VALUE", GD.Type.Single("int"), GD.Expression.Number(100));
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("const MAX_VALUE"));
            Assert.IsTrue(code.Contains("int"));
            Assert.IsTrue(code.Contains("100"));
            AssertHelper.NoInvalidTokens(decl);
        }

        #endregion

        #region Extension Method Tests

        [TestMethod]
        public void ClassMembers_AddVariable_WithTypedArray()
        {
            var classDecl = GD.Declaration.Class()
                .AddMembers(m => m
                    .AddVariable("items", GD.Type.Array("Item"))
                );
            var code = classDecl.ToString();

            Assert.IsTrue(code.Contains("var items"));
            Assert.IsTrue(code.Contains("Array[Item]"));
            AssertHelper.NoInvalidTokens(classDecl);
        }

        [TestMethod]
        public void ClassMembers_AddVariable_WithTypedArray_AndInitializer()
        {
            var classDecl = GD.Declaration.Class()
                .AddMembers(m => m
                    .AddVariable("enemies", GD.Type.Array("Enemy"), GD.Expression.Array())
                );
            var code = classDecl.ToString();

            Assert.IsTrue(code.Contains("var enemies"));
            Assert.IsTrue(code.Contains("Array[Enemy]"));
            Assert.IsTrue(code.Contains("[]"));
            AssertHelper.NoInvalidTokens(classDecl);
        }

        [TestMethod]
        public void ClassMembers_AddVariable_WithTypedDictionary()
        {
            var classDecl = GD.Declaration.Class()
                .AddMembers(m => m
                    .AddVariable("scores", GD.Type.Dictionary("String", "int"))
                );
            var code = classDecl.ToString();

            Assert.IsTrue(code.Contains("var scores"));
            Assert.IsTrue(code.Contains("Dictionary[String, int]"));
            AssertHelper.NoInvalidTokens(classDecl);
        }

        [TestMethod]
        public void ClassMembers_AddConst_WithTypedArray()
        {
            var classDecl = GD.Declaration.Class()
                .AddMembers(m => m
                    .AddConst("DEFAULT_ITEMS", GD.Type.Array("String"), GD.Expression.Array(
                        GD.Expression.String("item1"),
                        GD.Expression.String("item2")
                    ))
                );
            var code = classDecl.ToString();

            Assert.IsTrue(code.Contains("const DEFAULT_ITEMS"));
            Assert.IsTrue(code.Contains("Array[String]"));
            Assert.IsTrue(code.Contains("item1"));
            Assert.IsTrue(code.Contains("item2"));
            AssertHelper.NoInvalidTokens(classDecl);
        }

        #endregion

        #region Round-Trip Tests

        [TestMethod]
        public void RoundTrip_Variable_TypedArray()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Variable("items", GD.Type.Array("Node"))
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Variable_TypedDictionary()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Variable("data", GD.Type.Dictionary("String", "int"))
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Variable_TypedArray_WithInitializer()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Variable("items", GD.Type.Array("Item"), GD.Expression.Array())
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Const_TypedArray()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Const("ITEMS", GD.Type.Array("String"), GD.Expression.Array())
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        #endregion

        #region Complex Type Combinations

        [TestMethod]
        public void CompleteClass_WithMultipleTypedMembers()
        {
            var classDecl = GD.Declaration.Class()
                .AddMembers(m => m
                    .AddExtendsAttribute("Node")
                    .AddNewLine()
                    .AddNewLine()
                    .AddVariable("players", GD.Type.Array("Player"))
                    .AddNewLine()
                    .AddVariable("scores", GD.Type.Dictionary("String", "int"), GD.Expression.Dictionary())
                    .AddNewLine()
                    .AddConst("MAX_PLAYERS", GD.Type.Single("int"), GD.Expression.Number(4))
                );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            Assert.IsTrue(code.Contains("extends Node"));
            Assert.IsTrue(code.Contains("var players: Array[Player]"));
            Assert.IsTrue(code.Contains("var scores: Dictionary[String, int]"));
            Assert.IsTrue(code.Contains("const MAX_PLAYERS: int = 4"));
            AssertHelper.NoInvalidTokens(classDecl);

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        #endregion
    }
}
