using FluentAssertions;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for randi() return type - should return "int", not "UInt32".
    /// This ensures proper type mapping from C# uint to GDScript int.
    /// </summary>
    [TestClass]
    public class RandiTypeTests
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
        public void Randi_ReturnsInt_NotUInt32()
        {
            var funcInfo = _godotProvider.GetGlobalFunction("randi");

            funcInfo.Should().NotBeNull("randi function should exist");
            funcInfo.ReturnType.Should().Be("int", "randi() should return 'int', not 'UInt32'");
        }

        [TestMethod]
        public void Randi_ModuloInt_NoTypeWarning()
        {
            var code = @"
func test():
    var value: int = randi() % 3
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckTypes = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(w => w.Message.Contains("UInt32") || w.Message.Contains("type mismatch"))
                .Should().BeEmpty("randi() % 3 should not produce type warnings because randi() returns int");
        }

        [TestMethod]
        public void Randi_AssignToInt_NoWarning()
        {
            var code = @"
func test():
    var x: int = randi()
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckTypes = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(w => w.Message.Contains("Cannot assign") && w.Message.Contains("randi"))
                .Should().BeEmpty("randi() returns int, so assigning to int should not produce warnings");
        }

        [TestMethod]
        public void Randi_InExpression_NoWarning()
        {
            var code = @"
func test():
    var options: Array = [""a"", ""b"", ""c""]
    var index: int = randi() % options.size()
    print(options[index])
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckTypes = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(w => w.Message.Contains("UInt32"))
                .Should().BeEmpty("randi() should work in expressions without UInt32 warnings");
        }

        [TestMethod]
        public void Randf_ReturnsFloat()
        {
            var funcInfo = _godotProvider.GetGlobalFunction("randf");

            funcInfo.Should().NotBeNull("randf function should exist");
            funcInfo.ReturnType.Should().Be("float", "randf() should return 'float'");
        }

        [TestMethod]
        public void RandiRange_ReturnsInt()
        {
            var funcInfo = _godotProvider.GetGlobalFunction("randi_range");

            funcInfo.Should().NotBeNull("randi_range function should exist");
            funcInfo.ReturnType.Should().Be("int", "randi_range() should return 'int'");
        }
    }
}
