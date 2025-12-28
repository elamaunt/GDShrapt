using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Parsing
{
    /// <summary>
    /// Tests for parsing enum declarations.
    /// </summary>
    [TestClass]
    public class EnumParsingTests
    {
        [TestMethod]
        public void ParseEnum_WithNameAndValues()
        {
            var reader = new GDScriptReader();

            var code = @"enum test { a : 1, b, c : 3}";

            var classDeclaration = reader.ParseFileContent(code);

            Assert.IsNotNull(classDeclaration);
            Assert.AreEqual(1, classDeclaration.Members.Count);
            Assert.IsInstanceOfType(classDeclaration.Members[0], typeof(GDEnumDeclaration));

            var enumDeclaration = (GDEnumDeclaration)classDeclaration.Members[0];

            Assert.AreEqual("test", enumDeclaration.Identifier.Sequence);
            Assert.AreEqual(3, enumDeclaration.Values.Count);

            Assert.AreEqual("a", enumDeclaration.Values[0].Identifier?.ToString());
            Assert.AreEqual("b", enumDeclaration.Values[1].Identifier?.ToString());
            Assert.AreEqual("c", enumDeclaration.Values[2].Identifier?.ToString());

            Assert.AreEqual("1", enumDeclaration.Values[0].Value?.ToString());
            Assert.IsNull(enumDeclaration.Values[1].Value);
            Assert.AreEqual("3", enumDeclaration.Values[2].Value?.ToString());

            AssertHelper.CompareCodeStrings(code, classDeclaration.ToString());
            AssertHelper.NoInvalidTokens(classDeclaration);
        }

        [TestMethod]
        public void ParseEnum_Anonymous()
        {
            var reader = new GDScriptReader();

            var code = @"enum {a,b,c = 10}";

            var classDeclaration = reader.ParseFileContent(code);

            Assert.IsNotNull(classDeclaration);
            Assert.AreEqual(1, classDeclaration.Members.Count);
            Assert.IsInstanceOfType(classDeclaration.Members[0], typeof(GDEnumDeclaration));

            var enumDeclaration = (GDEnumDeclaration)classDeclaration.Members[0];

            Assert.IsNull(enumDeclaration.Identifier);
            Assert.AreEqual(3, enumDeclaration.Values.Count);

            Assert.AreEqual("a", enumDeclaration.Values[0].Identifier?.ToString());
            Assert.AreEqual("b", enumDeclaration.Values[1].Identifier?.ToString());
            Assert.AreEqual("c", enumDeclaration.Values[2].Identifier?.ToString());

            Assert.IsNull(enumDeclaration.Values[0].Value);
            Assert.IsNull(enumDeclaration.Values[1].Value);
            Assert.IsNotNull(enumDeclaration.Values[2].Value);

            AssertHelper.CompareCodeStrings(code, classDeclaration.ToString());
            AssertHelper.NoInvalidTokens(classDeclaration);
        }

        [TestMethod]
        public void ParseEnum_WithMultilineFormat()
        {
            var reader = new GDScriptReader();

            var code = @"enum EnumName
{
a
,
b,
c
=
10
}";

            var classDeclaration = reader.ParseFileContent(code);

            Assert.IsNotNull(classDeclaration);
            Assert.AreEqual(1, classDeclaration.Members.Count);
            Assert.IsInstanceOfType(classDeclaration.Members[0], typeof(GDEnumDeclaration));

            var enumDeclaration = (GDEnumDeclaration)classDeclaration.Members[0];

            Assert.AreEqual("EnumName", enumDeclaration.Identifier.Sequence);
            Assert.AreEqual(3, enumDeclaration.Values.Count);

            Assert.AreEqual("a", enumDeclaration.Values[0].Identifier?.ToString());
            Assert.AreEqual("b", enumDeclaration.Values[1].Identifier?.ToString());
            Assert.AreEqual("c", enumDeclaration.Values[2].Identifier?.ToString());

            Assert.IsNull(enumDeclaration.Values[0].Value);
            Assert.IsNull(enumDeclaration.Values[1].Value);
            Assert.IsNotNull(enumDeclaration.Values[2].Value);

            AssertHelper.CompareCodeStrings(code, classDeclaration.ToString());
            AssertHelper.NoInvalidTokens(classDeclaration);
        }

        [TestMethod]
        public void ParseEnum_WithBitwiseValue()
        {
            var reader = new GDScriptReader();

            var code = @"enum State
{
	STATE_IDLE
	,
	STATE_JUMP
	=
	1
	<<
	3
	,
	STATE_SHOOT
}";

            var classDeclaration = reader.ParseFileContent(code);

            Assert.IsNotNull(classDeclaration);
            Assert.AreEqual(1, classDeclaration.Members.Count);
            Assert.IsInstanceOfType(classDeclaration.Members[0], typeof(GDEnumDeclaration));

            var enumDeclaration = (GDEnumDeclaration)classDeclaration.Members[0];

            Assert.AreEqual("State", enumDeclaration.Identifier.Sequence);
            Assert.AreEqual(3, enumDeclaration.Values.Count);

            AssertHelper.CompareCodeStrings(code, classDeclaration.ToString());
            AssertHelper.NoInvalidTokens(classDeclaration);
        }
    }
}
