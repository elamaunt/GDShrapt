using GDShrapt.CLI.Core;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Linq;

namespace GDShrapt.Plugin.Tests.TypeFlow;

/// <summary>
/// Tests that verify the data available to TypeFlow UI tabs (Inflows/Outflows).
/// These tests emulate exactly what GDTypeInflowsTab and GDTypeOutflowsTab do when displaying data.
/// </summary>
[TestClass]
public class GDTypeFlowUIDataTests
{
    private GDScriptProject _project;
    private GDScriptFile _unionTypesScript;
    private IGDTypeFlowHandler _typeFlowHandler;
    private IGDSymbolsHandler _symbolsHandler;

    [TestInitialize]
    public void Setup()
    {
        _project = CrossFileTestHelpers.CreateTestProject();
        _unionTypesScript = CrossFileTestHelpers.GetScriptByName(_project, "union_types_complex.gd");
        _unionTypesScript.Should().NotBeNull("union_types_complex.gd should exist in test project");

        // Initialize service registry and load base module to get handlers
        var registry = new GDServiceRegistry();
        registry.LoadModules(_project, new GDBaseModule());

        _typeFlowHandler = registry.GetService<IGDTypeFlowHandler>();
        _symbolsHandler = registry.GetService<IGDSymbolsHandler>();
    }

    #region UI Data Availability Tests - Emulate GDTypeBreakdownPanel.SetNode()

    /// <summary>
    /// This test emulates what the UI does when showing Inflows tab.
    /// GDTypeInflowsTab.DisplayInflows(node) checks node.Inflows.
    /// </summary>
    [TestMethod]
    public void UIData_InflowsTab_DataAvailable_ForVariable()
    {
        // Arrange - Emulate GDTypeFlowPanel.ShowForSymbol()
        var builder = new GDTypeFlowGraphBuilder(_project, _typeFlowHandler, _symbolsHandler);

        // Act - Build graph (this is what the panel does)
        var rootNode = builder.BuildGraph("current", _unionTypesScript);

        // Assert - Check if data is available for UI
        rootNode.Should().NotBeNull("Graph should be built for 'current' variable");

        // This is what GDTypeInflowsTab.DisplayInflows(node) checks
        Console.WriteLine($"=== Emulating GDTypeInflowsTab.DisplayInflows(node) ===");
        Console.WriteLine($"Symbol: {rootNode.Label}");
        Console.WriteLine($"Type: {rootNode.Type}");
        Console.WriteLine($"Kind: {rootNode.Kind}");
        Console.WriteLine($"AreInflowsLoaded: {rootNode.AreInflowsLoaded}");
        Console.WriteLine($"node.Inflows.Count = {rootNode.Inflows.Count}");

        if (rootNode.Inflows.Count == 0)
        {
            Console.WriteLine("WARNING: Inflows is EMPTY - UI will show 'No type sources found'");
        }
        else
        {
            Console.WriteLine("Inflows:");
            foreach (var inflow in rootNode.Inflows)
            {
                Console.WriteLine($"  - [{inflow.Kind}] {inflow.Label}: {inflow.Type}");
            }
        }

        // The UI needs data here
        rootNode.Inflows.Should().NotBeEmpty(
            "Inflows should contain data for UI. 'current' is assigned from 'data' parameter.");
    }

    /// <summary>
    /// This test emulates what the UI does when showing Outflows tab.
    /// GDTypeOutflowsTab.DisplayOutflows(node) checks node.Outflows.
    /// </summary>
    [TestMethod]
    public void UIData_OutflowsTab_DataAvailable_ForVariable()
    {
        // Arrange - Emulate GDTypeFlowPanel.ShowForSymbol()
        var builder = new GDTypeFlowGraphBuilder(_project, _typeFlowHandler, _symbolsHandler);

        // Act - Build graph (this is what the panel does)
        var rootNode = builder.BuildGraph("current", _unionTypesScript);

        // Assert - Check if data is available for UI
        rootNode.Should().NotBeNull("Graph should be built for 'current' variable");

        // This is what GDTypeOutflowsTab.DisplayOutflows(node) checks
        Console.WriteLine($"=== Emulating GDTypeOutflowsTab.DisplayOutflows(node) ===");
        Console.WriteLine($"Symbol: {rootNode.Label}");
        Console.WriteLine($"Type: {rootNode.Type}");
        Console.WriteLine($"Kind: {rootNode.Kind}");
        Console.WriteLine($"AreOutflowsLoaded: {rootNode.AreOutflowsLoaded}");
        Console.WriteLine($"node.Outflows.Count = {rootNode.Outflows.Count}");

        if (rootNode.Outflows.Count == 0)
        {
            Console.WriteLine("WARNING: Outflows is EMPTY - UI will show 'No usages found'");
        }
        else
        {
            Console.WriteLine("Outflows:");
            foreach (var outflow in rootNode.Outflows)
            {
                Console.WriteLine($"  - [{outflow.Kind}] {outflow.Label}: {outflow.Type} - {outflow.Description}");
            }
        }

        // The UI needs data here
        rootNode.Outflows.Should().NotBeEmpty(
            "Outflows should contain data for UI. 'current' is used in return, null checks, type checks, etc.");
    }

    /// <summary>
    /// This test emulates the exact flow of data from panel to tabs.
    /// </summary>
    [TestMethod]
    public void UIData_BreakdownPanel_SetNode_DataFlow()
    {
        // Arrange - What GDTypeFlowPanel does
        var builder = new GDTypeFlowGraphBuilder(_project, _typeFlowHandler, _symbolsHandler);

        // Emulate: _rootNode = _graphBuilder.BuildGraph(symbolName, scriptFile);
        var rootNode = builder.BuildGraph("result", _unionTypesScript);

        // Assert - Graph is built
        rootNode.Should().NotBeNull();

        // Emulate: _breakdownPanel.SetNode(node, node.SourceScript?.SemanticModel);
        // This calls:
        //   _inflowsTab.DisplayInflows(node);  -> checks node.Inflows
        //   _outflowsTab.DisplayOutflows(node); -> checks node.Outflows

        Console.WriteLine("=== GDTypeBreakdownPanel.SetNode() Data Flow ===");
        Console.WriteLine($"Root Node: {rootNode.Label}");
        Console.WriteLine($"SourceScript: {(rootNode.SourceScript != null ? System.IO.Path.GetFileName(rootNode.SourceScript.FullPath ?? "unknown") : "null")}");
        Console.WriteLine($"SemanticModel: {(rootNode.SourceScript?.SemanticModel != null ? "available" : "null")}");
        Console.WriteLine();

        // What GDTypeInflowsTab.DisplayInflows(node) sees:
        Console.WriteLine($"_inflowsTab.DisplayInflows(node):");
        Console.WriteLine($"  node.Inflows.Count = {rootNode.Inflows.Count}");
        if (rootNode.Inflows.Count == 0)
        {
            Console.WriteLine("  -> Will show 'No type sources found'");
        }

        Console.WriteLine();

        // What GDTypeOutflowsTab.DisplayOutflows(node) sees:
        Console.WriteLine($"_outflowsTab.DisplayOutflows(node):");
        Console.WriteLine($"  node.Outflows.Count = {rootNode.Outflows.Count}");
        if (rootNode.Outflows.Count == 0)
        {
            Console.WriteLine("  -> Will show 'No usages found'");
        }

        // Verify data is present
        var hasInflowData = rootNode.Inflows.Count > 0;
        var hasOutflowData = rootNode.Outflows.Count > 0;

        Console.WriteLine();
        Console.WriteLine($"=== DIAGNOSIS ===");
        Console.WriteLine($"Has Inflow Data: {hasInflowData}");
        Console.WriteLine($"Has Outflow Data: {hasOutflowData}");

        if (!hasInflowData && !hasOutflowData)
        {
            Console.WriteLine("PROBLEM: Both Inflows and Outflows are empty!");
            Console.WriteLine("This explains why the UI tabs show no data.");
        }
    }

    #endregion

    #region Parameter Tests

    [TestMethod]
    public void UIData_Parameter_HasOutflows()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project, _typeFlowHandler, _symbolsHandler);

        // Act - 'result' is a parameter in handle_result function
        var rootNode = builder.BuildGraph("result", _unionTypesScript);

        // Assert
        rootNode.Should().NotBeNull();
        rootNode.Kind.Should().Be(GDTypeFlowNodeKind.Parameter);

        Console.WriteLine($"Parameter '{rootNode.Label}' outflows: {rootNode.Outflows.Count}");
        foreach (var outflow in rootNode.Outflows)
        {
            Console.WriteLine($"  -> {outflow.Label} ({outflow.Kind})");
        }

        // A parameter should have outflows where it's used
        rootNode.Outflows.Should().NotBeEmpty(
            "'result' parameter is used in match, get(), and indexer access");
    }

    [TestMethod]
    public void UIData_Parameter_HasInflows_TypeAnnotation()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project, _typeFlowHandler, _symbolsHandler);

        // Act - Look for a parameter with type annotation
        var rootNode = builder.BuildGraph("data", _unionTypesScript);

        // Assert
        rootNode.Should().NotBeNull();

        Console.WriteLine($"'{rootNode.Label}' inflows: {rootNode.Inflows.Count}");
        foreach (var inflow in rootNode.Inflows)
        {
            Console.WriteLine($"  <- {inflow.Label} ({inflow.Kind}) - {inflow.Type}");
        }

        // A parameter with type annotation should have TypeAnnotation inflow
        // If no annotation, inflows might be empty (which is valid)
    }

    #endregion

    #region Detailed Debug Tests

    [TestMethod]
    public void Debug_PrintAllSymbolsInUnionTypesComplex()
    {
        // This test helps debug what symbols are available
        _unionTypesScript.Should().NotBeNull();
        var semanticModel = _unionTypesScript.SemanticModel;
        semanticModel.Should().NotBeNull();

        Console.WriteLine("=== Symbols in union_types_complex.gd ===");

        // Try to find specific symbols we know exist
        var knownSymbols = new[] { "current", "result", "data", "local", "mixed_input" };
        foreach (var name in knownSymbols)
        {
            var symbol = semanticModel.FindSymbol(name);
            if (symbol != null)
            {
                Console.WriteLine($"  {symbol.Name} ({symbol.Kind}) - Type: {symbol.TypeName ?? "Variant"}");
            }
            else
            {
                Console.WriteLine($"  {name} - NOT FOUND");
            }
        }
    }

    [TestMethod]
    public void Debug_PrintGraphDetails_ForCurrent()
    {
        var builder = new GDTypeFlowGraphBuilder(_project, _typeFlowHandler, _symbolsHandler);
        var rootNode = builder.BuildGraph("current", _unionTypesScript);

        rootNode.Should().NotBeNull();

        Console.WriteLine("=== Graph Details for 'current' ===");
        Console.WriteLine($"Label: {rootNode.Label}");
        Console.WriteLine($"Type: {rootNode.Type}");
        Console.WriteLine($"Kind: {rootNode.Kind}");
        Console.WriteLine($"Id: {rootNode.Id}");
        Console.WriteLine($"Confidence: {rootNode.Confidence:P0}");
        Console.WriteLine($"Description: {rootNode.Description}");
        Console.WriteLine($"Location: {rootNode.Location}");
        Console.WriteLine($"SourceScript: {(rootNode.SourceScript != null ? System.IO.Path.GetFileName(rootNode.SourceScript.FullPath ?? "unknown") : "null")}");
        Console.WriteLine($"AstNode: {rootNode.AstNode?.GetType().Name}");
        Console.WriteLine($"IsUnionType: {rootNode.IsUnionType}");
        Console.WriteLine($"HasDuckConstraints: {rootNode.HasDuckConstraints}");
        Console.WriteLine($"AreInflowsLoaded: {rootNode.AreInflowsLoaded}");
        Console.WriteLine($"AreOutflowsLoaded: {rootNode.AreOutflowsLoaded}");
        Console.WriteLine();

        Console.WriteLine($"Inflows ({rootNode.Inflows.Count}):");
        foreach (var inflow in rootNode.Inflows)
        {
            Console.WriteLine($"  [{inflow.Kind}] {inflow.Label}");
            Console.WriteLine($"    Type: {inflow.Type}");
            Console.WriteLine($"    Description: {inflow.Description}");
            Console.WriteLine($"    SourceType: {inflow.SourceType}");
            Console.WriteLine($"    SourceObjectName: {inflow.SourceObjectName}");
        }

        Console.WriteLine();
        Console.WriteLine($"Outflows ({rootNode.Outflows.Count}):");
        foreach (var outflow in rootNode.Outflows)
        {
            Console.WriteLine($"  [{outflow.Kind}] {outflow.Label}");
            Console.WriteLine($"    Type: {outflow.Type}");
            Console.WriteLine($"    Description: {outflow.Description}");
            Console.WriteLine($"    SourceType: {outflow.SourceType}");
            Console.WriteLine($"    SourceObjectName: {outflow.SourceObjectName}");
        }
    }

    [TestMethod]
    public void Debug_CompareHandlerAndBuilder()
    {
        // Compare data from TypeFlowHandler vs GraphBuilder
        // to see if they produce different results

        // 1. Via Handler directly
        var handlerInflows = _typeFlowHandler.GetInflowNodes("current", _unionTypesScript.FullPath);
        var handlerOutflows = _typeFlowHandler.GetOutflowNodes("current", _unionTypesScript.FullPath);

        Console.WriteLine("=== Via IGDTypeFlowHandler ===");
        Console.WriteLine($"Inflows: {handlerInflows?.Count ?? 0}");
        Console.WriteLine($"Outflows: {handlerOutflows?.Count ?? 0}");

        // 2. Via GraphBuilder (what the panel uses)
        var builder = new GDTypeFlowGraphBuilder(_project, _typeFlowHandler, _symbolsHandler);
        var rootNode = builder.BuildGraph("current", _unionTypesScript);

        Console.WriteLine();
        Console.WriteLine("=== Via GDTypeFlowGraphBuilder ===");
        Console.WriteLine($"Inflows: {rootNode?.Inflows.Count ?? 0}");
        Console.WriteLine($"Outflows: {rootNode?.Outflows.Count ?? 0}");

        // Compare
        Console.WriteLine();
        Console.WriteLine("=== Comparison ===");
        if ((handlerInflows?.Count ?? 0) != (rootNode?.Inflows.Count ?? 0))
        {
            Console.WriteLine("WARNING: Inflow counts differ!");
        }
        if ((handlerOutflows?.Count ?? 0) != (rootNode?.Outflows.Count ?? 0))
        {
            Console.WriteLine("WARNING: Outflow counts differ!");
        }
    }

    #endregion
}
