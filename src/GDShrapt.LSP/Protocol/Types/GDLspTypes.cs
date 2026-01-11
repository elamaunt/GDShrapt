using System;
using System.Text.Json.Serialization;

namespace GDShrapt.LSP;

/// <summary>
/// Position in a text document expressed as zero-based line and character offset.
/// </summary>
public class GDLspPosition
{
    /// <summary>
    /// Line position in a document (zero-based).
    /// </summary>
    [JsonPropertyName("line")]
    public int Line { get; set; }

    /// <summary>
    /// Character offset on a line in a document (zero-based).
    /// </summary>
    [JsonPropertyName("character")]
    public int Character { get; set; }

    public GDLspPosition() { }

    public GDLspPosition(int line, int character)
    {
        Line = line;
        Character = character;
    }
}

/// <summary>
/// A range in a text document expressed as (zero-based) start and end positions.
/// </summary>
public class GDLspRange
{
    /// <summary>
    /// The range's start position.
    /// </summary>
    [JsonPropertyName("start")]
    public GDLspPosition Start { get; set; } = new();

    /// <summary>
    /// The range's end position.
    /// </summary>
    [JsonPropertyName("end")]
    public GDLspPosition End { get; set; } = new();

    public GDLspRange() { }

    public GDLspRange(GDLspPosition start, GDLspPosition end)
    {
        Start = start;
        End = end;
    }

    public GDLspRange(int startLine, int startChar, int endLine, int endChar)
    {
        Start = new GDLspPosition(startLine, startChar);
        End = new GDLspPosition(endLine, endChar);
    }
}

/// <summary>
/// Represents a location inside a resource, such as a line inside a text file.
/// </summary>
public class GDLspLocation
{
    /// <summary>
    /// The URI of the document.
    /// </summary>
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// The range inside the document.
    /// </summary>
    [JsonPropertyName("range")]
    public GDLspRange Range { get; set; } = new();

    public GDLspLocation() { }

    public GDLspLocation(string uri, GDLspRange range)
    {
        Uri = uri;
        Range = range;
    }
}

/// <summary>
/// A text edit applicable to a text document.
/// </summary>
public class GDLspTextEdit
{
    /// <summary>
    /// The range of the text document to be manipulated.
    /// </summary>
    [JsonPropertyName("range")]
    public GDLspRange Range { get; set; } = new();

    /// <summary>
    /// The string to be inserted.
    /// </summary>
    [JsonPropertyName("newText")]
    public string NewText { get; set; } = string.Empty;
}

/// <summary>
/// Text document identifier.
/// </summary>
public class GDLspTextDocumentIdentifier
{
    /// <summary>
    /// The text document's URI.
    /// </summary>
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;
}

/// <summary>
/// Versioned text document identifier.
/// </summary>
public class GDLspVersionedTextDocumentIdentifier : GDLspTextDocumentIdentifier
{
    /// <summary>
    /// The version number of this document.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }
}

/// <summary>
/// An item to transfer a text document from the client to the server.
/// </summary>
public class GDLspTextDocumentItem
{
    /// <summary>
    /// The text document's URI.
    /// </summary>
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// The text document's language identifier.
    /// </summary>
    [JsonPropertyName("languageId")]
    public string LanguageId { get; set; } = string.Empty;

    /// <summary>
    /// The version number of this document.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }

    /// <summary>
    /// The content of the opened text document.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// A parameter literal used in requests to pass a text document and a position inside that document.
/// </summary>
public class GDLspTextDocumentPositionParams
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
}

/// <summary>
/// Diagnostic severity.
/// </summary>
public enum GDLspDiagnosticSeverity
{
    Error = 1,
    Warning = 2,
    Information = 3,
    Hint = 4
}

/// <summary>
/// Represents a diagnostic, such as a compiler error or warning.
/// </summary>
public class GDLspDiagnostic
{
    /// <summary>
    /// The range at which the message applies.
    /// </summary>
    [JsonPropertyName("range")]
    public GDLspRange Range { get; set; } = new();

    /// <summary>
    /// The diagnostic's severity.
    /// </summary>
    [JsonPropertyName("severity")]
    public GDLspDiagnosticSeverity? Severity { get; set; }

    /// <summary>
    /// The diagnostic's code.
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// A human-readable string describing the source of this diagnostic.
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// The diagnostic's message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Symbol kind.
/// </summary>
public enum GDLspSymbolKind
{
    File = 1,
    Module = 2,
    Namespace = 3,
    Package = 4,
    Class = 5,
    Method = 6,
    Property = 7,
    Field = 8,
    Constructor = 9,
    Enum = 10,
    Interface = 11,
    Function = 12,
    Variable = 13,
    Constant = 14,
    String = 15,
    Number = 16,
    Boolean = 17,
    Array = 18,
    Object = 19,
    Key = 20,
    Null = 21,
    EnumMember = 22,
    Struct = 23,
    Event = 24,
    Operator = 25,
    TypeParameter = 26
}

/// <summary>
/// Represents information about programming constructs like variables, classes, interfaces etc.
/// </summary>
public class GDLspDocumentSymbol
{
    /// <summary>
    /// The name of this symbol.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// More detail for this symbol, e.g the signature of a function.
    /// </summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    /// <summary>
    /// The kind of this symbol.
    /// </summary>
    [JsonPropertyName("kind")]
    public GDLspSymbolKind Kind { get; set; }

    /// <summary>
    /// The range enclosing this symbol.
    /// </summary>
    [JsonPropertyName("range")]
    public GDLspRange Range { get; set; } = new();

    /// <summary>
    /// The range that should be selected and revealed when this symbol is being picked.
    /// </summary>
    [JsonPropertyName("selectionRange")]
    public GDLspRange SelectionRange { get; set; } = new();

    /// <summary>
    /// Children of this symbol.
    /// </summary>
    [JsonPropertyName("children")]
    public GDLspDocumentSymbol[]? Children { get; set; }
}

/// <summary>
/// The result of a hover request.
/// </summary>
public class GDLspHover
{
    /// <summary>
    /// The hover's content.
    /// </summary>
    [JsonPropertyName("contents")]
    public GDLspMarkupContent Contents { get; set; } = new();

    /// <summary>
    /// An optional range.
    /// </summary>
    [JsonPropertyName("range")]
    public GDLspRange? Range { get; set; }
}

/// <summary>
/// A MarkupContent literal represents a string value which content is interpreted based on its kind flag.
/// </summary>
public class GDLspMarkupContent
{
    /// <summary>
    /// The type of the Markup.
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "plaintext";

    /// <summary>
    /// The content itself.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    public static GDLspMarkupContent PlainText(string text) => new() { Kind = "plaintext", Value = text };
    public static GDLspMarkupContent Markdown(string text) => new() { Kind = "markdown", Value = text };
}
