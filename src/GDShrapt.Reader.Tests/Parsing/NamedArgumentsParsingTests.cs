using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Tests for parsing named arguments in function calls (Godot 4 feature)
    /// and default parameters in function declarations.
    /// </summary>
    [TestClass]
    public class NamedArgumentsParsingTests
    {
        #region Default Parameters in Declarations

        [TestMethod]
        public void ParseDefaultParam_SimpleNull()
        {
            var reader = new GDScriptReader();

            var code = @"func greet(name = null):
	print(name)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseDefaultParam_StringValue()
        {
            var reader = new GDScriptReader();

            var code = @"func greet(name = ""World""):
	print(name)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseDefaultParam_IntValue()
        {
            var reader = new GDScriptReader();

            var code = @"func calculate(x, multiplier = 1):
	return x * multiplier";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseDefaultParam_FloatValue()
        {
            var reader = new GDScriptReader();

            var code = @"func wait_time(seconds = 1.5):
	await get_tree().create_timer(seconds).timeout";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseDefaultParam_BoolValue()
        {
            var reader = new GDScriptReader();

            var code = @"func toggle(enabled = true):
	visible = enabled";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseDefaultParam_ArrayValue()
        {
            var reader = new GDScriptReader();

            var code = @"func process_items(items = []):
	for item in items:
		print(item)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseDefaultParam_DictionaryValue()
        {
            var reader = new GDScriptReader();

            var code = @"func configure(options = {}):
	for key in options:
		print(key)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseDefaultParam_VectorValue()
        {
            var reader = new GDScriptReader();

            var code = @"func move(direction = Vector2.ZERO):
	position += direction";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseDefaultParam_MultipleParams()
        {
            var reader = new GDScriptReader();

            var code = @"func create_entity(name = ""Entity"", health = 100, position = Vector2.ZERO):
	var entity = Entity.new()
	entity.name = name
	entity.health = health
	entity.position = position
	return entity";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseDefaultParam_MixedRequiredAndOptional()
        {
            var reader = new GDScriptReader();

            var code = @"func spawn(scene: PackedScene, count = 1, spread = 10.0):
	for i in count:
		var instance = scene.instantiate()
		add_child(instance)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseDefaultParam_WithTypeHint()
        {
            var reader = new GDScriptReader();

            var code = @"func greet(name: String = ""World"") -> void:
	print(""Hello, "", name)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseDefaultParam_WithComplexExpression()
        {
            var reader = new GDScriptReader();

            var code = @"func calculate(value = 2 + 3 * 4):
	return value";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion

        #region Named Arguments in Function Calls

        [TestMethod]
        public void ParseNamedArg_SingleArgument()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	greet(name = ""World"")";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseNamedArg_MultipleArguments()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	create_entity(name = ""Player"", health = 100, position = Vector2(10, 20))";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseNamedArg_MixedPositionalAndNamed()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	spawn(enemy_scene, count = 5, spread = 20.0)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseNamedArg_OutOfOrder()
        {
            var reader = new GDScriptReader();

            // Named arguments can be in any order
            var code = @"func test():
	configure(debug = true, timeout = 30, name = ""test"")";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseNamedArg_WithComplexExpression()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	process(data = get_data(), transform = func(x): return x * 2)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseNamedArg_InMethodChain()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	node.configure(enabled = true).start(delay = 1.0)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseNamedArg_InConstructor()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	var rect = Rect2(position = Vector2.ZERO, size = Vector2(100, 100))";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseNamedArg_NestedCalls()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	outer(inner(value = 10), name = ""test"")";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion

        #region Combined Scenarios

        [TestMethod]
        public void ParseNamedArg_DeclarationAndCall()
        {
            var reader = new GDScriptReader();

            var code = @"func greet(name = ""World"", greeting = ""Hello""):
	print(greeting, "", "", name)

func test():
	greet(name = ""Claude"", greeting = ""Hi"")";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseNamedArg_SignalConnect()
        {
            var reader = new GDScriptReader();

            var code = @"func _ready():
	button.pressed.connect(_on_pressed, CONNECT_ONE_SHOT)
	timer.timeout.connect(_on_timeout, flags = CONNECT_DEFERRED)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseNamedArg_TweenMethods()
        {
            var reader = new GDScriptReader();

            var code = @"func animate():
	var tween = create_tween()
	tween.tween_property(self, ""position"", Vector2(100, 100), duration = 1.0)
	tween.tween_callback(finish, delay = 0.5)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseNamedArg_StaticMethod()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	var result = MyClass.create(name = ""instance"", parent = self)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion
    }
}
