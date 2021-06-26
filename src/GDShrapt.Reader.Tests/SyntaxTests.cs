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
            AssertHelper.CompareCodeStrings(code, "\n"+expression.ToString());
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
    }
}
