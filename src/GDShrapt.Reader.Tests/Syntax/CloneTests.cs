using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Syntax
{
    /// <summary>
    /// Tests for AST cloning.
    /// </summary>
    [TestClass]
    public class CloneTests
    {
        [TestMethod]
        public void Clone_PreservesCodeAndComments()
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
    }
}
