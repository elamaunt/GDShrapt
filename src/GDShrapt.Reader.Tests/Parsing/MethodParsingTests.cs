using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Parsing
{
    /// <summary>
    /// Tests for parsing method declarations.
    /// </summary>
    [TestClass]
    public class MethodParsingTests
    {
        [TestMethod]
        public void ParseMethod_WithStaticAndReturnType()
        {
            var reader = new GDScriptReader();

            var code = @"static func my_int_function() -> int:
    return 0";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Methods.Count());

            var method = declaration.Methods.ElementAt(0);

            Assert.IsNotNull(method);
            Assert.AreEqual(1, method.Statements.Count);
            Assert.IsInstanceOfType(method.Statements[0], typeof(GDExpressionStatement));
            Assert.IsInstanceOfType(((GDExpressionStatement)method.Statements[0]).Expression, typeof(GDReturnExpression));

            Assert.IsNotNull(method);
            Assert.AreEqual("int", (method.ReturnType as GDSingleTypeNode)?.Type?.Sequence);
            Assert.AreEqual("my_int_function", method.Identifier?.Sequence);
            Assert.AreEqual(true, method.IsStatic);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseMethod_WithMultilineParameters()
        {
            var reader = new GDScriptReader();

            var code = @"func my_method(
	a,
	b,
	c,
	d,
	e
):
    print(a)
    print(b)
    print(c)
    print(d)
    print(e)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Methods.Count());

            var method = declaration.Methods.ElementAt(0);

            Assert.IsNotNull(method);
            Assert.AreEqual(5, method.Parameters.Count);
            Assert.AreEqual(5, method.Statements.Count);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseMethod_WithMixedMultilineParameters()
        {
            var reader = new GDScriptReader();

            var code = @"func my_method(
	a, b, c,
	d,
	e):
    print(a)
    print(b)
    print(c)
    print(d)
    print(e)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Methods.Count());

            var method = declaration.Methods.ElementAt(0);

            Assert.IsNotNull(method);
            Assert.AreEqual(5, method.Parameters.Count);
            Assert.AreEqual(5, method.Statements.Count);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseMethod_WithInlineLambdaAssignment()
        {
            var reader = new GDScriptReader();

            var code = @"func f():
	var my_lambda = func(x): print(x)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseMethod_WithMultilineLambdaAssignment()
        {
            var reader = new GDScriptReader();

            var code = @"func f():
	var my_lambda = func(x):
		print(x)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseMethod_WithLambdaClosure()
        {
            var reader = new GDScriptReader();

            var code = @"func f():
	var y = 10
	var my_lambda = func(x):
		print(y)
		print(x)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseMethod_WithLambdaExpressionStatement()
        {
            var reader = new GDScriptReader();

            var code = @"func f():
	var x = func():
		print(123)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Methods.Count());
            Assert.AreEqual(1, declaration.Methods.First().Statements.Count);

            var statement = declaration.Methods.First().Statements[0] as GDVariableDeclarationStatement;
            Assert.IsNotNull(statement);
            Assert.IsNotNull(statement.Initializer);
            Assert.IsInstanceOfType(statement.Initializer, typeof(GDMethodExpression));

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseMethod_WithInlinedLambdaInFilter()
        {
            var reader = new GDScriptReader();

            var code = @"func f():
	a.filter(func(number): return number != active_number)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            var method = declaration.Methods.First();
            Assert.IsNotNull(method);
            Assert.AreEqual(1, method.Statements.Count);

            var expressionStatement = method.Statements[0] as GDExpressionStatement;
            Assert.IsNotNull(expressionStatement);

            var callExpression = expressionStatement.Expression as GDCallExpression;
            Assert.IsNotNull(callExpression);

            Assert.IsInstanceOfType(callExpression.CallerExpression, typeof(GDMemberOperatorExpression));

            var member = callExpression.CallerExpression as GDMemberOperatorExpression;
            Assert.IsNotNull(member);
            Assert.AreEqual("a", (member.CallerExpression as GDIdentifierExpression)?.Identifier?.Sequence);
            Assert.AreEqual("filter", member.Identifier?.Sequence);

            Assert.AreEqual(1, callExpression.Parameters.Count);
            Assert.IsInstanceOfType(callExpression.Parameters[0], typeof(GDMethodExpression));

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseMethod_WithStaticVariablesAndFunction()
        {
            var reader = new GDScriptReader();

            var code = @"@static_unload
class_name Test

static var a = 1
static var b = 2
static var c = 3

static func f():
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(3, declaration.Variables.Count());
            Assert.AreEqual(1, declaration.Methods.Count());

            foreach (var variable in declaration.Variables)
            {
                Assert.IsTrue(variable.IsStatic);
            }

            Assert.IsTrue(declaration.Methods.First().IsStatic);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }
    }
}
