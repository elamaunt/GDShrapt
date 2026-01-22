using GDShrapt.Abstractions;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for TypeFlow navigation and graph building.
/// Provides access to type inference flow visualization.
/// </summary>
public interface IGDTypeFlowHandler
{
    /// <summary>
    /// The currently focused node in the TypeFlow graph.
    /// </summary>
    GDTypeFlowNode? CurrentNode { get; }

    /// <summary>
    /// The root node (first symbol shown in this session).
    /// </summary>
    GDTypeFlowNode? RootNode { get; }

    /// <summary>
    /// Whether the user can navigate back.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// The name of the currently displayed symbol.
    /// </summary>
    string? CurrentSymbolName { get; }

    /// <summary>
    /// Shows the type flow for a symbol from a script.
    /// This is like clicking on a symbol in the editor and selecting "Show Type Flow".
    /// </summary>
    /// <param name="symbolName">The name of the symbol to show.</param>
    /// <param name="script">The script containing the symbol.</param>
    /// <returns>The root node if found, null otherwise.</returns>
    GDTypeFlowNode? ShowForSymbol(string symbolName, GDScriptFile script);

    /// <summary>
    /// Shows the type flow for a symbol at a specific position.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="line">Line number (1-based).</param>
    /// <param name="column">Column number (1-based).</param>
    /// <returns>The root node if found, null otherwise.</returns>
    GDTypeFlowNode? ShowForPosition(string filePath, int line, int column);

    /// <summary>
    /// Navigates to a specific node (like clicking on an inflow or outflow).
    /// </summary>
    /// <param name="node">The node to navigate to.</param>
    /// <returns>True if navigation was successful.</returns>
    bool NavigateToNode(GDTypeFlowNode node);

    /// <summary>
    /// Navigates to an inflow by index.
    /// </summary>
    bool NavigateToInflow(int index);

    /// <summary>
    /// Navigates to an outflow by index.
    /// </summary>
    bool NavigateToOutflow(int index);

    /// <summary>
    /// Navigates to a node by its label.
    /// Searches in both inflows and outflows.
    /// </summary>
    bool NavigateToLabel(string label);

    /// <summary>
    /// Goes back to the previous node in navigation history.
    /// </summary>
    bool GoBack();

    /// <summary>
    /// Checks if navigating to a node would create a cycle.
    /// </summary>
    bool WouldCreateCycle(GDTypeFlowNode node);

    /// <summary>
    /// Clears the current session and resets navigation.
    /// </summary>
    void Clear();
}

/// <summary>
/// Represents a navigation entry in the TypeFlow history.
/// </summary>
public class GDTypeFlowNavigationEntry
{
    /// <summary>
    /// The node at this navigation point.
    /// </summary>
    public GDTypeFlowNode Node { get; }

    /// <summary>
    /// The action that caused navigation to this node.
    /// </summary>
    public GDTypeFlowNavigationAction Action { get; }

    /// <summary>
    /// Timestamp when this entry was created.
    /// </summary>
    public DateTime Timestamp { get; }

    public GDTypeFlowNavigationEntry(GDTypeFlowNode node, GDTypeFlowNavigationAction action)
    {
        Node = node;
        Action = action;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Types of navigation actions in TypeFlow.
/// </summary>
public enum GDTypeFlowNavigationAction
{
    /// <summary>
    /// Initial navigation (first symbol shown).
    /// </summary>
    Initial,

    /// <summary>
    /// User clicked on a node.
    /// </summary>
    ClickedNode,

    /// <summary>
    /// User went back in history.
    /// </summary>
    Back
}
