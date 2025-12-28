using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Parsing
{
    /// <summary>
    /// Tests for parsing signal declarations.
    /// </summary>
    [TestClass]
    public class SignalParsingTests
    {
        [TestMethod]
        public void ParseSignal_WithParameters()
        {
            var reader = new GDScriptReader();

            var code = @"signal my_signal(value, other_value)";

            var classDeclaration = reader.ParseFileContent(code);

            Assert.IsNotNull(classDeclaration);
            Assert.AreEqual(1, classDeclaration.Members.Count);
            Assert.IsInstanceOfType(classDeclaration.Members[0], typeof(GDSignalDeclaration));

            var signalDeclaration = (GDSignalDeclaration)classDeclaration.Members[0];

            Assert.IsNotNull(signalDeclaration.Identifier);
            Assert.AreEqual("my_signal", signalDeclaration.Identifier.Sequence);

            Assert.AreEqual(2, signalDeclaration.Parameters.Count);
            Assert.AreEqual("value", signalDeclaration.Parameters[0].Identifier?.Sequence);
            Assert.AreEqual("other_value", signalDeclaration.Parameters[1].Identifier?.Sequence);

            AssertHelper.CompareCodeStrings(code, classDeclaration.ToString());
            AssertHelper.NoInvalidTokens(classDeclaration);
        }
    }
}
