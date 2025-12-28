using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Parsing
{
    /// <summary>
    /// Tests for parsing property declarations (get/set).
    /// </summary>
    [TestClass]
    public class PropertyParsingTests
    {
        [TestMethod]
        public void ParseProperty_WithGetterAndSetter()
        {
            var reader = new GDScriptReader();

            var code = @"var test:
	get:
		return 1
	set(value):
		pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Variables.Count());
            var variable = declaration.Variables.First();
            Assert.IsNotNull(variable.FirstAccessorDeclarationNode);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseProperty_WithTypeAndAccessors()
        {
            var reader = new GDScriptReader();

            var code = @"var test: int:
	get:
		return 1
	set(value):
		pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Variables.Count());
            var variable = declaration.Variables.First();
            Assert.IsNotNull(variable.FirstAccessorDeclarationNode);
            Assert.IsNotNull(variable.Type);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseProperty_WithTypeInitializerAndAccessors()
        {
            var reader = new GDScriptReader();

            var code = @"var test: int = 1:
	get:
		return 1
	set(value):
		pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Variables.Count());
            var variable = declaration.Variables.First();
            Assert.IsNotNull(variable.FirstAccessorDeclarationNode);
            Assert.IsNotNull(variable.Type);
            Assert.IsNotNull(variable.Initializer);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseProperty_WithInferredTypeAndAccessors()
        {
            var reader = new GDScriptReader();

            var code = @"var test := 1:
	get:
		return 1
	set(value):
		pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Variables.Count());
            var variable = declaration.Variables.First();
            Assert.IsNotNull(variable.FirstAccessorDeclarationNode);
            Assert.IsNotNull(variable.Initializer);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseProperty_WithSetterOnly()
        {
            var reader = new GDScriptReader();

            var code = @"var test:
	set(value):
		pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Variables.Count());
            var variable = declaration.Variables.First();
            Assert.IsNotNull(variable.FirstAccessorDeclarationNode);
            Assert.IsInstanceOfType(variable.FirstAccessorDeclarationNode, typeof(GDSetAccessorBodyDeclaration));

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseProperty_WithGetterOnly()
        {
            var reader = new GDScriptReader();

            var code = @"var test:
	get:
		return 1";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Variables.Count());
            var variable = declaration.Variables.First();
            Assert.IsNotNull(variable.FirstAccessorDeclarationNode);
            Assert.IsInstanceOfType(variable.FirstAccessorDeclarationNode, typeof(GDGetAccessorBodyDeclaration));

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseProperty_WithAccessorMethods()
        {
            var reader = new GDScriptReader();

            var code = @"var test:
	get = get_test
	set = set_test";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Variables.Count());
            var variable = declaration.Variables.First();
            Assert.IsNotNull(variable.FirstAccessorDeclarationNode);

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseProperty_GetWithoutSet()
        {
            var reader = new GDScriptReader();

            var code = @"var test:
	get:
		return 123";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);

            Assert.AreEqual(1, declaration.Variables.Count());

            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }
    }
}
