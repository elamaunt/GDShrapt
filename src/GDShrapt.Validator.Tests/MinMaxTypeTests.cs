using FluentAssertions;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for min/max/mini/maxi function return types.
    /// In GDScript:
    /// - min()/max() return float
    /// - mini()/maxi() return int
    /// - minf()/maxf() return float
    /// </summary>
    [TestClass]
    public class MinMaxTypeTests
    {
        private GDValidator _validator;
        private GDGodotTypesProvider _godotProvider;

        [TestInitialize]
        public void Setup()
        {
            _validator = new GDValidator();
            _godotProvider = new GDGodotTypesProvider();
        }

        [TestMethod]
        public void Min_ReturnsFloat()
        {
            var funcInfo = _godotProvider.GetGlobalFunction("min");

            funcInfo.Should().NotBeNull("min function should exist");
            funcInfo.ReturnType.Should().Be("float", "min() should return float (use mini() for int)");
        }

        [TestMethod]
        public void Mini_ReturnsInt()
        {
            var funcInfo = _godotProvider.GetGlobalFunction("mini");

            funcInfo.Should().NotBeNull("mini function should exist");
            funcInfo.ReturnType.Should().Be("int", "mini() should return int");
        }

        [TestMethod]
        public void Max_ReturnsFloat()
        {
            var funcInfo = _godotProvider.GetGlobalFunction("max");

            funcInfo.Should().NotBeNull("max function should exist");
            funcInfo.ReturnType.Should().Be("float", "max() should return float (use maxi() for int)");
        }

        [TestMethod]
        public void Maxi_ReturnsInt()
        {
            var funcInfo = _godotProvider.GetGlobalFunction("maxi");

            funcInfo.Should().NotBeNull("maxi function should exist");
            funcInfo.ReturnType.Should().Be("int", "maxi() should return int");
        }

        [TestMethod]
        public void Clamp_ReturnsFloat()
        {
            var funcInfo = _godotProvider.GetGlobalFunction("clamp");

            funcInfo.Should().NotBeNull("clamp function should exist");
            funcInfo.ReturnType.Should().Be("float", "clamp() should return float");
        }

        [TestMethod]
        public void Clampi_ReturnsInt()
        {
            var funcInfo = _godotProvider.GetGlobalFunction("clampi");

            funcInfo.Should().NotBeNull("clampi function should exist");
            funcInfo.ReturnType.Should().Be("int", "clampi() should return int");
        }

        [TestMethod]
        public void Abs_ReturnsFloat()
        {
            var funcInfo = _godotProvider.GetGlobalFunction("abs");

            funcInfo.Should().NotBeNull("abs function should exist");
            funcInfo.ReturnType.Should().Be("float", "abs() should return float");
        }

        [TestMethod]
        public void Absi_ReturnsInt()
        {
            var funcInfo = _godotProvider.GetGlobalFunction("absi");

            funcInfo.Should().NotBeNull("absi function should exist");
            funcInfo.ReturnType.Should().Be("int", "absi() should return int");
        }

        [TestMethod]
        public void MiniWithIntArgs_ReturnsInt_NoWarning()
        {
            var code = @"
func test():
    var result: int = mini(3, 5)
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckTypes = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(w => w.Message.Contains("type") && w.Message.Contains("mini"))
                .Should().BeEmpty("mini() returns int, no type warning expected");
        }

        [TestMethod]
        public void MaxiWithIntArgs_ReturnsInt_NoWarning()
        {
            var code = @"
func test():
    var result: int = maxi(3, 5)
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckTypes = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(w => w.Message.Contains("type") && w.Message.Contains("maxi"))
                .Should().BeEmpty("maxi() returns int, no type warning expected");
        }

        [TestMethod]
        public void ClampiWithIntArgs_NoWarning()
        {
            var code = @"
func test():
    var result: int = clampi(10, 0, 5)
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckTypes = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(w => w.Message.Contains("type") && w.Message.Contains("clampi"))
                .Should().BeEmpty("clampi() returns int, no type warning expected");
        }
    }
}
