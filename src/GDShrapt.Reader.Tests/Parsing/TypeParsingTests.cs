using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Parsing
{
    /// <summary>
    /// Tests for parsing typed arrays and dictionaries.
    /// </summary>
    [TestClass]
    public class TypeParsingTests
    {
        [TestMethod]
        public void ParseType_Array()
        {
            var reader = new GDScriptReader();

            var code = @"var a: Array[int]
var b: Array[Array[int]]
var c: Array[Array[Array[int]]]";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(3, declaration.Variables.Count());

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseType_Dictionary()
        {
            var reader = new GDScriptReader();

            var code = @"var a: Dictionary[int, string]
var b: Dictionary[string, Array[int]]
var c: Dictionary[int, Dictionary[string, float]]";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(3, declaration.Variables.Count());

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseType_ExportedDictionary()
        {
            var reader = new GDScriptReader();

            var code = @"
@export var typed_key_value: Dictionary[int, String ] = { 1: ""first value"", 2: ""second value"", 3: ""etc"" }
@export var typed_key: Dictionary[ int, Variant] = { 0: ""any value"", 10: 3.14, 100: null }
@export var typed_value: Dictionary [ Variant , int ] = { ""any value"": 0, 123: 456, null: -1 }";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);

            var types = @class.AllNodes.OfType<GDDictionaryTypeNode>();

            types.Select(x => x.ToString()).Should().BeEquivalentTo(new[] {
                "Dictionary[int, String ]",
                "Dictionary[ int, Variant]",
                "Dictionary [ Variant , int ]"
            });

            AssertHelper.CompareCodeStrings(code, @class.ToString());
            AssertHelper.NoInvalidTokens(@class);
        }

        [TestMethod]
        public void ParseType_ScriptSubtype()
        {
            var reader = new GDScriptReader();

            var code = @"func f():
	var d = MyScript.SubType.new()";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }
    }
}
