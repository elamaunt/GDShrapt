using System.Collections.Generic;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

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

    /// <summary>
    /// Resolves the union type for a symbol (when variable can have multiple types).
    /// </summary>
    /// <param name="symbolName">The name of the symbol.</param>
    /// <param name="filePath">Path to the file containing the symbol.</param>
    /// <returns>Union type information if the symbol has multiple possible types, null otherwise.</returns>
    GDUnionType? ResolveUnionType(string symbolName, string filePath);

    /// <summary>
    /// Resolves the duck type constraints for a symbol.
    /// </summary>
    /// <param name="symbolName">The name of the symbol.</param>
    /// <param name="filePath">Path to the file containing the symbol.</param>
    /// <returns>Duck type information if the symbol has inferred constraints, null otherwise.</returns>
    GDDuckType? ResolveDuckType(string symbolName, string filePath);

    /// <summary>
    /// Gets inflow nodes (where type comes from) for a symbol.
    /// </summary>
    /// <param name="symbolName">The name of the symbol.</param>
    /// <param name="filePath">Path to the file containing the symbol.</param>
    /// <returns>List of inflow nodes, or null if symbol not found.</returns>
    IReadOnlyList<GDTypeFlowNode>? GetInflowNodes(string symbolName, string filePath);

    /// <summary>
    /// Gets outflow nodes (where type goes to) for a symbol.
    /// </summary>
    /// <param name="symbolName">The name of the symbol.</param>
    /// <param name="filePath">Path to the file containing the symbol.</param>
    /// <returns>List of outflow nodes, or null if symbol not found.</returns>
    IReadOnlyList<GDTypeFlowNode>? GetOutflowNodes(string symbolName, string filePath);

    /// <summary>
    /// Finds a symbol by name in a file.
    /// </summary>
    /// <param name="symbolName">The name of the symbol.</param>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>Symbol information if found, null otherwise.</returns>
    Semantics.GDSymbolInfo? FindSymbol(string symbolName, string filePath);

    /// <summary>
    /// Resolves the type of an expression at a specific position.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="line">Line number (1-based).</param>
    /// <param name="column">Column number (1-based).</param>
    /// <returns>The resolved type name, or null if not found.</returns>
    string? ResolveTypeAtPosition(string filePath, int line, int column);
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
