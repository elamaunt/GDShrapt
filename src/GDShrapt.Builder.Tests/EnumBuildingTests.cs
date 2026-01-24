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

        [TestMethod]
        public void BuildEnum_WithExplicitValues()
        {
            var enumDecl = GD.Declaration.Enum("Priority",
                GD.Declaration.EnumValue("LOW", GD.Expression.Number(1)),
                GD.Declaration.EnumValue("MEDIUM", GD.Expression.Number(5)),
                GD.Declaration.EnumValue("HIGH", GD.Expression.Number(10))
            );

            var code = enumDecl.ToString();
            Assert.IsTrue(code.Contains("enum"));
            Assert.IsTrue(code.Contains("Priority"));
            Assert.IsTrue(code.Contains("LOW"));
            Assert.IsTrue(code.Contains("1"));
            Assert.IsTrue(code.Contains("MEDIUM"));
            Assert.IsTrue(code.Contains("5"));
            Assert.IsTrue(code.Contains("HIGH"));
            Assert.IsTrue(code.Contains("10"));
            AssertHelper.NoInvalidTokens(enumDecl);
        }

        [TestMethod]
        public void BuildEnum_WithMixedValues()
        {
            var enumDecl = GD.Declaration.Enum("MixedEnum",
                GD.Declaration.EnumValue("FIRST"),
                GD.Declaration.EnumValue("SECOND", GD.Expression.Number(100)),
                GD.Declaration.EnumValue("THIRD")
            );

            var code = enumDecl.ToString();
            Assert.IsTrue(code.Contains("enum"));
            Assert.IsTrue(code.Contains("MixedEnum"));
            Assert.IsTrue(code.Contains("FIRST"));
            Assert.IsTrue(code.Contains("SECOND"));
            Assert.IsTrue(code.Contains("100"));
            Assert.IsTrue(code.Contains("THIRD"));
            AssertHelper.NoInvalidTokens(enumDecl);
        }

        [TestMethod]
        public void BuildEnum_SingleValue()
        {
            var enumDecl = GD.Declaration.Enum("SingleValueEnum",
                GD.Declaration.EnumValue("ONLY_VALUE")
            );

            var code = enumDecl.ToString();
            Assert.IsTrue(code.Contains("enum"));
            Assert.IsTrue(code.Contains("SingleValueEnum"));
            Assert.IsTrue(code.Contains("ONLY_VALUE"));
            AssertHelper.NoInvalidTokens(enumDecl);
        }

        [TestMethod]
        public void BuildEnum_WithBinaryValues()
        {
            var enumDecl = GD.Declaration.Enum("Flags",
                GD.Declaration.EnumValue("FLAG_A", GD.Expression.DualOperator(
                    GD.Expression.Number(1),
                    GD.Syntax.DualOperator(GDDualOperatorType.BitShiftLeft),
                    GD.Expression.Number(0)
                )),
                GD.Declaration.EnumValue("FLAG_B", GD.Expression.DualOperator(
                    GD.Expression.Number(1),
                    GD.Syntax.DualOperator(GDDualOperatorType.BitShiftLeft),
                    GD.Expression.Number(1)
                )),
                GD.Declaration.EnumValue("FLAG_C", GD.Expression.DualOperator(
                    GD.Expression.Number(1),
                    GD.Syntax.DualOperator(GDDualOperatorType.BitShiftLeft),
                    GD.Expression.Number(2)
                ))
            );

            var code = enumDecl.ToString();
            Assert.IsTrue(code.Contains("enum"));
            Assert.IsTrue(code.Contains("Flags"));
            Assert.IsTrue(code.Contains("FLAG_A"));
            Assert.IsTrue(code.Contains("FLAG_B"));
            Assert.IsTrue(code.Contains("FLAG_C"));
            Assert.IsTrue(code.Contains("<<"));
            AssertHelper.NoInvalidTokens(enumDecl);
        }

        [TestMethod]
        public void BuildEnum_Empty()
        {
            var enumDecl = GD.Declaration.Enum("EmptyEnum");

            var code = enumDecl.ToString();
            Assert.IsTrue(code.Contains("enum"));
            Assert.IsTrue(code.Contains("EmptyEnum"));
            AssertHelper.NoInvalidTokens(enumDecl);
        }
    }
}
