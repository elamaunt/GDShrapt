namespace GDShrapt.Plugin;

/// <summary>
/// Computes layout positions for nodes in a type flow graph.
/// Uses a layered graph layout algorithm (Sugiyama-style).
/// </summary>
internal class GDTypeFlowLayoutEngine
{
    /// <summary>
    /// Horizontal spacing between nodes at the same level.
    /// </summary>
    public float HorizontalSpacing { get; set; } = 40f;

    /// <summary>
    /// Vertical spacing between levels.
    /// </summary>
    public float VerticalSpacing { get; set; } = 80f;

    /// <summary>
    /// Default node width.
    /// </summary>
    public float NodeWidth { get; set; } = 160f;

    /// <summary>
    /// Default node height.
    /// </summary>
    public float NodeHeight { get; set; } = 70f;

    /// <summary>
    /// Maximum number of inflow levels to display.
    /// </summary>
    public int MaxInflowLevels { get; set; } = 3;

    /// <summary>
    /// Maximum number of outflow levels to display.
    /// </summary>
    public int MaxOutflowLevels { get; set; } = 2;

    /// <summary>
    /// Computes layout for all nodes starting from the focus node.
    /// Assigns Position, Size, and Level to each node.
    /// </summary>
    /// <param name="focusNode">The central node (level 0).</param>
    /// <returns>All nodes with computed positions, or empty if focusNode is null.</returns>
    public List<GDTypeFlowNode> ComputeLayout(GDTypeFlowNode focusNode)
    {
        if (focusNode == null)
            return new List<GDTypeFlowNode>();

        var allNodes = new List<GDTypeFlowNode>();
        var visitedIds = new HashSet<string>();

        // Assign levels using BFS
        AssignLevels(focusNode, allNodes, visitedIds);

        // Group nodes by level
        var nodesByLevel = allNodes
            .GroupBy(n => n.Level)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Compute positions for each level
        ComputePositions(nodesByLevel);

        // Create edges
        CreateEdges(allNodes);

        return allNodes;
    }

    /// <summary>
    /// Assigns levels to nodes using BFS from focus node.
    /// Focus = 0, Inflows = negative, Outflows = positive.
    /// </summary>
    private void AssignLevels(GDTypeFlowNode focusNode, List<GDTypeFlowNode> allNodes, HashSet<string> visitedIds)
    {
        // Focus node is level 0
        focusNode.Level = 0;
        focusNode.Size = new Vector2(NodeWidth, NodeHeight);
        allNodes.Add(focusNode);
        visitedIds.Add(focusNode.Id);

        // BFS for inflows (negative levels)
        var inflowQueue = new Queue<(GDTypeFlowNode node, int level)>();
        foreach (var inflow in focusNode.Inflows)
        {
            if (inflow != null && !visitedIds.Contains(inflow.Id))
            {
                inflowQueue.Enqueue((inflow, -1));
            }
        }

        while (inflowQueue.Count > 0)
        {
            var (node, level) = inflowQueue.Dequeue();

            if (visitedIds.Contains(node.Id))
                continue;

            if (Math.Abs(level) > MaxInflowLevels)
                continue;

            node.Level = level;
            node.Size = new Vector2(NodeWidth, NodeHeight);
            allNodes.Add(node);
            visitedIds.Add(node.Id);

            // Add this node's inflows
            foreach (var inflow in node.Inflows)
            {
                if (inflow != null && !visitedIds.Contains(inflow.Id))
                {
                    inflowQueue.Enqueue((inflow, level - 1));
                }
            }
        }

        // BFS for outflows (positive levels)
        var outflowQueue = new Queue<(GDTypeFlowNode node, int level)>();
        foreach (var outflow in focusNode.Outflows)
        {
            if (outflow != null && !visitedIds.Contains(outflow.Id))
            {
                outflowQueue.Enqueue((outflow, 1));
            }
        }

        while (outflowQueue.Count > 0)
        {
            var (node, level) = outflowQueue.Dequeue();

            if (visitedIds.Contains(node.Id))
                continue;

            if (level > MaxOutflowLevels)
                continue;

            node.Level = level;
            node.Size = new Vector2(NodeWidth, NodeHeight);
            allNodes.Add(node);
            visitedIds.Add(node.Id);

            // Add this node's outflows
            foreach (var outflow in node.Outflows)
            {
                if (outflow != null && !visitedIds.Contains(outflow.Id))
                {
                    outflowQueue.Enqueue((outflow, level + 1));
                }
            }
        }
    }

    /// <summary>
    /// Computes X/Y positions for nodes, centering each level horizontally.
    /// </summary>
    private void ComputePositions(Dictionary<int, List<GDTypeFlowNode>> nodesByLevel)
    {
        // Find the maximum width at any level
        float maxLevelWidth = 0;
        foreach (var kvp in nodesByLevel)
        {
            var levelWidth = kvp.Value.Count * (NodeWidth + HorizontalSpacing) - HorizontalSpacing;
            maxLevelWidth = Math.Max(maxLevelWidth, levelWidth);
        }

        // Position nodes
        foreach (var kvp in nodesByLevel)
        {
            var level = kvp.Key;
            var nodes = kvp.Value;

            // Y position based on level (level 0 in the middle)
            var y = level * (NodeHeight + VerticalSpacing);

            // Center nodes horizontally
            var levelWidth = nodes.Count * (NodeWidth + HorizontalSpacing) - HorizontalSpacing;
            var startX = (maxLevelWidth - levelWidth) / 2;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                node.Position = new Vector2(startX + i * (NodeWidth + HorizontalSpacing), y);
            }
        }

        // Normalize positions to start from origin
        NormalizePositions(nodesByLevel.Values.SelectMany(x => x).ToList());
    }

    /// <summary>
    /// Normalizes positions so that the top-left is at (0, 0).
    /// </summary>
    private void NormalizePositions(List<GDTypeFlowNode> nodes)
    {
        if (nodes.Count == 0)
            return;

        var minX = nodes.Min(n => n.Position.X);
        var minY = nodes.Min(n => n.Position.Y);

        foreach (var node in nodes)
        {
            node.Position = new Vector2(
                node.Position.X - minX,
                node.Position.Y - minY
            );
        }
    }

    /// <summary>
    /// Creates edge objects between connected nodes.
    /// </summary>
    private void CreateEdges(List<GDTypeFlowNode> allNodes)
    {
        var nodeById = allNodes.ToDictionary(n => n.Id);
        int edgeId = 0;

        foreach (var node in allNodes)
        {
            // Clear existing edges
            node.IncomingEdges.Clear();
            node.OutgoingEdges.Clear();
        }

        foreach (var node in allNodes)
        {
            // Create edges for inflows
            foreach (var inflow in node.Inflows)
            {
                if (inflow == null || !nodeById.ContainsKey(inflow.Id))
                    continue;

                var sourceNode = nodeById[inflow.Id];
                var edge = new GDTypeFlowEdge
                {
                    Id = $"edge_{edgeId++}",
                    Source = sourceNode,
                    Target = node,
                    Kind = DetermineEdgeKind(sourceNode, node),
                    Confidence = Math.Min(sourceNode.Confidence, node.Confidence)
                };

                sourceNode.OutgoingEdges.Add(edge);
                node.IncomingEdges.Add(edge);
            }

            // Create edges for outflows
            foreach (var outflow in node.Outflows)
            {
                if (outflow == null || !nodeById.ContainsKey(outflow.Id))
                    continue;

                // Check if edge already exists
                var targetNode = nodeById[outflow.Id];
                if (node.OutgoingEdges.Any(e => e.Target?.Id == targetNode.Id))
                    continue;

                var edge = new GDTypeFlowEdge
                {
                    Id = $"edge_{edgeId++}",
                    Source = node,
                    Target = targetNode,
                    Kind = DetermineEdgeKind(node, targetNode),
                    Confidence = Math.Min(node.Confidence, targetNode.Confidence)
                };

                node.OutgoingEdges.Add(edge);
                targetNode.IncomingEdges.Add(edge);
            }
        }
    }

    /// <summary>
    /// Determines the edge kind based on source and target node kinds.
    /// </summary>
    private GDTypeFlowEdgeKind DetermineEdgeKind(GDTypeFlowNode source, GDTypeFlowNode target)
    {
        // Union member edge
        if (source.IsUnionType || target.Kind == GDTypeFlowNodeKind.TypeAnnotation)
            return GDTypeFlowEdgeKind.TypeFlow;

        // Duck constraint edge
        if (source.HasDuckConstraints || target.HasDuckConstraints)
            return GDTypeFlowEdgeKind.DuckConstraint;

        // Assignment edge
        if (source.Kind == GDTypeFlowNodeKind.Assignment || target.Kind == GDTypeFlowNodeKind.Assignment)
            return GDTypeFlowEdgeKind.Assignment;

        // Return edge
        if (source.Kind == GDTypeFlowNodeKind.ReturnValue || target.Kind == GDTypeFlowNodeKind.ReturnValue)
            return GDTypeFlowEdgeKind.Return;

        // Default type flow
        return GDTypeFlowEdgeKind.TypeFlow;
    }

    /// <summary>
    /// Gets the total bounds of all nodes in the layout.
    /// </summary>
    public Rect2 GetBounds(List<GDTypeFlowNode> nodes)
    {
        if (nodes == null || nodes.Count == 0)
            return new Rect2(0, 0, 400, 300);

        var minX = nodes.Min(n => n.Position.X);
        var minY = nodes.Min(n => n.Position.Y);
        var maxX = nodes.Max(n => n.Position.X + n.Size.X);
        var maxY = nodes.Max(n => n.Position.Y + n.Size.Y);

        return new Rect2(minX, minY, maxX - minX, maxY - minY);
    }
}
