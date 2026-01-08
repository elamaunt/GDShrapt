using GDShrapt.Semantics.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Integration;

/// <summary>
/// Diagnostic tests to verify the test project loading correctly.
/// </summary>
[TestClass]
public class DiagnosticTests
{
    [TestMethod]
    public void Diagnostic_ProjectLoaded_HasScripts()
    {
        var project = TestProjectFixture.Project;
        Assert.IsNotNull(project, "Project should be loaded");

        var scripts = project.ScriptFiles.ToList();
        Console.WriteLine($"Scripts found: {scripts.Count}");
        foreach (var script in scripts)
        {
            Console.WriteLine($"  - {script.FullPath}");
        }

        Assert.IsTrue(scripts.Count > 0, "Should have scripts");
    }

    [TestMethod]
    public void Diagnostic_RenameTestScript_Exists()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd should exist");

        Console.WriteLine($"Script path: {script.FullPath}");
        Console.WriteLine($"Has class: {script.Class != null}");
        Console.WriteLine($"Has analyzer: {script.Analyzer != null}");
    }

    [TestMethod]
    public void Diagnostic_RenameTestScript_HasClass()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd should exist");
        Assert.IsNotNull(script.Class, "Should have class");

        Console.WriteLine($"Class type: {script.Class.GetType().Name}");
    }

    [TestMethod]
    public void Diagnostic_RenameTestScript_AnalyzerHasReferences()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd should exist");

        Console.WriteLine($"Script path: {script.FullPath}");
        Console.WriteLine($"Script has class: {script.Class != null}");
        Console.WriteLine($"Script TypeName: {script.TypeName}");
        Console.WriteLine($"Script WasReadError: {script.WasReadError}");

        if (script.Analyzer == null)
        {
            Console.WriteLine("Analyzer is null - trying to analyze manually...");
            script.Analyze();
            Console.WriteLine($"After manual Analyze(): {script.Analyzer != null}");
        }

        Assert.IsNotNull(script.Analyzer, "Should have analyzer");
        Assert.IsNotNull(script.Analyzer.References, "Analyzer should have References");

        var symbols = script.Analyzer.Symbols.ToList();
        Console.WriteLine($"Symbols found: {symbols.Count}");
        foreach (var sym in symbols.Take(20))
        {
            Console.WriteLine($"  - {sym.Name} ({sym.Kind})");
        }
    }

    [TestMethod]
    public void Diagnostic_RenameTestScript_FindSymbol_Counter()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd should exist");
        Assert.IsNotNull(script.Analyzer, "Should have analyzer");

        var symbol = script.Analyzer.FindSymbol("counter");
        Console.WriteLine($"Symbol 'counter' found: {symbol != null}");
        if (symbol != null)
        {
            Console.WriteLine($"  Name: {symbol.Name}");
            Console.WriteLine($"  Kind: {symbol.Kind}");
            Console.WriteLine($"  Declaration: {symbol.Declaration != null}");
        }

        // List all symbols for debugging
        var allSymbols = script.Analyzer.Symbols.ToList();
        Console.WriteLine($"\nAll symbols ({allSymbols.Count}):");
        foreach (var sym in allSymbols)
        {
            Console.WriteLine($"  - '{sym.Name}' ({sym.Kind})");
        }
    }

    [TestMethod]
    public void Diagnostic_CollectReferences_Counter()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd should exist");

        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "counter");
        Console.WriteLine($"References to 'counter': {references.Count}");
        foreach (var r in references)
        {
            Console.WriteLine($"  - {r.Kind}: line {r.Line}, col {r.Column}");
        }
    }
}
