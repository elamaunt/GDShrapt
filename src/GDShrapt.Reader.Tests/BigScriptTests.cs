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
    }
}
