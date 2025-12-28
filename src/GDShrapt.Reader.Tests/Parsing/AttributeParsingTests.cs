using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Parsing
{
    /// <summary>
    /// Tests for parsing annotations and attributes.
    /// </summary>
    [TestClass]
    public class AttributeParsingTests
    {
        [TestMethod]
        public void ParseAttribute_Rpc()
        {
            var reader = new GDScriptReader();

            var code = @"@rpc(""any_peer"", ""call_local"")
func test():
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Methods.Count());

            var attributes = declaration.AllNodes.OfType<GDCustomAttribute>().ToArray();
            Assert.AreEqual(1, attributes.Length);
            Assert.AreEqual("rpc", attributes[0].Attribute.Name?.Sequence);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseAttribute_ExportVariants()
        {
            var reader = new GDScriptReader();

            var code = @"@export_range(0.0, 1.0, 0.01)
var test1: float

@export_enum(""a"", ""b"", ""c"")
var test2: String

@export
var test3: int

@export_file(""*.gd"")
var test4: String";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(4, declaration.Variables.Count());

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseAttribute_WarningIgnore()
        {
            var reader = new GDScriptReader();

            var code = @"@warning_ignore(""unused_variable"")
var test: float";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Variables.Count());

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseAttribute_WarningIgnoreInMethod()
        {
            var reader = new GDScriptReader();

            var code = @"func f():
	@warning_ignore(""unused_variable"")
	var test: float";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Methods.Count());
            Assert.AreEqual(1, declaration.Methods.First().Statements.Count);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseAttribute_Multiple()
        {
            var reader = new GDScriptReader();

            var code = @"@a @b
var test: int";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Variables.Count());

            var attributes = declaration.AllNodes.OfType<GDCustomAttribute>().ToArray();
            Assert.AreEqual(2, attributes.Length);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseAttribute_ExportDeclarationsFull()
        {
            var reader = new GDScriptReader();

            var code = @"
# If the exported value assigns a constant or constant expression,
# the type will be inferred and used in the editor.

@export var number = 5

# Export can take a basic data type as an argument, which will be
# used in the editor.

@export(int) var number

# Export can also take a resource type to use as a hint.

@export(Texture) var character_face
@export(PackedScene) var scene_file
# There are many resource types that can be used this way, try e.g.
# the following to list them:
@export(Resource) var resource

# Integers and strings hint enumerated values.

# Editor will enumerate as 0, 1 and 2.
@export(int, ""Warrior"", ""Magician"", ""Thief"") var character_class
# Editor will enumerate with string names.
@export(String, ""Rebecca"", ""Mary"", ""Leah"") var character_name

# Named enum values

# Editor will enumerate as THING_1, THING_2, ANOTHER_THING.
enum NamedEnum { THING_1, THING_2, ANOTHER_THING = -1 }
        @export(NamedEnum) var x

# Strings as paths

# String is a path to a file.
@export(String, FILE) var f
# String is a path to a directory.
@export(String, DIR) var f
# String is a path to a file, custom filter provided as hint.
@export(String, FILE, ""*.txt"") var f

# Using paths in the global filesystem is also possible,
# but only in scripts in ""tool"" mode.

# String is a path to a PNG file in the global filesystem.
@export(String, FILE, GLOBAL, ""*.png"") var tool_image
# String is a path to a directory in the global filesystem.
@export(String, DIR, GLOBAL) var tool_dir

# The MULTILINE setting tells the editor to show a large input
# field for editing over multiple lines.
@export(String, MULTILINE) var text

# Limiting editor input ranges

# Allow integer values from 0 to 20.
@export(int, 20) var i
# Allow integer values from -10 to 20.
@export(int, -10, 20) var j
# Allow floats from -10 to 20 and snap the value to multiples of 0.2.
@export(float, -10, 20, 0.2) var k
# Allow values 'y = exp(x)' where 'y' varies between 100 and 1000
# while snapping to steps of 20. The editor will present a
# slider for easily editing the value.
@export(float, EXP, 100, 1000, 20) var l

# Floats with easing hint

# Display a visual representation of the 'ease()' function
# when editing.
@export(float, EASE) var transition_speed

# Colors

# Color given as red-green-blue value (alpha will always be 1).
@export(Color, RGB) var col
# Color given as red-green-blue-alpha value.
@export(Color, RGBA) var col

# Nodes

# Another node in the scene can be exported as a NodePath.
@export(NodePath) var node_path
# Do take note that the node itself isn't being exported -
# there is one more step to call the true node:
var node = get_node(node_path)

# Resources

@export(Resource) var resource
# In the Inspector, you can then drag and drop a resource file
# from the FileSystem dock into the variable slot.

# Opening the inspector dropdown may result in an
# extremely long list of possible classes to create, however.
# Therefore, if you specify an extension of Resource such as:
@export(AnimationNode) var resource
# The drop-down menu will be limited to AnimationNode and all
# its inherited classes.
";
            var classDeclaration = reader.ParseFileContent(code);

            Assert.IsNotNull(classDeclaration);

            var exports = classDeclaration.AllNodes.OfType<GDCustomAttribute>().ToArray();

            Assert.AreEqual(24, exports.Length);

            AssertHelper.CompareCodeStrings(code, classDeclaration.ToString());
            AssertHelper.NoInvalidTokens(classDeclaration);
        }
    }
}
