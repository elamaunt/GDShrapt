﻿using FluentAssertions;
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

        [TestMethod]
        public void InvalidClassNameTest()
        {
            var reader = new GDScriptReader();

            var code = @"tool
class_name 123H+=Ter^5r3_-ain-DataSaver
extends ResourceFormatSaver
";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Select(x => x.ToString()).Should().BeEquivalentTo(new string[] 
            {
                "123",
                "H+=Ter^5r3_-ain-DataSaver"
            });
        }
    }
}
