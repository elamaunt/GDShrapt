using System.Text.Json.Serialization;

namespace GDShrapt.LSP.Protocol.Types;

/// <summary>
/// Completion item kinds.
/// </summary>
public enum GDLspCompletionItemKind
{
    Text = 1,
    Method = 2,
    Function = 3,
    Constructor = 4,
    Field = 5,
    Variable = 6,
    Class = 7,
    Interface = 8,
    Module = 9,
    Property = 10,
    Unit = 11,
    Value = 12,
    Enum = 13,
    Keyword = 14,
    Snippet = 15,
    Color = 16,
    File = 17,
    Reference = 18,
    Folder = 19,
    EnumMember = 20,
    Constant = 21,
    Struct = 22,
    Event = 23,
    Operator = 24,
    TypeParameter = 25
}

/// <summary>
/// Completion trigger kinds.
/// </summary>
public enum GDLspCompletionTriggerKind
{
    Invoked = 1,
    TriggerCharacter = 2,
    TriggerForIncompleteCompletions = 3
}

/// <summary>
/// Insert text format.
/// </summary>
public enum GDLspInsertTextFormat
{
    PlainText = 1,
    Snippet = 2
}

/// <summary>
/// Completion request parameters.
/// </summary>
public class GDCompletionParams
{
    /// <summary>
    /// The text document.
    /// </summary>
    [JsonPropertyName("textDocument")]
    public GDLspTextDocumentIdentifier TextDocument { get; set; } = new();

    /// <summary>
    /// The position inside the text document.
    /// </summary>
    [JsonPropertyName("position")]
    public GDLspPosition Position { get; set; } = new();

    /// <summary>
    /// The completion context.
    /// </summary>
    [JsonPropertyName("context")]
    public GDCompletionContext? Context { get; set; }
}

/// <summary>
/// Contains additional information about the context in which a completion request is triggered.
/// </summary>
public class GDCompletionContext
{
    /// <summary>
    /// How the completion was triggered.
    /// </summary>
    [JsonPropertyName("triggerKind")]
    public GDLspCompletionTriggerKind TriggerKind { get; set; }

    /// <summary>
    /// The trigger character (single character) that has triggered code completion.
    /// </summary>
    [JsonPropertyName("triggerCharacter")]
    public string? TriggerCharacter { get; set; }
}

/// <summary>
/// Represents a completion item.
/// </summary>
public class GDLspCompletionItem
{
    /// <summary>
    /// The label of this completion item.
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Additional details for the label.
    /// </summary>
    [JsonPropertyName("labelDetails")]
    public GDLspCompletionItemLabelDetails? LabelDetails { get; set; }

    /// <summary>
    /// The kind of this completion item.
    /// </summary>
    [JsonPropertyName("kind")]
    public GDLspCompletionItemKind? Kind { get; set; }

    /// <summary>
    /// Tags for this completion item.
    /// </summary>
    [JsonPropertyName("tags")]
    public int[]? Tags { get; set; }

    /// <summary>
    /// A human-readable string with additional information about this item.
    /// </summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    /// <summary>
    /// A human-readable string that represents a doc-comment.
    /// </summary>
    [JsonPropertyName("documentation")]
    public object? Documentation { get; set; }

    /// <summary>
    /// Indicates if this item is deprecated.
    /// </summary>
    [JsonPropertyName("deprecated")]
    public bool? Deprecated { get; set; }

    /// <summary>
    /// Select this item when showing.
    /// </summary>
    [JsonPropertyName("preselect")]
    public bool? Preselect { get; set; }

    /// <summary>
    /// A string that should be used when comparing this item with other items.
    /// </summary>
    [JsonPropertyName("sortText")]
    public string? SortText { get; set; }

    /// <summary>
    /// A string that should be used when filtering a set of completion items.
    /// </summary>
    [JsonPropertyName("filterText")]
    public string? FilterText { get; set; }

    /// <summary>
    /// A string that should be inserted into a document when selecting this completion.
    /// </summary>
    [JsonPropertyName("insertText")]
    public string? InsertText { get; set; }

    /// <summary>
    /// The format of the insert text.
    /// </summary>
    [JsonPropertyName("insertTextFormat")]
    public GDLspInsertTextFormat? InsertTextFormat { get; set; }

    /// <summary>
    /// How whitespace and indentation is handled during completion item insertion.
    /// </summary>
    [JsonPropertyName("insertTextMode")]
    public int? InsertTextMode { get; set; }

    /// <summary>
    /// An edit which is applied to a document when selecting this completion.
    /// </summary>
    [JsonPropertyName("textEdit")]
    public GDLspTextEdit? TextEdit { get; set; }

    /// <summary>
    /// An optional array of additional text edits that are applied when selecting this completion.
    /// </summary>
    [JsonPropertyName("additionalTextEdits")]
    public GDLspTextEdit[]? AdditionalTextEdits { get; set; }

    /// <summary>
    /// An optional set of characters that when pressed while this completion is active will accept it first and
    /// then type that character.
    /// </summary>
    [JsonPropertyName("commitCharacters")]
    public string[]? CommitCharacters { get; set; }

    /// <summary>
    /// An optional command that is executed after inserting this completion.
    /// </summary>
    [JsonPropertyName("command")]
    public GDLspCommand? Command { get; set; }

    /// <summary>
    /// A data entry field that is preserved on a completion item between a completion and a completion resolve request.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

/// <summary>
/// Additional details for a completion item label.
/// </summary>
public class GDLspCompletionItemLabelDetails
{
    /// <summary>
    /// An optional string which is rendered less prominently directly after label.
    /// </summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    /// <summary>
    /// An optional string which is rendered less prominently after detail.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Represents a reference to a command.
/// </summary>
public class GDLspCommand
{
    /// <summary>
    /// Title of the command.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The identifier of the actual command handler.
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Arguments that the command handler should be invoked with.
    /// </summary>
    [JsonPropertyName("arguments")]
    public object[]? Arguments { get; set; }
}

/// <summary>
/// Completion list returned from completion request.
/// </summary>
public class GDLspCompletionList
{
    /// <summary>
    /// This list is not complete. Further typing should result in recomputing this list.
    /// </summary>
    [JsonPropertyName("isIncomplete")]
    public bool IsIncomplete { get; set; }

    /// <summary>
    /// The completion items.
    /// </summary>
    [JsonPropertyName("items")]
    public GDLspCompletionItem[] Items { get; set; } = [];
}
