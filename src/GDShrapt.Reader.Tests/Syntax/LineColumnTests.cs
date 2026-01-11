using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Tests for line and column position tracking.
    /// </summary>
    [TestClass]
    public class LineColumnTests
    {
        [TestMethod]
        public void SyntaxPosition_SingleLineTracking()
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
        }

        [TestMethod]
        public void SyntaxPosition_MultiLineTracking()
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

        [TestMethod]
        public void SyntaxPosition_GetLineFromToken()
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

        private void CheckPosition(GDSyntaxToken token, int startLine, int startColumn, int endLine, int endColumn)
        {
            Assert.AreEqual(startLine, token.StartLine, $"StartLine of {token.TypeName}. Length: {token.Length}");
            Assert.AreEqual(startColumn, token.StartColumn, $"StartColumn of {token.TypeName}. Length: {token.Length}");
            Assert.AreEqual(endLine, token.EndLine, $"EndLine of {token.TypeName}. Length: {token.Length}");
            Assert.AreEqual(endColumn, token.EndColumn, $"EndColumn of {token.TypeName}. Length: {token.Length}");
        }
    }
}
