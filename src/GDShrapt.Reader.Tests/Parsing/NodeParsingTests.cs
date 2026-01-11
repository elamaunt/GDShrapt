using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Tests for parsing node paths and get_node expressions.
    /// </summary>
    [TestClass]
    public class NodeParsingTests
    {
        [TestMethod]
        public void ParseNode_GetNodeWithCall()
        {
            var reader = new GDScriptReader();

            var code = @"$Animation/Root/ _345/ end .CallMethod()";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);

            var getNodeExpression = expression.CastOrAssert<GDCallExpression>();
            Assert.AreEqual("$Animation/Root/ _345/ end .CallMethod", getNodeExpression.CallerExpression.ToString());

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void ParseNode_PathWithMethod()
        {
            var reader = new GDScriptReader();

            var code = @"^""/root/MyAutoload"".get_name(0)";

            var expression = reader.ParseExpression(code);

            var call = expression.CastOrAssert<GDCallExpression>();
            Assert.AreEqual("0", call.Parameters.ToString());

            var memberOperator = call.CallerExpression.CastOrAssert<GDMemberOperatorExpression>();
            Assert.AreEqual("get_name", memberOperator.Identifier.Sequence);

            var nodePathExpression = memberOperator.CallerExpression.CastOrAssert<GDNodePathExpression>();

            Assert.AreEqual("\"/root/MyAutoload\"", nodePathExpression.Path.ToString());

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void ParseNode_PathVariants()
        {
            var reader = new GDScriptReader();

            var samples = new string[]
            {
                "^\"A\"",
                "^\"A/B\"",
                "^\".\"",
                "^\"..\"",
                "^\"../C\"",
                "^\"/root\"",
                "^\"/root/Main\"",
                "^\"/root/MyAutoload\"",
                "^\"Path2D/PathFollow2D/Sprite\"",
                "^\"Path2D/PathFollow2D/Sprite:texture\"",
                "^\"Path2D/PathFollow2D/Sprite:position\"",
                "^\"Path2D/PathFollow2D/Sprite:position:x\"",
                "^\"/root/Level/Path2D\"",
                "^\"Path2D/PathFollow2D/Sprite:texture:load_path\""
            };


            for (int i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];

                var expression = reader.ParseExpression(sample);

                Assert.IsNotNull(expression);
                Assert.IsInstanceOfType(expression, typeof(GDNodePathExpression));

                var nodePathExpression = (GDNodePathExpression)expression;

                AssertHelper.CompareCodeStrings(sample, nodePathExpression.ToString());
                AssertHelper.NoInvalidTokens(nodePathExpression);
            }
        }

        [TestMethod]
        public void ParseNode_UniqueNode()
        {
            var reader = new GDScriptReader();

            var code = @"@onready var a = %Test";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Variables.Count());
            var variable = declaration.Variables.First();

            Assert.IsNotNull(variable.Initializer);
            Assert.IsInstanceOfType(variable.Initializer, typeof(GDGetUniqueNodeExpression));

            var getUniqueNode = (GDGetUniqueNodeExpression)variable.Initializer;
            Assert.AreEqual("%Test", getUniqueNode.ToString());

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseNode_WithOnready()
        {
            var reader = new GDScriptReader();

            var code = @"@onready var test = $Path/To/Node";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Variables.Count());

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseNode_WithHashInPath()
        {
            var reader = new GDScriptReader();

            var code = "$\"#HBox/a\".visible = false";

            var statement = reader.ParseStatement(code);
            Assert.IsNotNull(statement);

            AssertHelper.CompareCodeStrings(code, statement.ToString());
            AssertHelper.NoInvalidTokens(statement);
        }
    }
}
