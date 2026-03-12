namespace GDShrapt.Semantics;

/// <summary>
/// Centralized constants for well-known GDScript built-in function names.
/// </summary>
internal static class GDWellKnownFunctions
{
    public const string Preload = "preload";
    public const string Load = "load";
    public const string Range = "range";
    public const string Constructor = "new";
    public const string Instantiate = "instantiate";
    public const string GetChild = "get_child";
    public const string GetChildOrNull = "get_child_or_null";
    public const string AddChild = "add_child";
    public const string AddSibling = "add_sibling";
    public const string AddToGroup = "add_to_group";
    public const string GetNodesInGroup = "get_nodes_in_group";
    public const string GetFirstNodeInGroup = "get_first_node_in_group";

    public static bool IsResourceLoader(string? name) => name is Preload or Load;
    public static bool IsGetChild(string? name) => name is GetChild or GetChildOrNull;
    public static bool IsAddChild(string? name) => name is AddChild or AddSibling;
    public static bool IsGroupQuery(string? name) => name is GetNodesInGroup or GetFirstNodeInGroup;
}
