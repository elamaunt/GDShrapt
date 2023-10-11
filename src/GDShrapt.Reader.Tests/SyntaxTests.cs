using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    [TestClass]
    public class SyntaxTests
    {
        [TestMethod]
        public void CommentsTest()
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
        public void CommentsTest2()
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
        public void DictionaryCodeStyleTest()
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
        public void SyntaxCloneTest()
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

            var clone = @class.Clone();
            AssertHelper.CompareCodeStrings(@class.ToString(), clone.ToString());
        }

        [TestMethod]
        public void LineAndColumnTest()
        {
            var reader = new GDScriptReader();

            var code = @"func _init(res : string = ""Hello world"").(res) -> void:
	._init(""1234"");
	var array = [1,
                 2,
                 3]
	for i in array:
		print(i)
	pass";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);
            Assert.AreEqual(1, @class.Methods.Count());

            var tokens = @class.AllTokens.ToArray();

            int i = 0;

            CheckPosition(tokens[i++], 0, 0, 0, 0); // intendation
            CheckPosition(tokens[i++], 0, 0, 0, 4); // func
            CheckPosition(tokens[i++], 0, 4, 0, 5); // ' '
            CheckPosition(tokens[i++], 0, 5, 0, 10); // _init
            CheckPosition(tokens[i++], 0, 10, 0, 11); // (
            CheckPosition(tokens[i++], 0, 11, 0, 14); // res
            CheckPosition(tokens[i++], 0, 14, 0, 15); // ' '
            CheckPosition(tokens[i++], 0, 15, 0, 16); // :
            CheckPosition(tokens[i++], 0, 16, 0, 17); // ' '
            CheckPosition(tokens[i++], 0, 17, 0, 23); // string
            CheckPosition(tokens[i++], 0, 23, 0, 24); // ' '
            CheckPosition(tokens[i++], 0, 24, 0, 25); // =
            CheckPosition(tokens[i++], 0, 25, 0, 26); // ' '
            CheckPosition(tokens[i++], 0, 26, 0, 39); // "Hello world"
            CheckPosition(tokens[i++], 0, 39, 0, 40); // )
            CheckPosition(tokens[i++], 0, 40, 0, 41); // .
            CheckPosition(tokens[i++], 0, 41, 0, 42); // (
            CheckPosition(tokens[i++], 0, 42, 0, 45); // res
            CheckPosition(tokens[i++], 0, 45, 0, 46); // )
            CheckPosition(tokens[i++], 0, 46, 0, 47); // ' '
            CheckPosition(tokens[i++], 0, 47, 0, 49); // ->
            CheckPosition(tokens[i++], 0, 49, 0, 50); // ' '
            CheckPosition(tokens[i++], 0, 50, 0, 54); // void
            CheckPosition(tokens[i++], 0, 54, 0, 55); // :
            CheckPosition(tokens[i++], 0, 55, 1, 0); // \n

            CheckPosition(tokens[i++], 1, 0, 1, 1); // intendation
            CheckPosition(tokens[i++], 1, 1, 1, 2); // .
            CheckPosition(tokens[i++], 1, 2, 1, 7); // _init
            CheckPosition(tokens[i++], 1, 7, 1, 8); // (
            CheckPosition(tokens[i++], 1, 8, 1, 14); // "1234"
            CheckPosition(tokens[i++], 1, 14, 1, 15); // )
            CheckPosition(tokens[i++], 1, 15, 1, 16); // ;
            CheckPosition(tokens[i++], 1, 16, 2, 0); // \n

            CheckPosition(tokens[i++], 2, 0, 2, 1); // intendation
            CheckPosition(tokens[i++], 2, 1, 2, 4); // var
            CheckPosition(tokens[i++], 2, 4, 2, 5); // ' '
            CheckPosition(tokens[i++], 2, 5, 2, 10); // array
            CheckPosition(tokens[i++], 2, 10, 2, 11); // ' '
            CheckPosition(tokens[i++], 2, 11, 2, 12); // =
            CheckPosition(tokens[i++], 2, 12, 2, 13); // ' '
            CheckPosition(tokens[i++], 2, 13, 2, 14); // [
            CheckPosition(tokens[i++], 2, 14, 2, 15); // 1
            CheckPosition(tokens[i++], 2, 15, 2, 16); // ,
            CheckPosition(tokens[i++], 2, 16, 3, 0); // \n

            CheckPosition(tokens[i++], 3, 0, 3, 17); // '                 ' big space
            CheckPosition(tokens[i++], 3, 17, 3, 18); // 2
            CheckPosition(tokens[i++], 3, 18, 3, 19); // ,
            CheckPosition(tokens[i++], 3, 19, 4, 0); // \n

            CheckPosition(tokens[i++], 4, 0, 4, 17); // '                 ' big space
            CheckPosition(tokens[i++], 4, 17, 4, 18); // 3
            CheckPosition(tokens[i++], 4, 18, 4, 19); // ]
            CheckPosition(tokens[i++], 4, 19, 5, 0); // \n

            CheckPosition(tokens[i++], 5, 0, 5, 1); // intendation
            CheckPosition(tokens[i++], 5, 1, 5, 4); // for
            CheckPosition(tokens[i++], 5, 4, 5, 5); // ' '
            CheckPosition(tokens[i++], 5, 5, 5, 6); // i
            CheckPosition(tokens[i++], 5, 6, 5, 7); // ' '
            CheckPosition(tokens[i++], 5, 7, 5, 9); // in
            CheckPosition(tokens[i++], 5, 9, 5, 10); // ' '
            CheckPosition(tokens[i++], 5, 10, 5, 15); // array
            CheckPosition(tokens[i++], 5, 15, 5, 16); // :
            CheckPosition(tokens[i++], 5, 16, 6, 0); // \n

            CheckPosition(tokens[i++], 6, 0, 6, 2); // intendation 2
            CheckPosition(tokens[i++], 6, 2, 6, 7); // print
            CheckPosition(tokens[i++], 6, 7, 6, 8); // (
            CheckPosition(tokens[i++], 6, 8, 6, 9); // i
            CheckPosition(tokens[i++], 6, 9, 6, 10); // )
            CheckPosition(tokens[i++], 6, 10, 7, 0); // \n

            CheckPosition(tokens[i++], 7, 0, 7, 1); // intendation
            CheckPosition(tokens[i++], 7, 1, 7, 5); // pass
        }


        [TestMethod]
        public void LineAndColumnTest2()
        {
            var reader = new GDScriptReader();
            var code = @"extends Node2D

class_name Usage

# Declare member variables here. Examples:
# var a = 2
# var b = ""text""


# Called when the node enters the scene tree for the first time.
func _ready(): 
	pass

func updateSample(obj):
	var value = obj.t()

    print(value)
";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);
            Assert.AreEqual(2, @class.Methods.Count());

            var tokens = @class.AllTokens.ToArray();

            int i = 0;

            CheckPosition(tokens[i++], 0, 0, 0, 0); // intendation
            CheckPosition(tokens[i++], 0, 0, 0, 7); // extends
            CheckPosition(tokens[i++], 0, 7, 0, 8); // ' '
            CheckPosition(tokens[i++], 0, 8, 0, 14); // Node2D
            CheckPosition(tokens[i++], 0, 14, 1, 0); // \n
            CheckPosition(tokens[i++], 1, 0, 2, 0); // \n
            CheckPosition(tokens[i++], 2, 0, 2, 0); // intendation
            CheckPosition(tokens[i++], 2, 0, 2, 10); // class_name
            CheckPosition(tokens[i++], 2, 10, 2, 11); // ' '
            CheckPosition(tokens[i++], 2, 11, 2, 16); // Usage
        }

        private void CheckPosition(GDSyntaxToken token, int startLine, int startColumn, int endLine, int endColumn)
        {
            Assert.AreEqual(startLine, token.StartLine, $"StartLine of {token.TypeName}. Length: {token.Length}");
            Assert.AreEqual(startColumn, token.StartColumn, $"StartColumn of {token.TypeName}. Length: {token.Length}");

            Assert.AreEqual(endLine, token.EndLine, $"EndLine of {token.TypeName}. Length: {token.Length}");
            Assert.AreEqual(endColumn, token.EndColumn, $"EndColumn of {token.TypeName}. Length: {token.Length}");
        }

        [TestMethod]
        public void GetWholeLineTest()
        {
            var reader = new GDScriptReader();
            var code = @"extends Node2D 

class_name Usage 

# Declare member variables here. Examples:
# var a = 2
# var b = ""text""


# Called when the node enters the scene tree for the first time. 
func _ready(): 
	pass

func updateSample(obj):
	var value = obj.t()

    print(value)
";

            var @class = reader.ParseFileContent(code);

            var lines = code.Replace("\r", "").Split('\n');
           
            AssertHelper.CompareCodeStrings(code, @class.ToString());

            foreach (var token in @class.AllTokens)
            {
                var lineByToken = token.BuildLineThatContains();

                AssertHelper.CompareCodeStrings(lines[token.EndLine], lineByToken);
            }
        }

        [TestMethod]
        public void CommentsSyntaxTest()
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
    }
}
