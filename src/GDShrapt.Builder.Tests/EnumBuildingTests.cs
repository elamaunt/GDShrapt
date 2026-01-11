using GDShrapt.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Building
{
    /// <summary>
    /// Tests for building enum declarations programmatically.
    /// </summary>
    [TestClass]
    public class EnumBuildingTests
    {
        [TestMethod]
        public void BuildEnum_WithMultipleValues()
        {
            var enumDecl = GD.Declaration.Enum("State",
                GD.Declaration.EnumValue("IDLE"),
                GD.Declaration.EnumValue("RUNNING"),
                GD.Declaration.EnumValue("JUMPING", GD.Expression.Number(10))
            );

            var code = enumDecl.ToString();
            Assert.IsTrue(code.Contains("enum"));
            Assert.IsTrue(code.Contains("State"));
            Assert.IsTrue(code.Contains("IDLE"));
            Assert.IsTrue(code.Contains("RUNNING"));
            Assert.IsTrue(code.Contains("JUMPING"));
        }
    }
}
