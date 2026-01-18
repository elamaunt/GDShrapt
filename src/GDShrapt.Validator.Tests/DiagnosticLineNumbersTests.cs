using FluentAssertions;
using GDShrapt.Reader;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests that verify diagnostic output including line numbers.
    /// Simulates what the Plugin does for validation.
    /// </summary>
    [TestClass]
    public class DiagnosticLineNumbersTests
    {
        [TestMethod]
        public void DiagnosticLineNumbers_AreZeroBased()
        {
            // Parse a simple code and verify line numbers
            // Note: GDShrapt uses 0-based line numbers internally
            var code = @"extends Node

func test():
    var undefined_var = nonexistent_function()
";
            var reader = new GDScriptReader();
            var classDecl = reader.ParseFileContent(code);

            var options = new GDValidationOptions
            {
                CheckScope = true,
                CheckCalls = true
            };

            var diagnosticsService = new GDDiagnosticsService(options);
            var result = diagnosticsService.Diagnose(classDecl);

            Console.WriteLine("Diagnostics:");
            foreach (var diag in result.Diagnostics)
            {
                Console.WriteLine($"  [{diag.Code}] Line {diag.StartLine}:{diag.StartColumn} - {diag.Message}");
            }

            // The undefined function call is on line 3 (0-based, which is line 4 in editor)
            var undefinedError = result.Diagnostics
                .FirstOrDefault(d => d.Message.Contains("nonexistent_function"));

            if (undefinedError != null)
            {
                // Line 3 is the 4th line (0-indexed: 0=extends, 1=empty, 2=func, 3=var)
                undefinedError.StartLine.Should().Be(3,
                    "nonexistent_function is on line 3 (0-based)");
                undefinedError.StartColumn.Should().BeGreaterThanOrEqualTo(0,
                    "Column should be non-negative");
            }

            // All line numbers should be non-negative (0-based)
            foreach (var diag in result.Diagnostics)
            {
                diag.StartLine.Should().BeGreaterThanOrEqualTo(0,
                    $"Line numbers should be non-negative for {diag.Code}: {diag.Message}");
            }
        }

        [TestMethod]
        public void KeySpace_WithRuntimeProvider_NoUndefinedError()
        {
            // This tests the KEY_SPACE global enum constant fix
            var code = @"extends Node

func test():
    var space_key: Key = KEY_SPACE
    var enter_key: Key = KEY_ENTER
    print(space_key, enter_key)
";
            var reader = new GDScriptReader();
            var classDecl = reader.ParseFileContent(code);

            var provider = new GDGodotTypesProvider();
            var options = new GDValidationOptions
            {
                RuntimeProvider = provider,
                CheckScope = true
            };

            var diagnosticsService = new GDDiagnosticsService(options);
            var result = diagnosticsService.Diagnose(classDecl);

            Console.WriteLine($"Diagnostics ({result.Diagnostics.Count} total):");
            foreach (var diag in result.Diagnostics.OrderBy(d => d.StartLine))
            {
                Console.WriteLine($"  [{diag.Code}] Line {diag.StartLine}: {diag.Message}");
            }

            // KEY_SPACE and KEY_ENTER should NOT be reported as undefined variables
            var keySpaceErrors = result.Diagnostics
                .Where(d => d.Message.Contains("KEY_SPACE") && d.Code.Contains("GD5"))
                .ToList();

            keySpaceErrors.Should().BeEmpty(
                "KEY_SPACE should be recognized as a global enum constant");

            var keyEnterErrors = result.Diagnostics
                .Where(d => d.Message.Contains("KEY_ENTER") && d.Code.Contains("GD5"))
                .ToList();

            keyEnterErrors.Should().BeEmpty(
                "KEY_ENTER should be recognized as a global enum constant");
        }

        [TestMethod]
        public void PackedArrayTypes_WithRuntimeProvider_NoUndefinedError()
        {
            // This tests the PackedInt32Array recognition fix
            var code = @"extends Node

func test_packed_array_inference():
    var packed_ints := PackedInt32Array([1, 2, 3])
    var packed_floats := PackedFloat32Array([1.0, 2.0, 3.0])
    var packed_strings := PackedStringArray([""a"", ""b"", ""c""])
    print(packed_ints, packed_floats, packed_strings)
";
            var reader = new GDScriptReader();
            var classDecl = reader.ParseFileContent(code);

            var provider = new GDGodotTypesProvider();
            var options = new GDValidationOptions
            {
                RuntimeProvider = provider,
                CheckScope = true,
                CheckCalls = true
            };

            var diagnosticsService = new GDDiagnosticsService(options);
            var result = diagnosticsService.Diagnose(classDecl);

            Console.WriteLine($"Diagnostics ({result.Diagnostics.Count} total):");
            foreach (var diag in result.Diagnostics.OrderBy(d => d.StartLine))
            {
                Console.WriteLine($"  [{diag.Code}] Line {diag.StartLine}: {diag.Message}");
            }

            // PackedInt32Array, PackedFloat32Array, etc. should NOT be undefined
            var packedArrayErrors = result.Diagnostics
                .Where(d => d.Message.Contains("Packed") && d.Message.Contains("Array") &&
                           d.Code.Contains("GD5"))
                .ToList();

            packedArrayErrors.Should().BeEmpty(
                "PackedArray types should be recognized");
        }

        [TestMethod]
        public void RandiReturnType_NoUInt32Warning()
        {
            // This tests the randi() return type fix (should be int, not UInt32)
            var code = @"extends Node

func test_conditional_inference():
    var number := randi()
    var clamped := 100 if number > 100 else number
    print(clamped)

func test_match_inference():
    var value := randi() % 3
    match value:
        0:
            print(""zero"")
        1:
            print(""one"")
        _:
            print(""other"")
";
            var reader = new GDScriptReader();
            var classDecl = reader.ParseFileContent(code);

            var provider = new GDGodotTypesProvider();
            var options = new GDValidationOptions
            {
                RuntimeProvider = provider,
                CheckScope = true,
                CheckTypes = true
            };

            var diagnosticsService = new GDDiagnosticsService(options);
            var result = diagnosticsService.Diagnose(classDecl);

            Console.WriteLine($"Diagnostics ({result.Diagnostics.Count} total):");
            foreach (var diag in result.Diagnostics.OrderBy(d => d.StartLine))
            {
                Console.WriteLine($"  [{diag.Code}] Line {diag.StartLine}: {diag.Message}");
            }

            // randi() % 3 should not produce type mismatch warnings about UInt32
            var uint32Warnings = result.Diagnostics
                .Where(d => d.Message.Contains("UInt32"))
                .ToList();

            uint32Warnings.Should().BeEmpty(
                "randi() should return int, not UInt32");
        }

        [TestMethod]
        public void StrFunction_VarArgs_NoArgCountError()
        {
            // This tests the str() varargs fix
            var code = @"extends Node

func test():
    var s1 = str()
    var s2 = str(42)
    var s3 = str(""Value: "", 42, "" items"")
    print(s1, s2, s3)
";
            var reader = new GDScriptReader();
            var classDecl = reader.ParseFileContent(code);

            var provider = new GDGodotTypesProvider();
            var options = new GDValidationOptions
            {
                RuntimeProvider = provider,
                CheckScope = true,
                CheckCalls = true
            };

            var diagnosticsService = new GDDiagnosticsService(options);
            var result = diagnosticsService.Diagnose(classDecl);

            Console.WriteLine($"Diagnostics ({result.Diagnostics.Count} total):");
            foreach (var diag in result.Diagnostics.OrderBy(d => d.StartLine))
            {
                Console.WriteLine($"  [{diag.Code}] Line {diag.StartLine}: {diag.Message}");
            }

            // str() should accept multiple arguments (varargs)
            var strArgCountErrors = result.Diagnostics
                .Where(d => d.Message.Contains("'str'") && d.Message.Contains("argument"))
                .ToList();

            strArgCountErrors.Should().BeEmpty(
                "str() should accept any number of arguments (varargs)");
        }

        [TestMethod]
        public void MinFunction_NoUndefinedError()
        {
            // This tests the min() function - it should return float for min(float, float)
            var code = @"extends Node

func test():
    var a = 5
    var b = 10
    var result = min(a, b)
    print(result)
";
            var reader = new GDScriptReader();
            var classDecl = reader.ParseFileContent(code);

            var provider = new GDGodotTypesProvider();
            var options = new GDValidationOptions
            {
                RuntimeProvider = provider,
                CheckScope = true,
                CheckCalls = true
            };

            var diagnosticsService = new GDDiagnosticsService(options);
            var result = diagnosticsService.Diagnose(classDecl);

            Console.WriteLine($"Diagnostics ({result.Diagnostics.Count} total):");
            foreach (var diag in result.Diagnostics.OrderBy(d => d.StartLine))
            {
                Console.WriteLine($"  [{diag.Code}] Line {diag.StartLine}: {diag.Message}");
            }

            // min() should be recognized as a valid function
            var minFunctionErrors = result.Diagnostics
                .Where(d => d.Message.Contains("'min'") && d.Code.Contains("GD5"))
                .ToList();

            minFunctionErrors.Should().BeEmpty(
                "min() should be recognized as a global function");
        }

        [TestMethod]
        public void ComplexScript_DiagnosticReport()
        {
            // Comprehensive test that simulates plugin diagnostics on a complex script
            var code = @"extends Node2D
class_name CompletionTest

enum TestEnum { VALUE_ONE, VALUE_TWO, VALUE_THREE }

const MY_CONSTANT := 42
const STRING_CONSTANT := ""test""

var my_string: String = """"
var my_int: int = 0
var my_vector: Vector2 = Vector2.ZERO
var my_enum: TestEnum = TestEnum.VALUE_ONE

@onready var child_sprite: Sprite2D = $ChildSprite

signal custom_signal(value: int, name: String)

func _ready() -> void:
    pass

func test_enum_completion() -> void:
    my_enum = TestEnum.VALUE_ONE
    my_enum = TestEnum.VALUE_TWO

    var space_key: Key = KEY_SPACE
    var enter_key: Key = KEY_ENTER
    print(space_key, enter_key)

func test_packed_arrays() -> void:
    var packed_ints := PackedInt32Array([1, 2, 3])
    var packed_floats := PackedFloat32Array([1.0, 2.0, 3.0])
    print(packed_ints, packed_floats)

func test_randi() -> void:
    var value := randi() % 3
    match value:
        0:
            print(""zero"")
        _:
            print(""other"")

func test_str() -> void:
    var s = str(""Value: "", MY_CONSTANT, "" items"")
    print(s)

func test_min() -> void:
    var result = min(my_int, MY_CONSTANT)
    print(result)
";
            var reader = new GDScriptReader();
            var classDecl = reader.ParseFileContent(code);

            var provider = new GDGodotTypesProvider();
            var config = new GDProjectConfig();
            config.Linting.Enabled = true;

            var options = new GDValidationOptions
            {
                RuntimeProvider = provider,
                CheckScope = true,
                CheckCalls = true,
                CheckTypes = true
            };

            var diagnosticsService = new GDDiagnosticsService(options);
            var result = diagnosticsService.Diagnose(classDecl);

            Console.WriteLine("=== Diagnostic Report for Complex Script ===\n");
            Console.WriteLine($"Total: {result.Diagnostics.Count} diagnostics");
            Console.WriteLine($"  Errors: {result.ErrorCount}");
            Console.WriteLine($"  Warnings: {result.WarningCount}");
            Console.WriteLine($"  Hints: {result.HintCount}");
            Console.WriteLine();

            if (result.Diagnostics.Any())
            {
                Console.WriteLine("Details:");
                foreach (var diag in result.Diagnostics.OrderBy(d => d.StartLine).ThenBy(d => d.StartColumn))
                {
                    var icon = diag.Severity switch
                    {
                        GDUnifiedDiagnosticSeverity.Error => "ERROR",
                        GDUnifiedDiagnosticSeverity.Warning => "WARN",
                        GDUnifiedDiagnosticSeverity.Hint => "HINT",
                        _ => "INFO"
                    };

                    Console.WriteLine($"  [{icon}] {diag.Code} at {diag.StartLine}:{diag.StartColumn}-{diag.EndLine}:{diag.EndColumn}");
                    Console.WriteLine($"         {diag.Message}");
                }
            }
            else
            {
                Console.WriteLine("No diagnostics - all checks passed!");
            }

            // Verify key fixes are working
            var criticalErrors = result.Diagnostics
                .Where(d => d.Severity == GDUnifiedDiagnosticSeverity.Error &&
                           (d.Message.Contains("KEY_SPACE") ||
                            d.Message.Contains("KEY_ENTER") ||
                            d.Message.Contains("PackedInt32Array") ||
                            d.Message.Contains("PackedFloat32Array") ||
                            d.Message.Contains("UInt32") ||
                            (d.Message.Contains("'str'") && d.Message.Contains("argument")) ||
                            (d.Message.Contains("'min'") && d.Code.Contains("GD5"))))
                .ToList();

            criticalErrors.Should().BeEmpty(
                "All 6 diagnostic fixes should be working");
        }
    }
}
