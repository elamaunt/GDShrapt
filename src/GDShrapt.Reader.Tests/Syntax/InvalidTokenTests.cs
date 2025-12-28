using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Syntax
{
    /// <summary>
    /// Tests for invalid token detection.
    /// </summary>
    [TestClass]
    public class InvalidTokenTests
    {
        #region Static Keyword Tests

        [TestMethod]
        public void InvalidStaticOnSignal()
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
        public void DoubleStatic()
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
        public void InvalidStaticOnEnum()
        {
            var reader = new GDScriptReader();

            var code = @"static enum MyEnum { A, B, C }";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.AllInvalidTokens.Count());
            Assert.AreEqual("static", declaration.AllInvalidTokens.First().Sequence);
            AssertHelper.CompareCodeStrings(code, declaration.ToString());
        }

        [TestMethod]
        public void InvalidStaticOnClass()
        {
            var reader = new GDScriptReader();

            var code = @"static class InnerClass:
    pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            // Parser creates 2 invalid tokens for "static" and "class"
            declaration.AllInvalidTokens.Should().NotBeEmpty();
            declaration.AllInvalidTokens.First().Sequence.Should().Be("static");
            AssertHelper.CompareCodeStrings(code, declaration.ToString());
        }

        #endregion

        #region Class Declaration Tests

        [TestMethod]
        public void InvalidClassName()
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

        [TestMethod]
        public void InvalidClassNameStartsWithNumber()
        {
            var reader = new GDScriptReader();

            var code = @"class_name 123MyClass";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
            @class.AllInvalidTokens.First().Sequence.Should().Be("123");
        }

        [TestMethod]
        public void InvalidExtendsPath()
        {
            var reader = new GDScriptReader();

            var code = @"extends !!!InvalidPath";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        #endregion

        #region Variable Declaration Tests

        [TestMethod]
        public void InvalidVariableName()
        {
            var reader = new GDScriptReader();

            var code = @"var 123invalid = 5";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        [TestMethod]
        public void InvalidConstName()
        {
            var reader = new GDScriptReader();

            var code = @"const 456CONST = 10";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        [TestMethod]
        public void InvalidVariableTypeAnnotation()
        {
            var reader = new GDScriptReader();

            var code = @"var my_var: 123Type = null";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        #endregion

        #region Function Declaration Tests

        [TestMethod]
        public void InvalidFunctionName()
        {
            var reader = new GDScriptReader();

            var code = @"func 123func():
    pass";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        [TestMethod]
        public void InvalidParameterName()
        {
            var reader = new GDScriptReader();

            var code = @"func my_func(123param):
    pass";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        [TestMethod]
        public void InvalidReturnType()
        {
            var reader = new GDScriptReader();

            var code = @"func my_func() -> 123InvalidType:
    pass";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        #endregion

        #region Signal Declaration Tests

        [TestMethod]
        public void InvalidSignalName()
        {
            var reader = new GDScriptReader();

            var code = @"signal 123signal";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        [TestMethod]
        public void InvalidSignalParameterName()
        {
            var reader = new GDScriptReader();

            var code = @"signal my_signal(123param)";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        #endregion

        #region Enum Declaration Tests

        [TestMethod]
        public void InvalidEnumName()
        {
            var reader = new GDScriptReader();

            var code = @"enum 123Enum { A, B }";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        [TestMethod]
        public void InvalidEnumValueName()
        {
            var reader = new GDScriptReader();

            var code = @"enum MyEnum { 123A, B }";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        #endregion

        #region Expression Tests

        [TestMethod]
        public void InvalidCharInExpression()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    var x = 5 § 3";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        [TestMethod]
        public void InvalidOperatorSequence()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    var x = 5 +++ 3";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            // Multiple + might be parsed differently, check for any issues
            AssertHelper.CompareCodeStrings(code, @class.ToString());
        }

        [TestMethod]
        public void UnmatchedParenthesis()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    var x = (5 + 3";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            // Parser should handle unmatched parenthesis
            AssertHelper.CompareCodeStrings(code, @class.ToString());
        }

        [TestMethod]
        public void UnmatchedBracket()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    var arr = [1, 2, 3";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            AssertHelper.CompareCodeStrings(code, @class.ToString());
        }

        [TestMethod]
        public void UnmatchedBrace()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    var dict = {""a"": 1";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            AssertHelper.CompareCodeStrings(code, @class.ToString());
        }

        #endregion

        #region String Tests

        [TestMethod]
        public void UnterminatedString()
        {
            var reader = new GDScriptReader();

            var code = @"var x = ""unterminated string";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            // Parser should preserve the content
            AssertHelper.CompareCodeStrings(code, @class.ToString());
        }

        [TestMethod]
        public void UnterminatedSingleQuoteString()
        {
            var reader = new GDScriptReader();

            var code = @"var x = 'unterminated";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            AssertHelper.CompareCodeStrings(code, @class.ToString());
        }

        #endregion

        #region Number Tests

        [TestMethod]
        public void InvalidNumberFormat()
        {
            var reader = new GDScriptReader();

            var code = @"var x = 123abc";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            // Number followed by letters might create invalid tokens
            AssertHelper.CompareCodeStrings(code, @class.ToString());
        }

        [TestMethod]
        public void InvalidHexNumber()
        {
            var reader = new GDScriptReader();

            var code = @"var x = 0xGHI";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            AssertHelper.CompareCodeStrings(code, @class.ToString());
        }

        [TestMethod]
        public void InvalidBinaryNumber()
        {
            var reader = new GDScriptReader();

            var code = @"var x = 0b123";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            AssertHelper.CompareCodeStrings(code, @class.ToString());
        }

        [TestMethod]
        public void MultipleDecimalPoints()
        {
            var reader = new GDScriptReader();

            var code = @"var x = 1.2.3";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            AssertHelper.CompareCodeStrings(code, @class.ToString());
        }

        #endregion

        #region Statement Tests

        [TestMethod]
        public void InvalidIfCondition()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    if §§§:
        pass";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        [TestMethod]
        public void InvalidForVariable()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    for 123i in range(10):
        pass";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        [TestMethod]
        public void InvalidMatchExpression()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    match §§§:
        1:
            pass";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        #endregion

        #region Annotation Tests

        [TestMethod]
        public void InvalidAnnotationName()
        {
            var reader = new GDScriptReader();

            var code = @"@123invalid
var x = 5";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            // Check that code is preserved
            AssertHelper.CompareCodeStrings(code, @class.ToString());
        }

        [TestMethod]
        public void InvalidAnnotationParameters()
        {
            var reader = new GDScriptReader();

            var code = @"@export_range(§§§)
var x: int = 5";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        #endregion

        #region Type Tests

        [TestMethod]
        public void InvalidGenericType()
        {
            var reader = new GDScriptReader();

            var code = @"var arr: Array[123] = []";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        [TestMethod]
        public void InvalidTypeInCast()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    var x = value as 123Type";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        #endregion

        #region Inner Class Tests

        [TestMethod]
        public void InvalidInnerClassName()
        {
            var reader = new GDScriptReader();

            var code = @"class 123InnerClass:
    pass";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        [TestMethod]
        public void InvalidInnerClassExtends()
        {
            var reader = new GDScriptReader();

            var code = @"class InnerClass extends 123Base:
    pass";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        #endregion

        #region Special Character Tests

        [TestMethod]
        public void InvalidUnicodeCharInCode()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    var π = 3.14";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            // Greek letter π might be invalid as identifier
            AssertHelper.CompareCodeStrings(code, @class.ToString());
        }

        [TestMethod]
        public void InvalidControlCharacter()
        {
            var reader = new GDScriptReader();

            var code = "func test():\n    var x = 5\u0001";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            // Control characters should be handled
        }

        [TestMethod]
        public void RandomSpecialCharacters()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    var x = ©®™";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        #endregion

        #region Mixed Invalid Tokens Tests

        [TestMethod]
        public void MultipleInvalidTokensInOneLine()
        {
            var reader = new GDScriptReader();

            var code = @"var §§§ = ¤¤¤";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            // Parser may combine consecutive invalid chars into one token
            @class.AllInvalidTokens.Should().NotBeEmpty();
            AssertHelper.CompareCodeStrings(code, @class.ToString());
        }

        [TestMethod]
        public void InvalidTokensAcrossMultipleLines()
        {
            var reader = new GDScriptReader();

            var code = @"var x§ = 1
var y¤ = 2
var z© = 3";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        [TestMethod]
        public void InvalidTokenInNestedStructure()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    if true:
        for i in range(10):
            var x§ = i";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        #endregion

        #region Property Declaration Tests

        [TestMethod]
        public void InvalidPropertyGetterName()
        {
            var reader = new GDScriptReader();

            var code = @"var my_prop: int:
    123get:
        return 0";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        [TestMethod]
        public void InvalidPropertySetterName()
        {
            var reader = new GDScriptReader();

            var code = @"var my_prop: int:
    get:
        return _value
    123set(value):
        _value = value";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        #endregion

        #region Lambda Tests

        [TestMethod]
        public void InvalidLambdaParameter()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    var f = func(123x): return 123x * 2";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        #endregion

        #region Dictionary Tests

        [TestMethod]
        public void InvalidDictionaryKey()
        {
            var reader = new GDScriptReader();

            var code = @"var dict = { §§§: 1 }";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        [TestMethod]
        public void InvalidDictionaryValue()
        {
            var reader = new GDScriptReader();

            var code = @"var dict = { ""key"": §§§ }";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        #endregion

        #region Array Tests

        [TestMethod]
        public void InvalidArrayElement()
        {
            var reader = new GDScriptReader();

            var code = @"var arr = [1, §§§, 3]";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        #endregion

        #region Call Expression Tests

        [TestMethod]
        public void InvalidFunctionCallArgument()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    my_func(§§§)";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        [TestMethod]
        public void InvalidMethodChainElement()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    obj.§§§.method()";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        #endregion

        #region Preservation Tests

        [TestMethod]
        public void InvalidTokensPreservedInOutput()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
    var x§ = 5
    var y = x§ + 1";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            // The key is that the code is preserved even with invalid tokens
            AssertHelper.CompareCodeStrings(code, @class.ToString());
        }

        [TestMethod]
        public void ValidCodeWithInvalidTokensStillParses()
        {
            var reader = new GDScriptReader();

            var code = @"extends Node

var valid_var = 10
var §invalid = 20
var another_valid = 30

func valid_method():
    pass";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            // Should have some invalid tokens but still parse
            @class.AllInvalidTokens.Should().NotBeEmpty();
            AssertHelper.CompareCodeStrings(code, @class.ToString());
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void EmptyFile()
        {
            var reader = new GDScriptReader();

            var code = "";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);
            Assert.AreEqual(0, @class.AllInvalidTokens.Count());
        }

        [TestMethod]
        public void OnlyWhitespace()
        {
            var reader = new GDScriptReader();

            var code = "   \n\t\n   ";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);
            Assert.AreEqual(0, @class.AllInvalidTokens.Count());
            AssertHelper.CompareCodeStrings(code, @class.ToString());
        }

        [TestMethod]
        public void OnlyInvalidTokens()
        {
            var reader = new GDScriptReader();

            var code = "§§§ ¤¤¤ ©©©";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        [TestMethod]
        public void VeryLongInvalidToken()
        {
            var reader = new GDScriptReader();

            var invalidChars = new string('§', 1000);
            var code = $"var x = {invalidChars}";

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            @class.AllInvalidTokens.Should().NotBeEmpty();
        }

        #endregion
    }
}
