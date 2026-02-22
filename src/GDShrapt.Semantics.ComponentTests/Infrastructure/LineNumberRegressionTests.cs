using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Regression tests: all collectors must return 1-based line numbers.
/// Prevents re-introduction of the 0-based line number bug (GetNodeLine returning token.StartLine without +1).
/// </summary>
[TestClass]
public class LineNumberRegressionTests
{
    [TestMethod]
    public void TypeNodeCollector_AllLineNumbers_AreOneBased()
    {
        var file = TestProjectFixture.GetScript("simple_class.gd");
        file.Should().NotBeNull();

        var collector = new TypeNodeCollector();
        var nodes = collector.CollectNodes(file!);

        nodes.Should().NotBeEmpty("simple_class.gd should produce type nodes");

        foreach (var node in nodes)
        {
            node.Line.Should().BeGreaterThanOrEqualTo(1,
                $"TypeNodeCollector: '{node.Name}' ({node.NodeKind}) has line {node.Line} — must be 1-based, never 0");
        }

        // "speed" is declared on line 12 of simple_class.gd
        var speedNode = nodes.FirstOrDefault(n => n.Name == "speed" && n.NodeKind == "Variable");
        speedNode.Should().NotBeNull();
        speedNode!.Line.Should().Be(12, "'speed' is on line 12 of simple_class.gd");
    }

    [TestMethod]
    public void TypeInfoCollector_AllLineNumbers_AreOneBased()
    {
        var file = TestProjectFixture.GetScript("simple_class.gd");
        file.Should().NotBeNull();

        var collector = new TypeInfoCollector();
        var entries = collector.CollectEntries(file!);

        entries.Should().NotBeEmpty("simple_class.gd should produce type info entries");

        foreach (var entry in entries)
        {
            entry.Line.Should().BeGreaterThanOrEqualTo(1,
                $"TypeInfoCollector: '{entry.Name}' ({entry.SymbolKind}) has line {entry.Line} — must be 1-based, never 0");
        }

        // "health" is declared on line 13 of simple_class.gd
        var healthEntry = entries.FirstOrDefault(e => e.Name == "health" && e.SymbolKind == "Variable");
        healthEntry.Should().NotBeNull();
        healthEntry!.Line.Should().Be(13, "'health' is on line 13 of simple_class.gd");
    }

    [TestMethod]
    public void FlowNarrowingCollector_AllLineNumbers_AreOneBased()
    {
        var file = TestProjectFixture.GetScript("type_guards.gd");
        file.Should().NotBeNull();

        var collector = new FlowNarrowingCollector();
        var entries = collector.CollectEntries(file!);

        // type_guards.gd should have narrowing entries from "is" checks
        foreach (var entry in entries)
        {
            entry.Line.Should().BeGreaterThanOrEqualTo(1,
                $"FlowNarrowingCollector: '{entry.VariableName}' in {entry.MethodName} has line {entry.Line} — must be 1-based, never 0");
        }
    }

    [TestMethod]
    public void DuckTypeCollector_AllLineNumbers_AreOneBased()
    {
        // duck_typing_advanced.gd has untyped parameters (source, target) on line 21
        var file = TestProjectFixture.GetScript("duck_typing_advanced.gd");
        file.Should().NotBeNull();

        var collector = new DuckTypeCollector();
        var entries = collector.CollectEntries(file!);

        entries.Should().NotBeEmpty("duck_typing_advanced.gd should produce duck type entries");

        foreach (var entry in entries)
        {
            entry.Line.Should().BeGreaterThanOrEqualTo(1,
                $"DuckTypeCollector: '{entry.ParameterName}' in {entry.MethodName} has line {entry.Line} — must be 1-based, never 0");
        }

        // "source" parameter in process_attack is on line 21
        var sourceEntry = entries.FirstOrDefault(e => e.ParameterName == "source" && e.MethodName == "process_attack");
        sourceEntry.Should().NotBeNull();
        sourceEntry!.Line.Should().Be(21, "'source' parameter is on line 21 of duck_typing_advanced.gd");
    }
}
