using FluentAssertions;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests that verify diagnostic output including line numbers.
/// Simulates what the Plugin does for validation.
/// </summary>
[TestClass]
public class PluginDiagnosticRunnerTests
{
    [TestMethod]
    public void RunDiagnostics_OnTestProject_ShowsCorrectLineNumbers()
    {
        // Arrange
        var project = TestProjectFixture.Project;
        project.Should().NotBeNull("Test project should be loaded");

        var config = new GDProjectConfig();
        config.Linting.Enabled = true;

        var diagnosticsService = GDDiagnosticsService.FromConfig(config);

        Console.WriteLine("=== Diagnostic Run on TestProject ===\n");

        var totalErrors = 0;
        var totalWarnings = 0;
        var totalHints = 0;

        // Act - run diagnostics on each script file
        foreach (var script in project.ScriptFiles.OrderBy(s => s.FullPath))
        {
            var result = diagnosticsService.Diagnose(script);

            if (result.Diagnostics.Any())
            {
                var fileName = System.IO.Path.GetFileName(script.FullPath ?? "unknown");
                Console.WriteLine($"📄 {fileName}");
                Console.WriteLine(new string('-', 60));

                foreach (var diag in result.Diagnostics.OrderBy(d => d.StartLine).ThenBy(d => d.StartColumn))
                {
                    var icon = diag.Severity switch
                    {
                        GDUnifiedDiagnosticSeverity.Error => "❌",
                        GDUnifiedDiagnosticSeverity.Warning => "⚠️",
                        GDUnifiedDiagnosticSeverity.Hint => "💡",
                        _ => "ℹ️"
                    };

                    // Line numbers should be 1-based
                    Console.WriteLine($"  {icon} [{diag.Code}] Line {diag.StartLine}:{diag.StartColumn}-{diag.EndLine}:{diag.EndColumn}");
                    Console.WriteLine($"     {diag.Message}");
                }
                Console.WriteLine();

                totalErrors += result.ErrorCount;
                totalWarnings += result.WarningCount;
                totalHints += result.HintCount;
            }
        }

        Console.WriteLine("=== Summary ===");
        Console.WriteLine($"Total Errors: {totalErrors}");
        Console.WriteLine($"Total Warnings: {totalWarnings}");
        Console.WriteLine($"Total Hints: {totalHints}");

        // Assert - line numbers should be positive (1-based)
        foreach (var script in project.ScriptFiles)
        {
            var result = diagnosticsService.Diagnose(script);
            foreach (var diag in result.Diagnostics)
            {
                diag.StartLine.Should().BeGreaterThan(0,
                    $"Line numbers should be 1-based for {diag.Code}: {diag.Message}");
                diag.StartColumn.Should().BeGreaterThanOrEqualTo(0,
                    $"Column should be non-negative for {diag.Code}: {diag.Message}");
            }
        }
    }

    [TestMethod]
    public void RunDiagnostics_OnCompletionTest_KeySpaceShouldNotBeUndefined()
    {
        // This tests the KEY_SPACE global enum constant fix
        var script = TestProjectFixture.GetScript("completion_test.gd");
        script.Should().NotBeNull("completion_test.gd should exist");

        var config = new GDProjectConfig();
        var diagnosticsService = GDDiagnosticsService.FromConfig(config);

        var result = diagnosticsService.Diagnose(script!);

        Console.WriteLine($"Diagnostics for completion_test.gd ({result.Diagnostics.Count} total):");
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
    public void RunDiagnostics_OnTypeInference_PackedArraysShouldBeRecognized()
    {
        // This tests the PackedInt32Array recognition fix
        var script = TestProjectFixture.GetScript("type_inference.gd");
        script.Should().NotBeNull("type_inference.gd should exist");

        var config = new GDProjectConfig();
        var diagnosticsService = GDDiagnosticsService.FromConfig(config);

        var result = diagnosticsService.Diagnose(script!);

        Console.WriteLine($"Diagnostics for type_inference.gd ({result.Diagnostics.Count} total):");
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
    public void RunDiagnostics_OnTypeInference_RandiShouldReturnInt()
    {
        // This tests the randi() return type fix (should be int, not UInt32)
        var script = TestProjectFixture.GetScript("type_inference.gd");
        script.Should().NotBeNull("type_inference.gd should exist");

        var config = new GDProjectConfig();
        var diagnosticsService = GDDiagnosticsService.FromConfig(config);

        var result = diagnosticsService.Diagnose(script!);

        // randi() % 3 should not produce type mismatch warnings about UInt32
        var uint32Warnings = result.Diagnostics
            .Where(d => d.Message.Contains("UInt32"))
            .ToList();

        if (uint32Warnings.Any())
        {
            Console.WriteLine("UInt32 warnings found:");
            foreach (var w in uint32Warnings)
            {
                Console.WriteLine($"  [{w.Code}] Line {w.StartLine}: {w.Message}");
            }
        }

        uint32Warnings.Should().BeEmpty(
            "randi() should return int, not UInt32");
    }

    [TestMethod]
    public void RunDiagnostics_LineNumbersAreOneBased()
    {
        // Parse a simple code and verify line numbers are 1-based
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

        // The undefined function call is on line 4 (1-based)
        var undefinedError = result.Diagnostics
            .FirstOrDefault(d => d.Message.Contains("nonexistent_function"));

        if (undefinedError != null)
        {
            // Line 4 is: var undefined_var = nonexistent_function()
            // (1-indexed: 1=extends, 2=empty, 3=func, 4=var)
            undefinedError.StartLine.Should().Be(4,
                "nonexistent_function is on line 4 (1-based)");
        }
    }

    [TestMethod]
    public void RunDiagnostics_OnBaseEntity_MinFunctionShouldWork()
    {
        // This tests the min() function - it should return float for min(float, float)
        var script = TestProjectFixture.GetScript("base_entity.gd");
        script.Should().NotBeNull("base_entity.gd should exist");

        var config = new GDProjectConfig();
        var diagnosticsService = GDDiagnosticsService.FromConfig(config);

        var result = diagnosticsService.Diagnose(script!);

        Console.WriteLine($"Diagnostics for base_entity.gd ({result.Diagnostics.Count} total):");
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
    public void RunDiagnostics_OnTodoExamples_StrFunctionShouldAcceptMultipleArgs()
    {
        // This tests the str() varargs fix
        var script = TestProjectFixture.GetScript("todo_examples.gd");
        script.Should().NotBeNull("todo_examples.gd should exist");

        var config = new GDProjectConfig();
        var diagnosticsService = GDDiagnosticsService.FromConfig(config);

        var result = diagnosticsService.Diagnose(script!);

        Console.WriteLine($"Diagnostics for todo_examples.gd ({result.Diagnostics.Count} total):");
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
    public void RunDiagnostics_OnPathExtendsTest_ShowsInheritanceStatus()
    {
        // This tests path-based extends: extends "res://test_scripts/base_entity.gd"
        var script = TestProjectFixture.GetScript("path_extends_test.gd");
        script.Should().NotBeNull("path_extends_test.gd should exist");

        var config = new GDProjectConfig();
        var diagnosticsService = GDDiagnosticsService.FromConfig(config);

        var result = diagnosticsService.Diagnose(script!);

        Console.WriteLine($"=== Diagnostics for path_extends_test.gd ({result.Diagnostics.Count} total) ===");
        Console.WriteLine($"Errors: {result.ErrorCount}, Warnings: {result.WarningCount}, Hints: {result.HintCount}");
        Console.WriteLine();

        foreach (var diag in result.Diagnostics.OrderBy(d => d.StartLine))
        {
            var icon = diag.Severity switch
            {
                GDUnifiedDiagnosticSeverity.Error => "ERROR",
                GDUnifiedDiagnosticSeverity.Warning => "WARN",
                _ => "INFO"
            };
            Console.WriteLine($"  [{icon}] {diag.Code} Line {diag.StartLine}: {diag.Message}");
        }

        // Check for inherited member errors
        var inheritedMemberErrors = result.Diagnostics
            .Where(d => d.Code.Contains("GD5") &&
                       (d.Message.Contains("max_health") ||
                        d.Message.Contains("current_health") ||
                        d.Message.Contains("take_damage") ||
                        d.Message.Contains("heal") ||
                        d.Message.Contains("health_changed")))
            .ToList();

        Console.WriteLine($"\nInherited member errors: {inheritedMemberErrors.Count}");
        foreach (var e in inheritedMemberErrors)
        {
            Console.WriteLine($"  - {e.Message}");
        }

        // Path-based extends should now work - inherited members should be recognized
        inheritedMemberErrors.Should().BeEmpty(
            "Path-based extends should allow access to inherited members (max_health, take_damage, etc.)");
    }
}
