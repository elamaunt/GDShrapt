using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Tests for correct function boundary detection.
    /// These tests verify that the parser correctly identifies where one function ends
    /// and the next begins, especially in complex scenarios with nested constructs.
    /// </summary>
    [TestClass]
    public class FunctionBoundaryTests
    {
        #region Basic Function Boundary Tests

        [TestMethod]
        public void ParseFunctions_TwoSimpleFunctions()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	pass


func second():
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");
            declaration.Methods.ElementAt(0).Identifier?.ToString().Should().Be("first");
            declaration.Methods.ElementAt(1).Identifier?.ToString().Should().Be("second");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_ThreeFunctions()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	pass


func second():
	pass


func third():
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(3, "should detect three functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_WithReturnTypes()
        {
            var reader = new GDScriptReader();

            var code = @"func first() -> void:
	pass


func second() -> int:
	return 42";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_WithParameters()
        {
            var reader = new GDScriptReader();

            var code = @"func first(a, b):
	print(a, b)


func second(x: int, y: int) -> int:
	return x + y";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_WithDefaultParameters()
        {
            var reader = new GDScriptReader();

            var code = @"func first(a = null):
	print(a)


func second(x = 1, y = 2):
	return x + y";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion

        #region Functions with Complex Bodies

        [TestMethod]
        public void ParseFunctions_WithIfStatements()
        {
            var reader = new GDScriptReader();

            var code = @"func first(a):
	if a:
		return a
	return null


func second(b):
	if b > 0:
		return true
	else:
		return false";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_WithForLoops()
        {
            var reader = new GDScriptReader();

            var code = @"func first(arr):
	for item in arr:
		print(item)


func second(count):
	for i in range(count):
		print(i)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_WithWhileLoops()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	while true:
		break


func second(n):
	while n > 0:
		n -= 1";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_WithMatchStatements()
        {
            var reader = new GDScriptReader();

            var code = @"func first(value):
	match value:
		1:
			return ""one""
		2:
			return ""two""
		_:
			return ""other""


func second(type):
	match type:
		""a"":
			pass
		""b"":
			pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_WithNestedBlocks()
        {
            var reader = new GDScriptReader();

            var code = @"func first(data):
	if data:
		for item in data:
			if item > 0:
				print(item)


func second(value):
	while value:
		if value > 10:
			break
		value += 1";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion

        #region Functions with Lambdas

        [TestMethod]
        public void ParseFunctions_WithInlineLambda()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	var f := func(): pass
	f.call()


func second():
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_WithMultilineLambda()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	var f := func(x):
		print(x)
	f.call(1)


func second():
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_WithMultilineLambdaAndDefaultParam()
        {
            var reader = new GDScriptReader();

            // Without await - this should work
            var code = @"func first():
	var f := func(x):
		print(x)
	f.call(1)


func second(a = null):
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_WithMultipleLambdas()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	var f1 := func(): print(1)
	var f2 := func(x):
		print(x)
	f1.call()
	f2.call(2)


func second():
	var g := func(a, b): return a + b
	print(g.call(1, 2))";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion

        #region Functions with Await

        [TestMethod]
        public void ParseFunctions_WithAwait_TwoFunctions()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	await some_signal


func second():
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");
            declaration.Methods.ElementAt(0).Identifier?.ToString().Should().Be("first");
            declaration.Methods.ElementAt(1).Identifier?.ToString().Should().Be("second");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_WithAwaitAndCode_TwoFunctions()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	await get_tree().create_timer(1.0).timeout
	print(""done"")


func second():
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_WithAwaitAndDefaultParam_TwoFunctions()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	await some_signal
	print(""done"")


func second(a = null):
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_WithAwaitAndComplexNextFunction()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	await some_signal
	print(""done"")


func second(a):
	if a:
		return a
	return null";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_WithAwait_SingleFunction()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	await some_signal
	print(""done"")";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(1);
            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_AwaitAsLastStatement()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	await some_signal


func second():
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion

        #region Await + Lambda + Default Param

        [TestMethod]
        public void ParseFunctions_AwaitLambdaDefaultParam_TwoFunctions()
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
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_AwaitLambdaDefaultParam_SimpleBody()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	var f := func():
		pass
	await s


func second(a = null):
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_AwaitInlineLambdaDefaultParam()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	var f := func(): pass
	await s


func second(a = null):
	if a:
		return a
	return null";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_MultilineLambdaNoAwait_DefaultParam()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	var f := func():
		pass
	f.call()


func second(a = null):
	if a:
		return a
	return null";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFunctions_MultilineLambdaAwait_NoDefaultParam()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	var f := func():
		pass
	await s


func second(a):
	if a:
		return a
	return null";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion

        #region Mixed Class Members

        [TestMethod]
        public void ParseMembers_FunctionAfterVariable()
        {
            var reader = new GDScriptReader();

            var code = @"var my_var = 10


func my_func():
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Variables.Count().Should().Be(1);
            declaration.Methods.Count().Should().Be(1);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseMembers_FunctionAfterSignal()
        {
            var reader = new GDScriptReader();

            var code = @"signal my_signal


func my_func():
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Signals.Count().Should().Be(1);
            declaration.Methods.Count().Should().Be(1);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseMembers_FunctionAfterEnum()
        {
            var reader = new GDScriptReader();

            var code = @"enum State { IDLE, RUNNING, STOPPED }


func my_func():
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Enums.Count().Should().Be(1);
            declaration.Methods.Count().Should().Be(1);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseMembers_VariableAfterFunction()
        {
            var reader = new GDScriptReader();

            var code = @"func my_func():
	pass


var my_var = 10";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(1);
            declaration.Variables.Count().Should().Be(1);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion
    }
}
