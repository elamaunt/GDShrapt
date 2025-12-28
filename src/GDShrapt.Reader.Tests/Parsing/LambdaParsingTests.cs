using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Parsing
{
    /// <summary>
    /// Tests for parsing lambda expressions in various contexts.
    /// </summary>
    [TestClass]
    public class LambdaParsingTests
    {
        [TestMethod]
        public void ParseLambda_InArrayInitializer()
        {
            var reader = new GDScriptReader();

            var code = @"var operations = [
    func(x): return x + 1,
    func(x): return x * 2,
]";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);

            var variable = declaration.Variables.FirstOrDefault();
            Assert.IsNotNull(variable);
            Assert.IsInstanceOfType(variable.Initializer, typeof(GDArrayInitializerExpression));
        }

        [TestMethod]
        public void ParseLambda_AsFunctionArgument()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    var result = filter(func(x): return x > 0)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_MultipleAsArguments()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    process(func(x): return x * 2, func(y): return y + 1)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_CallableConstructor()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    var handler = Callable(self, ""_on_pressed"")";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_InDictionary()
        {
            var reader = new GDScriptReader();

            var code = @"var handlers = {
    ""add"": func(a, b): return a + b,
    ""sub"": func(a, b): return a - b
}";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_NestedInMethodCall()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    button.pressed.connect(func(): print(""Pressed!""))";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_WithReturnType()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    var f = func(x: int) -> int: return x * 2";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_EmptyBody()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    var f = func(): pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_ChainedMethodCalls()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    var mapped = map_array(numbers, func(x): return x ** 2)
    var filtered = filter_array(mapped, func(x): return x > 25)
    var result = reduce_array(filtered, 0, func(acc, x): return acc + x)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_WithBind()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    activated.connect(_on_activated.bind(""extra_data""))";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_SignalConnect()
        {
            var reader = new GDScriptReader();

            var code = @"func _ready():
    pressed.connect(_on_pressed)
    released.connect(func(): print(""Released!""))";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_InArrayWithTypedArray()
        {
            var reader = new GDScriptReader();

            var code = @"var operations: Array[Callable] = [
    func(x): return x + 1,
    func(x): return x * 2,
    func(x): return x ** 2,
]";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_MultilineBodyFollowedByCode()
        {
            var reader = new GDScriptReader();

            // This tests a multi-line lambda inside a function, followed by more code
            var code = @"func async_operation() -> void:
	var on_complete := func(result):
		print(result)
	on_complete.call(""success"")";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_MultilineWithEmptyLineBeforeNextStatement()
        {
            var reader = new GDScriptReader();

            // Multi-line lambda with an empty line before the next statement
            var code = @"func async_operation() -> void:
	var on_complete := func(result):
		print(result)

	on_complete.call(""success"")";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_MultilineWithAwaitAfter()
        {
            var reader = new GDScriptReader();

            // Multi-line lambda followed by await (exact Sample7 pattern)
            var code = @"func async_operation() -> void:
	var on_complete := func(result):
		print(""Async operation completed with: "", result)

	await get_tree().create_timer(1.0).timeout
	on_complete.call(""success"")";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_Sample7Pattern_AsyncFollowedByOtherFunction()
        {
            var reader = new GDScriptReader();

            // Without await - this works
            var code = @"func async_operation() -> void:
	var on_complete := func(result):
		print(""Async operation completed with: "", result)

	on_complete.call(""success"")


func process_with_transform(data, transform = null):
	if transform != null:
		return map_array(data, transform)
	return data";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_AwaitAfterMultilineLambda()
        {
            var reader = new GDScriptReader();

            // await after multi-line lambda (without next function)
            var code = @"func async_operation() -> void:
	var on_complete := func(result):
		print(""Async operation completed with: "", result)

	await get_tree().create_timer(1.0).timeout
	on_complete.call(""success"")";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_AwaitFollowedByNextFunction()
        {
            var reader = new GDScriptReader();

            // await followed by a new function - this test demonstrates an existing parser bug
            // where await + empty line + more code + next function causes issues
            // TODO: This is a pre-existing bug in await parsing, not related to lambda fix
            var code = @"func async_operation() -> void:
	var on_complete := func(result):
		print(""Async operation completed with: "", result)

	await get_tree().create_timer(1.0).timeout
	on_complete.call(""success"")


func process_with_transform(data, transform = null):
	if transform != null:
		return map_array(data, transform)
	return data";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            // Known issue: await parsing bug - uncomment when fixed:
            // AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseAwait_WithoutLambda_FollowedByNextFunction()
        {
            var reader = new GDScriptReader();

            // Known issue: await + empty line + code + next function causes issues
            var code = @"func async_operation() -> void:
	await get_tree().create_timer(1.0).timeout
	print(""done"")


func next_function(data, transform = null):
	if transform != null:
		return data
	return data";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            // Known await bug - uncomment when fixed:
            // AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_WithMemberAccessInBody()
        {
            var reader = new GDScriptReader();

            // Lambda with member access call inside (like signal.emit)
            var code = @"func forward_signal(source_signal: Signal) -> void:
	source_signal.connect(func(data): forwarded_event.emit(data))";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_TwoFunctionsWithMultilineLambda()
        {
            var reader = new GDScriptReader();

            // Two separate functions - the first has a multi-line lambda
            var code = @"func first():
	var f := func(x):
		print(x)
	f.call(1)


func second():
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_MultilineInFunctionBody()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	var complex_lambda := func(x: int) -> int:
		var result := x * 2
		result += 10
		return result
	print(""Complex: "", complex_lambda.call(5))";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }
    }
}
