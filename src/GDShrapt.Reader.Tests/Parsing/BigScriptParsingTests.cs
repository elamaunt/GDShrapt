using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Tests for parsing large real-world scripts.
    /// </summary>
    [TestClass]
    public class BigScriptParsingTests
    {
        [TestMethod]
        public void ParseBigScript_Sample1()
        {
            var reader = new GDScriptReader();

            var path = Path.Combine("Scripts", "Sample.gd");
            var declaration = reader.ParseFile(path);

            var fileText = File.ReadAllText(path);

            AssertHelper.CompareCodeStrings(fileText, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseBigScript_Sample2()
        {
            var reader = new GDScriptReader();

            var path = Path.Combine("Scripts", "Sample2.gd");
            var declaration = reader.ParseFile(path);

            var fileText = File.ReadAllText(path);

            AssertHelper.CompareCodeStrings(fileText, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseBigScript_Sample3()
        {
            var reader = new GDScriptReader();

            var path = Path.Combine("Scripts", "Sample3.gd");
            var declaration = reader.ParseFile(path);

            var fileText = File.ReadAllText(path);

            AssertHelper.CompareCodeStrings(fileText, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseBigScript_Sample4()
        {
            var reader = new GDScriptReader();

            var path = Path.Combine("Scripts", "Sample4.gd");
            var declaration = reader.ParseFile(path);

            var fileText = File.ReadAllText(path);

            AssertHelper.CompareCodeStrings(fileText, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseBigScript_Sample5()
        {
            var reader = new GDScriptReader();

            var path = Path.Combine("Scripts", "Sample5.gd");
            var declaration = reader.ParseFile(path);

            var fileText = File.ReadAllText(path);

            AssertHelper.CompareCodeStrings(fileText, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseBigScript_InventoryDropData()
        {
            var reader = new GDScriptReader();

            var code = "# _at_position is not used because it doesn't matter where on the panel\r\n# the item is dropped\r\nfunc _can_drop_data(_at_position: Vector2, data: Variant) -> bool:\t\r\n\tif data is InventoryItem:\r\n\t\t#This is the text that displays uupon pulling an item out.\r\n\t\t%summary.text =( str(\"atk:\" + str(data.physicalattack) +'\\n' + data.lore))\r\n\t\tif type == InventoryItem.Type.MAIN:\r\n\t\t\tif get_child_count() == 0:\r\n\t\t\t\treturn true\r\n\t\t\telse:\r\n\t\t\t\t# Swap two items\r\n\t\t\t\treturn get_child(0).type == data.type\r\n\t\telse:\r\n\t\t\treturn data.type == type\r\n\t\t\t\r\n\t\r\n\treturn false";

            var declaration = reader.ParseFileContent(code);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseBigScript_WithDifferentIndentations()
        {
            var reader = new GDScriptReader();

            var code = @"if Input.is_action_just_pressed(""move_left""):
        target_direction = Vector2.LEFT
elif Input.is_action_just_pressed(""move_right""):
    target_direction = Vector2.RIGHT
elif Input.is_action_just_pressed(""move_up""):
    target_direction = Vector2.UP
elif Input.is_action_just_pressed(""move_down""):
    target_direction = Vector2.DOWN";

            var statements = reader.ParseStatementsList(code);

            Assert.IsNotNull(statements);
            Assert.AreEqual(1, statements.Count);

            AssertHelper.CompareCodeStrings(code, statements.ToString());
            AssertHelper.NoInvalidTokens(statements);
        }

        [TestMethod]
        public void ParseBigScript_WithInvalidIndentation()
        {
            var reader = new GDScriptReader();

            var code = @"if Input.is_action_just_pressed(""move_left""):
                target_direction = Vector2.LEFT
        elif Input.is_action_just_pressed(""move_right""):
    target_direction = Vector2.RIGHT
        elif Input.is_action_just_pressed(""move_up""):
            target_direction = Vector2.UP
        elif Input.is_action_just_pressed(""move_down""):
                    target_direction = Vector2.DOWN";

            var statements = reader.ParseStatementsList(code);

            Assert.IsNotNull(statements);

            AssertHelper.CompareCodeStrings(code, statements.ToString());
        }

        [TestMethod]
        public void ParseBigScript_WithShaderParameter()
        {
            var reader = new GDScriptReader();

            var code = @"func _ready():
	shader.set_shader_parameter(""density"", density)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseBigScript_WithYield()
        {
            var reader = new GDScriptReader();

            var code = @"func f():
	yield(get_tree().create_timer(1.0), ""timeout"")";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseBigScript_WithMultilineYield()
        {
            var reader = new GDScriptReader();

            var code = @"func f():
	yield(
		get_tree()
		.create_timer(1.0), ""timeout"")";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseBigScript_Sample6_InnerClasses()
        {
            var reader = new GDScriptReader();

            var path = Path.Combine("Scripts", "Sample6_InnerClasses.gd");
            var declaration = reader.ParseFile(path);

            var fileText = File.ReadAllText(path);

            AssertHelper.CompareCodeStrings(fileText, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseBigScript_Sample7_Lambdas()
        {
            var reader = new GDScriptReader();

            var path = Path.Combine("Scripts", "Sample7_Lambdas.gd");
            var declaration = reader.ParseFile(path);

            var fileText = File.ReadAllText(path);

            AssertHelper.CompareCodeStrings(fileText, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseBigScript_Sample8_MatchPatterns()
        {
            var reader = new GDScriptReader();

            var path = Path.Combine("Scripts", "Sample8_MatchPatterns.gd");
            var declaration = reader.ParseFile(path);

            var fileText = File.ReadAllText(path);

            AssertHelper.CompareCodeStrings(fileText, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseBigScript_Sample9_Properties()
        {
            var reader = new GDScriptReader();

            var path = Path.Combine("Scripts", "Sample9_Properties.gd");
            var declaration = reader.ParseFile(path);

            var fileText = File.ReadAllText(path);

            AssertHelper.CompareCodeStrings(fileText, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseBigScript_Sample10_Operators()
        {
            var reader = new GDScriptReader();

            var path = Path.Combine("Scripts", "Sample10_Operators.gd");
            var declaration = reader.ParseFile(path);

            var fileText = File.ReadAllText(path);

            AssertHelper.CompareCodeStrings(fileText, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseBigScript_Sample11_Annotations()
        {
            var reader = new GDScriptReader();

            var path = Path.Combine("Scripts", "Sample11_Annotations.gd");
            var declaration = reader.ParseFile(path);

            var fileText = File.ReadAllText(path);

            AssertHelper.CompareCodeStrings(fileText, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseBigScript_Sample12_Signals()
        {
            var reader = new GDScriptReader();

            var path = Path.Combine("Scripts", "Sample12_Signals.gd");
            var declaration = reader.ParseFile(path);

            var fileText = File.ReadAllText(path);

            AssertHelper.CompareCodeStrings(fileText, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseBigScript_Sample13_TypeSystem()
        {
            var reader = new GDScriptReader();

            var path = Path.Combine("Scripts", "Sample13_TypeSystem.gd");
            var declaration = reader.ParseFile(path);

            var fileText = File.ReadAllText(path);

            AssertHelper.CompareCodeStrings(fileText, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }
    }
}
