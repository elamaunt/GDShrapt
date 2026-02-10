using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Tests for parsing expressions.
    /// </summary>
    [TestClass]
    public class ExpressionParsingTests
    {
        [TestMethod]
        public void ParseExpression_WithLogicalAndOperator()
        {
            var reader = new GDScriptReader();

            var code = @"a > b and c > d";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExpression));

            var @dualOperator = (GDDualOperatorExpression)expression;
            Assert.AreEqual(GDDualOperatorType.And2, @dualOperator.OperatorType);

            var leftExpression = @dualOperator.LeftExpression;

            Assert.IsNotNull(leftExpression);
            Assert.IsInstanceOfType(leftExpression, typeof(GDDualOperatorExpression));

            var rightExpression = @dualOperator.RightExpression;

            Assert.IsNotNull(rightExpression);
            Assert.IsInstanceOfType(rightExpression, typeof(GDDualOperatorExpression));

            var @leftDualOperator = (GDDualOperatorExpression)leftExpression;

            Assert.IsInstanceOfType(@leftDualOperator.LeftExpression, typeof(GDIdentifierExpression));
            Assert.IsNotNull(@leftDualOperator.LeftExpression);
            Assert.IsInstanceOfType(@leftDualOperator.RightExpression, typeof(GDIdentifierExpression));
            Assert.IsNotNull(@leftDualOperator.RightExpression);

            Assert.AreEqual("a", ((GDIdentifierExpression)@leftDualOperator.LeftExpression).Identifier.Sequence);
            Assert.AreEqual("b", ((GDIdentifierExpression)@leftDualOperator.RightExpression).Identifier.Sequence);

            var @rightDualOperator = (GDDualOperatorExpression)rightExpression;

            Assert.IsInstanceOfType(@rightDualOperator.LeftExpression, typeof(GDIdentifierExpression));
            Assert.IsNotNull(@rightDualOperator.LeftExpression);
            Assert.IsInstanceOfType(@rightDualOperator.RightExpression, typeof(GDIdentifierExpression));
            Assert.IsNotNull(@rightDualOperator.RightExpression);

            Assert.AreEqual("c", ((GDIdentifierExpression)@rightDualOperator.LeftExpression).Identifier.Sequence);
            Assert.AreEqual("d", ((GDIdentifierExpression)@rightDualOperator.RightExpression).Identifier.Sequence);

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void ParseExpression_WithOperatorPriority()
        {
            var reader = new GDScriptReader();

            var code = @"a > b > c = d = e > f > g";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);

            Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExpression));

            var @dualOperator = (GDDualOperatorExpression)expression;

            Assert.AreEqual(GDDualOperatorType.Assignment, @dualOperator.OperatorType);
            Assert.AreEqual("a > b > c", @dualOperator.LeftExpression.ToString());

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void ParseExpression_WithNegativeNumber()
        {
            var reader = new GDScriptReader();

            var code = "-10";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDNumberExpression));

            var numberExpression = (GDNumberExpression)expression;

            Assert.IsNotNull(numberExpression.Number);
            Assert.AreEqual(GDNumberType.LongDecimal, numberExpression.Number.ResolveNumberType());
            Assert.AreEqual(-10, numberExpression.Number.ValueInt64);

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void ParseExpression_WithBracketsAndNegation()
        {
            var reader = new GDScriptReader();

            var code = "13 + -2 * -(10-20) / 3.0";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void ParseExpression_WithMethodChain()
        {
            var reader = new GDScriptReader();

            var code = @"var a = b.c(d).e(f).g(h)

func f():
    b.c(d).e(f).g(h)
    return b.c(d).e(f).g(h)";

            var declaration = reader.ParseFileContent(code);

            Assert.IsNotNull(declaration);
            Assert.AreEqual(1, declaration.Variables.Count());
            Assert.AreEqual(1, declaration.Methods.Count());

            Assert.AreEqual("b.c(d).e(f).g(h)", declaration.Variables.First().Initializer.ToString());
            Assert.AreEqual(2, declaration.Methods.First().Statements.Count);

            var call = declaration.Methods.First().Statements[0] as GDExpressionStatement;
            Assert.IsNotNull(call);
            Assert.AreEqual("b.c(d).e(f).g(h)", call.Expression.ToString());

            var returnStmnt = declaration.Methods.First().Statements[1] as GDExpressionStatement;
            Assert.IsNotNull(returnStmnt);
            Assert.IsInstanceOfType(returnStmnt.Expression, typeof(GDReturnExpression));
            Assert.AreEqual("return b.c(d).e(f).g(h)", returnStmnt.Expression.ToString());

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseExpression_WithBaseMethodCall()
        {
            var reader = new GDScriptReader();

            var code = @"extends Node2D

func _ready():
    pass

func ready2():
    ._ready()
    .super_method()";

            var declaration = reader.ParseFileContent(code);

            Assert.IsNotNull(declaration);
            Assert.AreEqual(2, declaration.Methods.Count());

            Assert.AreEqual(1, declaration.Methods.ElementAt(0).Statements.Count);
            Assert.AreEqual(2, declaration.Methods.ElementAt(1).Statements.Count);

            var call1 = declaration.Methods.ElementAt(1).Statements[0] as GDExpressionStatement;
            var call2 = declaration.Methods.ElementAt(1).Statements[1] as GDExpressionStatement;

            Assert.IsNotNull(call1);
            Assert.IsNotNull(call2);

            Assert.IsInstanceOfType(call1.Expression, typeof(GDCallExpression));
            Assert.IsInstanceOfType(call2.Expression, typeof(GDCallExpression));

            Assert.AreEqual("._ready()", call1.Expression.ToString());
            Assert.AreEqual(".super_method()", call2.Expression.ToString());

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseExpression_WithMultilineSplitting()
        {
            var reader = new GDScriptReader();

            var code = @"func _ready():
    if (2 == 2
        and 3 == 3
        and 4 == 4
        and 5 == 5
        and 6 == 6):
            print(""The parenthesis way of putting 'if' statements on multiple lines."")

    if 2 == 2 \
        and 3 == 3 \
        and 4 == 4 \
        and 5 == 5 \
        and 6 == 6:
            print(""The backslash way of putting 'if' statements on multiple lines."")";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);

            Assert.AreEqual(1, @class.Methods.Count());

            var method = @class.Methods.First();

            Assert.AreEqual(2, method.Statements.Count);

            var statement = method.Statements[1];

            Assert.IsInstanceOfType(statement, typeof(GDIfStatement));

            var ifStatement = (GDIfStatement)statement;

            Assert.IsNotNull(ifStatement.IfBranch);

            Assert.IsNotNull(ifStatement.IfBranch.Condition);
            AssertHelper.CompareCodeStrings(@"2 == 2 \
        and 3 == 3 \
        and 4 == 4 \
        and 5 == 5 \
        and 6 == 6", ifStatement.IfBranch.Condition.ToString());

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void ParseExpression_WithSameLineStatements()
        {
            var reader = new GDScriptReader();

            var code = @"func f():
	a = b; c = d";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseExpression_WithCurrying()
        {
            var reader = new GDScriptReader();

            var code = @"func f():
	return get_a()(1)()(2, 3)()
	return self.get_a()(1)()(2, 3)()";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Methods.Count());
            Assert.AreEqual(2, declaration.Methods.First().Statements.Count);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseExpression_WithNestedCurrying()
        {
            var reader = new GDScriptReader();

            var code = @"
class_name Helper

func curry_test(f):
    var sum_all = func(x, y, z):
	    return x + y + z

    var curry = func(f):
        return func(x):
            return func(y):
                return func(z):
                    return f.call(x, y, z)

    var curried_sum = curry.call(sum_all)
    var partial_sum_x = curried_sum.call(1)
    var partial_sum_y = partial_sum_x.call(2)

    print(partial_sum_y.call(3))
    print(curried_sum.call(1).call(2).call(3))";

            var @class = reader.ParseFileContent(code);

            var method = @class.Methods.First();

            var statements = method.Statements.ToArray();
            var second = statements[1];

            Assert.AreEqual(7, statements.Length);

            var declaration = (GDVariableDeclarationStatement)second;

            var methodExpr = (GDMethodExpression)declaration.Initializer;

            var returnExpr = (GDReturnExpression)((GDExpressionStatement)methodExpr.Statements.First()).Expression;
            var method2Expr = (GDMethodExpression)returnExpr.Expression;

            var return2Expr = (GDReturnExpression)((GDExpressionStatement)method2Expr.Statements.First()).Expression;
            var method3Expr = (GDMethodExpression)return2Expr.Expression;

            var return3Expr = (GDReturnExpression)((GDExpressionStatement)method3Expr.Statements.First()).Expression;
            var method4Expr = (GDMethodExpression)return3Expr.Expression;

            Assert.AreEqual(1, method4Expr.Statements.Count);
            Assert.AreEqual("\n                    return f.call(x, y, z)", method4Expr.Statements.ToString());

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void ParseExpression_WithNotInOperator()
        {
            var reader = new GDScriptReader();

            var code = @"x not in arr";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExpression));

            var dualOperator = (GDDualOperatorExpression)expression;
            Assert.AreEqual(GDDualOperatorType.In, dualOperator.OperatorType);
            Assert.IsNotNull(dualOperator.NotKeyword);
            Assert.IsTrue(dualOperator.IsNotIn);

            Assert.IsInstanceOfType(dualOperator.LeftExpression, typeof(GDIdentifierExpression));
            Assert.AreEqual("x", ((GDIdentifierExpression)dualOperator.LeftExpression).Identifier.Sequence);

            Assert.IsInstanceOfType(dualOperator.RightExpression, typeof(GDIdentifierExpression));
            Assert.AreEqual("arr", ((GDIdentifierExpression)dualOperator.RightExpression).Identifier.Sequence);

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void ParseExpression_WithNotInOperator_InConditionContext()
        {
            var reader = new GDScriptReader();

            var code = @"area not in enemies_in_range";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExpression));

            var dualOperator = (GDDualOperatorExpression)expression;
            Assert.AreEqual(GDDualOperatorType.In, dualOperator.OperatorType);
            Assert.IsNotNull(dualOperator.NotKeyword);
            Assert.IsTrue(dualOperator.IsNotIn);

            Assert.AreEqual("area", ((GDIdentifierExpression)dualOperator.LeftExpression).Identifier.Sequence);
            Assert.AreEqual("enemies_in_range", ((GDIdentifierExpression)dualOperator.RightExpression).Identifier.Sequence);

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void ParseExpression_NotAsUnaryOperator_StillWorks()
        {
            var reader = new GDScriptReader();

            var code = @"not x";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDSingleOperatorExpression));

            var singleOp = (GDSingleOperatorExpression)expression;
            Assert.AreEqual(GDSingleOperatorType.Not2, singleOp.OperatorType);

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void ParseExpression_NotInWithComplexExpressions()
        {
            var reader = new GDScriptReader();

            var code = @"a + b not in get_list()";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExpression));

            var dualOperator = (GDDualOperatorExpression)expression;
            Assert.AreEqual(GDDualOperatorType.In, dualOperator.OperatorType);
            Assert.IsNotNull(dualOperator.NotKeyword);
            Assert.IsTrue(dualOperator.IsNotIn);

            Assert.IsInstanceOfType(dualOperator.LeftExpression, typeof(GDDualOperatorExpression));
            var leftAdd = (GDDualOperatorExpression)dualOperator.LeftExpression;
            Assert.AreEqual(GDDualOperatorType.Addition, leftAdd.OperatorType);

            Assert.IsInstanceOfType(dualOperator.RightExpression, typeof(GDCallExpression));

            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void ParseExpression_InOperator_StillWorks()
        {
            var reader = new GDScriptReader();

            var code = @"x in arr";

            var expression = reader.ParseExpression(code);

            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDDualOperatorExpression));

            var dualOperator = (GDDualOperatorExpression)expression;
            Assert.AreEqual(GDDualOperatorType.In, dualOperator.OperatorType);

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }
    }
}
