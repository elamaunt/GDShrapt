using GDShrapt.CLI.Core;

namespace GDShrapt.Plugin;

/// <summary>
/// Outflows tab for Type Breakdown panel.
/// Shows a tree of where type information GOES (usages).
/// </summary>
internal partial class GDTypeOutflowsTab : ScrollContainer
{
    private Tree _tree;

    // Colors
    private static readonly Color HighConfidenceColor = new(0.3f, 0.8f, 0.3f);
    private static readonly Color MediumConfidenceColor = new(1.0f, 0.7f, 0.3f);
    private static readonly Color LowConfidenceColor = new(0.8f, 0.3f, 0.3f);
    private static readonly Color HintColor = new(0.5f, 0.5f, 0.5f);
    private static readonly Color OutflowColor = new(0.7f, 0.5f, 0.5f);

    // Tree depth limit
    private const int MaxDepth = 3;
    private const int MaxChildrenPerLevel = 10;

    // Node lookup by index (Godot Variant cannot store custom C# objects)
    private readonly Dictionary<int, GDTypeFlowNode> _nodeMap = new();
    private int _nextNodeId;

    /// <summary>
    /// Fired when a node is double-clicked or activated.
    /// </summary>
    public event Action<GDTypeFlowNode> NodeActivated;

    public GDTypeOutflowsTab()
    {
        HorizontalScrollMode = ScrollMode.Disabled;
        VerticalScrollMode = ScrollMode.Auto;
        // ExpandFill to take all available space in TabContainer
        SizeFlagsVertical = SizeFlags.ExpandFill;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        _tree = new Tree
        {
            Columns = 3,
            HideRoot = true,
            SelectMode = Tree.SelectModeEnum.Single,
            // Tree inside ScrollContainer should ExpandFill to use scroll area
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        _tree.SetColumnTitle(0, "Target");
        _tree.SetColumnTitle(1, "Usage");
        _tree.SetColumnTitle(2, "Location");
        _tree.ColumnTitlesVisible = true;

        _tree.SetColumnExpand(0, true);
        _tree.SetColumnExpand(1, true);
        _tree.SetColumnExpand(2, false);

        // Set minimum widths to prevent column collapse in narrow windows
        _tree.SetColumnCustomMinimumWidth(0, 80);  // Target column minimum
        _tree.SetColumnCustomMinimumWidth(1, 100); // Usage column minimum (more text)
        _tree.SetColumnCustomMinimumWidth(2, 60);  // Location column (reduced from 80)

        _tree.ItemActivated += OnItemActivated;

        AddChild(_tree);
    }

    /// <summary>
    /// Displays outflows for the given node.
    /// </summary>
    public void DisplayOutflows(GDTypeFlowNode node)
    {
        Clear();

        if (node == null)
            return;

        var root = _tree.CreateItem();

        if (node.Outflows.Count == 0)
        {
            var emptyItem = _tree.CreateItem(root);
            emptyItem.SetText(0, "No usages found");
            emptyItem.SetCustomColor(0, HintColor);
            return;
        }

        // Add root header
        var headerItem = _tree.CreateItem(root);
        headerItem.SetText(0, $"Where '{node.Label}' type flows to");
        headerItem.SetCustomColor(0, HintColor);
        headerItem.SetSelectable(0, false);
        headerItem.SetSelectable(1, false);
        headerItem.SetSelectable(2, false);

        // Add each outflow as a tree item
        foreach (var outflow in node.Outflows)
        {
            AddOutflowItem(root, outflow, 0);
        }
    }

    /// <summary>
    /// Clears the tree.
    /// </summary>
    public void Clear()
    {
        _tree.Clear();
        _nodeMap.Clear();
        _nextNodeId = 0;
    }

    private void AddOutflowItem(TreeItem parent, GDTypeFlowNode outflow, int depth)
    {
        var item = _tree.CreateItem(parent);

        // Column 0: Target name with arrow
        var targetText = $"→ {outflow.Label}";
        item.SetText(0, targetText);
        item.SetCustomColor(0, OutflowColor);

        // Column 1: Usage description
        var usageDesc = GetUsageDescription(outflow);
        item.SetText(1, usageDesc);
        item.SetCustomColor(1, new Color(0.8f, 0.8f, 0.8f));

        // Column 2: Location
        var locationText = GetLocationText(outflow);
        item.SetText(2, locationText);
        item.SetCustomColor(2, HintColor);

        // Store node reference for click handling via index lookup
        var nodeId = _nextNodeId++;
        _nodeMap[nodeId] = outflow;
        item.SetMetadata(0, nodeId);

        // Add tooltip with full information
        var tooltip = BuildTooltip(outflow);
        item.SetTooltipText(0, tooltip);
        item.SetTooltipText(1, tooltip);
        item.SetTooltipText(2, tooltip);

        // Recursively add child outflows (with depth limit)
        if (depth < MaxDepth && outflow.Outflows.Count > 0)
        {
            var childCount = 0;
            foreach (var childOutflow in outflow.Outflows)
            {
                if (childCount >= MaxChildrenPerLevel)
                {
                    var moreItem = _tree.CreateItem(item);
                    moreItem.SetText(0, $"... and {outflow.Outflows.Count - childCount} more");
                    moreItem.SetCustomColor(0, HintColor);
                    moreItem.SetSelectable(0, false);
                    moreItem.SetSelectable(1, false);
                    moreItem.SetSelectable(2, false);
                    break;
                }

                AddOutflowItem(item, childOutflow, depth + 1);
                childCount++;
            }

            // Collapse by default if not top level
            if (depth > 0)
            {
                item.Collapsed = true;
            }
        }
    }

    private string GetUsageDescription(GDTypeFlowNode node)
    {
        var kindDesc = node.Kind switch
        {
            GDTypeFlowNodeKind.MethodCall => $"Passed to {node.Label}()",
            GDTypeFlowNodeKind.PropertyAccess => $"Property access: .{node.Label}",
            GDTypeFlowNodeKind.IndexerAccess => $"Indexed access: [{node.Label}]",
            GDTypeFlowNodeKind.TypeCheck => $"Type checked: is {node.Type}",
            GDTypeFlowNodeKind.NullCheck => "Null checked",
            GDTypeFlowNodeKind.Assignment => $"Assigned to {node.Label}",
            GDTypeFlowNodeKind.Parameter => $"Parameter of {node.Label}",
            GDTypeFlowNodeKind.MemberVariable => $"Set on .{node.Label}",
            GDTypeFlowNodeKind.LocalVariable => $"Assigned to {node.Label}",
            GDTypeFlowNodeKind.ReturnValue => "Returned from function",
            GDTypeFlowNodeKind.Comparison => $"Compared with {node.Label}",
            GDTypeFlowNodeKind.Unknown => node.Description ?? "Used",
            _ => $"Used in {node.Label}"
        };

        // Add result type if different
        if (!string.IsNullOrEmpty(node.Type) && node.Type != "Variant")
        {
            kindDesc += $" → {node.Type}";
        }

        return kindDesc;
    }

    private string GetLocationText(GDTypeFlowNode node)
    {
        if (node.Location == null || !node.Location.IsValid)
            return "";

        return $":{node.Location.StartLine + 1}";
    }

    private string BuildTooltip(GDTypeFlowNode node)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"Target: {node.Label}");
        sb.AppendLine($"Type: {node.Type ?? "Variant"}");
        sb.AppendLine($"Kind: {node.Kind}");
        sb.AppendLine($"Confidence: {GDConfidenceBadge.GetConfidenceDots(node.Confidence)} ({node.Confidence:P0})");

        if (!string.IsNullOrEmpty(node.Description))
            sb.AppendLine($"Description: {node.Description}");

        if (node.Location != null && node.Location.IsValid)
            sb.AppendLine($"Location: {node.Location}");

        sb.AppendLine();
        sb.AppendLine("Double-click to navigate");

        return sb.ToString();
    }

    private void OnItemActivated()
    {
        var selected = _tree.GetSelected();
        if (selected == null)
            return;

        var meta = selected.GetMetadata(0);
        if (meta.VariantType != Variant.Type.Int)
            return;

        var nodeId = meta.AsInt32();
        if (_nodeMap.TryGetValue(nodeId, out var node))
        {
            NodeActivated?.Invoke(node);
        }
    }
}
