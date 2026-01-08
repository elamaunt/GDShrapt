using System;

namespace GDShrapt.Plugin.Completion;

/// <summary>
/// Represents a single completion item in the autocomplete list.
/// </summary>
internal class CompletionItem
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
    public CompletionItemKind Kind { get; init; } = CompletionItemKind.Text;

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
    public CompletionSource Source { get; init; } = CompletionSource.Unknown;

    /// <summary>
    /// Creates a method completion item.
    /// </summary>
    public static CompletionItem Method(string name, string returnType, string? parameters = null, CompletionSource source = CompletionSource.GodotApi)
    {
        var insertText = parameters != null ? $"{name}({parameters})" : $"{name}()";
        var detail = parameters != null ? $"({parameters}) -> {returnType}" : $"() -> {returnType}";

        return new CompletionItem
        {
            Label = name,
            InsertText = name, // Just insert method name, user adds parentheses
            Detail = detail,
            TypeName = returnType,
            Kind = CompletionItemKind.Method,
            Source = source,
            SortPriority = 50
        };
    }

    /// <summary>
    /// Creates a property completion item.
    /// </summary>
    public static CompletionItem Property(string name, string typeName, CompletionSource source = CompletionSource.GodotApi)
    {
        return new CompletionItem
        {
            Label = name,
            InsertText = name,
            Detail = typeName,
            TypeName = typeName,
            Kind = CompletionItemKind.Property,
            Source = source,
            SortPriority = 40
        };
    }

    /// <summary>
    /// Creates a variable completion item.
    /// </summary>
    public static CompletionItem Variable(string name, string? typeName = null, CompletionSource source = CompletionSource.Local)
    {
        return new CompletionItem
        {
            Label = name,
            InsertText = name,
            Detail = typeName,
            TypeName = typeName,
            Kind = CompletionItemKind.Variable,
            Source = source,
            SortPriority = 10 // Local variables have highest priority
        };
    }

    /// <summary>
    /// Creates a signal completion item.
    /// </summary>
    public static CompletionItem Signal(string name, CompletionSource source = CompletionSource.GodotApi)
    {
        return new CompletionItem
        {
            Label = name,
            InsertText = name,
            Detail = "Signal",
            Kind = CompletionItemKind.Event,
            Source = source,
            SortPriority = 60
        };
    }

    /// <summary>
    /// Creates a constant completion item.
    /// </summary>
    public static CompletionItem Constant(string name, string? typeName = null, CompletionSource source = CompletionSource.GodotApi)
    {
        return new CompletionItem
        {
            Label = name,
            InsertText = name,
            Detail = typeName ?? "const",
            TypeName = typeName,
            Kind = CompletionItemKind.Constant,
            Source = source,
            SortPriority = 55
        };
    }

    /// <summary>
    /// Creates a class/type completion item.
    /// </summary>
    public static CompletionItem Class(string name, CompletionSource source = CompletionSource.GodotApi)
    {
        return new CompletionItem
        {
            Label = name,
            InsertText = name,
            Kind = CompletionItemKind.Class,
            Source = source,
            SortPriority = 70
        };
    }

    /// <summary>
    /// Creates an enum value completion item.
    /// </summary>
    public static CompletionItem EnumValue(string name, string? enumName = null, CompletionSource source = CompletionSource.GodotApi)
    {
        return new CompletionItem
        {
            Label = name,
            InsertText = name,
            Detail = enumName,
            Kind = CompletionItemKind.EnumMember,
            Source = source,
            SortPriority = 56
        };
    }

    /// <summary>
    /// Creates a keyword completion item.
    /// </summary>
    public static CompletionItem Keyword(string keyword)
    {
        return new CompletionItem
        {
            Label = keyword,
            InsertText = keyword,
            Kind = CompletionItemKind.Keyword,
            Source = CompletionSource.Keyword,
            SortPriority = 90
        };
    }

    /// <summary>
    /// Creates a snippet completion item.
    /// </summary>
    public static CompletionItem Snippet(string label, string insertText, string? detail = null)
    {
        return new CompletionItem
        {
            Label = label,
            InsertText = insertText,
            Detail = detail ?? "Snippet",
            Kind = CompletionItemKind.Snippet,
            Source = CompletionSource.Snippet,
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
internal enum CompletionItemKind
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
internal enum CompletionSource
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
