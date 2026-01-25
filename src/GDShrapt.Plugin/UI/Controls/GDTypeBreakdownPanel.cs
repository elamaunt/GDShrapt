using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.Plugin;

/// <summary>
/// Panel containing Overview, Inflows, and Outflows tabs for type flow analysis.
/// </summary>
internal partial class GDTypeBreakdownPanel : TabContainer
{
    private GDTypeOverviewTab _overviewTab;
    private GDTypeInflowsTab _inflowsTab;
    private GDTypeOutflowsTab _outflowsTab;

    /// <summary>
    /// Fired when user wants to navigate to a node.
    /// </summary>
    public event Action<GDTypeFlowNode> NodeNavigationRequested;

    /// <summary>
    /// Fired when user wants to open a node in a new tab.
    /// </summary>
    public event Action<GDTypeFlowNode> NodeOpenInTabRequested;

    public GDTypeBreakdownPanel()
    {
        // ExpandFill to take all available vertical space in content area
        SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        CreateTabs();
    }

    private void CreateTabs()
    {
        // Overview tab
        _overviewTab = new GDTypeOverviewTab();
        _overviewTab.Name = "Overview";
        _overviewTab.NodeClicked += OnNodeClicked;
        _overviewTab.NodeDoubleClicked += OnNodeDoubleClicked;
        AddChild(_overviewTab);

        // Inflows tab
        _inflowsTab = new GDTypeInflowsTab();
        _inflowsTab.Name = "Inflows";
        _inflowsTab.NodeActivated += OnNodeDoubleClicked;
        AddChild(_inflowsTab);

        // Outflows tab
        _outflowsTab = new GDTypeOutflowsTab();
        _outflowsTab.Name = "Outflows";
        _outflowsTab.NodeActivated += OnNodeDoubleClicked;
        AddChild(_outflowsTab);
    }

    /// <summary>
    /// Displays information for the given node.
    /// </summary>
    public void SetNode(GDTypeFlowNode node, GDSemanticModel semanticModel = null)
    {
        if (node == null)
        {
            ClearAll();
            return;
        }

        _overviewTab.DisplayOverview(node, semanticModel);
        _inflowsTab.DisplayInflows(node);
        _outflowsTab.DisplayOutflows(node);
    }

    /// <summary>
    /// Clears all tabs.
    /// </summary>
    public void ClearAll()
    {
        _overviewTab.Clear();
        _inflowsTab.Clear();
        _outflowsTab.Clear();
    }

    /// <summary>
    /// Cycles to the next tab.
    /// </summary>
    public void CycleTab()
    {
        var nextTab = (CurrentTab + 1) % GetTabCount();
        CurrentTab = nextTab;
    }

    /// <summary>
    /// Sets compact mode for space-constrained layouts.
    /// </summary>
    public void SetCompactMode(bool compact)
    {
        if (compact)
        {
            // In compact mode, hide tab bar and show only overview
            TabsVisible = false;
            CurrentTab = 0;
        }
        else
        {
            TabsVisible = true;
        }
    }

    private void OnNodeClicked(GDTypeFlowNode node)
    {
        NodeNavigationRequested?.Invoke(node);
    }

    private void OnNodeDoubleClicked(GDTypeFlowNode node)
    {
        NodeOpenInTabRequested?.Invoke(node);
    }
}
