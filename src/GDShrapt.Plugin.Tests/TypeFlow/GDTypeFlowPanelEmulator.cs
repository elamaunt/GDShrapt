using GDShrapt.Semantics;
using GDShrapt.CLI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDShrapt.Plugin.Tests.TypeFlow;

/// <summary>
/// Emulates the TypeFlow panel UI for testing purposes.
/// Allows selecting symbols, navigating through the type flow graph,
/// and verifying what a developer would see when using the TypeFlow panel.
/// </summary>
internal class GDTypeFlowPanelEmulator
{
    private readonly GDScriptProject _project;
    private readonly GDTypeFlowGraphBuilder _builder;
    private readonly Stack<NavigationEntry> _navigationStack = new();
    private readonly HashSet<string> _visitedNodesInSession = new();

    /// <summary>
    /// Creates a new TypeFlow panel emulator for the given project.
    /// </summary>
    public GDTypeFlowPanelEmulator(GDScriptProject project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));

        // Initialize service registry and load base module to get handlers
        var registry = new GDServiceRegistry();
        registry.LoadModules(_project, new GDBaseModule());

        var typeFlowHandler = registry.GetService<IGDTypeFlowHandler>();
        var symbolsHandler = registry.GetService<IGDSymbolsHandler>();

        _builder = new GDTypeFlowGraphBuilder(project, typeFlowHandler, symbolsHandler);
    }

    #region State Properties

    /// <summary>
    /// The currently focused node in the panel.
    /// </summary>
    public GDTypeFlowNode CurrentNode { get; private set; }

    /// <summary>
    /// The root node (first symbol shown in this session).
    /// </summary>
    public GDTypeFlowNode RootNode { get; private set; }

    /// <summary>
    /// List of inflow nodes (type sources) for the current node.
    /// </summary>
    public IReadOnlyList<GDTypeFlowNode> Inflows => CurrentNode?.Inflows ?? new List<GDTypeFlowNode>();

    /// <summary>
    /// List of outflow nodes (type usages) for the current node.
    /// </summary>
    public IReadOnlyList<GDTypeFlowNode> Outflows => CurrentNode?.Outflows ?? new List<GDTypeFlowNode>();

    /// <summary>
    /// The navigation stack showing browsing history.
    /// </summary>
    public IReadOnlyCollection<NavigationEntry> NavigationHistory => _navigationStack.ToList().AsReadOnly();

    /// <summary>
    /// Whether the user can navigate back.
    /// </summary>
    public bool CanGoBack => _navigationStack.Count > 1;

    /// <summary>
    /// The name of the currently displayed symbol.
    /// </summary>
    public string CurrentSymbolName => CurrentNode?.Label;

    /// <summary>
    /// Whether the current node is a union type.
    /// </summary>
    public bool IsCurrentNodeUnion => CurrentNode?.IsUnionType ?? false;

    /// <summary>
    /// Whether the current node has duck type constraints.
    /// </summary>
    public bool HasCurrentNodeDuckConstraints => CurrentNode?.HasDuckConstraints ?? false;

    #endregion

    #region Navigation Methods

    /// <summary>
    /// Shows the type flow for a symbol from a script.
    /// This is like clicking on a symbol in the editor and selecting "Show Type Flow".
    /// </summary>
    /// <param name="symbolName">The name of the symbol to show.</param>
    /// <param name="script">The script containing the symbol.</param>
    /// <returns>True if the symbol was found and displayed.</returns>
    public bool ShowForSymbol(string symbolName, GDScriptFile script)
    {
        if (string.IsNullOrEmpty(symbolName) || script == null)
            return false;

        var node = _builder.BuildGraph(symbolName, script);
        if (node == null)
            return false;

        // Clear previous session
        _navigationStack.Clear();
        _visitedNodesInSession.Clear();

        // Set as root and current
        RootNode = node;
        CurrentNode = node;

        // Add to navigation stack
        _navigationStack.Push(new NavigationEntry(node, NavigationAction.Initial));
        _visitedNodesInSession.Add(node.Id);

        return true;
    }

    /// <summary>
    /// Navigates to a specific node (like clicking on an inflow or outflow).
    /// </summary>
    /// <param name="node">The node to navigate to.</param>
    /// <returns>True if navigation was successful.</returns>
    public bool NavigateToNode(GDTypeFlowNode node)
    {
        if (node == null)
            return false;

        // Ensure the node has its inflows and outflows loaded
        EnsureNodeLoaded(node);

        // Track visited nodes for cycle detection
        _visitedNodesInSession.Add(node.Id);

        // Push to navigation stack
        _navigationStack.Push(new NavigationEntry(node, NavigationAction.ClickedNode));
        CurrentNode = node;

        return true;
    }

    /// <summary>
    /// Navigates to an inflow by index.
    /// </summary>
    public bool NavigateToInflow(int index)
    {
        if (index < 0 || index >= Inflows.Count)
            return false;

        return NavigateToNode(Inflows[index]);
    }

    /// <summary>
    /// Navigates to an outflow by index.
    /// </summary>
    public bool NavigateToOutflow(int index)
    {
        if (index < 0 || index >= Outflows.Count)
            return false;

        return NavigateToNode(Outflows[index]);
    }

    /// <summary>
    /// Navigates to a node by its label (like clicking on a label in the UI).
    /// Searches in both inflows and outflows.
    /// </summary>
    public bool NavigateToLabel(string label)
    {
        var node = GetNodeByLabel(label);
        if (node == null)
            return false;

        return NavigateToNode(node);
    }

    /// <summary>
    /// Goes back to the previous node in navigation history.
    /// </summary>
    public bool GoBack()
    {
        if (!CanGoBack)
            return false;

        // Pop current
        _navigationStack.Pop();

        // Get previous
        var previous = _navigationStack.Peek();
        CurrentNode = previous.Node;

        return true;
    }

    /// <summary>
    /// Navigates through a chain of labels.
    /// Useful for testing deep navigation paths.
    /// </summary>
    /// <param name="labels">Labels to navigate through in order.</param>
    /// <returns>True if all navigations succeeded.</returns>
    public bool NavigateThrough(params string[] labels)
    {
        foreach (var label in labels)
        {
            if (!NavigateToLabel(label))
                return false;
        }
        return true;
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Finds a node by its label in both inflows and outflows.
    /// </summary>
    public GDTypeFlowNode GetNodeByLabel(string label)
    {
        if (CurrentNode == null || string.IsNullOrEmpty(label))
            return null;

        // Search in inflows
        var inflow = Inflows.FirstOrDefault(n =>
            n.Label != null && n.Label.Contains(label, StringComparison.OrdinalIgnoreCase));
        if (inflow != null)
            return inflow;

        // Search in outflows
        return Outflows.FirstOrDefault(n =>
            n.Label != null && n.Label.Contains(label, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the first outflow node of a specific kind.
    /// </summary>
    public GDTypeFlowNode GetOutflowByKind(GDTypeFlowNodeKind kind)
    {
        return Outflows.FirstOrDefault(n => n.Kind == kind);
    }

    /// <summary>
    /// Gets all outflow nodes of a specific kind.
    /// </summary>
    public IEnumerable<GDTypeFlowNode> GetOutflowsByKind(GDTypeFlowNodeKind kind)
    {
        return Outflows.Where(n => n.Kind == kind);
    }

    /// <summary>
    /// Gets the first inflow node of a specific kind.
    /// </summary>
    public GDTypeFlowNode GetInflowByKind(GDTypeFlowNodeKind kind)
    {
        return Inflows.FirstOrDefault(n => n.Kind == kind);
    }

    /// <summary>
    /// Gets all inflow nodes of a specific kind.
    /// </summary>
    public IEnumerable<GDTypeFlowNode> GetInflowsByKind(GDTypeFlowNodeKind kind)
    {
        return Inflows.Where(n => n.Kind == kind);
    }

    /// <summary>
    /// Gets all method call outflows.
    /// </summary>
    public IEnumerable<GDTypeFlowNode> GetMethodCallOutflows()
    {
        return GetOutflowsByKind(GDTypeFlowNodeKind.MethodCall);
    }

    /// <summary>
    /// Gets all indexer access outflows.
    /// </summary>
    public IEnumerable<GDTypeFlowNode> GetIndexerOutflows()
    {
        return GetOutflowsByKind(GDTypeFlowNodeKind.IndexerAccess);
    }

    /// <summary>
    /// Gets all type check outflows (x is Type).
    /// </summary>
    public IEnumerable<GDTypeFlowNode> GetTypeCheckOutflows()
    {
        return GetOutflowsByKind(GDTypeFlowNodeKind.TypeCheck);
    }

    /// <summary>
    /// Gets all null check outflows (x == null).
    /// </summary>
    public IEnumerable<GDTypeFlowNode> GetNullCheckOutflows()
    {
        return GetOutflowsByKind(GDTypeFlowNodeKind.NullCheck);
    }

    /// <summary>
    /// Gets all return value outflows.
    /// </summary>
    public IEnumerable<GDTypeFlowNode> GetReturnOutflows()
    {
        return GetOutflowsByKind(GDTypeFlowNodeKind.ReturnValue);
    }

    /// <summary>
    /// Gets all comparison outflows.
    /// </summary>
    public IEnumerable<GDTypeFlowNode> GetComparisonOutflows()
    {
        return GetOutflowsByKind(GDTypeFlowNodeKind.Comparison);
    }

    /// <summary>
    /// Gets all outflows that have a specific source object name.
    /// </summary>
    public IEnumerable<GDTypeFlowNode> GetOutflowsWithSourceObject(string name)
    {
        return Outflows.Where(n =>
            n.SourceObjectName != null &&
            n.SourceObjectName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all outflows that have a specific source type.
    /// </summary>
    public IEnumerable<GDTypeFlowNode> GetOutflowsWithSourceType(string type)
    {
        return Outflows.Where(n =>
            n.SourceType != null &&
            n.SourceType.Equals(type, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all inflows that have a specific label containing text.
    /// </summary>
    public IEnumerable<GDTypeFlowNode> GetInflowsContaining(string text)
    {
        return Inflows.Where(n =>
            n.Label != null &&
            n.Label.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all outflows that have a specific label containing text.
    /// </summary>
    public IEnumerable<GDTypeFlowNode> GetOutflowsContaining(string text)
    {
        return Outflows.Where(n =>
            n.Label != null &&
            n.Label.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Cycle Detection

    /// <summary>
    /// Checks if the current graph has cycles.
    /// </summary>
    public bool HasCycles()
    {
        if (CurrentNode == null)
            return false;

        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        return HasCyclesDfs(CurrentNode, visited, recursionStack);
    }

    /// <summary>
    /// Checks if navigating to a target node would create a cycle.
    /// </summary>
    public bool WouldCreateCycle(GDTypeFlowNode target)
    {
        if (target == null)
            return false;

        return _visitedNodesInSession.Contains(target.Id);
    }

    /// <summary>
    /// Checks if a specific node has already been visited in this session.
    /// </summary>
    public bool IsNodeVisited(GDTypeFlowNode node)
    {
        return node != null && _visitedNodesInSession.Contains(node.Id);
    }

    private bool HasCyclesDfs(GDTypeFlowNode node, HashSet<string> visited, HashSet<string> recursionStack)
    {
        if (node == null || string.IsNullOrEmpty(node.Id))
            return false;

        if (recursionStack.Contains(node.Id))
            return true;

        if (visited.Contains(node.Id))
            return false;

        visited.Add(node.Id);
        recursionStack.Add(node.Id);

        foreach (var outflow in node.Outflows)
        {
            if (HasCyclesDfs(outflow, visited, recursionStack))
                return true;
        }

        recursionStack.Remove(node.Id);
        return false;
    }

    #endregion

    #region Graph Traversal

    /// <summary>
    /// Collects all nodes reachable from the current node.
    /// </summary>
    /// <param name="maxNodes">Maximum number of nodes to collect (safety limit).</param>
    public IReadOnlyList<GDTypeFlowNode> CollectAllReachableNodes(int maxNodes = 1000)
    {
        if (CurrentNode == null)
            return new List<GDTypeFlowNode>();

        var visited = new HashSet<string>();
        var result = new List<GDTypeFlowNode>();
        var queue = new Queue<GDTypeFlowNode>();
        queue.Enqueue(CurrentNode);

        while (queue.Count > 0 && result.Count < maxNodes)
        {
            var node = queue.Dequeue();
            if (node == null || string.IsNullOrEmpty(node.Id) || visited.Contains(node.Id))
                continue;

            visited.Add(node.Id);
            result.Add(node);

            // Ensure node is loaded
            EnsureNodeLoaded(node);

            foreach (var inflow in node.Inflows)
                queue.Enqueue(inflow);

            foreach (var outflow in node.Outflows)
                queue.Enqueue(outflow);
        }

        return result;
    }

    /// <summary>
    /// Gets the count of all node kinds in the graph.
    /// </summary>
    public Dictionary<GDTypeFlowNodeKind, int> GetNodeKindCounts()
    {
        var nodes = CollectAllReachableNodes();
        return nodes
            .GroupBy(n => n.Kind)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Gets all unique types referenced in the graph.
    /// </summary>
    public IReadOnlyList<string> GetAllReferencedTypes()
    {
        var nodes = CollectAllReachableNodes();
        return nodes
            .Select(n => n.Type)
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct()
            .OrderBy(t => t)
            .ToList();
    }

    #endregion

    #region Display Methods

    /// <summary>
    /// Gets a display string showing the current state of the panel.
    /// Similar to what a developer would see in the UI.
    /// </summary>
    public string GetStateDisplay()
    {
        if (CurrentNode == null)
            return "No symbol selected";

        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"=== {CurrentNode.Label} ({CurrentNode.Kind}) ===");
        sb.AppendLine($"Type: {CurrentNode.Type}");
        sb.AppendLine($"Confidence: {CurrentNode.Confidence:P0}");

        if (CurrentNode.IsUnionType)
            sb.AppendLine($"Union Type: Yes");

        if (CurrentNode.HasDuckConstraints)
            sb.AppendLine($"Duck Constraints: Yes");

        if (!string.IsNullOrEmpty(CurrentNode.Description))
            sb.AppendLine($"Description: {CurrentNode.Description}");

        // Inflows
        sb.AppendLine();
        sb.AppendLine($"Inflows ({Inflows.Count}):");
        foreach (var inflow in Inflows)
        {
            var sourceInfo = !string.IsNullOrEmpty(inflow.SourceType)
                ? $" [Source: {inflow.SourceType}]"
                : "";
            sb.AppendLine($"  <- {inflow.Label} ({inflow.Kind}) - {inflow.Type}{sourceInfo}");
            if (!string.IsNullOrEmpty(inflow.Description))
                sb.AppendLine($"     {inflow.Description}");
        }

        // Outflows
        sb.AppendLine();
        sb.AppendLine($"Outflows ({Outflows.Count}):");
        foreach (var outflow in Outflows)
        {
            var sourceInfo = !string.IsNullOrEmpty(outflow.SourceType)
                ? $" [Source: {outflow.SourceType}]"
                : "";
            var sourceObjInfo = !string.IsNullOrEmpty(outflow.SourceObjectName)
                ? $" (on {outflow.SourceObjectName})"
                : "";
            sb.AppendLine($"  -> {outflow.Label} ({outflow.Kind}) - {outflow.Type}{sourceInfo}{sourceObjInfo}");
            if (!string.IsNullOrEmpty(outflow.Description))
                sb.AppendLine($"     {outflow.Description}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets a compact one-line summary of the current state.
    /// </summary>
    public string GetCompactSummary()
    {
        if (CurrentNode == null)
            return "No symbol";

        return $"{CurrentNode.Label}: {CurrentNode.Type} ({CurrentNode.Kind}) - " +
               $"{Inflows.Count} inflows, {Outflows.Count} outflows";
    }

    /// <summary>
    /// Gets a display of the navigation history.
    /// </summary>
    public string GetNavigationHistoryDisplay()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Navigation History:");

        var entries = _navigationStack.Reverse().ToList();
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var marker = i == entries.Count - 1 ? ">> " : "   ";
            sb.AppendLine($"{marker}{i + 1}. {entry.Node.Label} ({entry.Action})");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Prints the current state to console (for debugging tests).
    /// </summary>
    public void PrintState()
    {
        Console.WriteLine(GetStateDisplay());
    }

    /// <summary>
    /// Prints navigation history to console.
    /// </summary>
    public void PrintNavigationHistory()
    {
        Console.WriteLine(GetNavigationHistoryDisplay());
    }

    #endregion

    #region Helper Methods

    private void EnsureNodeLoaded(GDTypeFlowNode node)
    {
        if (node == null)
            return;

        if (!node.AreInflowsLoaded)
        {
            _builder.LoadInflowsFor(node);
        }

        if (!node.AreOutflowsLoaded)
        {
            _builder.LoadOutflowsFor(node);
        }
    }

    #endregion
}

/// <summary>
/// An entry in the navigation stack.
/// </summary>
internal class NavigationEntry
{
    public GDTypeFlowNode Node { get; }
    public NavigationAction Action { get; }
    public DateTime Timestamp { get; }

    public NavigationEntry(GDTypeFlowNode node, NavigationAction action)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
        Action = action;
        Timestamp = DateTime.Now;
    }
}

/// <summary>
/// The action that caused a navigation entry.
/// </summary>
internal enum NavigationAction
{
    /// <summary>
    /// Initial symbol selection.
    /// </summary>
    Initial,

    /// <summary>
    /// User clicked on a node label.
    /// </summary>
    ClickedNode,

    /// <summary>
    /// User navigated back.
    /// </summary>
    Back
}
