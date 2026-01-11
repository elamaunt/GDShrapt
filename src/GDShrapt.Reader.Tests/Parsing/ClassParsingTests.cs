using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Tests for parsing class declarations.
    /// </summary>
    [TestClass]
    public class ClassParsingTests
    {
        [TestMethod]
        public void ParseClass_WithToolAndExtends()
        {
            var reader = new GDScriptReader();

            var code = @"tool
class_name HTerrainDataSaver
extends ResourceFormatSaver

const HTerrainData = preload(""./ hterrain_data.gd"")

signal on_save

func get_recognized_extensions(res):
	if res != null and res is HTerrainData:
		return PoolStringArray([HTerrainData.META_EXTENSION])
	return PoolStringArray()


func recognize(res):
	return res is HTerrainData


func save(path, resource, flags):
	resource.save_data(path.get_base_dir())
    on_save.emit();
";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);
            Assert.AreEqual("ResourceFormatSaver", (@class.Extends?.Type as GDSingleTypeNode)?.Type?.Sequence);
            Assert.AreEqual("HTerrainDataSaver", @class.ClassName?.Identifier?.Sequence);
            Assert.AreEqual(true, @class.IsTool);

            Assert.AreEqual(3, @class.Attributes.Count());
            Assert.AreEqual(8, @class.Members.Count);
            Assert.AreEqual(1, @class.Variables.Count());
            Assert.AreEqual(3, @class.Methods.Count());
            Assert.AreEqual(1, @class.Signals.Count());
            Assert.AreEqual(2, @class.Methods.ElementAt(0).Statements.Count);
            Assert.AreEqual(1, @class.Methods.ElementAt(1).Statements.Count);
            Assert.AreEqual(2, @class.Methods.ElementAt(2).Statements.Count);

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void ParseClass_WithClassNameAndExtends()
        {
            var reader = new GDScriptReader();

            var code = @"class_name Test extends Node";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);
            Assert.AreEqual("Test", @class.ClassName?.Identifier?.Sequence);
            Assert.AreEqual("Node", (@class.Extends?.Type as GDSingleTypeNode)?.Type?.Sequence);

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void ParseClass_WithExtendsOnly()
        {
            var reader = new GDScriptReader();

            var code = @"extends Node";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);
            Assert.AreEqual("Node", (@class.Extends?.Type as GDSingleTypeNode)?.Type?.Sequence);

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void ParseClass_WithExtendsFromPath()
        {
            var reader = new GDScriptReader();

            var code = @"extends 'res://path/to/character.gd'

func a():
    pass";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);
            Assert.AreEqual("'res://path/to/character.gd'", @class.Extends?.Type?.ToString());

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void ParseClass_WithInnerClasses()
        {
            var reader = new GDScriptReader();

            var code = @"# Inherit from Character.gd

extends ""res://path/to/character.gd""

# Declare member variables here. Examples:

var a = 2
var b = ""text""


# Called when the node enters the scene tree for the first time.

func _ready():
	pass # Replace with function body.

class InnerClass:
	var inner_a = 123

	func inner_ready():
		pass

class InnerClass2:
	var inner_b = ""inner""

	func inner_ready2():
		pass";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);
            Assert.AreEqual(2, @class.InnerClasses.Count());

            var inner1 = @class.InnerClasses.ElementAt(0);
            Assert.AreEqual("InnerClass", inner1.Identifier?.Sequence);
            Assert.AreEqual(1, inner1.Variables.Count());
            Assert.AreEqual(1, inner1.Methods.Count());

            var inner2 = @class.InnerClasses.ElementAt(1);
            Assert.AreEqual("InnerClass2", inner2.Identifier?.Sequence);
            Assert.AreEqual(1, inner2.Variables.Count());
            Assert.AreEqual(1, inner2.Methods.Count());

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void ParseClass_WithAnnotationAttributes()
        {
            var reader = new GDScriptReader();

            var code = @"@static_unload
@tool
extends Node";

            var @class = reader.ParseFileContent(code);
            var attributes = @class.AllNodes.OfType<GDClassAttribute>().ToArray();
            Assert.AreEqual(3, attributes.Length);

            Assert.AreEqual("@static_unload", attributes[0].ToString());
            Assert.AreEqual("@tool", attributes[1].ToString());
            Assert.AreEqual("extends Node", attributes[2].ToString());

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }
    }
}
