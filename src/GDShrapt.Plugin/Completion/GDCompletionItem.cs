using System;

namespace GDShrapt.Plugin;

/// <summary>
/// Represents a single completion item in the autocomplete list.
/// </summary>
internal class GDCompletionItem
{
    /// <summary>
    /// The text to display in the completion list.
    /// </summary>
    public string Label { get; init; } = "";

    /// <summary>
    /// The text to insert when this item is selected.
    /// </summary>
    public string InsertText { get; init; } = "";

    /// <summary>
    /// Additional detail text shown alongside the label.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>
    /// Documentation or description of the item.
    /// </summary>
    public string? Documentation { get; init; }

    /// <summary>
    /// The kind of completion item (method, property, variable, etc.).
    /// </summary>
    public GDCompletionItemKind Kind { get; init; } = GDCompletionItemKind.Text;

    /// <summary>
    /// The type of the item (for display).
    /// </summary>
    public string? TypeName { get; init; }

    /// <summary>
    /// Sort priority (lower = higher priority).
    /// </summary>
    public int SortPriority { get; init; } = 100;

    /// <summary>
    /// Whether this is a snippet that requires cursor positioning.
    /// </summary>
    public bool IsSnippet { get; init; }

    /// <summary>
    /// Source of the completion (local, Godot API, project, etc.).
    /// </summary>
    public GDCompletionSource Source { get; init; } = GDCompletionSource.Unknown;

    /// <summary>
    /// Creates a method completion item.
    /// </summary>
    public static GDCompletionItem Method(string name, string returnType, string? parameters = null, GDCompletionSource source = GDCompletionSource.GodotApi)
    {
        var insertText = parameters != null ? $"{name}({parameters})" : $"{name}()";
        var detail = parameters != null ? $"({parameters}) -> {returnType}" : $"() -> {returnType}";

        return new GDCompletionItem
        {
            Label = name,
            InsertText = name, // Just insert method name, user adds parentheses
            Detail = detail,
            TypeName = returnType,
            Kind = GDCompletionItemKind.Method,
            Source = source,
            SortPriority = 50
        };
    }

    /// <summary>
    /// Creates a property completion item.
    /// </summary>
    public static GDCompletionItem Property(string name, string typeName, GDCompletionSource source = GDCompletionSource.GodotApi)
    {
        return new GDCompletionItem
        {
            Label = name,
            InsertText = name,
            Detail = typeName,
            TypeName = typeName,
            Kind = GDCompletionItemKind.Property,
            Source = source,
            SortPriority = 40
        };
    }

    /// <summary>
    /// Creates a variable completion item.
    /// </summary>
    public static GDCompletionItem Variable(string name, string? typeName = null, GDCompletionSource source = GDCompletionSource.Local)
    {
        return new GDCompletionItem
        {
            Label = name,
            InsertText = name,
            Detail = typeName,
            TypeName = typeName,
            Kind = GDCompletionItemKind.Variable,
            Source = source,
            SortPriority = 10 // Local variables have highest priority
        };
    }

    /// <summary>
    /// Creates a signal completion item.
    /// </summary>
    public static GDCompletionItem Signal(string name, GDCompletionSource source = GDCompletionSource.GodotApi)
    {
        return new GDCompletionItem
        {
            Label = name,
            InsertText = name,
            Detail = "Signal",
            Kind = GDCompletionItemKind.Event,
            Source = source,
            SortPriority = 60
        };
    }

    /// <summary>
    /// Creates a constant completion item.
    /// </summary>
    public static GDCompletionItem Constant(string name, string? typeName = null, GDCompletionSource source = GDCompletionSource.GodotApi)
    {
        return new GDCompletionItem
        {
            Label = name,
            InsertText = name,
            Detail = typeName ?? "const",
            TypeName = typeName,
            Kind = GDCompletionItemKind.Constant,
            Source = source,
            SortPriority = 55
        };
    }

    /// <summary>
    /// Creates a class/type completion item.
    /// </summary>
    public static GDCompletionItem Class(string name, GDCompletionSource source = GDCompletionSource.GodotApi)
    {
        return new GDCompletionItem
        {
            Label = name,
            InsertText = name,
            Kind = GDCompletionItemKind.Class,
            Source = source,
            SortPriority = 70
        };
    }

    /// <summary>
    /// Creates an enum value completion item.
    /// </summary>
    public static GDCompletionItem EnumValue(string name, string? enumName = null, GDCompletionSource source = GDCompletionSource.GodotApi)
    {
        return new GDCompletionItem
        {
            Label = name,
            InsertText = name,
            Detail = enumName,
            Kind = GDCompletionItemKind.EnumMember,
            Source = source,
            SortPriority = 56
        };
    }

    /// <summary>
    /// Creates a keyword completion item.
    /// </summary>
    public static GDCompletionItem Keyword(string keyword)
    {
        return new GDCompletionItem
        {
            Label = keyword,
            InsertText = keyword,
            Kind = GDCompletionItemKind.Keyword,
            Source = GDCompletionSource.Keyword,
            SortPriority = 90
        };
    }

    /// <summary>
    /// Creates a snippet completion item.
    /// </summary>
    public static GDCompletionItem Snippet(string label, string insertText, string? detail = null)
    {
        return new GDCompletionItem
        {
            Label = label,
            InsertText = insertText,
            Detail = detail ?? "Snippet",
            Kind = GDCompletionItemKind.Snippet,
            Source = GDCompletionSource.Snippet,
            IsSnippet = true,
            SortPriority = 80
        };
    }

    public override string ToString()
    {
        return $"{Kind}: {Label}";
    }
}

/// <summary>
/// The kind of completion item.
/// </summary>
internal enum GDCompletionItemKind
{
    Text,
    Method,
    Function,
    Constructor,
    Field,
    Variable,
    Class,
    Interface,
    Module,
    Property,
    Unit,
    Value,
    Enum,
    EnumMember,
    Keyword,
    Snippet,
    Color,
    File,
    Reference,
    Folder,
    Event,
    Operator,
    Constant
}

/// <summary>
/// Source of the completion item.
/// </summary>
internal enum GDCompletionSource
{
    Unknown,
    Local,        // Local variable/parameter
    Script,       // From current script (class member)
    Project,      // From project class
    GodotApi,     // From Godot API
    Keyword,      // GDScript keyword
    Snippet,      // Code snippet
    BuiltIn       // Built-in function/constant
}
