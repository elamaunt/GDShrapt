using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    [TestClass]
    public class InvalidTokenTests
    {
        [TestMethod]
        public void InvalidStaticTest()
        {
            var reader = new GDScriptReader();

            var code = @"static signal my_signal(value, other_value)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.AllInvalidTokens.Count());
            Assert.AreEqual("static", declaration.AllInvalidTokens.First().Sequence);
            AssertHelper.CompareCodeStrings(code, declaration.ToString());
        }

        [TestMethod]
        public void DoubleStaticTest()
        {
            var reader = new GDScriptReader();

            var code = @"static static func my_method(value): return value > 0";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.AllInvalidTokens.Count());
            Assert.AreEqual("static", declaration.AllInvalidTokens.First().Sequence);
            AssertHelper.CompareCodeStrings(code, declaration.ToString());
        }
    }
}
