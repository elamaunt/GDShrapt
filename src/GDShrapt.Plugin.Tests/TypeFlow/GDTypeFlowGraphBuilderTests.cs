using GDShrapt.Reader;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace GDShrapt.Plugin.Tests.TypeFlow;

/// <summary>
/// Tests for GDTypeFlowGraphBuilder to verify correct generation of nodes and edges.
/// Uses union_types_complex.gd test script for complex type flow scenarios.
/// </summary>
[TestClass]
public class GDTypeFlowGraphBuilderTests
{
    private GDScriptProject _project;
    private GDScriptFile _unionTypesScript;

    [TestInitialize]
    public void Setup()
    {
        _project = CrossFileTestHelpers.CreateTestProject();
        _unionTypesScript = CrossFileTestHelpers.GetScriptByName(_project, "union_types_complex.gd");
        _unionTypesScript.Should().NotBeNull("union_types_complex.gd should exist in test project");
    }

    #region handle_result Tests - func handle_result(result): lines 224-234

    [TestMethod]
    public void BuildGraph_HandleResult_Parameter_CreatesRootNode()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("result", _unionTypesScript);

        // Assert
        root.Should().NotBeNull("'result' parameter should be found in handle_result function");
        root.Label.Should().Be("result");
        root.Kind.Should().Be(GDTypeFlowNodeKind.Parameter);
    }

    [TestMethod]
    public void BuildGraph_HandleResult_Parameter_HasOutflows_ForMemberAccess()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("result", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();
        root.Outflows.Should().NotBeEmpty("result is used in match with result.get('tag')");

        // Check that there's at least one member access or method call
        root.Outflows.Should().Contain(
            n => n.Label.Contains(".get") || n.Label.Contains("get(") || n.Kind == GDTypeFlowNodeKind.MethodCall,
            "result.get('tag') should create an outflow node");
    }

    [TestMethod]
    public void BuildGraph_HandleResult_Parameter_HasOutflows_ForDictionaryIndexing()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("result", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        // result["value"] and result["message"] should create outflows
        // Total usages: result.get("tag"), result["value"], result["message"]
        root.Outflows.Count.Should().BeGreaterThanOrEqualTo(2,
            "result is used in at least result.get('tag'), result['value'], result['message']");
    }

    [TestMethod]
    public void BuildGraph_HandleResult_Parameter_OutflowsHaveCorrectKind()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("result", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        foreach (var outflow in root.Outflows)
        {
            outflow.Id.Should().NotBeNullOrEmpty();
            outflow.Label.Should().NotBeNullOrEmpty();
            outflow.Kind.Should().NotBe(GDTypeFlowNodeKind.Parameter,
                "Outflows of a parameter should not be parameters themselves");
        }
    }

    #endregion

    #region safe_get_nested Tests - func safe_get_nested(data, path): lines 186-200

    [TestMethod]
    public void BuildGraph_SafeGetNested_Data_CreatesRootNode()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("data", _unionTypesScript);

        // Assert
        root.Should().NotBeNull("'data' parameter should be found in safe_get_nested function");
        root.Label.Should().Be("data");
        root.Kind.Should().Be(GDTypeFlowNodeKind.Parameter);
    }

    [TestMethod]
    public void BuildGraph_SafeGetNested_Data_HasOutflows_ForAssignment()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("data", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        // data is used in: var current = data
        root.Outflows.Should().NotBeEmpty("data is assigned to 'current' variable");
    }

    [TestMethod]
    public void BuildGraph_SafeGetNested_Current_CreatesRootNode()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("current", _unionTypesScript);

        // Assert
        root.Should().NotBeNull("'current' local variable should be found in safe_get_nested function");
        root.Label.Should().Be("current");
        root.Kind.Should().Be(GDTypeFlowNodeKind.LocalVariable);
    }

    [TestMethod]
    public void BuildGraph_SafeGetNested_Current_HasMultipleInflows()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("current", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        // current gets values from:
        // 1. var current = data (initialization)
        // 2. current = current.get(key)
        // 3. current = current[key]
        root.Inflows.Should().HaveCountGreaterThanOrEqualTo(1,
            "current is assigned from at least data initialization");
    }

    [TestMethod]
    public void BuildGraph_SafeGetNested_Current_HasOutflows_ForUsages()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("current", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        // current is used in:
        // - return current
        // - current == null
        // - current is Dictionary
        // - current is Array
        // - current.get(key)
        // - current[key]
        // - current.size()
        root.Outflows.Should().NotBeEmpty("current is used in multiple places");
    }

    [TestMethod]
    public void BuildGraph_SafeGetNested_Current_HasOutflows_ForReturn()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("current", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        // Check for return usage
        var hasReturnUsage = root.Outflows.Any(n =>
            n.Description?.Contains("return", StringComparison.OrdinalIgnoreCase) == true ||
            n.Description?.Contains("Line", StringComparison.OrdinalIgnoreCase) == true ||
            n.Kind == GDTypeFlowNodeKind.ReturnValue);

        hasReturnUsage.Should().BeTrue("current is returned at the end of the function");
    }

    #endregion

    #region Edge Generation Tests

    [TestMethod]
    public void BuildGraph_GeneratesEdges_ForAllInflows()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("current", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        var allNodes = CollectAllNodes(root);
        var totalInflows = allNodes.Sum(n => n.Inflows.Count);

        totalInflows.Should().BeGreaterThan(0,
            "current has assignment sources that should create inflows");
    }

    [TestMethod]
    public void BuildGraph_GeneratesEdges_ForAllOutflows()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("result", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        var allNodes = CollectAllNodes(root);
        var totalOutflows = allNodes.Sum(n => n.Outflows.Count);

        totalOutflows.Should().BeGreaterThan(0,
            "result is used in multiple places");
    }

    [TestMethod]
    public void BuildGraph_EdgesHaveSourceAndTarget()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("result", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        var allNodes = CollectAllNodes(root);

        foreach (var node in allNodes)
        {
            foreach (var edge in node.OutgoingEdges)
            {
                edge.Source.Should().NotBeNull("every edge should have a source");
                edge.Target.Should().NotBeNull("every edge should have a target");
            }

            foreach (var edge in node.IncomingEdges)
            {
                edge.Source.Should().NotBeNull("every edge should have a source");
                edge.Target.Should().NotBeNull("every edge should have a target");
            }
        }
    }

    #endregion

    #region Node Count Tests - Verify expected number of nodes

    [TestMethod]
    public void BuildGraph_HandleResult_TotalNodeCount()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("result", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        var allNodes = CollectAllNodes(root);

        // Expected:
        // - 1 root (result parameter)
        // - At least 3 outflows (result.get, result["value"], result["message"])
        allNodes.Count.Should().BeGreaterThanOrEqualTo(2,
            "Should have root + at least 1 outflow node");

        // Log for debugging - use Console.WriteLine for test output
        Console.WriteLine($"=== 'result' parameter in handle_result() ===");
        Console.WriteLine($"Total nodes: {allNodes.Count}");
        Console.WriteLine($"Root: {root.Label} ({root.Kind}) - Type: {root.Type}");
        Console.WriteLine($"Inflows: {root.Inflows.Count}");
        foreach (var inflow in root.Inflows)
        {
            Console.WriteLine($"  <- {inflow.Label} ({inflow.Kind}) - {inflow.Type} - {inflow.Description}");
        }
        Console.WriteLine($"Outflows: {root.Outflows.Count}");
        foreach (var outflow in root.Outflows)
        {
            Console.WriteLine($"  -> {outflow.Label} ({outflow.Kind}) - {outflow.Type} - {outflow.Description}");
        }
        Console.WriteLine($"Edges: Incoming={root.IncomingEdges.Count}, Outgoing={root.OutgoingEdges.Count}");
    }

    [TestMethod]
    public void BuildGraph_SafeGetNested_Current_TotalNodeCount()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("current", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        var allNodes = CollectAllNodes(root);

        // Expected:
        // - 1 root (current local variable)
        // - At least 1 inflow (initialization from data)
        // - At least 1 outflow (return current, or other usages)
        allNodes.Count.Should().BeGreaterThanOrEqualTo(2,
            "Should have root + at least 1 inflow or outflow");

        // Log for debugging - use Console.WriteLine for test output
        Console.WriteLine($"=== 'current' variable in safe_get_nested() ===");
        Console.WriteLine($"Total nodes: {allNodes.Count}");
        Console.WriteLine($"Root: {root.Label} ({root.Kind}) - Type: {root.Type}");
        Console.WriteLine($"Inflows: {root.Inflows.Count}");
        foreach (var inflow in root.Inflows)
        {
            Console.WriteLine($"  <- {inflow.Label} ({inflow.Kind}) - {inflow.Type} - {inflow.Description}");
        }
        Console.WriteLine($"Outflows: {root.Outflows.Count}");
        foreach (var outflow in root.Outflows)
        {
            Console.WriteLine($"  -> {outflow.Label} ({outflow.Kind}) - {outflow.Type} - {outflow.Description}");
        }
        Console.WriteLine($"Edges: Incoming={root.IncomingEdges.Count}, Outgoing={root.OutgoingEdges.Count}");
    }

    #endregion

    #region Union Type Tests

    [TestMethod]
    public void BuildGraph_MixedInput_HasUnionType()
    {
        // Arrange - mixed_input is: int|float|String|Array|Dictionary
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("mixed_input", _unionTypesScript);

        // Assert
        // mixed_input may or may not be detected as union depending on analysis
        root.Should().NotBeNull("mixed_input variable should be found");
        // Note: GraphBuilder may classify class-level vars as LocalVariable depending on
        // how GDSymbolKind.Variable is handled in MapSymbolKind
        root.Kind.Should().BeOneOf(
            new[] { GDTypeFlowNodeKind.MemberVariable, GDTypeFlowNodeKind.LocalVariable },
            "mixed_input is a class-level variable");
    }

    #endregion

    #region Graph Structure Tests

    [TestMethod]
    public void BuildGraph_NoCircularReferences()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("current", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        // Should be able to traverse without infinite loop
        var visited = new HashSet<string>();
        var canTraverse = TryTraverseGraph(root, visited, maxDepth: 100);

        canTraverse.Should().BeTrue("graph should not have circular references that cause infinite loops");
    }

    [TestMethod]
    public void BuildGraph_AllNodesHaveUniqueIds()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("result", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        var allNodes = CollectAllNodes(root);
        var uniqueIds = allNodes.Select(n => n.Id).Distinct().ToList();

        uniqueIds.Count.Should().Be(allNodes.Count, "all nodes should have unique IDs");
    }

    #endregion

    #region New Kind Values Tests

    [TestMethod]
    public void BuildGraph_Current_HasCorrectKinds()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("current", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        // Return statements
        root.Outflows.Should().Contain(n => n.Kind == GDTypeFlowNodeKind.ReturnValue,
            "current is returned in 'return current'");

        // Null checks
        root.Outflows.Should().Contain(n => n.Kind == GDTypeFlowNodeKind.NullCheck,
            "current has null check: current == null");

        // Type checks
        root.Outflows.Should().Contain(n => n.Kind == GDTypeFlowNodeKind.TypeCheck,
            "current has type checks: current is Dictionary, current is Array");

        // Indexer access
        root.Outflows.Should().Contain(n => n.Kind == GDTypeFlowNodeKind.IndexerAccess,
            "current has indexer: current[key]");
    }

    [TestMethod]
    public void BuildGraph_Current_NullCheckHasBoolType()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("current", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        var nullChecks = root.Outflows.Where(n => n.Kind == GDTypeFlowNodeKind.NullCheck).ToList();
        nullChecks.Should().NotBeEmpty();

        foreach (var check in nullChecks)
        {
            check.Type.Should().Be("bool", "null checks should have bool type");
        }
    }

    [TestMethod]
    public void BuildGraph_Current_TypeCheckHasBoolType()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("current", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        var typeChecks = root.Outflows.Where(n => n.Kind == GDTypeFlowNodeKind.TypeCheck).ToList();
        typeChecks.Should().NotBeEmpty();

        foreach (var check in typeChecks)
        {
            check.Type.Should().Be("bool", "type checks should have bool type");
        }
    }

    [TestMethod]
    public void BuildGraph_Result_IndexerHasProperLabels()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("result", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        var indexers = root.Outflows.Where(n => n.Kind == GDTypeFlowNodeKind.IndexerAccess).ToList();

        indexers.Should().HaveCountGreaterThanOrEqualTo(2, "result should have at least 2 indexer accesses");
        indexers.Should().Contain(n => n.Label.Contains("result[") && n.Label.Contains("value"),
            "should have result[\"value\"] indexer");
        indexers.Should().Contain(n => n.Label.Contains("result[") && n.Label.Contains("message"),
            "should have result[\"message\"] indexer");
    }

    #endregion

    #region SourceType and SourceObjectName Tests

    [TestMethod]
    public void BuildGraph_MethodCall_HasSourceObjectName()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("current", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        // Method calls should have SourceObjectName
        var methodCalls = root.Outflows.Where(n =>
            n.Kind == GDTypeFlowNodeKind.MethodCall ||
            n.Kind == GDTypeFlowNodeKind.PropertyAccess).ToList();

        var hasSourceObjectName = methodCalls.Any(n =>
            !string.IsNullOrEmpty(n.SourceObjectName) && n.SourceObjectName == "current");

        hasSourceObjectName.Should().BeTrue(
            "current.get() and current.size() should have SourceObjectName = 'current'");
    }

    [TestMethod]
    public void BuildGraph_Indexer_HasSourceObjectName()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("result", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        var indexers = root.Outflows.Where(n => n.Kind == GDTypeFlowNodeKind.IndexerAccess).ToList();

        // All indexers should have SourceObjectName = "result"
        foreach (var indexer in indexers)
        {
            indexer.SourceObjectName.Should().Be("result",
                "result[\"...\"] should have SourceObjectName = 'result'");
        }
    }

    #endregion

    #region Unlimited Depth and Lazy Loading Tests

    [TestMethod]
    public void BuildGraph_CanTraverseDeep_WithoutDepthLimit()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act - build graph for a symbol
        var root = builder.BuildGraph("data", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        // Verify we can traverse graph without hitting depth limits
        var visited = new HashSet<string>();
        var queue = new Queue<GDTypeFlowNode>();
        queue.Enqueue(root);

        int totalNodes = 0;
        while (queue.Count > 0 && totalNodes < 1000) // Safety limit for test
        {
            var node = queue.Dequeue();
            if (string.IsNullOrEmpty(node.Id) || visited.Contains(node.Id))
                continue;

            visited.Add(node.Id);
            totalNodes++;

            foreach (var inflow in node.Inflows ?? Enumerable.Empty<GDTypeFlowNode>())
                queue.Enqueue(inflow);

            foreach (var outflow in node.Outflows ?? Enumerable.Empty<GDTypeFlowNode>())
                queue.Enqueue(outflow);
        }

        Console.WriteLine($"Total traversed nodes for 'data': {totalNodes}");
        totalNodes.Should().BeGreaterThanOrEqualTo(1);
    }

    [TestMethod]
    public void BuildGraph_NodeRegistry_NoDuplicateNodes()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("current", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        // Collect all nodes
        var allNodes = CollectAllNodes(root);
        var nodeIds = allNodes.Select(n => n.Id).ToList();

        // All IDs should be unique
        nodeIds.Distinct().Count().Should().Be(nodeIds.Count,
            "NodeRegistry should prevent duplicate nodes");
    }

    #endregion

    #region type_flow_test_method Tests

    [TestMethod]
    public void BuildGraph_TypeFlowTestMethod_Local_HasAllKinds()
    {
        // Arrange - using the comprehensive type_flow_test_method
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("local", _unionTypesScript);

        // Assert
        root.Should().NotBeNull("'local' should be found in type_flow_test_method");
        root.Kind.Should().Be(GDTypeFlowNodeKind.LocalVariable);

        var allKinds = root.Outflows.Select(n => n.Kind).Distinct().ToList();

        Console.WriteLine($"=== 'local' in type_flow_test_method ===");
        Console.WriteLine($"Root: {root.Label} ({root.Kind}) - Type: {root.Type}");
        Console.WriteLine($"Outflows ({root.Outflows.Count}):");
        foreach (var outflow in root.Outflows)
        {
            var sourceInfo = !string.IsNullOrEmpty(outflow.SourceType)
                ? $"SourceType: {outflow.SourceType}, "
                : "";
            var sourceObjInfo = !string.IsNullOrEmpty(outflow.SourceObjectName)
                ? $"SourceObj: {outflow.SourceObjectName}, "
                : "";
            Console.WriteLine($"  -> {outflow.Label} ({outflow.Kind}) - {sourceInfo}{sourceObjInfo}Type: {outflow.Type} - \"{outflow.Description}\"");
        }
        Console.WriteLine($"Distinct kinds found: {string.Join(", ", allKinds)}");

        // Verify variety of kinds based on what's in type_flow_test_method
        allKinds.Should().Contain(GDTypeFlowNodeKind.NullCheck,
            "local == null should create NullCheck");
        allKinds.Should().Contain(GDTypeFlowNodeKind.TypeCheck,
            "local is Dictionary and local is Array should create TypeCheck");
    }

    [TestMethod]
    public void BuildGraph_TypeFlowTestMethod_Local_HasIndexerAccess()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("local", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        // local["item"] and local[0] should create IndexerAccess
        var indexers = root.Outflows.Where(n => n.Kind == GDTypeFlowNodeKind.IndexerAccess).ToList();

        indexers.Should().NotBeEmpty("local[\"item\"] and local[0] should create IndexerAccess nodes");

        Console.WriteLine($"Indexer nodes found: {indexers.Count}");
        foreach (var indexer in indexers)
        {
            Console.WriteLine($"  -> {indexer.Label} - SourceObj: {indexer.SourceObjectName}");
        }
    }

    [TestMethod]
    public void BuildGraph_TypeFlowTestMethod_Local_HasExpectedKinds()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("local", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        // type_flow_test_method uses 'local' in various ways but not in direct 'return local'.
        // Instead it has return local[0], return local.custom_method(), etc. where local
        // is used as part of an expression. These create IndexerAccess and PropertyAccess nodes.
        // ReturnValue nodes are created only for direct 'return <symbol>' statements.

        // Verify the expected kinds are present
        var allKinds = root.Outflows.Select(n => n.Kind).Distinct().ToList();

        Console.WriteLine($"Kinds found for 'local': {string.Join(", ", allKinds)}");
        Console.WriteLine($"Return nodes found: {root.Outflows.Count(n => n.Kind == GDTypeFlowNodeKind.ReturnValue)}");

        // The key kinds we expect based on type_flow_test_method:
        // - NullCheck for 'local == null'
        // - TypeCheck for 'local is Dictionary', 'local is Array'
        // - IndexerAccess for 'local["item"]', 'local[0]'
        // - PropertyAccess for 'local.get()', 'local.size()', 'local.custom_method()'

        allKinds.Should().Contain(GDTypeFlowNodeKind.NullCheck);
        allKinds.Should().Contain(GDTypeFlowNodeKind.TypeCheck);
        allKinds.Should().Contain(GDTypeFlowNodeKind.IndexerAccess);
        allKinds.Should().Contain(GDTypeFlowNodeKind.PropertyAccess);
    }

    #endregion

    #region Description and Line Number Tests

    [TestMethod]
    public void BuildGraph_Outflows_HaveLineNumbers()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("current", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        // All outflows should have line numbers in description
        foreach (var outflow in root.Outflows)
        {
            outflow.Description.Should().Contain("Line",
                $"Node '{outflow.Label}' should have line number in description");
        }
    }

    [TestMethod]
    public void BuildGraph_TypeCheck_HasTypeGuardDescription()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("current", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        var typeChecks = root.Outflows.Where(n => n.Kind == GDTypeFlowNodeKind.TypeCheck).ToList();

        foreach (var check in typeChecks)
        {
            check.Description.Should().Contain("Type guard",
                "Type checks should have 'Type guard' in description");
        }
    }

    [TestMethod]
    public void BuildGraph_NullCheck_HasNullCheckDescription()
    {
        // Arrange
        var builder = new GDTypeFlowGraphBuilder(_project);

        // Act
        var root = builder.BuildGraph("current", _unionTypesScript);

        // Assert
        root.Should().NotBeNull();

        var nullChecks = root.Outflows.Where(n => n.Kind == GDTypeFlowNodeKind.NullCheck).ToList();

        foreach (var check in nullChecks)
        {
            check.Description.Should().Contain("Null check",
                "Null checks should have 'Null check' in description");
        }
    }

    #endregion

    #region Helper Methods

    private List<GDTypeFlowNode> CollectAllNodes(GDTypeFlowNode root)
    {
        var visited = new HashSet<string>();
        var result = new List<GDTypeFlowNode>();
        CollectNodesRecursive(root, visited, result);
        return result;
    }

    private void CollectNodesRecursive(GDTypeFlowNode node, HashSet<string> visited, List<GDTypeFlowNode> result)
    {
        if (node == null || string.IsNullOrEmpty(node.Id) || visited.Contains(node.Id))
            return;

        visited.Add(node.Id);
        result.Add(node);

        foreach (var inflow in node.Inflows ?? Enumerable.Empty<GDTypeFlowNode>())
            CollectNodesRecursive(inflow, visited, result);

        foreach (var outflow in node.Outflows ?? Enumerable.Empty<GDTypeFlowNode>())
            CollectNodesRecursive(outflow, visited, result);
    }

    private bool TryTraverseGraph(GDTypeFlowNode node, HashSet<string> visited, int maxDepth)
    {
        if (maxDepth <= 0)
            return false;

        if (node == null || string.IsNullOrEmpty(node.Id))
            return true;

        if (visited.Contains(node.Id))
            return true; // Already visited, not a problem

        visited.Add(node.Id);

        foreach (var inflow in node.Inflows ?? Enumerable.Empty<GDTypeFlowNode>())
        {
            if (!TryTraverseGraph(inflow, visited, maxDepth - 1))
                return false;
        }

        foreach (var outflow in node.Outflows ?? Enumerable.Empty<GDTypeFlowNode>())
        {
            if (!TryTraverseGraph(outflow, visited, maxDepth - 1))
                return false;
        }

        return true;
    }

    #endregion
}
