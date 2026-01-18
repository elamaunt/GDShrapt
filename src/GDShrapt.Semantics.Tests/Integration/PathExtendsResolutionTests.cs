using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Diagnostic tests to understand path-based extends resolution.
/// </summary>
[TestClass]
public class PathExtendsResolutionTests
{
    [TestMethod]
    public void Diagnostic_PathExtendsTest_ShowsResolutionDetails()
    {
        var project = TestProjectFixture.Project;

        Console.WriteLine("=== Path-Based Extends Resolution Diagnostic ===\n");

        // 1. Find path_extends_test.gd
        var pathExtendsScript = TestProjectFixture.GetScript("path_extends_test.gd");
        pathExtendsScript.Should().NotBeNull("path_extends_test.gd should exist");

        Console.WriteLine($"1. Script: path_extends_test.gd");
        Console.WriteLine($"   FullPath: {pathExtendsScript!.FullPath}");
        Console.WriteLine($"   ResPath: {pathExtendsScript.ResPath}");
        Console.WriteLine($"   TypeName: {pathExtendsScript.TypeName}");

        // 2. Get the extends clause
        var extendsType = pathExtendsScript.Class?.Extends?.Type;
        Console.WriteLine($"\n2. Extends clause:");
        Console.WriteLine($"   Type: {extendsType?.GetType().Name ?? "null"}");
        Console.WriteLine($"   BuildName(): {extendsType?.BuildName() ?? "null"}");

        // 3. Find base_entity.gd
        var baseEntityScript = TestProjectFixture.GetScript("base_entity.gd");
        baseEntityScript.Should().NotBeNull("base_entity.gd should exist");

        Console.WriteLine($"\n3. Base script: base_entity.gd");
        Console.WriteLine($"   FullPath: {baseEntityScript!.FullPath}");
        Console.WriteLine($"   ResPath: {baseEntityScript.ResPath}");
        Console.WriteLine($"   TypeName: {baseEntityScript.TypeName}");

        // 4. Check if BaseEntity has the expected members
        if (baseEntityScript.Analyzer != null)
        {
            var symbols = baseEntityScript.Analyzer.Symbols.ToList();
            Console.WriteLine($"\n4. BaseEntity symbols ({symbols.Count}):");
            foreach (var sym in symbols.Take(15))
            {
                Console.WriteLine($"   - {sym.Name} ({sym.Kind})");
            }
        }

        // 5. Check path resolution - compare extends path vs script paths
        Console.WriteLine($"\n5. Path comparison:");
        var extendsPath = extendsType?.BuildName() ?? "";
        Console.WriteLine($"   Extends path: '{extendsPath}'");

        // Find base script by various methods
        var baseByResPath = project.ScriptFiles.FirstOrDefault(s =>
            s.ResPath != null && s.ResPath.Equals(extendsPath, StringComparison.OrdinalIgnoreCase));
        var baseByFullPath = project.ScriptFiles.FirstOrDefault(s =>
            s.FullPath != null && s.FullPath.EndsWith("base_entity.gd", StringComparison.OrdinalIgnoreCase));

        Console.WriteLine($"   Found by ResPath match: {baseByResPath != null}");
        Console.WriteLine($"   Found by FullPath ends with: {baseByFullPath != null}");

        if (baseByFullPath != null)
        {
            Console.WriteLine($"   Base FullPath: {baseByFullPath.FullPath}");
            Console.WriteLine($"   Base ResPath: {baseByFullPath.ResPath}");
            Console.WriteLine($"   Base TypeName: {baseByFullPath.TypeName}");
        }

        // 6. List all scripts in project with their ResPath
        Console.WriteLine($"\n6. All project scripts:");
        foreach (var script in project.ScriptFiles)
        {
            Console.WriteLine($"   - TypeName: {script.TypeName ?? "(none)"}");
            Console.WriteLine($"     FullPath: {script.FullPath}");
            Console.WriteLine($"     ResPath: {script.ResPath ?? "(none)"}");
        }

        // 7. Check if path_extends_test sees inherited members
        Console.WriteLine($"\n7. PathExtendsTest inherited member resolution:");
        if (pathExtendsScript.Analyzer != null)
        {
            var maxHealthSymbol = pathExtendsScript.Analyzer.FindSymbol("max_health");
            Console.WriteLine($"   max_health found: {maxHealthSymbol != null}");
            if (maxHealthSymbol != null)
            {
                Console.WriteLine($"   - Name: {maxHealthSymbol.Name}");
                Console.WriteLine($"   - Kind: {maxHealthSymbol.Kind}");
                Console.WriteLine($"   - IsInherited: {maxHealthSymbol.IsInherited}");
            }

            var takeDamageSymbol = pathExtendsScript.Analyzer.FindSymbol("take_damage");
            Console.WriteLine($"   take_damage found: {takeDamageSymbol != null}");
        }
        else
        {
            Console.WriteLine("   Analyzer is null!");
        }
    }

    [TestMethod]
    public void Diagnostic_BuildName_ReturnsPathWithoutQuotes()
    {
        var pathExtendsScript = TestProjectFixture.GetScript("path_extends_test.gd");
        pathExtendsScript.Should().NotBeNull();

        var extendsType = pathExtendsScript!.Class?.Extends?.Type;
        extendsType.Should().NotBeNull("Should have extends type");

        var buildName = extendsType!.BuildName();
        Console.WriteLine($"BuildName result: '{buildName}'");

        // Should NOT contain quotes
        buildName.Should().NotStartWith("\"", "BuildName should not start with quote");
        buildName.Should().NotEndWith("\"", "BuildName should not end with quote");

        // Should be a valid res:// path
        buildName.Should().StartWith("res://", "Should be a resource path");
        buildName.Should().EndWith(".gd", "Should end with .gd");
    }
}
