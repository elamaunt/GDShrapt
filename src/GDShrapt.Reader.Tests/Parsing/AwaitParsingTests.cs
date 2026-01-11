using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Tests for await expression parsing.
    /// These tests verify that await expressions are correctly parsed
    /// and don't break function boundary detection.
    /// </summary>
    [TestClass]
    public class AwaitParsingTests
    {
        #region Basic Await Parsing

        [TestMethod]
        public void ParseAwait_SimpleSignal()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	await pressed";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseAwait_WithTimer()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	await get_tree().create_timer(1.0).timeout";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseAwait_FollowedByCode()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	await get_tree().create_timer(1.0).timeout
	print(""done"")";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion

        #region Await Followed by Another Function

        [TestMethod]
        public void ParseAwait_FollowedBySimpleFunction()
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

        [TestMethod]
        public void ParseAwait_FollowedByFunctionWithParams()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	await some_signal


func second(data, value):
	print(data)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseAwait_FollowedByFunctionWithDefaultParam()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	await some_signal


func second(data, value = null):
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion

        #region Await + Lambda Combinations

        [TestMethod]
        public void ParseAwait_WithMultilineLambda_SimpleNextFunc()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	var callback := func(x):
		print(x)

	await get_tree().create_timer(1.0).timeout
	callback.call(""test"")


func second(data, value = null):
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseAwait_NoLambda_DefaultParam()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	await get_tree().create_timer(1.0).timeout
	print(""done"")


func second(data, value = null):
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseAwait_WithLambda_NoDefaultParam()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	var callback := func(x):
		print(x)

	await get_tree().create_timer(1.0).timeout
	callback.call(""test"")


func second(data, value):
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseAwait_InlineLambda_DefaultParam()
        {
            var reader = new GDScriptReader();

            var code = @"func first():
	var callback := func(x): print(x)

	await get_tree().create_timer(1.0).timeout
	callback.call(""test"")


func second(data, value = null):
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseAwait_MultilineLambda_DefaultParam_ComplexBody()
        {
            var reader = new GDScriptReader();

            var code = @"func async_operation() -> void:
	var on_complete := func(result):
		print(""Async operation completed with: "", result)

	await get_tree().create_timer(1.0).timeout
	on_complete.call(""success"")


func process_with_transform(data, transform = null):
	if transform != null:
		return data
	return data";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion

        #region Minimal Reproduction Cases

        [TestMethod]
        public void ParseAwait_Minimal_MultilineLambdaAwaitDefaultParam()
        {
            var reader = new GDScriptReader();

            var code = @"func f():
	var x := func():
		pass
	await s


func g(a = null):
	if a:
		return a
	return a";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseAwait_Minimal_InlineLambdaAwaitDefaultParam()
        {
            var reader = new GDScriptReader();

            var code = @"func f():
	var x := func(): pass
	await s


func g(a = null):
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseAwait_Minimal_NoLambda()
        {
            var reader = new GDScriptReader();

            var code = @"func f():
	await s


func g(a = null):
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseAwait_Minimal_NoDefaultParam()
        {
            var reader = new GDScriptReader();

            var code = @"func f():
	var x := func(): pass
	await s


func g(a):
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseAwait_Minimal_NoAwait()
        {
            var reader = new GDScriptReader();

            var code = @"func f():
	var x := func(): pass
	x.call()


func g(a = null):
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            declaration.Methods.Count().Should().Be(2, "should detect two functions");

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion
    }
}
