using GDShrapt.CLI.Core;

namespace GDShrapt.Plugin;

/// <summary>
/// Inflows tab for Type Breakdown panel.
/// Shows a tree of where type information comes FROM (sources).
/// </summary>
internal partial class GDTypeInflowsTab : ScrollContainer
{
    private Tree _tree;

    // Colors
    private static readonly Color HighConfidenceColor = new(0.3f, 0.8f, 0.3f);
    private static readonly Color MediumConfidenceColor = new(1.0f, 0.7f, 0.3f);
    private static readonly Color LowConfidenceColor = new(0.8f, 0.3f, 0.3f);
    private static readonly Color HintColor = new(0.5f, 0.5f, 0.5f);

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

    public GDTypeInflowsTab()
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

        _tree.SetColumnTitle(0, "Type");
        _tree.SetColumnTitle(1, "Source");
        _tree.SetColumnTitle(2, "Location");
        _tree.ColumnTitlesVisible = true;

        _tree.SetColumnExpand(0, true);
        _tree.SetColumnExpand(1, true);
        _tree.SetColumnExpand(2, false);
        _tree.SetColumnCustomMinimumWidth(2, 80);

        _tree.ItemActivated += OnItemActivated;

        AddChild(_tree);
    }

    /// <summary>
    /// Displays inflows for the given node.
    /// </summary>
    public void DisplayInflows(GDTypeFlowNode node)
    {
        Clear();

        if (node == null)
            return;

        var root = _tree.CreateItem();

        if (node.Inflows.Count == 0)
        {
            var emptyItem = _tree.CreateItem(root);
            emptyItem.SetText(0, "No type sources found");
            emptyItem.SetCustomColor(0, HintColor);
            return;
        }

        // Add root header
        var headerItem = _tree.CreateItem(root);
        headerItem.SetText(0, $"Sources of type information for '{node.Label}'");
        headerItem.SetCustomColor(0, HintColor);
        headerItem.SetSelectable(0, false);
        headerItem.SetSelectable(1, false);
        headerItem.SetSelectable(2, false);

        // Add each inflow as a tree item
        foreach (var inflow in node.Inflows)
        {
            AddInflowItem(root, inflow, 0);
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

    private void AddInflowItem(TreeItem parent, GDTypeFlowNode inflow, int depth)
    {
        var item = _tree.CreateItem(parent);

        // Column 0: Type with confidence color
        var typeText = inflow.Type ?? "Variant";
        item.SetText(0, typeText);
        item.SetCustomColor(0, GetConfidenceColor(inflow.Confidence));

        // Column 1: Source description
        var sourceDesc = GetSourceDescription(inflow);
        item.SetText(1, sourceDesc);
        item.SetCustomColor(1, new Color(0.8f, 0.8f, 0.8f));

        // Column 2: Location
        var locationText = GetLocationText(inflow);
        item.SetText(2, locationText);
        item.SetCustomColor(2, HintColor);

        // Store node reference for click handling via index lookup
        var nodeId = _nextNodeId++;
        _nodeMap[nodeId] = inflow;
        item.SetMetadata(0, nodeId);

        // Add tooltip with full information
        var tooltip = BuildTooltip(inflow);
        item.SetTooltipText(0, tooltip);
        item.SetTooltipText(1, tooltip);
        item.SetTooltipText(2, tooltip);

        // Recursively add child inflows (with depth limit)
        if (depth < MaxDepth && inflow.Inflows.Count > 0)
        {
            var childCount = 0;
            foreach (var childInflow in inflow.Inflows)
            {
                if (childCount >= MaxChildrenPerLevel)
                {
                    var moreItem = _tree.CreateItem(item);
                    moreItem.SetText(0, $"... and {inflow.Inflows.Count - childCount} more");
                    moreItem.SetCustomColor(0, HintColor);
                    moreItem.SetSelectable(0, false);
                    moreItem.SetSelectable(1, false);
                    moreItem.SetSelectable(2, false);
                    break;
                }

                AddInflowItem(item, childInflow, depth + 1);
                childCount++;
            }

            // Collapse by default if not top level
            if (depth > 0)
            {
                item.Collapsed = true;
            }
        }
    }

    private string GetSourceDescription(GDTypeFlowNode node)
    {
        var kindDesc = node.Kind switch
        {
            GDTypeFlowNodeKind.TypeAnnotation => "Type annotation",
            GDTypeFlowNodeKind.Literal => $"Literal: {node.Label}",
            GDTypeFlowNodeKind.MethodCall => $"Method call: {node.Label}()",
            GDTypeFlowNodeKind.PropertyAccess => $"Property: {node.Label}",
            GDTypeFlowNodeKind.IndexerAccess => $"Indexer: {node.Label}[]",
            GDTypeFlowNodeKind.TypeCheck => $"Type check: is {node.Type}",
            GDTypeFlowNodeKind.NullCheck => "Null check",
            GDTypeFlowNodeKind.Assignment => $"Assignment to {node.Label}",
            GDTypeFlowNodeKind.Parameter => $"Parameter: {node.Label}",
            GDTypeFlowNodeKind.MemberVariable => $"Member: {node.Label}",
            GDTypeFlowNodeKind.LocalVariable => $"Variable: {node.Label}",
            GDTypeFlowNodeKind.ReturnValue => "Return value",
            GDTypeFlowNodeKind.Comparison => "Comparison",
            GDTypeFlowNodeKind.Unknown => node.Description ?? node.Label,
            _ => node.Label
        };

        // Add source object context if available
        if (!string.IsNullOrEmpty(node.SourceType) && node.SourceType != "Variant")
        {
            kindDesc += $" on {node.SourceType}";
        }
        else if (!string.IsNullOrEmpty(node.SourceObjectName))
        {
            kindDesc += $" on {node.SourceObjectName}";
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

        sb.AppendLine($"Name: {node.Label}");
        sb.AppendLine($"Type: {node.Type ?? "Variant"}");
        sb.AppendLine($"Kind: {node.Kind}");
        sb.AppendLine($"Confidence: {GDConfidenceBadge.GetConfidenceDots(node.Confidence)} ({node.Confidence:P0})");

        if (!string.IsNullOrEmpty(node.SourceType))
            sb.AppendLine($"Source Type: {node.SourceType}");

        if (!string.IsNullOrEmpty(node.SourceObjectName))
            sb.AppendLine($"Source Object: {node.SourceObjectName}");

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

    private static Color GetConfidenceColor(float confidence)
    {
        return confidence switch
        {
            >= 0.8f => HighConfidenceColor,
            >= 0.5f => MediumConfidenceColor,
            _ => LowConfidenceColor
        };
    }
}
