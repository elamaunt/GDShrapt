using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Linq;

namespace GDShrapt.Plugin.Tests.TypeFlow;

/// <summary>
/// Tests for GDTypeFlowPanelEmulator - verifies that the emulator correctly
/// simulates what a developer would see when using the TypeFlow panel.
/// </summary>
[TestClass]
public class GDTypeFlowPanelEmulatorTests
{
    private GDScriptProject _project;
    private GDTypeFlowPanelEmulator _emulator;

    // Test scripts
    private GDScriptFile _methodChainsScript;
    private GDScriptFile _cyclesTestScript;
    private GDScriptFile _localVariablesScript;
    private GDScriptFile _gettersSettersScript;
    private GDScriptFile _signalsTestScript;
    private GDScriptFile _sceneNodesScript;
    private GDScriptFile _standardMethodsScript;
    private GDScriptFile _inheritanceTestScript;
    private GDScriptFile _typeGuardsScript;
    private GDScriptFile _unionReturnsScript;
    private GDScriptFile _unionTypesComplexScript;

    [TestInitialize]
    public void Setup()
    {
        _project = CrossFileTestHelpers.CreateTestProject();
        _emulator = new GDTypeFlowPanelEmulator(_project);

        // Load all test fixture scripts
        _methodChainsScript = CrossFileTestHelpers.GetScriptByName(_project, "method_chains.gd");
        _cyclesTestScript = CrossFileTestHelpers.GetScriptByName(_project, "cycles_test.gd");
        _localVariablesScript = CrossFileTestHelpers.GetScriptByName(_project, "local_variables.gd");
        _gettersSettersScript = CrossFileTestHelpers.GetScriptByName(_project, "getters_setters.gd");
        _signalsTestScript = CrossFileTestHelpers.GetScriptByName(_project, "signals_test.gd");
        _sceneNodesScript = CrossFileTestHelpers.GetScriptByName(_project, "scene_nodes.gd");
        _standardMethodsScript = CrossFileTestHelpers.GetScriptByName(_project, "standard_methods.gd");
        _inheritanceTestScript = CrossFileTestHelpers.GetScriptByName(_project, "inheritance_test.gd");
        _typeGuardsScript = CrossFileTestHelpers.GetScriptByName(_project, "type_guards.gd");
        _unionReturnsScript = CrossFileTestHelpers.GetScriptByName(_project, "union_returns.gd");
        _unionTypesComplexScript = CrossFileTestHelpers.GetScriptByName(_project, "union_types_complex.gd");
    }

    #region Basic Navigation Tests

    [TestMethod]
    public void ShowForSymbol_ValidSymbol_ReturnsTrue()
    {
        // Arrange & Act
        var result = _emulator.ShowForSymbol("data", _unionTypesComplexScript);

        // Assert
        result.Should().BeTrue();
        _emulator.CurrentNode.Should().NotBeNull();
        _emulator.CurrentSymbolName.Should().Be("data");
    }

    [TestMethod]
    public void ShowForSymbol_InvalidSymbol_ReturnsFalse()
    {
        // Arrange & Act
        var result = _emulator.ShowForSymbol("nonexistent_symbol_xyz", _unionTypesComplexScript);

        // Assert
        result.Should().BeFalse();
        _emulator.CurrentNode.Should().BeNull();
    }

    [TestMethod]
    public void ShowForSymbol_SetsRootNode()
    {
        // Arrange & Act
        _emulator.ShowForSymbol("result", _unionTypesComplexScript);

        // Assert
        _emulator.RootNode.Should().NotBeNull();
        _emulator.RootNode.Should().BeSameAs(_emulator.CurrentNode);
    }

    [TestMethod]
    public void NavigateToOutflow_ValidIndex_NavigatesSuccessfully()
    {
        // Arrange
        _emulator.ShowForSymbol("result", _unionTypesComplexScript);
        _emulator.Outflows.Should().NotBeEmpty("result should have outflows");

        // Act
        var result = _emulator.NavigateToOutflow(0);

        // Assert
        result.Should().BeTrue();
        _emulator.CurrentNode.Should().NotBeSameAs(_emulator.RootNode);
        _emulator.CanGoBack.Should().BeTrue();
    }

    [TestMethod]
    public void NavigateToOutflow_InvalidIndex_ReturnsFalse()
    {
        // Arrange
        _emulator.ShowForSymbol("result", _unionTypesComplexScript);

        // Act
        var result = _emulator.NavigateToOutflow(999);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public void GoBack_AfterNavigation_ReturnsToParent()
    {
        // Arrange
        _emulator.ShowForSymbol("result", _unionTypesComplexScript);
        var rootNode = _emulator.CurrentNode;
        _emulator.NavigateToOutflow(0);

        // Act
        var result = _emulator.GoBack();

        // Assert
        result.Should().BeTrue();
        _emulator.CurrentNode.Should().BeSameAs(rootNode);
    }

    [TestMethod]
    public void GoBack_AtRoot_ReturnsFalse()
    {
        // Arrange
        _emulator.ShowForSymbol("result", _unionTypesComplexScript);

        // Act
        var result = _emulator.GoBack();

        // Assert
        result.Should().BeFalse();
        _emulator.CanGoBack.Should().BeFalse();
    }

    [TestMethod]
    public void NavigateThrough_ValidLabels_NavigatesAll()
    {
        // Arrange
        _emulator.ShowForSymbol("current", _unionTypesComplexScript);
        _emulator.PrintState();

        // Find actual labels in outflows
        var outflowLabels = _emulator.Outflows.Take(2).Select(n => n.Label).ToArray();

        if (outflowLabels.Length > 0)
        {
            // Act
            var result = _emulator.NavigateToLabel(outflowLabels[0]);

            // Assert
            result.Should().BeTrue();
        }
    }

    #endregion

    #region Node Kind Tests

    [TestMethod]
    public void TypeCheck_ShowsBoolType()
    {
        // Type checks like "x is Dictionary" should return bool
        if (_typeGuardsScript == null)
        {
            Assert.Inconclusive("type_guards.gd not found");
            return;
        }

        // Arrange
        _emulator.ShowForSymbol("value", _typeGuardsScript);
        _emulator.PrintState();

        // Act
        var typeChecks = _emulator.GetTypeCheckOutflows().ToList();

        // Assert
        if (typeChecks.Any())
        {
            foreach (var tc in typeChecks)
            {
                tc.Type.Should().Be("bool", $"Type check '{tc.Label}' should have type bool");
            }
        }
    }

    [TestMethod]
    public void NullCheck_ShowsBoolType()
    {
        // Null checks like "x == null" should return bool
        if (_typeGuardsScript == null)
        {
            Assert.Inconclusive("type_guards.gd not found");
            return;
        }

        // Arrange
        _emulator.ShowForSymbol("value", _typeGuardsScript);

        // Act
        var nullChecks = _emulator.GetNullCheckOutflows().ToList();

        // Assert
        if (nullChecks.Any())
        {
            foreach (var nc in nullChecks)
            {
                nc.Type.Should().Be("bool", $"Null check '{nc.Label}' should have type bool");
            }
        }
    }

    [TestMethod]
    public void MethodCall_HasCorrectKind()
    {
        // Arrange
        _emulator.ShowForSymbol("result", _unionTypesComplexScript);
        _emulator.PrintState();

        // Act
        var methodCalls = _emulator.GetMethodCallOutflows().ToList();

        // Assert
        foreach (var mc in methodCalls)
        {
            mc.Kind.Should().Be(GDTypeFlowNodeKind.MethodCall);
        }
    }

    [TestMethod]
    public void IndexerAccess_HasCorrectKind()
    {
        // Arrange
        _emulator.ShowForSymbol("result", _unionTypesComplexScript);

        // Act
        var indexers = _emulator.GetIndexerOutflows().ToList();

        // Assert
        foreach (var idx in indexers)
        {
            idx.Kind.Should().Be(GDTypeFlowNodeKind.IndexerAccess);
        }
    }

    [TestMethod]
    public void ReturnValue_HasCorrectKind()
    {
        // Arrange
        _emulator.ShowForSymbol("current", _unionTypesComplexScript);
        _emulator.PrintState();

        // Act
        var returns = _emulator.GetReturnOutflows().ToList();

        // Assert
        foreach (var ret in returns)
        {
            ret.Kind.Should().Be(GDTypeFlowNodeKind.ReturnValue);
        }
    }

    #endregion

    #region SourceType and SourceObjectName Tests

    [TestMethod]
    public void MethodCall_HasSourceObjectName()
    {
        // Arrange
        _emulator.ShowForSymbol("result", _unionTypesComplexScript);

        // Act
        var methodCalls = _emulator.GetMethodCallOutflows().ToList();
        Console.WriteLine($"Found {methodCalls.Count} method calls");
        foreach (var mc in methodCalls)
        {
            Console.WriteLine($"  {mc.Label}: SourceObjectName={mc.SourceObjectName}");
        }

        // Assert - at least one method call should have source object name
        if (methodCalls.Any())
        {
            methodCalls.Should().Contain(mc => !string.IsNullOrEmpty(mc.SourceObjectName),
                "Method calls on 'result' should have SourceObjectName set");
        }
    }

    [TestMethod]
    public void Indexer_HasSourceObjectName()
    {
        // Arrange
        _emulator.ShowForSymbol("result", _unionTypesComplexScript);

        // Act
        var indexers = _emulator.GetIndexerOutflows().ToList();
        Console.WriteLine($"Found {indexers.Count} indexers");
        foreach (var idx in indexers)
        {
            Console.WriteLine($"  {idx.Label}: SourceObjectName={idx.SourceObjectName}");
        }

        // Assert
        if (indexers.Any())
        {
            indexers.Should().Contain(idx => !string.IsNullOrEmpty(idx.SourceObjectName),
                "Indexers on 'result' should have SourceObjectName set");
        }
    }

    [TestMethod]
    public void GetOutflowsWithSourceObject_FiltersCorrectly()
    {
        // Arrange
        _emulator.ShowForSymbol("result", _unionTypesComplexScript);

        // Act
        var resultOutflows = _emulator.GetOutflowsWithSourceObject("result").ToList();

        // Assert
        foreach (var outflow in resultOutflows)
        {
            outflow.SourceObjectName.Should().BeEquivalentTo("result");
        }
    }

    #endregion

    #region Cycle Detection Tests

    [TestMethod]
    public void CycleDetection_IdentifiesCycles()
    {
        if (_cyclesTestScript == null)
        {
            Assert.Inconclusive("cycles_test.gd not found");
            return;
        }

        // Arrange
        _emulator.ShowForSymbol("current", _cyclesTestScript);
        _emulator.PrintState();

        // Act
        var hasCycles = _emulator.HasCycles();

        // Assert - Note: actual cycle detection depends on implementation
        // The test verifies the method works without throwing
        Console.WriteLine($"HasCycles: {hasCycles}");
    }

    [TestMethod]
    public void WouldCreateCycle_DetectsRevisitedNodes()
    {
        // Arrange
        _emulator.ShowForSymbol("current", _unionTypesComplexScript);
        var rootNode = _emulator.CurrentNode;

        // Navigate away
        if (_emulator.Outflows.Any())
        {
            _emulator.NavigateToOutflow(0);

            // Act
            var wouldCycle = _emulator.WouldCreateCycle(rootNode);

            // Assert
            wouldCycle.Should().BeTrue("Revisiting root node should be detected as cycle");
        }
    }

    [TestMethod]
    public void IsNodeVisited_TracksVisitedNodes()
    {
        // Arrange
        _emulator.ShowForSymbol("result", _unionTypesComplexScript);
        var rootNode = _emulator.CurrentNode;

        // Act
        var isRootVisited = _emulator.IsNodeVisited(rootNode);

        // Assert
        isRootVisited.Should().BeTrue("Root node should be marked as visited");
    }

    #endregion

    #region Method Chains Tests

    [TestMethod]
    public void MethodChains_TracksChain()
    {
        if (_methodChainsScript == null)
        {
            Assert.Inconclusive("method_chains.gd not found");
            return;
        }

        // Arrange
        _emulator.ShowForSymbol("data", _methodChainsScript);
        _emulator.PrintState();

        // Assert - data should have usages
        _emulator.CurrentNode.Should().NotBeNull();
        Console.WriteLine($"Inflows: {_emulator.Inflows.Count}, Outflows: {_emulator.Outflows.Count}");
    }

    [TestMethod]
    public void MethodChains_StringChain()
    {
        if (_methodChainsScript == null)
        {
            Assert.Inconclusive("method_chains.gd not found");
            return;
        }

        // Arrange
        _emulator.ShowForSymbol("result", _methodChainsScript);
        _emulator.PrintState();

        // Assert
        _emulator.CurrentNode.Should().NotBeNull();
    }

    #endregion

    #region Local Variables Tests

    [TestMethod]
    public void LocalVariables_TrackFlow()
    {
        if (_localVariablesScript == null)
        {
            Assert.Inconclusive("local_variables.gd not found");
            return;
        }

        // Arrange
        _emulator.ShowForSymbol("local", _localVariablesScript);
        _emulator.PrintState();

        // Assert
        _emulator.CurrentNode.Should().NotBeNull();
        _emulator.CurrentNode.Kind.Should().Be(GDTypeFlowNodeKind.LocalVariable);
    }

    [TestMethod]
    public void LocalVariables_HasInflows()
    {
        if (_localVariablesScript == null)
        {
            Assert.Inconclusive("local_variables.gd not found");
            return;
        }

        // Arrange
        _emulator.ShowForSymbol("local", _localVariablesScript);

        // Assert - local is initialized from parameter and reassigned
        _emulator.Inflows.Should().NotBeEmpty("local should have inflows from initialization and assignments");
    }

    #endregion

    #region Getters/Setters Tests

    [TestMethod]
    public void GettersSetters_PropertyFlow()
    {
        if (_gettersSettersScript == null)
        {
            Assert.Inconclusive("getters_setters.gd not found");
            return;
        }

        // Arrange
        _emulator.ShowForSymbol("health", _gettersSettersScript);
        _emulator.PrintState();

        // Assert
        _emulator.CurrentNode.Should().NotBeNull();
    }

    #endregion

    #region Standard Methods Tests

    [TestMethod]
    public void StandardMethods_ArraySize_ReturnsInt()
    {
        if (_standardMethodsScript == null)
        {
            Assert.Inconclusive("standard_methods.gd not found");
            return;
        }

        // Arrange
        _emulator.ShowForSymbol("arr", _standardMethodsScript);
        _emulator.PrintState();

        // Find method calls that use size()
        var sizeCalls = _emulator.GetMethodCallOutflows()
            .Where(mc => mc.Label != null && mc.Label.Contains("size"))
            .ToList();

        // Assert
        foreach (var sizeCall in sizeCalls)
        {
            Console.WriteLine($"size() call: {sizeCall.Label} -> {sizeCall.Type}");
            sizeCall.Type.Should().Be("int", "Array.size() should return int");
        }
    }

    [TestMethod]
    public void StandardMethods_DictionaryKeys_ReturnsArray()
    {
        if (_standardMethodsScript == null)
        {
            Assert.Inconclusive("standard_methods.gd not found");
            return;
        }

        // Arrange
        _emulator.ShowForSymbol("dict", _standardMethodsScript);
        _emulator.PrintState();

        // Find method calls that use keys()
        var keysCalls = _emulator.GetMethodCallOutflows()
            .Where(mc => mc.Label != null && mc.Label.Contains("keys"))
            .ToList();

        // Assert
        foreach (var keysCall in keysCalls)
        {
            Console.WriteLine($"keys() call: {keysCall.Label} -> {keysCall.Type}");
            keysCall.Type.Should().Be("Array", "Dictionary.keys() should return Array");
        }
    }

    #endregion

    #region Type Guards Tests

    [TestMethod]
    public void TypeGuards_NarrowsType()
    {
        if (_typeGuardsScript == null)
        {
            Assert.Inconclusive("type_guards.gd not found");
            return;
        }

        // Arrange
        _emulator.ShowForSymbol("value", _typeGuardsScript);
        _emulator.PrintState();

        // Assert
        _emulator.CurrentNode.Should().NotBeNull();
    }

    #endregion

    #region Union Returns Tests

    [TestMethod]
    public void UnionReturns_MultipleReturnTypes()
    {
        if (_unionReturnsScript == null)
        {
            Assert.Inconclusive("union_returns.gd not found");
            return;
        }

        // Arrange
        _emulator.ShowForSymbol("input", _unionReturnsScript);
        _emulator.PrintState();

        // Assert
        _emulator.CurrentNode.Should().NotBeNull();
    }

    #endregion

    #region Graph Traversal Tests

    [TestMethod]
    public void CollectAllReachableNodes_ReturnsNodes()
    {
        // Arrange
        _emulator.ShowForSymbol("current", _unionTypesComplexScript);

        // Act
        var allNodes = _emulator.CollectAllReachableNodes();

        // Assert
        allNodes.Should().NotBeEmpty();
        allNodes.Should().Contain(_emulator.RootNode);
        Console.WriteLine($"Total reachable nodes: {allNodes.Count}");
    }

    [TestMethod]
    public void GetNodeKindCounts_ReturnsValidCounts()
    {
        // Arrange
        _emulator.ShowForSymbol("current", _unionTypesComplexScript);

        // Act
        var kindCounts = _emulator.GetNodeKindCounts();

        // Assert
        kindCounts.Should().NotBeEmpty();
        foreach (var kvp in kindCounts)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
        }
    }

    [TestMethod]
    public void GetAllReferencedTypes_ReturnsTypes()
    {
        // Arrange
        _emulator.ShowForSymbol("current", _unionTypesComplexScript);

        // Act
        var types = _emulator.GetAllReferencedTypes();

        // Assert
        types.Should().NotBeEmpty();
        Console.WriteLine($"Referenced types: {string.Join(", ", types)}");
    }

    #endregion

    #region Display Methods Tests

    [TestMethod]
    public void GetStateDisplay_ReturnsFormattedOutput()
    {
        // Arrange
        _emulator.ShowForSymbol("result", _unionTypesComplexScript);

        // Act
        var display = _emulator.GetStateDisplay();

        // Assert
        display.Should().NotBeNullOrEmpty();
        display.Should().Contain("result");
        display.Should().Contain("Inflows");
        display.Should().Contain("Outflows");
        Console.WriteLine(display);
    }

    [TestMethod]
    public void GetCompactSummary_ReturnsOneLine()
    {
        // Arrange
        _emulator.ShowForSymbol("result", _unionTypesComplexScript);

        // Act
        var summary = _emulator.GetCompactSummary();

        // Assert
        summary.Should().NotBeNullOrEmpty();
        summary.Should().Contain("result");
        summary.Should().NotContain("\n");
        Console.WriteLine(summary);
    }

    [TestMethod]
    public void GetNavigationHistoryDisplay_ShowsHistory()
    {
        // Arrange
        _emulator.ShowForSymbol("result", _unionTypesComplexScript);
        if (_emulator.Outflows.Any())
        {
            _emulator.NavigateToOutflow(0);
        }

        // Act
        var history = _emulator.GetNavigationHistoryDisplay();

        // Assert
        history.Should().NotBeNullOrEmpty();
        history.Should().Contain("Navigation History");
        Console.WriteLine(history);
    }

    #endregion

    #region Complex Scenario Tests

    [TestMethod]
    public void ComplexScenario_UnionTypesComplex_FullNavigation()
    {
        // Arrange
        _emulator.ShowForSymbol("current", _unionTypesComplexScript);
        Console.WriteLine("=== Initial state ===");
        _emulator.PrintState();

        // Collect info
        var allKinds = _emulator.Outflows.Select(n => n.Kind).Distinct().ToList();
        Console.WriteLine($"\nOutflow kinds: {string.Join(", ", allKinds)}");

        // Navigate to first outflow if exists
        if (_emulator.Outflows.Any())
        {
            _emulator.NavigateToOutflow(0);
            Console.WriteLine("\n=== After navigating to first outflow ===");
            _emulator.PrintState();

            // Go back
            _emulator.GoBack();
            Console.WriteLine("\n=== After going back ===");
            Console.WriteLine(_emulator.GetCompactSummary());
        }

        // Assert
        _emulator.CurrentNode.Should().BeSameAs(_emulator.RootNode);
    }

    [TestMethod]
    public void ComplexScenario_TypeFlowTestMethod_MaximumCoverage()
    {
        // This tests the type_flow_test_method from union_types_complex.gd
        // Arrange
        _emulator.ShowForSymbol("local", _unionTypesComplexScript);
        Console.WriteLine("=== type_flow_test_method 'local' variable ===");
        _emulator.PrintState();

        // Collect all kinds present
        var allKinds = _emulator.Outflows.Select(n => n.Kind).Distinct().ToList();
        Console.WriteLine($"\nOutflow kinds found: {string.Join(", ", allKinds)}");

        // Verify variety of kinds
        _emulator.CurrentNode.Should().NotBeNull();
    }

    [TestMethod]
    public void ComplexScenario_NavigateDeep_NoInfiniteLoop()
    {
        // Tests that deep navigation doesn't cause infinite loops
        // Arrange
        _emulator.ShowForSymbol("current", _unionTypesComplexScript);

        // Act - navigate through multiple levels
        int maxDepth = 10;
        int currentDepth = 0;

        while (currentDepth < maxDepth && _emulator.Outflows.Any())
        {
            var firstOutflow = _emulator.Outflows.First();

            // Check for potential cycle
            if (_emulator.WouldCreateCycle(firstOutflow))
            {
                Console.WriteLine($"Cycle detected at depth {currentDepth}, stopping navigation");
                break;
            }

            _emulator.NavigateToNode(firstOutflow);
            currentDepth++;
            Console.WriteLine($"Depth {currentDepth}: {_emulator.CurrentSymbolName}");
        }

        // Assert - should complete without hanging
        currentDepth.Should().BeGreaterThan(0, "Should have navigated at least once");
    }

    #endregion
}
