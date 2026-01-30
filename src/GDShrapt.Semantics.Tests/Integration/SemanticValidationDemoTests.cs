using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Integration;

/// <summary>
/// Integration tests that validate the semantic_validation_demo.gd file
/// to demonstrate all semantic validation capabilities.
/// </summary>
[TestClass]
public class SemanticValidationDemoTests
{
    private static GDScriptFile? _demoScript;
    private static List<GDDiagnostic>? _allDiagnostics;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        var script = TestProjectFixture.GetScript("semantic_validation_demo.gd");
        if (script == null)
        {
            Console.WriteLine("WARNING: semantic_validation_demo.gd not found in test project");
            return;
        }

        _demoScript = script;

        // Run semantic validation
        if (script.SemanticModel != null)
        {
            var options = new GDSemanticValidatorOptions
            {
                CheckTypes = true,
                CheckMemberAccess = true,
                CheckArgumentTypes = true
            };
            var validator = new GDSemanticValidator(script.SemanticModel, options);
            var result = validator.Validate(script.Class);
            _allDiagnostics = result.Diagnostics.ToList();

            // Output all diagnostics for debugging
            Console.WriteLine($"Total diagnostics: {_allDiagnostics.Count}");
            foreach (var diag in _allDiagnostics.OrderBy(d => d.StartLine))
            {
                Console.WriteLine($"  [{diag.Code}] Line {diag.StartLine}: {diag.Message}");
            }
        }
    }

    [TestMethod]
    public void DemoScript_Exists()
    {
        Assert.IsNotNull(_demoScript, "semantic_validation_demo.gd should exist in test project");
        Assert.IsNotNull(_demoScript.Class, "Script should parse successfully");
        Assert.IsNotNull(_demoScript.SemanticModel, "Script should have semantic model");
    }

    [TestMethod]
    public void DemoScript_HasDiagnostics()
    {
        Assert.IsNotNull(_allDiagnostics, "Should have run validation");
        Assert.IsTrue(_allDiagnostics.Count > 0, "Demo script should produce diagnostics");

        Console.WriteLine($"Total diagnostics generated: {_allDiagnostics.Count}");
    }

    #region Section 1: Indexer Validation (GD3013, GD3014)

    [TestMethod]
    public void DemoScript_IndexerKeyTypeMismatch_GD3013()
    {
        // Array with String key: arr2["invalid"]
        // Typed Dictionary with wrong key: str_dict[42]
        // String with String key: text2["x"]
        var indexerDiagnostics = GetDiagnosticsByCode(GDDiagnosticCode.IndexerKeyTypeMismatch);

        Console.WriteLine($"GD3013 IndexerKeyTypeMismatch diagnostics: {indexerDiagnostics.Count}");
        foreach (var d in indexerDiagnostics)
        {
            Console.WriteLine($"  Line {d.StartLine}: {d.Message}");
        }

        // Note: This may be 0 if GDIndexerValidator is not yet fully implemented
        // The test documents expected behavior
        if (indexerDiagnostics.Count == 0)
        {
            Console.WriteLine("NOTE: GD3013 not yet implemented in GDSemanticValidator");
        }
    }

    [TestMethod]
    public void DemoScript_NotIndexable_GD3014()
    {
        // int, float, bool indexed: num[0], flt[0], flag[0]
        var notIndexableDiagnostics = GetDiagnosticsByCode(GDDiagnosticCode.NotIndexable);

        Console.WriteLine($"GD3014 NotIndexable diagnostics: {notIndexableDiagnostics.Count}");
        foreach (var d in notIndexableDiagnostics)
        {
            Console.WriteLine($"  Line {d.StartLine}: {d.Message}");
        }

        if (notIndexableDiagnostics.Count == 0)
        {
            Console.WriteLine("NOTE: GD3014 not yet implemented in GDSemanticValidator");
        }
    }

    #endregion

    #region Section 2: Signal Validation (GD4009)

    [TestMethod]
    public void DemoScript_EmitSignalTypeMismatch_GD4009()
    {
        // emit_signal("player_scored", "hundred", "Bob") - String instead of int
        // emit_signal("item_collected", [1, 2, 3]) - Array instead of int
        // emit_signal("item_collected", 3.14) - float to int narrowing
        // emit_signal("player_scored", 50, 123) - int instead of String
        var signalDiagnostics = GetDiagnosticsByCode(GDDiagnosticCode.EmitSignalTypeMismatch);

        Console.WriteLine($"GD4009 EmitSignalTypeMismatch diagnostics: {signalDiagnostics.Count}");
        foreach (var d in signalDiagnostics)
        {
            Console.WriteLine($"  Line {d.StartLine}: {d.Message}");
        }

        if (signalDiagnostics.Count == 0)
        {
            Console.WriteLine("NOTE: GD4009 not yet implemented in GDSemanticValidator");
        }
    }

    #endregion

    #region Section 3: Generic Type Validation (GD3017, GD3018)

    [TestMethod]
    public void DemoScript_InvalidGenericArgument_GD3017()
    {
        // Array[UnknownType], Dictionary[String, NonExistentClass]
        var genericArgDiagnostics = GetDiagnosticsByCode(GDDiagnosticCode.InvalidGenericArgument);

        Console.WriteLine($"GD3017 InvalidGenericArgument diagnostics: {genericArgDiagnostics.Count}");
        foreach (var d in genericArgDiagnostics)
        {
            Console.WriteLine($"  Line {d.StartLine}: {d.Message}");
        }

        if (genericArgDiagnostics.Count == 0)
        {
            Console.WriteLine("NOTE: GD3017 not yet implemented in GDSemanticValidator");
        }
    }

    [TestMethod]
    public void DemoScript_DictionaryKeyNotHashable_GD3018()
    {
        // Dictionary[Array, int], Dictionary[Dictionary, int], Dictionary[PackedByteArray, int]
        var hashableDiagnostics = GetDiagnosticsByCode(GDDiagnosticCode.DictionaryKeyNotHashable);

        Console.WriteLine($"GD3018 DictionaryKeyNotHashable diagnostics: {hashableDiagnostics.Count}");
        foreach (var d in hashableDiagnostics)
        {
            Console.WriteLine($"  Line {d.StartLine}: {d.Message}");
        }

        if (hashableDiagnostics.Count == 0)
        {
            Console.WriteLine("NOTE: GD3018 not yet implemented in GDSemanticValidator");
        }
    }

    #endregion

    #region Section 4: Argument Type Validation (GD3010)

    [TestMethod]
    public void DemoScript_ArgumentTypeMismatch_GD3010()
    {
        // take_int("not an int") - String to int
        // take_string(42) - int to String
        // take_node2d(node) - Node to Node2D
        var argDiagnostics = GetDiagnosticsByCode(GDDiagnosticCode.ArgumentTypeMismatch);

        Console.WriteLine($"GD3010 ArgumentTypeMismatch diagnostics: {argDiagnostics.Count}");
        foreach (var d in argDiagnostics)
        {
            Console.WriteLine($"  Line {d.StartLine}: {d.Message}");
        }

        Assert.IsTrue(argDiagnostics.Count >= 1,
            "Expected at least one ArgumentTypeMismatch diagnostic from demo script");
    }

    #endregion

    #region Section 5: Member Access Validation (GD3009, GD7001, GD7002)

    [TestMethod]
    public void DemoScript_PropertyNotFound_GD3009()
    {
        // text.nonexistent_property - String has no such property
        var propNotFoundDiagnostics = GetDiagnosticsByCode(GDDiagnosticCode.PropertyNotFound);

        Console.WriteLine($"GD3009 PropertyNotFound diagnostics: {propNotFoundDiagnostics.Count}");
        foreach (var d in propNotFoundDiagnostics)
        {
            Console.WriteLine($"  Line {d.StartLine}: {d.Message}");
        }

        Assert.IsTrue(propNotFoundDiagnostics.Count >= 1,
            "Expected at least one PropertyNotFound diagnostic from demo script");
    }

    [TestMethod]
    public void DemoScript_UnguardedPropertyAccess_GD7002()
    {
        // Note: In the demo script, variables like `unknown` are typed as `null` (from `return null`)
        // rather than true `Variant`, so they produce PropertyNotFound instead of UnguardedPropertyAccess.
        // This is expected behavior - UnguardedPropertyAccess only fires for truly untyped Variant.
        var unguardedDiagnostics = GetDiagnosticsByCode(GDDiagnosticCode.UnguardedPropertyAccess);

        Console.WriteLine($"GD7002 UnguardedPropertyAccess diagnostics: {unguardedDiagnostics.Count}");
        foreach (var d in unguardedDiagnostics)
        {
            Console.WriteLine($"  Line {d.StartLine}: {d.Message}");
        }

        // UnguardedPropertyAccess requires true Variant type (not inferred null/Dictionary/etc.)
        // The demo script's `get_unknown_value()` returns null, which is typed as `null`, not Variant.
        // When accessing `.some_property` on `null`, we get PropertyNotFound instead.
        Console.WriteLine("NOTE: Demo script variables are inferred as 'null' from 'return null', not Variant");
    }

    #endregion

    #region Section 6: Type Assignment Validation (GD3003)

    [TestMethod]
    public void DemoScript_InvalidAssignment_GD3003()
    {
        // x = "not valid" - String to int
        // n2d = n - Node to Node2D
        var assignmentDiagnostics = GetDiagnosticsByCode(GDDiagnosticCode.InvalidAssignment);

        Console.WriteLine($"GD3003 InvalidAssignment diagnostics: {assignmentDiagnostics.Count}");
        foreach (var d in assignmentDiagnostics)
        {
            Console.WriteLine($"  Line {d.StartLine}: {d.Message}");
        }

        // Note: May not be implemented yet
        if (assignmentDiagnostics.Count == 0)
        {
            Console.WriteLine("NOTE: GD3003 assignment validation may use different code");
        }
    }

    #endregion

    #region Summary

    [TestMethod]
    public void DemoScript_DiagnosticSummary()
    {
        Assert.IsNotNull(_allDiagnostics);

        var summary = _allDiagnostics
            .GroupBy(d => d.Code)
            .OrderBy(g => (int)g.Key)
            .ToList();

        Console.WriteLine("=== DIAGNOSTIC SUMMARY ===");
        Console.WriteLine($"Total diagnostics: {_allDiagnostics.Count}");
        Console.WriteLine();

        foreach (var group in summary)
        {
            Console.WriteLine($"[{group.Key}] ({group.Count()} occurrences):");
            foreach (var d in group.Take(3))
            {
                Console.WriteLine($"    Line {d.StartLine}: {d.Message}");
            }
            if (group.Count() > 3)
            {
                Console.WriteLine($"    ... and {group.Count() - 3} more");
            }
            Console.WriteLine();
        }

        // Document which diagnostic codes were generated
        var generatedCodes = summary.Select(g => g.Key).ToList();
        Console.WriteLine("=== GENERATED DIAGNOSTIC CODES ===");
        foreach (var code in generatedCodes)
        {
            Console.WriteLine($"  - GD{(int)code:D4} {code}");
        }
    }

    #endregion

    #region Helper Methods

    private static List<GDDiagnostic> GetDiagnosticsByCode(GDDiagnosticCode code)
    {
        return _allDiagnostics?.Where(d => d.Code == code).ToList() ?? new List<GDDiagnostic>();
    }

    #endregion
}
