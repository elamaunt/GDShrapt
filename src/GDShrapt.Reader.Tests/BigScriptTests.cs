using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace GDShrapt.Reader.Tests
{
    [TestClass]
    public class BigScriptTests
    {
        [TestMethod]
        public void BigScriptParsingTest()
        {
            var reader = new GDScriptReader();

            var path = Path.Combine("Scripts", "Sample.gd");
            var declaration = reader.ParseFile(path);

            var fileText = File.ReadAllText(path);

            AssertHelper.CompareCodeStrings(fileText, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void BigScriptParsingTest2()
        {
            var reader = new GDScriptReader();

            var path = Path.Combine("Scripts", "Sample2.gd");
            var declaration = reader.ParseFile(path);

            var fileText = File.ReadAllText(path);

            AssertHelper.CompareCodeStrings(fileText, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void BigScriptParsingTest3()
        {
            var reader = new GDScriptReader();

            var path = Path.Combine("Scripts", "Sample3.gd");
            var declaration = reader.ParseFile(path);

            var fileText = File.ReadAllText(path);

            AssertHelper.CompareCodeStrings(fileText, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void BigScriptParsingTest4()
        {
            var reader = new GDScriptReader();

            var path = Path.Combine("Scripts", "Sample4.gd");
            var declaration = reader.ParseFile(path);

            var fileText = File.ReadAllText(path);

            AssertHelper.CompareCodeStrings(fileText, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void BigScriptParsingTest5()
        {
            var reader = new GDScriptReader();

            var path = Path.Combine("Scripts", "Sample5.gd");
            var declaration = reader.ParseFile(path);

            var fileText = File.ReadAllText(path);

            AssertHelper.CompareCodeStrings(fileText, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ScriptTest1()
        {
            var reader = new GDScriptReader();

            var code = "# _at_position is not used because it doesn't matter where on the panel\r\n# the item is dropped\r\nfunc _can_drop_data(_at_position: Vector2, data: Variant) -> bool:\t\r\n\tif data is InventoryItem:\r\n\t\t#This is the text that displays uupon pulling an item out.\r\n\t\t%summary.text =( str(\"atk:\" + str(data.physicalattack) +'\\n' + data.lore))\r\n\t\tif type == InventoryItem.Type.MAIN:\r\n\t\t\tif get_child_count() == 0:\r\n\t\t\t\treturn true\r\n\t\t\telse:\r\n\t\t\t\t# Swap two items\r\n\t\t\t\treturn get_child(0).type == data.type\r\n\t\telse:\r\n\t\t\treturn data.type == type\r\n\t\t\t\r\n\t\r\n\treturn false";

            var declaration = reader.ParseFileContent(code);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }
    }
}
