using System.Collections.Generic;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for code completion.
/// </summary>
public interface IGDCompletionHandler
{
    /// <summary>
    /// Gets completion items for the given context.
    /// </summary>
    /// <param name="request">Completion request with position and context.</param>
    /// <returns>List of completion items.</returns>
    IReadOnlyList<GDCompletionItem> GetCompletions(GDCompletionRequest request);

    /// <summary>
    /// Gets member completions for a type (after dot).
    /// </summary>
    /// <param name="typeName">Name of the type.</param>
    /// <returns>List of member completion items.</returns>
    IReadOnlyList<GDCompletionItem> GetMemberCompletions(string typeName);

    /// <summary>
    /// Gets type completions (for type annotations).
    /// </summary>
    /// <returns>List of type completion items.</returns>
    IReadOnlyList<GDCompletionItem> GetTypeCompletions();

    /// <summary>
    /// Gets keyword completions.
    /// </summary>
    /// <returns>List of keyword completion items.</returns>
    IReadOnlyList<GDCompletionItem> GetKeywordCompletions();
}

/// <summary>
/// Request for code completion.
/// </summary>
public class GDCompletionRequest
{
    /// <summary>
    /// Path to the file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Line number (1-based).
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Column number (1-based).
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// Text before the cursor on the current line.
    /// </summary>
    public string? TextBeforeCursor { get; init; }

    /// <summary>
    /// The word being typed (prefix for filtering).
    /// </summary>
    public string? WordPrefix { get; init; }

    /// <summary>
    /// Type of completion requested.
    /// </summary>
    public GDCompletionType CompletionType { get; init; }

    /// <summary>
    /// For member access, the expression before the dot.
    /// </summary>
    public string? MemberAccessExpression { get; init; }

    /// <summary>
    /// For member access, the inferred type of the expression.
    /// </summary>
    public string? MemberAccessType { get; init; }
}

/// <summary>
/// Type of completion.
/// </summary>
public enum GDCompletionType
{
    /// <summary>
    /// General symbol completion.
    /// </summary>
    Symbol,

    /// <summary>
    /// Member access completion (after dot).
    /// </summary>
    MemberAccess,

    /// <summary>
    /// Type annotation completion (after colon).
    /// </summary>
    TypeAnnotation
}

/// <summary>
/// A completion item.
/// </summary>
public class GDCompletionItem
{
    /// <summary>
    /// Display label.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Kind of completion item.
    /// </summary>
    public GDCompletionItemKind Kind { get; init; }

    /// <summary>
    /// Text to insert (if different from label).
    /// </summary>
    public string? InsertText { get; init; }

    /// <summary>
    /// Detail/type information.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    public string? Documentation { get; init; }

    /// <summary>
    /// Sort priority (lower = higher priority).
    /// </summary>
    public int SortPriority { get; init; }

    /// <summary>
    /// Source of the completion item.
    /// </summary>
    public GDCompletionSource Source { get; init; }

    // Factory methods
    public static GDCompletionItem Keyword(string name) => new()
    {
        Label = name,
        Kind = GDCompletionItemKind.Keyword,
        SortPriority = 100,
        Source = GDCompletionSource.BuiltIn
    };

    public static GDCompletionItem Method(string name, string? returnType, string? signature, GDCompletionSource source) => new()
    {
        Label = name,
        Kind = GDCompletionItemKind.Method,
        Detail = returnType,
        Documentation = signature,
        InsertText = name + "()",
        SortPriority = 10,
        Source = source
    };

    public static GDCompletionItem Variable(string name, string? type, GDCompletionSource source) => new()
    {
        Label = name,
        Kind = GDCompletionItemKind.Variable,
        Detail = type,
        SortPriority = 5,
        Source = source
    };

    public static GDCompletionItem Property(string name, string? type, GDCompletionSource source) => new()
    {
        Label = name,
        Kind = GDCompletionItemKind.Property,
        Detail = type,
        SortPriority = 10,
        Source = source
    };

    public static GDCompletionItem Constant(string name, string? type, GDCompletionSource source) => new()
    {
        Label = name,
        Kind = GDCompletionItemKind.Constant,
        Detail = type,
        SortPriority = 15,
        Source = source
    };

    public static GDCompletionItem Signal(string name, GDCompletionSource source) => new()
    {
        Label = name,
        Kind = GDCompletionItemKind.Event,
        SortPriority = 20,
        Source = source
    };

    public static GDCompletionItem Class(string name, GDCompletionSource source) => new()
    {
        Label = name,
        Kind = GDCompletionItemKind.Class,
        Detail = source == GDCompletionSource.GodotApi ? "built-in type" : null,
        SortPriority = 30,
        Source = source
    };

    public static GDCompletionItem EnumValue(string name, string? enumType, GDCompletionSource source) => new()
    {
        Label = name,
        Kind = GDCompletionItemKind.EnumMember,
        Detail = enumType,
        SortPriority = 15,
        Source = source
    };

    public static GDCompletionItem Snippet(string name, string insertText, string? description) => new()
    {
        Label = name,
        Kind = GDCompletionItemKind.Snippet,
        InsertText = insertText,
        Documentation = description,
        SortPriority = 50,
        Source = GDCompletionSource.BuiltIn
    };
}

/// <summary>
/// Kind of completion item.
/// </summary>
public enum GDCompletionItemKind
{
    Method,
    Function,
    Variable,
    Field,
    Property,
    Class,
    Interface,
    Struct,
    Enum,
    EnumMember,
    Constant,
    Event,
    Keyword,
    Snippet,
    Text
}

/// <summary>
/// Source of a completion item.
/// </summary>
public enum GDCompletionSource
{
    /// <summary>
    /// Built-in GDScript functions/keywords.
    /// </summary>
    BuiltIn,

    /// <summary>
    /// Godot API (engine classes, methods, properties).
    /// </summary>
    GodotApi,

    /// <summary>
    /// Current script file.
    /// </summary>
    Script,

    /// <summary>
    /// Project-defined types.
    /// </summary>
    Project,

    /// <summary>
    /// Local scope (parameters, local variables).
    /// </summary>
    Local
}
