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

        #region Parser Bug: $NodePath as Type

        /// <summary>
        /// BUG: Parser incorrectly tokenizes "$Sprite2D as Sprite2D" as a single GDPathSpecifier
        /// containing "Sprite2D as Sprite2D" instead of parsing it as GDDualOperatorExpression
        /// with left=$Sprite2D, operator=as, right=Sprite2D.
        ///
        /// Expected: GDDualOperatorExpression { Left=GDGetNodeExpression, Op=As, Right=GDIdentifierExpression }
        /// Actual: GDGetNodeExpression with path containing "Sprite2D as Sprite2D"
        /// </summary>
        [TestMethod]
        public void ParseNode_WithAsCast_ShouldBeDualOperator()
        {
            var reader = new GDScriptReader();

            var code = "$Sprite2D as Sprite2D";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);

            // BUG: Currently fails - parser returns GDGetNodeExpression instead of GDDualOperatorExpression
            Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExpression),
                "Expression '$Sprite2D as Sprite2D' should be parsed as GDDualOperatorExpression, " +
                $"but got {expression.GetType().Name}");

            var dualOp = (GDDualOperatorExpression)expression;

            Assert.AreEqual(GDDualOperatorType.As, dualOp.OperatorType,
                "Operator should be 'as'");

            Assert.IsInstanceOfType(dualOp.LeftExpression, typeof(GDGetNodeExpression),
                "Left side should be GDGetNodeExpression ($Sprite2D)");

            Assert.IsInstanceOfType(dualOp.RightExpression, typeof(GDIdentifierExpression),
                "Right side should be GDIdentifierExpression (Sprite2D)");

            var rightId = (GDIdentifierExpression)dualOp.RightExpression;
            Assert.AreEqual("Sprite2D", rightId.Identifier?.Sequence,
                "Right identifier should be 'Sprite2D'");

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        /// <summary>
        /// BUG: Same issue with quoted path syntax: $"Node Path" as Type
        /// This case is even worse - it crashes the parser with InvalidOperationException
        /// during priority rebuilding in SwapRight().
        /// </summary>
        [TestMethod]
        public void ParseNode_QuotedPath_WithAsCast_ShouldBeDualOperator()
        {
            var reader = new GDScriptReader();

            var code = "$\"Sprite2D\" as Sprite2D";

            // BUG: Currently throws InvalidOperationException in SwapRight during priority rebuilding
            // Expected: Should parse without exception and return GDDualOperatorExpression
            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);

            Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExpression),
                "Expression '$\"Sprite2D\" as Sprite2D' should be parsed as GDDualOperatorExpression, " +
                $"but got {expression.GetType().Name}");

            var dualOp = (GDDualOperatorExpression)expression;

            Assert.AreEqual(GDDualOperatorType.As, dualOp.OperatorType,
                "Operator should be 'as'");

            Assert.IsInstanceOfType(dualOp.LeftExpression, typeof(GDGetNodeExpression),
                "Left side should be GDGetNodeExpression ($\"Sprite2D\")");

            Assert.IsInstanceOfType(dualOp.RightExpression, typeof(GDIdentifierExpression),
                "Right side should be GDIdentifierExpression (Sprite2D)");

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        /// <summary>
        /// BUG: With @onready annotation - common real-world pattern
        /// </summary>
        [TestMethod]
        public void ParseNode_OnreadyWithAsCast_ShouldBeDualOperator()
        {
            var reader = new GDScriptReader();

            var code = "@onready var sprite := $Sprite2D as Sprite2D";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Variables.Count());
            var variable = declaration.Variables.First();

            Assert.IsNotNull(variable.Initializer,
                "Variable should have initializer");

            // BUG: Currently fails - initializer is GDGetNodeExpression instead of GDDualOperatorExpression
            Assert.IsInstanceOfType(variable.Initializer, typeof(GDDualOperatorExpression),
                "Initializer '$Sprite2D as Sprite2D' should be GDDualOperatorExpression, " +
                $"but got {variable.Initializer.GetType().Name}");

            var dualOp = (GDDualOperatorExpression)variable.Initializer;
            Assert.AreEqual(GDDualOperatorType.As, dualOp.OperatorType);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        /// <summary>
        /// BUG: Quoted path with space: $"Node Path" as Type
        /// </summary>
        [TestMethod]
        public void ParseNode_QuotedPathWithSpace_WithAsCast_ShouldBeDualOperator()
        {
            var reader = new GDScriptReader();

            var code = "$\"My Sprite\" as Sprite2D";

            // BUG: Expected to fail similarly to other $Path as Type cases
            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);

            Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExpression),
                "Expression '$\"My Sprite\" as Sprite2D' should be parsed as GDDualOperatorExpression, " +
                $"but got {expression.GetType().Name}");

            var dualOp = (GDDualOperatorExpression)expression;

            Assert.AreEqual(GDDualOperatorType.As, dualOp.OperatorType,
                "Operator should be 'as'");

            Assert.IsInstanceOfType(dualOp.LeftExpression, typeof(GDGetNodeExpression),
                "Left side should be GDGetNodeExpression ($\"My Sprite\")");

            Assert.IsInstanceOfType(dualOp.RightExpression, typeof(GDIdentifierExpression),
                "Right side should be GDIdentifierExpression (Sprite2D)");

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        /// <summary>
        /// Control test: get_node() as Type should work correctly (no bug)
        /// </summary>
        [TestMethod]
        public void ParseNode_GetNodeCall_WithAsCast_WorksCorrectly()
        {
            var reader = new GDScriptReader();

            var code = "get_node(\"Sprite2D\") as Sprite2D";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExpression),
                "get_node() as Type should be GDDualOperatorExpression");

            var dualOp = (GDDualOperatorExpression)expression;
            Assert.AreEqual(GDDualOperatorType.As, dualOp.OperatorType);

            Assert.IsInstanceOfType(dualOp.LeftExpression, typeof(GDCallExpression),
                "Left side should be GDCallExpression (get_node(...))");

            Assert.IsInstanceOfType(dualOp.RightExpression, typeof(GDIdentifierExpression),
                "Right side should be GDIdentifierExpression (Sprite2D)");

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        #endregion
    }
}
