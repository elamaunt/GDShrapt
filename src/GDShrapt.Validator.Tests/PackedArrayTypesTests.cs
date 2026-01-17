using FluentAssertions;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for PackedArray types recognition (PackedInt32Array, PackedFloat32Array, etc.).
    /// </summary>
    [TestClass]
    public class PackedArrayTypesTests
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
        public void PackedInt32Array_IsKnownType()
        {
            _godotProvider.IsKnownType("PackedInt32Array")
                .Should().BeTrue("PackedInt32Array should be a known type");
        }

        [TestMethod]
        public void PackedFloat32Array_IsKnownType()
        {
            _godotProvider.IsKnownType("PackedFloat32Array")
                .Should().BeTrue("PackedFloat32Array should be a known type");
        }

        [TestMethod]
        public void PackedStringArray_IsKnownType()
        {
            _godotProvider.IsKnownType("PackedStringArray")
                .Should().BeTrue("PackedStringArray should be a known type");
        }

        [TestMethod]
        public void PackedByteArray_IsKnownType()
        {
            _godotProvider.IsKnownType("PackedByteArray")
                .Should().BeTrue("PackedByteArray should be a known type");
        }

        [TestMethod]
        public void PackedVector2Array_IsKnownType()
        {
            _godotProvider.IsKnownType("PackedVector2Array")
                .Should().BeTrue("PackedVector2Array should be a known type");
        }

        [TestMethod]
        public void PackedVector3Array_IsKnownType()
        {
            _godotProvider.IsKnownType("PackedVector3Array")
                .Should().BeTrue("PackedVector3Array should be a known type");
        }

        [TestMethod]
        public void PackedColorArray_IsKnownType()
        {
            _godotProvider.IsKnownType("PackedColorArray")
                .Should().BeTrue("PackedColorArray should be a known type");
        }

        [TestMethod]
        public void PackedInt64Array_IsKnownType()
        {
            _godotProvider.IsKnownType("PackedInt64Array")
                .Should().BeTrue("PackedInt64Array should be a known type");
        }

        [TestMethod]
        public void PackedFloat64Array_IsKnownType()
        {
            _godotProvider.IsKnownType("PackedFloat64Array")
                .Should().BeTrue("PackedFloat64Array should be a known type");
        }

        [TestMethod]
        public void PackedInt32Array_Constructor_NoError()
        {
            var code = @"
func test():
    var packed_ints := PackedInt32Array([1, 2, 3])
    print(packed_ints)
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckScope = true,
                CheckCalls = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Errors
                .Where(e => e.Code == GDDiagnosticCode.UndefinedVariable &&
                           e.Message.Contains("PackedInt32Array"))
                .Should().BeEmpty("PackedInt32Array should be recognized as a type");
        }

        [TestMethod]
        public void PackedFloat32Array_Constructor_NoError()
        {
            var code = @"
func test():
    var packed_floats := PackedFloat32Array([1.0, 2.0, 3.0])
    print(packed_floats)
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckScope = true,
                CheckCalls = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Errors
                .Where(e => e.Code == GDDiagnosticCode.UndefinedVariable &&
                           e.Message.Contains("PackedFloat32Array"))
                .Should().BeEmpty("PackedFloat32Array should be recognized as a type");
        }

        [TestMethod]
        public void PackedStringArray_Constructor_NoError()
        {
            var code = @"
func test():
    var packed_strings := PackedStringArray([""a"", ""b"", ""c""])
    print(packed_strings)
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckScope = true,
                CheckCalls = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Errors
                .Where(e => e.Code == GDDiagnosticCode.UndefinedVariable &&
                           e.Message.Contains("PackedStringArray"))
                .Should().BeEmpty("PackedStringArray should be recognized as a type");
        }

        [TestMethod]
        public void PackedInt32Array_TypeAnnotation_NoError()
        {
            var code = @"
func test():
    var packed: PackedInt32Array = PackedInt32Array()
    print(packed)
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckScope = true,
                CheckTypes = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Errors
                .Where(e => e.Message.Contains("PackedInt32Array"))
                .Should().BeEmpty("PackedInt32Array should be recognized in type annotations");
        }

        [TestMethod]
        public void AllPackedArrayTypes_Constructor_NoError()
        {
            var code = @"
func test():
    var bytes := PackedByteArray()
    var ints32 := PackedInt32Array()
    var ints64 := PackedInt64Array()
    var floats32 := PackedFloat32Array()
    var floats64 := PackedFloat64Array()
    var strings := PackedStringArray()
    var vec2s := PackedVector2Array()
    var vec3s := PackedVector3Array()
    var colors := PackedColorArray()
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckScope = true,
                CheckCalls = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Errors
                .Where(e => e.Code == GDDiagnosticCode.UndefinedVariable &&
                           e.Message.Contains("Packed"))
                .Should().BeEmpty("All PackedArray types should be recognized");
        }
    }
}
