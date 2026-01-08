using System.Text.Json.Serialization;

namespace GDShrapt.LSP.Protocol.Types;

/// <summary>
/// DidOpenTextDocument notification parameters.
/// </summary>
public class GDDidOpenTextDocumentParams
{
    /// <summary>
    /// The document that was opened.
    /// </summary>
    [JsonPropertyName("textDocument")]
    public GDLspTextDocumentItem TextDocument { get; set; } = new();
}

/// <summary>
/// DidChangeTextDocument notification parameters.
/// </summary>
public class GDDidChangeTextDocumentParams
{
    /// <summary>
    /// The document that did change.
    /// </summary>
    [JsonPropertyName("textDocument")]
    public GDLspVersionedTextDocumentIdentifier TextDocument { get; set; } = new();

    /// <summary>
    /// The actual content changes.
    /// </summary>
    [JsonPropertyName("contentChanges")]
    public GDTextDocumentContentChangeEvent[] ContentChanges { get; set; } = [];
}

/// <summary>
/// An event describing a change to a text document.
/// </summary>
public class GDTextDocumentContentChangeEvent
{
    /// <summary>
    /// The range of the document that changed (for incremental sync).
    /// </summary>
    [JsonPropertyName("range")]
    public GDLspRange? Range { get; set; }

    /// <summary>
    /// The length of the range that got replaced (for incremental sync).
    /// </summary>
    [JsonPropertyName("rangeLength")]
    public int? RangeLength { get; set; }

    /// <summary>
    /// The new text of the range/document.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// DidCloseTextDocument notification parameters.
/// </summary>
public class GDDidCloseTextDocumentParams
{
    /// <summary>
    /// The document that was closed.
    /// </summary>
    [JsonPropertyName("textDocument")]
    public GDLspTextDocumentIdentifier TextDocument { get; set; } = new();
}

/// <summary>
/// DidSaveTextDocument notification parameters.
/// </summary>
public class GDDidSaveTextDocumentParams
{
    /// <summary>
    /// The document that was saved.
    /// </summary>
    [JsonPropertyName("textDocument")]
    public GDLspTextDocumentIdentifier TextDocument { get; set; } = new();

    /// <summary>
    /// Optional the content when saved. Depends on the includeText value when the save notification was requested.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

/// <summary>
/// PublishDiagnostics notification parameters.
/// </summary>
public class GDPublishDiagnosticsParams
{
    /// <summary>
    /// The URI for which diagnostic information is reported.
    /// </summary>
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// Optional the version number of the document the diagnostics are published for.
    /// </summary>
    [JsonPropertyName("version")]
    public int? Version { get; set; }

    /// <summary>
    /// An array of diagnostic information items.
    /// </summary>
    [JsonPropertyName("diagnostics")]
    public GDLspDiagnostic[] Diagnostics { get; set; } = [];
}

/// <summary>
/// Definition request parameters.
/// </summary>
public class GDDefinitionParams : GDLspTextDocumentPositionParams
{
}

/// <summary>
/// References request parameters.
/// </summary>
public class GDReferencesParams : GDLspTextDocumentPositionParams
{
    /// <summary>
    /// Context carrying additional information.
    /// </summary>
    [JsonPropertyName("context")]
    public GDReferenceContext Context { get; set; } = new();
}

/// <summary>
/// Reference context.
/// </summary>
public class GDReferenceContext
{
    /// <summary>
    /// Include the declaration of the current symbol.
    /// </summary>
    [JsonPropertyName("includeDeclaration")]
    public bool IncludeDeclaration { get; set; }
}

/// <summary>
/// Hover request parameters.
/// </summary>
public class GDHoverParams : GDLspTextDocumentPositionParams
{
}

/// <summary>
/// DocumentSymbol request parameters.
/// </summary>
public class GDDocumentSymbolParams
{
    /// <summary>
    /// The text document.
    /// </summary>
    [JsonPropertyName("textDocument")]
    public GDLspTextDocumentIdentifier TextDocument { get; set; } = new();
}

/// <summary>
/// Rename request parameters.
/// </summary>
public class GDRenameParams : GDLspTextDocumentPositionParams
{
    /// <summary>
    /// The new name of the symbol.
    /// </summary>
    [JsonPropertyName("newName")]
    public string NewName { get; set; } = string.Empty;
}

/// <summary>
/// A workspace edit represents changes to many resources managed in the workspace.
/// </summary>
public class GDWorkspaceEdit
{
    /// <summary>
    /// Holds changes to existing resources.
    /// </summary>
    [JsonPropertyName("changes")]
    public System.Collections.Generic.Dictionary<string, GDLspTextEdit[]>? Changes { get; set; }
}
