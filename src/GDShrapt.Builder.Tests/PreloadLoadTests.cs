using GDShrapt.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Building
{
    /// <summary>
    /// Tests for Preload and Load convenience methods.
    /// </summary>
    [TestClass]
    public class PreloadLoadTests
    {
        [TestMethod]
        public void BuildExpression_Preload_SimpleResource()
        {
            var expr = GD.Expression.Preload("res://player.gd");
            var code = expr.ToString();

            Assert.IsTrue(code.Contains("preload"));
            Assert.IsTrue(code.Contains("res://player.gd"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Preload_TextureResource()
        {
            var expr = GD.Expression.Preload("res://assets/icon.png");
            var code = expr.ToString();

            Assert.AreEqual("preload(\"res://assets/icon.png\")", code);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Load_SimpleResource()
        {
            var expr = GD.Expression.Load("res://scenes/main.tscn");
            var code = expr.ToString();

            Assert.IsTrue(code.Contains("load"));
            Assert.IsTrue(code.Contains("res://scenes/main.tscn"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Load_AudioResource()
        {
            var expr = GD.Expression.Load("res://audio/music.ogg");
            var code = expr.ToString();

            Assert.AreEqual("load(\"res://audio/music.ogg\")", code);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildDeclaration_Variable_WithPreload()
        {
            var decl = GD.Declaration.Variable("Player", GD.Expression.Preload("res://player.gd"));
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("var Player"));
            Assert.IsTrue(code.Contains("preload"));
            AssertHelper.NoInvalidTokens(decl);
        }

        [TestMethod]
        public void BuildDeclaration_Const_WithPreload()
        {
            var decl = GD.Declaration.Const("ICON", GD.Expression.Preload("res://icon.png"));
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("const ICON"));
            Assert.IsTrue(code.Contains("preload"));
            AssertHelper.NoInvalidTokens(decl);
        }
    }
}
