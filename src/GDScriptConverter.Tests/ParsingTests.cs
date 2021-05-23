using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDScriptConverter.Tests
{
    [TestClass]
    public class ParsingTests
    {
        [TestMethod]
        public void ParseClassTest1()
        {
            var parser = new GDScriptParser();

            var code = @"
tool
class_name HTerrainDataSaver
extends ResourceFormatSaver

const HTerrainData = preload(""./ hterrain_data.gd"")


func get_recognized_extensions(res):
	if res != null and res is HTerrainData:
		return PoolStringArray([HTerrainData.META_EXTENSION])
	return PoolStringArray()


func recognize(res):
	return res is HTerrainData


func save(path, resource, flags):
	resource.save_data(path.get_base_dir())
";

            var declaration = parser.ParseFileContent(code);

            Assert.IsNotNull(declaration);
            Assert.IsInstanceOfType(declaration, typeof(GDClassDeclaration));

            var @class = (GDClassDeclaration)declaration;

            Assert.AreEqual("ResourceFormatSaver", @class.ExtendsClass?.Sequence);
            Assert.AreEqual("HTerrainDataSaver", @class.Name?.Sequence);
            Assert.AreEqual(true, @class.IsTool);
        }
    }
}
