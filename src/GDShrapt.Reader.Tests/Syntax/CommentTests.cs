using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Tests for comment parsing and preservation.
    /// </summary>
    [TestClass]
    public class CommentTests
    {
        [TestMethod]
        public void CommentsPreserved()
        {
            var reader = new GDScriptReader();

            var code = @"
# before tool comment
tool # tool comment

# before class name comment
class_name HTerrainDataSaver # class name comment

# before extends comment
extends ResourceFormatSaver # extends comment

# before const comment
const HTerrainData = preload(""./ hterrain_data.gd"") # const comment

# before func comment 1
# before func comment 2
func get_recognized_extensions(res): # func comment

    # before if statement comment
	if res != null and res is HTerrainData: # if expression comment
# before return statement comment
		return PoolStringArray([HTerrainData.META_EXTENSION]) # if true statement comment

	return PoolStringArray()

# end file comment 1
# end file comment 2
";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(17, comments.Length);

            comments.Should().BeEquivalentTo(new[]
            {
                "# before tool comment",
                "# tool comment",
                "# before class name comment",
                "# class name comment",
                "# before extends comment",
                "# extends comment",
                "# before const comment",
                "# const comment",
                "# before func comment 1",
                "# before func comment 2",
                "# func comment",
                "# before if statement comment",
                "# if expression comment",
                "# before return statement comment",
                "# if true statement comment",
                "# end file comment 1",
                "# end file comment 2"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInEnum()
        {
            var reader = new GDScriptReader();

            var code = @"
# before enum comment
enum { a, # a comment
# before b comment
       b, # b comment
# before c comment
       c # c comment}
# after c comment
      } # enum ending comment
";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
               .OfType<GDComment>()
               .Select(x => x.ToString())
               .ToArray();

            Assert.AreEqual(8, comments.Length);

            comments.Should().BeEquivalentTo(new[]
            {
                "# before enum comment",
                "# a comment",
                "# before b comment",
                "# b comment",
                "# before c comment",
                "# c comment}",
                "# after c comment",
                "# enum ending comment"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
        }

        [TestMethod]
        public void CommentsInBrackets()
        {
            var reader = new GDScriptReader();
            var code = @"extends Node2D

#func _ready() -> void:
func _process(delta) -> void:
	var t = TYPE_NIL
	if !(
		t == TYPE_NIL
		|| t == TYPE_AABB
		|| t == TYPE_ARRAY
		|| t == TYPE_BASIS
		|| t == TYPE_BOOL
		|| t == TYPE_COLOR
		|| t == TYPE_COLOR_ARRAY
		|| t == TYPE_DICTIONARY
		|| t == TYPE_INT
		|| t == TYPE_VECTOR3_ARRAY
		#			# TODOGODOT4
		#			|| t == TYPE_VECTOR2I
		#			|| t == TYPE_VECTOR3I
		#			|| t == TYPE_STRING_NAME
		#			|| t == TYPE_RECT2I
		#			|| t == TYPE_FLOAT64_ARRAY
		#			|| t == TYPE_INT64_ARRAY
		#			|| t == TYPE_CALLABLE
	):
		return ";

            var @class = reader.ParseFileContent(code);

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInDictionary()
        {
            var reader = new GDScriptReader();

            var code = @"
{
a: 0,
""1"": x # : x [1,2,3] + d = l
    f + d = lkj  :[1,2,3]
# : xa: 0,
}";

            var expression = reader.ParseExpression(code);
            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDDictionaryInitializerExpression));
            Assert.AreEqual(3, ((GDDictionaryInitializerExpression)expression).KeyValues.Count);
            Assert.AreEqual(2, expression.AllTokens.OfType<GDComment>().Count());
            AssertHelper.CompareCodeStrings(code, "\n" + expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void CommentsInArray()
        {
            var reader = new GDScriptReader();

            var code = @"[
    1, # first element
    # before second
    2, # second element
    # before third
    3 # third element
    # after third
]";

            var expression = reader.ParseExpression(code);
            Assert.IsNotNull(expression);
            Assert.IsInstanceOfType(expression, typeof(GDArrayInitializerExpression));

            var comments = expression.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(6, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# first element",
                "# before second",
                "# second element",
                "# before third",
                "# third element",
                "# after third"
            });

            AssertHelper.CompareCodeStrings(code, expression.ToString());
            AssertHelper.NoInvalidTokens(expression);
        }

        [TestMethod]
        public void CommentsInMethodParameters()
        {
            var reader = new GDScriptReader();

            var code = @"
# function comment
func my_func(
    # before a
    a, # after a
    # before b
    b: int, # after b
    # before c
    c: float = 1.0 # after c
    # after all params
) -> void: # return type comment
    pass
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(9, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# function comment",
                "# before a",
                "# after a",
                "# before b",
                "# after b",
                "# before c",
                "# after c",
                "# after all params",
                "# return type comment"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInSignalDeclaration()
        {
            var reader = new GDScriptReader();

            var code = @"
# before signal comment
signal my_signal # after signal name
signal my_signal_with_params(a, b) # after signal params
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(3, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# before signal comment",
                "# after signal name",
                "# after signal params"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInForLoop()
        {
            var reader = new GDScriptReader();

            var code = @"
func test():
    # before for
    for i in range(10): # after for header
        # inside for body
        print(i) # after print
    # after for loop
    pass
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(5, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# before for",
                "# after for header",
                "# inside for body",
                "# after print",
                "# after for loop"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInWhileLoop()
        {
            var reader = new GDScriptReader();

            var code = @"
func test():
    var x = 0
    # before while
    while x < 10: # after while condition
        # inside while body
        x += 1 # after increment
    # after while loop
    pass
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(5, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# before while",
                "# after while condition",
                "# inside while body",
                "# after increment",
                "# after while loop"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInMatchStatement()
        {
            var reader = new GDScriptReader();

            var code = @"
func test(x):
    # before match
    match x: # after match expression
        # before case 1
        1: # after pattern 1
            print(""one"") # case 1 body
        # before case 2
        2, 3: # after pattern 2,3
            print(""two or three"") # case 2 body
        # before default case
        _: # after default pattern
            print(""other"") # default body
    # after match
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(12, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# before match",
                "# after match expression",
                "# before case 1",
                "# after pattern 1",
                "# case 1 body",
                "# before case 2",
                "# after pattern 2,3",
                "# case 2 body",
                "# before default case",
                "# after default pattern",
                "# default body",
                "# after match"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInVariableDeclarations()
        {
            var reader = new GDScriptReader();

            var code = @"
# before var comment
var my_var = 10 # after var comment

# before typed var
var typed_var: int = 20 # typed var comment

# before const
const MY_CONST = 100 # const comment

# before onready
@onready var node = get_node(""Node"") # onready comment

# before static var
static var static_var = ""test"" # static var comment
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(10, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# before var comment",
                "# after var comment",
                "# before typed var",
                "# typed var comment",
                "# before const",
                "# const comment",
                "# before onready",
                "# onready comment",
                "# before static var",
                "# static var comment"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInInnerClass()
        {
            var reader = new GDScriptReader();

            var code = @"
# before inner class
class InnerClass: # after class name
    # before inner var
    var inner_var = 5 # inner var comment

    # before inner func
    func inner_func(): # inner func comment
        pass
# after inner class
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(7, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# before inner class",
                "# after class name",
                "# before inner var",
                "# inner var comment",
                "# before inner func",
                "# inner func comment",
                "# after inner class"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInLambdaExpression()
        {
            var reader = new GDScriptReader();

            var code = @"
func test():
    # before lambda
    var my_lambda = func(x): return x * 2 # lambda comment
    # before lambda with body
    var complex_lambda = func(
        # before param
        a, # param comment
        b
    ): # after params
        return a + b
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(6, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# before lambda",
                "# lambda comment",
                "# before lambda with body",
                "# before param",
                "# param comment",
                "# after params"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInIfExpression()
        {
            var reader = new GDScriptReader();

            var code = @"
func test():
    # ternary expression
    var result = ""yes"" if true else ""no"" # after ternary
    # nested ternary
    var nested = 1 if a else 2 if b else 3 # nested comment
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(4, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# ternary expression",
                "# after ternary",
                "# nested ternary",
                "# nested comment"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInIndexerExpression()
        {
            var reader = new GDScriptReader();

            var code = @"
func test():
    var arr = [1, 2, 3]
    # before indexer
    var first = arr[0] # after indexer
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(2, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# before indexer",
                "# after indexer"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInCallExpression()
        {
            var reader = new GDScriptReader();

            var code = @"
func test():
    # before call
    my_func(
        # before arg1
        arg1, # after arg1
        # before arg2
        arg2, # after arg2
        # before arg3
        arg3 # after arg3
        # after all args
    ) # after call
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(9, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# before call",
                "# before arg1",
                "# after arg1",
                "# before arg2",
                "# after arg2",
                "# before arg3",
                "# after arg3",
                "# after all args",
                "# after call"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void EmptyComment()
        {
            var reader = new GDScriptReader();

            var code = @"
#
var x = 1 #
#
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(3, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "#",
                "#",
                "#"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsWithSpecialCharacters()
        {
            var reader = new GDScriptReader();

            var code = @"
# Special chars: !@#$%^&*()_+-={}[]|\:"";<>?,./~`
var x = 1 # Unicode: ąęćżźń日本語中文العربية

# TODO: Fix this later
# FIXME: Bug here
# NOTE: Important note
# HACK: Workaround
# XXX: Needs attention

# URL: https://example.com/path?query=value&other=test
# Email: test@example.com
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(9, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# Special chars: !@#$%^&*()_+-={}[]|\\:\";<>?,./~`",
                "# Unicode: ąęćżźń日本語中文العربية",
                "# TODO: Fix this later",
                "# FIXME: Bug here",
                "# NOTE: Important note",
                "# HACK: Workaround",
                "# XXX: Needs attention",
                "# URL: https://example.com/path?query=value&other=test",
                "# Email: test@example.com"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInExportDeclaration()
        {
            var reader = new GDScriptReader();

            var code = @"
# before export
@export var simple_export: int # simple export comment

# before export range
@export_range(0, 100) var ranged_var: int # range comment

# before export enum
@export_enum(""Option1"", ""Option2"") var enum_var: int # enum export comment

# before export multiline
@export_multiline var multiline_text: String # multiline comment

# before export group
@export_group(""My Group"") # group comment
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(10, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# before export",
                "# simple export comment",
                "# before export range",
                "# range comment",
                "# before export enum",
                "# enum export comment",
                "# before export multiline",
                "# multiline comment",
                "# before export group",
                "# group comment"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInMultilineExpressions()
        {
            var reader = new GDScriptReader();

            var code = @"
func test():
    # multiline arithmetic
    var result = 1 + # after 1
        2 + # after 2
        3 + # after 3
        4 # after 4

    # multiline boolean
    var flag = true and # after true
        false or # after false
        true # final value

    # multiline string concatenation
    var text = ""hello "" + # after hello
        ""world"" # after world
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(12, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# multiline arithmetic",
                "# after 1",
                "# after 2",
                "# after 3",
                "# after 4",
                "# multiline boolean",
                "# after true",
                "# after false",
                "# final value",
                "# multiline string concatenation",
                "# after hello",
                "# after world"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInElseIfChain()
        {
            var reader = new GDScriptReader();

            var code = @"
func test(x):
    # before if
    if x == 1: # condition 1
        print(""one"") # body 1
    # before elif 1
    elif x == 2: # condition 2
        print(""two"") # body 2
    # before elif 2
    elif x == 3: # condition 3
        print(""three"") # body 3
    # before else
    else: # else block
        print(""other"") # else body
    # after if chain
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(13, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# before if",
                "# condition 1",
                "# body 1",
                "# before elif 1",
                "# condition 2",
                "# body 2",
                "# before elif 2",
                "# condition 3",
                "# body 3",
                "# before else",
                "# else block",
                "# else body",
                "# after if chain"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInAwaitExpression()
        {
            var reader = new GDScriptReader();

            var code = @"
func test():
    # before await
    await get_tree().create_timer(1.0).timeout # after await
    # before await signal
    await my_signal # await signal comment
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(4, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# before await",
                "# after await",
                "# before await signal",
                "# await signal comment"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInGetterSetter()
        {
            var reader = new GDScriptReader();

            var code = @"
# before property
var my_property: int: # property type comment
    # before getter
    get: # getter comment
        return _value # getter body
    # before setter
    set(value): # setter comment
        _value = value # setter body

# inline getter setter
var inline_prop: int = 0: # inline comment
    get: # inline get
        return _inline
    set(v): # inline set
        _inline = v
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(12, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# before property",
                "# property type comment",
                "# before getter",
                "# getter comment",
                "# getter body",
                "# before setter",
                "# setter comment",
                "# setter body",
                "# inline getter setter",
                "# inline comment",
                "# inline get",
                "# inline set"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInTypeHints()
        {
            var reader = new GDScriptReader();

            var code = @"
# typed array
var arr: Array[int] = [] # array type comment

# typed dictionary
var dict: Dictionary = {} # dict comment

# function with typed return
func get_value() -> int: # return type
    return 0

# function with complex types
func process(items: Array[String], callback: Callable) -> Dictionary: # complex return
    return {}
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(8, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# typed array",
                "# array type comment",
                "# typed dictionary",
                "# dict comment",
                "# function with typed return",
                "# return type",
                "# function with complex types",
                "# complex return"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInStaticMethods()
        {
            var reader = new GDScriptReader();

            var code = @"
# before static func
static func static_method(): # static func comment
    pass

# before static var
static var static_var: int = 0 # static var comment

# class with static members
class MyClass:
    # inner static func
    static func inner_static(): # inner static comment
        pass
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(7, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# before static func",
                "# static func comment",
                "# before static var",
                "# static var comment",
                "# class with static members",
                "# inner static func",
                "# inner static comment"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInAnnotations()
        {
            var reader = new GDScriptReader();

            var code = @"
# before tool annotation
@tool # tool annotation comment

# before icon annotation
@icon(""res://icon.png"") # icon comment

# before onready
@onready var ready_node = get_node(""Node"") # onready comment

# before warning ignore
@warning_ignore(""unused_variable"") # warning comment
var unused = 0
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(8, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# before tool annotation",
                "# tool annotation comment",
                "# before icon annotation",
                "# icon comment",
                "# before onready",
                "# onready comment",
                "# before warning ignore",
                "# warning comment"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInStringInterpolation()
        {
            var reader = new GDScriptReader();

            var code = @"
func test():
    var name = ""World""
    # before string
    var greeting = ""Hello, %s!"" % name # after string
    # format string
    var formatted = ""Value: %d, Float: %.2f"" % [10, 3.14] # format comment
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(4, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# before string",
                "# after string",
                "# format string",
                "# format comment"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentOnlyFile()
        {
            var reader = new GDScriptReader();

            var code = @"# This is a comment-only file
# With multiple lines
# And nothing else
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(3, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# This is a comment-only file",
                "# With multiple lines",
                "# And nothing else"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInComplexNestedStructure()
        {
            var reader = new GDScriptReader();

            var code = @"
# Main class comment
class_name ComplexClass # class name comment
extends Node # extends comment

# Signal section
signal data_changed(old_value, new_value) # signal comment

# Enum section
enum State { # enum comment
    IDLE, # idle state
    RUNNING, # running state
    STOPPED # stopped state
}

# Inner class
class DataHolder: # data holder comment
    var data: Dictionary # data var

# Main method
func process_data(input: Array, options: Dictionary = {}) -> bool: # return type
    # Local variables
    var result = true # result var

    # Process loop
    for item in input: # for comment
        # Check condition
        if item is Dictionary: # type check
            # Nested if
            if item.has(""key""): # key check
                result = result and true # update result

    # Return statement
    return result # final return
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            // Verify all comments are preserved
            Assert.IsTrue(comments.Length >= 18, $"Expected at least 18 comments, got {comments.Length}");

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsWithIndentation()
        {
            var reader = new GDScriptReader();

            var code = @"
func test():
    # Level 1 indent
    if true:
        # Level 2 indent
        if true:
            # Level 3 indent
            if true:
                # Level 4 indent
                pass
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(4, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# Level 1 indent",
                "# Level 2 indent",
                "# Level 3 indent",
                "# Level 4 indent"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInPreloadAndLoad()
        {
            var reader = new GDScriptReader();

            var code = @"
# Preload constants
const Scene1 = preload(""res://scene1.tscn"") # scene 1
const Scene2 = preload(""res://scene2.tscn"") # scene 2

func test():
    # Dynamic load
    var resource = load(""res://resource.tres"") # load comment
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(5, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# Preload constants",
                "# scene 1",
                "# scene 2",
                "# Dynamic load",
                "# load comment"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInSuperCalls()
        {
            var reader = new GDScriptReader();

            var code = @"
extends Node

func _ready():
    # Before super call
    super._ready() # super call comment
    # After super call
    pass

func _process(delta):
    # Super with args
    super._process(delta) # process super
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(5, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# Before super call",
                "# super call comment",
                "# After super call",
                "# Super with args",
                "# process super"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void CommentsInAssertAndPush()
        {
            var reader = new GDScriptReader();

            var code = @"
func test():
    # Assert statement
    assert(true, ""Should be true"") # assert comment

    # Push error
    push_error(""Error message"") # push error comment

    # Push warning
    push_warning(""Warning message"") # push warning comment

    # Print debug
    print_debug(""Debug info"") # debug comment
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var comments = @class.AllTokens
                .OfType<GDComment>()
                .Select(x => x.ToString())
                .ToArray();

            Assert.AreEqual(8, comments.Length);
            comments.Should().BeEquivalentTo(new[]
            {
                "# Assert statement",
                "# assert comment",
                "# Push error",
                "# push error comment",
                "# Push warning",
                "# push warning comment",
                "# Print debug",
                "# debug comment"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }
    }
}
