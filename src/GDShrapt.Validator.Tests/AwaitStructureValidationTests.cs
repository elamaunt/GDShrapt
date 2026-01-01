using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests that verify await-related structure is parsed correctly.
    /// These tests check that function boundaries are properly detected
    /// when await is present.
    /// </summary>
    [TestClass]
    public class AwaitStructureValidationTests
    {
        #region Await + Function Boundary Tests

        [TestMethod]
        public void AwaitStructure_TwoFunctions_BothDetected()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	await some_signal


func second():
	pass";

            var declaration = reader.ParseFileContent(code);

            // Should detect 2 methods
            declaration.Methods.Count().Should().Be(2, "both functions should be detected");

            var first = declaration.Methods.ElementAt(0);
            var second = declaration.Methods.ElementAt(1);

            first.Identifier?.ToString().Should().Be("first");
            second.Identifier?.ToString().Should().Be("second");

            // First function should NOT contain second function as nested
            first.AllNodes.OfType<GDMethodDeclaration>().Should().BeEmpty(
                "first function should not contain nested method declarations");

            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void AwaitStructure_TwoFunctions_StatementsInCorrectParent()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	await some_signal


func second():
	print(1)
	print(2)";

            var declaration = reader.ParseFileContent(code);

            declaration.Methods.Count().Should().Be(2, "both functions should be detected");

            var first = declaration.Methods.ElementAt(0);
            var second = declaration.Methods.ElementAt(1);

            // First function should have only 1 statement (await)
            first.Statements.Count.Should().Be(1, "first function should have 1 statement");

            // Second function should have 2 statements (print, print)
            second.Statements.Count.Should().Be(2, "second function should have 2 statements");

            // Verify no print calls in first function
            first.AllNodes.OfType<GDCallExpression>()
                .Count(c => c.CallerExpression?.ToString() == "print")
                .Should().Be(0, "first function should not contain print statements from second");
        }

        [TestMethod]
        public void AwaitStructure_TwoFunctions_IfStatementInCorrectParent()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	await some_signal


func second(a):
	if a:
		return a
	return null";

            var declaration = reader.ParseFileContent(code);

            declaration.Methods.Count().Should().Be(2, "both functions should be detected");

            var first = declaration.Methods.ElementAt(0);
            var second = declaration.Methods.ElementAt(1);

            // First function should NOT contain if statement
            first.AllNodes.OfType<GDIfStatement>().Should().BeEmpty(
                "first function should not contain if statement from second");

            // Second function SHOULD contain if statement
            second.AllNodes.OfType<GDIfStatement>().Should().NotBeEmpty(
                "second function should contain its if statement");
        }

        [TestMethod]
        public void AwaitStructure_ThreeFunctions_AllDetected()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	await some_signal


func second():
	pass


func third():
	pass";

            var declaration = reader.ParseFileContent(code);

            declaration.Methods.Count().Should().Be(3, "all three functions should be detected");

            var names = declaration.Methods.Select(m => m.Identifier?.ToString()).ToList();
            names.Should().Contain("first");
            names.Should().Contain("second");
            names.Should().Contain("third");

            // No function should contain nested method declarations
            foreach (var method in declaration.Methods)
            {
                method.AllNodes.OfType<GDMethodDeclaration>().Should().BeEmpty(
                    $"method {method.Identifier} should not contain nested method declarations");
            }

            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion

        #region Await + Lambda + Function Boundary Tests

        [TestMethod]
        public void AwaitStructure_LambdaAndAwait_NextFunctionDetected()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	var f := func():
		pass
	await some_signal


func second():
	pass";

            var declaration = reader.ParseFileContent(code);

            declaration.Methods.Count().Should().Be(2, "both functions should be detected");

            var first = declaration.Methods.ElementAt(0);
            var second = declaration.Methods.ElementAt(1);

            first.Identifier?.ToString().Should().Be("first");
            second.Identifier?.ToString().Should().Be("second");

            // Second function should not be nested inside first
            first.AllNodes.OfType<GDMethodDeclaration>().Should().BeEmpty(
                "first function should not contain second function as nested method");

            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void AwaitStructure_LambdaAwaitDefaultParam_BothFunctionsDetected()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	var f := func():
		pass
	await s


func second(a = null):
	if a:
		return a
	return null";

            var declaration = reader.ParseFileContent(code);

            // Should detect 2 methods with no invalid tokens
            declaration.Methods.Count().Should().Be(2, "both functions should be detected");
            AssertHelper.NoInvalidTokens(declaration);

            var first = declaration.Methods.ElementAt(0);
            var second = declaration.Methods.ElementAt(1);

            first.Identifier?.ToString().Should().Be("first");
            second.Identifier?.ToString().Should().Be("second");

            // If statement should be in second function, not first
            first.AllNodes.OfType<GDIfStatement>().Should().BeEmpty(
                "first function should not contain if statement from second");
            second.AllNodes.OfType<GDIfStatement>().Should().NotBeEmpty(
                "second function should contain its if statement");
        }

        #endregion

        #region Working Cases - No Await

        [TestMethod]
        public void AwaitStructure_NoAwait_TwoFunctions()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	print(1)


func second():
	print(2)";

            var declaration = reader.ParseFileContent(code);

            // Without await, both functions should be detected
            declaration.Methods.Count().Should().Be(2);

            var first = declaration.Methods.ElementAt(0);
            var second = declaration.Methods.ElementAt(1);

            first.Identifier?.ToString().Should().Be("first");
            second.Identifier?.ToString().Should().Be("second");

            // Each should have only its own statements
            first.Statements.Count.Should().Be(1);
            second.Statements.Count.Should().Be(1);

            // No nested functions
            first.AllNodes.OfType<GDMethodDeclaration>().Should().BeEmpty();
            second.AllNodes.OfType<GDMethodDeclaration>().Should().BeEmpty();
        }

        [TestMethod]
        public void AwaitStructure_LambdaNoAwait_TwoFunctions()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	var f := func():
		pass
	f.call()


func second():
	pass";

            var declaration = reader.ParseFileContent(code);

            // Without await, lambda doesn't break boundary detection
            declaration.Methods.Count().Should().Be(2);

            var first = declaration.Methods.ElementAt(0);
            var second = declaration.Methods.ElementAt(1);

            first.Identifier?.ToString().Should().Be("first");
            second.Identifier?.ToString().Should().Be("second");
        }

        [TestMethod]
        public void AwaitStructure_AwaitInSingleFunction()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	await some_signal
	print(""done"")";

            var declaration = reader.ParseFileContent(code);

            // Single function with await works fine
            declaration.Methods.Count().Should().Be(1);

            var method = declaration.Methods.First();
            method.Identifier?.ToString().Should().Be("test");
            method.Statements.Count.Should().Be(2, "both await and print should be statements");

            // Verify the content is there
            var hasAwait = method.AllNodes.OfType<GDAwaitExpression>().Any();
            hasAwait.Should().BeTrue("await should be present in the function");
        }

        #endregion

        #region Variable/Signal After Await Function

        [TestMethod]
        public void AwaitStructure_VariableAfterAwaitFunction()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	await some_signal


var my_var = 10";

            var declaration = reader.ParseFileContent(code);

            // Variable should be at class level, not absorbed into function
            declaration.Variables.Count().Should().Be(1, "variable should be at class level");
            declaration.Methods.Count().Should().Be(1);

            var method = declaration.Methods.First();
            method.AllTokens.OfType<GDVarKeyword>().Should().BeEmpty(
                "function should not contain class-level var keyword");
        }

        [TestMethod]
        public void AwaitStructure_SignalAfterAwaitFunction()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	await some_signal


signal my_signal";

            var declaration = reader.ParseFileContent(code);

            // Signal should be at class level
            declaration.Signals.Count().Should().Be(1, "signal should be at class level");
            declaration.Methods.Count().Should().Be(1);

            var method = declaration.Methods.First();
            method.AllTokens.Any(t => t.ToString() == "signal").Should().BeFalse(
                "function should not contain signal declaration");
        }

        #endregion

        #region Complex Scenarios

        [TestMethod]
        public void AwaitStructure_AwaitInMiddleOfFile()
        {
            var reader = new GDScriptReader();

            var code = @"func before():
	pass


func with_await():
	await some_signal


func after():
	pass";

            var declaration = reader.ParseFileContent(code);

            declaration.Methods.Count().Should().Be(3, "all three functions should be detected");

            var names = declaration.Methods.Select(m => m.Identifier?.ToString()).ToList();
            names.Should().Contain("before");
            names.Should().Contain("with_await");
            names.Should().Contain("after");
        }

        [TestMethod]
        public void AwaitStructure_MultipleAwaitsMultipleFunctions()
        {
            var reader = new GDScriptReader();

            var code = @"func a():
	await signal1


func b():
	await signal2


func c():
	pass";

            var declaration = reader.ParseFileContent(code);

            declaration.Methods.Count().Should().Be(3, "all three functions should be detected");

            // Each function should have its own await, not absorb others
            var funcA = declaration.Methods.ElementAt(0);
            var funcB = declaration.Methods.ElementAt(1);
            var funcC = declaration.Methods.ElementAt(2);

            funcA.AllNodes.OfType<GDAwaitExpression>().Count().Should().Be(1);
            funcB.AllNodes.OfType<GDAwaitExpression>().Count().Should().Be(1);
            funcC.AllNodes.OfType<GDAwaitExpression>().Count().Should().Be(0);
        }

        [TestMethod]
        public void AwaitStructure_AwaitWithCodeAfter_NextFunctionDetected()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	await get_tree().create_timer(1.0).timeout
	print(""done"")


func second():
	pass";

            var declaration = reader.ParseFileContent(code);

            declaration.Methods.Count().Should().Be(2, "both functions should be detected");
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void AwaitStructure_AwaitWithDefaultParamNextFunction()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	await some_signal
	print(""done"")


func second(a = null):
	pass";

            var declaration = reader.ParseFileContent(code);

            declaration.Methods.Count().Should().Be(2, "both functions should be detected");
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion
    }
}
