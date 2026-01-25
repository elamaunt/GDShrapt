using GDShrapt.CLI.Core;

namespace GDShrapt.Plugin;

/// <summary>
/// Tab bar for Type Flow panel that supports:
/// - Cursor Tab (Follow Mode): Always-present tab that follows cursor position
/// - Symbol Tabs: Individual tabs for specific symbols that can be closed
/// </summary>
internal partial class GDTypeFlowTabBar : TabBar
{
    private const int CursorTabIndex = 0;
    private const string CursorTabTitle = "⟳ Cursor";
    private const int MaxSymbolTabs = 10;

    // Track symbol tabs by their node ID
    private readonly Dictionary<string, int> _symbolTabIndices = new();

    // Node lookup by tab index (Godot Variant cannot store custom C# objects)
    private readonly Dictionary<int, GDTypeFlowNode> _tabNodeMap = new();

    /// <summary>
    /// Fired when the Cursor Tab is activated.
    /// </summary>
    public event Action CursorTabActivated;

    /// <summary>
    /// Fired when a Symbol Tab is activated.
    /// </summary>
    public event Action<GDTypeFlowNode> SymbolTabActivated;

    /// <summary>
    /// Fired when a Symbol Tab is closed.
    /// </summary>
    public event Action<GDTypeFlowNode> SymbolTabClosed;

    public GDTypeFlowTabBar()
    {
        // Configure tab bar appearance
        TabCloseDisplayPolicy = CloseButtonDisplayPolicy.ShowActiveOnly;
        DragToRearrangeEnabled = false; // Cursor tab should stay first
        ScrollingEnabled = true;
        SelectWithRmb = false;

        // Add Cursor Tab (cannot be closed)
        AddTab(CursorTabTitle, null);
        SetTabDisabled(CursorTabIndex, false);

        // Connect signals
        TabClosePressed += OnTabClosePressed;
        TabChanged += OnTabChanged;
    }

    /// <summary>
    /// Whether the Cursor Tab (Follow Mode) is currently active.
    /// </summary>
    public bool IsCursorTabActive => CurrentTab == CursorTabIndex;

    /// <summary>
    /// Gets the currently active node (null if Cursor Tab is active).
    /// </summary>
    public GDTypeFlowNode GetCurrentNode()
    {
        if (CurrentTab <= CursorTabIndex || CurrentTab >= TabCount)
            return null;

        return _tabNodeMap.TryGetValue(CurrentTab, out var node) ? node : null;
    }

    /// <summary>
    /// Adds a new symbol tab or activates existing one.
    /// </summary>
    /// <param name="node">The node to show in the tab.</param>
    /// <returns>The tab index.</returns>
    public int AddSymbolTab(GDTypeFlowNode node)
    {
        if (node == null)
            return -1;

        // Use a unique key based on symbol name and file path, not the internal node ID
        // (node IDs like "node_0" are reset when building new graphs)
        var nodeKey = BuildUniqueNodeKey(node);

        // Check if tab already exists
        if (_symbolTabIndices.TryGetValue(nodeKey, out var existingIdx))
        {
            // Update the node reference in case it was rebuilt
            _tabNodeMap[existingIdx] = node;
            CurrentTab = existingIdx;
            return existingIdx;
        }

        // Limit number of tabs
        if (_symbolTabIndices.Count >= MaxSymbolTabs)
        {
            // Close oldest tab (first symbol tab after cursor)
            if (TabCount > 1)
            {
                CloseTabAt(1);
            }
        }

        // Create new tab
        var title = TruncateLabel(node.Label, 12);
        var icon = GetConfidenceIcon(node.Confidence);
        AddTab(title, icon);

        var newIdx = TabCount - 1;
        _tabNodeMap[newIdx] = node;
        SetTabTooltip(newIdx, BuildTabTooltip(node));

        // Update index tracking
        _symbolTabIndices[nodeKey] = newIdx;

        // Activate new tab
        CurrentTab = newIdx;

        return newIdx;
    }

    /// <summary>
    /// Activates the Cursor Tab (Follow Mode).
    /// </summary>
    public void ActivateCursorTab()
    {
        CurrentTab = CursorTabIndex;
    }

    /// <summary>
    /// Closes all symbol tabs, keeping only the Cursor Tab.
    /// </summary>
    public void CloseAllSymbolTabs()
    {
        while (TabCount > 1)
        {
            RemoveTab(1);
        }
        _symbolTabIndices.Clear();
        _tabNodeMap.Clear();
        CurrentTab = CursorTabIndex;
    }

    /// <summary>
    /// Gets the node associated with a specific tab index.
    /// Returns null for Cursor Tab or invalid indices.
    /// </summary>
    public GDTypeFlowNode GetNodeForTab(int tabIndex)
    {
        return GetNodeAtTab(tabIndex);
    }

    /// <summary>
    /// Closes a specific tab by index.
    /// The Cursor Tab cannot be closed.
    /// </summary>
    public void CloseTab(int tabIndex)
    {
        CloseTabAt(tabIndex);
    }

    /// <summary>
    /// Activates the tab associated with a specific node.
    /// If no tab exists for the node, activates Cursor Tab.
    /// </summary>
    public void ActivateTabForNode(GDTypeFlowNode node)
    {
        if (node == null)
        {
            CurrentTab = CursorTabIndex;
            return;
        }

        var nodeKey = BuildUniqueNodeKey(node);
        if (_symbolTabIndices.TryGetValue(nodeKey, out var tabIdx))
        {
            CurrentTab = tabIdx;
        }
        else
        {
            // Node doesn't have a tab, activate cursor tab
            CurrentTab = CursorTabIndex;
        }
    }

    /// <summary>
    /// Updates the Cursor Tab appearance based on follow mode state.
    /// </summary>
    public void SetFollowModeActive(bool active)
    {
        var title = active ? "⟳ Cursor" : "◯ Cursor";
        SetTabTitle(CursorTabIndex, title);
    }

    private void OnTabClosePressed(long tab)
    {
        var tabIdx = (int)tab;

        // Cursor Tab cannot be closed
        if (tabIdx == CursorTabIndex)
            return;

        var node = GetNodeAtTab(tabIdx);
        CloseTabAt(tabIdx);

        if (node != null)
        {
            SymbolTabClosed?.Invoke(node);
        }
    }

    private void OnTabChanged(long tab)
    {
        var tabIdx = (int)tab;

        if (tabIdx == CursorTabIndex)
        {
            CursorTabActivated?.Invoke();
        }
        else
        {
            var node = GetNodeAtTab(tabIdx);
            if (node != null)
            {
                SymbolTabActivated?.Invoke(node);
            }
        }
    }

    private void CloseTabAt(int tabIdx)
    {
        if (tabIdx <= CursorTabIndex || tabIdx >= TabCount)
            return;

        var node = GetNodeAtTab(tabIdx);
        if (node != null)
        {
            var nodeKey = BuildUniqueNodeKey(node);
            _symbolTabIndices.Remove(nodeKey);
        }

        _tabNodeMap.Remove(tabIdx);
        RemoveTab(tabIdx);

        // Update indices for remaining tabs
        RebuildIndexTracking();

        // If we closed the active tab, switch to cursor tab
        if (CurrentTab >= TabCount)
        {
            CurrentTab = CursorTabIndex;
        }
    }

    private void RebuildIndexTracking()
    {
        // Rebuild both maps after tab removal
        var oldNodeMap = new Dictionary<int, GDTypeFlowNode>(_tabNodeMap);
        _tabNodeMap.Clear();
        _symbolTabIndices.Clear();

        for (int i = 1; i < TabCount; i++)
        {
            // Find the node that was at old index i+1 (before removal)
            // or try to get from the remaining entries
            GDTypeFlowNode node = null;
            foreach (var kvp in oldNodeMap)
            {
                if (kvp.Value != null)
                {
                    var nodeKey = BuildUniqueNodeKey(kvp.Value);
                    if (!_symbolTabIndices.ContainsKey(nodeKey))
                    {
                        node = kvp.Value;
                        _tabNodeMap[i] = node;
                        _symbolTabIndices[nodeKey] = i;
                        oldNodeMap.Remove(kvp.Key);
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Builds a unique key for a node based on symbol name and file path.
    /// This is more stable than using node.Id which resets when graphs are rebuilt.
    /// </summary>
    private static string BuildUniqueNodeKey(GDTypeFlowNode node)
    {
        var symbolName = node.Label ?? "?";
        var filePath = node.SourceScript?.FullPath ?? node.Location?.FilePath ?? "";

        // Include line number for disambiguation of symbols with same name in different scopes
        var line = node.Location?.StartLine ?? 0;

        return $"{filePath}:{symbolName}:{line}";
    }

    private GDTypeFlowNode GetNodeAtTab(int tabIdx)
    {
        if (tabIdx <= CursorTabIndex || tabIdx >= TabCount)
            return null;

        return _tabNodeMap.TryGetValue(tabIdx, out var node) ? node : null;
    }

    private static string TruncateLabel(string label, int maxLength)
    {
        if (string.IsNullOrEmpty(label))
            return "?";

        if (label.Length <= maxLength)
            return label;

        return label.Substring(0, maxLength - 1) + "…";
    }

    private static string BuildTabTooltip(GDTypeFlowNode node)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(node.Label);
        sb.Append(": ");
        sb.Append(node.Type ?? "Variant");

        if (node.Location != null && node.Location.IsValid)
        {
            sb.Append("\n");
            sb.Append(node.Location.ToString());
        }

        var confidence = node.Confidence;
        sb.Append($"\nConfidence: {GDConfidenceBadge.GetConfidenceDots(confidence)} ({confidence:P0})");

        return sb.ToString();
    }

    private static Texture2D GetConfidenceIcon(float confidence)
    {
        // For now, return null - icons will be added later if needed
        // Could use colored circle textures based on confidence
        return null;
    }
}
