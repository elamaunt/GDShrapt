using System.Text.Json.Serialization;

namespace GDShrapt.LSP;

/// <summary>
/// Initialize request parameters.
/// </summary>
public class GDInitializeParams
{
    /// <summary>
    /// The process Id of the parent process that started the server.
    /// </summary>
    [JsonPropertyName("processId")]
    public int? ProcessId { get; set; }

    /// <summary>
    /// The rootUri of the workspace.
    /// </summary>
    [JsonPropertyName("rootUri")]
    public string? RootUri { get; set; }

    /// <summary>
    /// The rootPath of the workspace (deprecated, use rootUri).
    /// </summary>
    [JsonPropertyName("rootPath")]
    public string? RootPath { get; set; }

    /// <summary>
    /// The capabilities provided by the client.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public GDClientCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// User provided initialization options.
    /// </summary>
    [JsonPropertyName("initializationOptions")]
    public object? InitializationOptions { get; set; }
}

/// <summary>
/// Client capabilities.
/// </summary>
public class GDClientCapabilities
{
    /// <summary>
    /// Text document specific client capabilities.
    /// </summary>
    [JsonPropertyName("textDocument")]
    public GDTextDocumentClientCapabilities? TextDocument { get; set; }

    /// <summary>
    /// Workspace specific client capabilities.
    /// </summary>
    [JsonPropertyName("workspace")]
    public GDWorkspaceClientCapabilities? Workspace { get; set; }
}

/// <summary>
/// Text document client capabilities.
/// </summary>
public class GDTextDocumentClientCapabilities
{
    [JsonPropertyName("synchronization")]
    public GDTextDocumentSyncClientCapabilities? Synchronization { get; set; }

    [JsonPropertyName("completion")]
    public GDCompletionClientCapabilities? Completion { get; set; }

    [JsonPropertyName("hover")]
    public GDHoverClientCapabilities? Hover { get; set; }

    [JsonPropertyName("definition")]
    public GDDefinitionClientCapabilities? Definition { get; set; }

    [JsonPropertyName("references")]
    public GDReferencesClientCapabilities? References { get; set; }

    [JsonPropertyName("documentSymbol")]
    public GDDocumentSymbolClientCapabilities? DocumentSymbol { get; set; }
}

public class GDTextDocumentSyncClientCapabilities
{
    [JsonPropertyName("dynamicRegistration")]
    public bool? DynamicRegistration { get; set; }

    [JsonPropertyName("willSave")]
    public bool? WillSave { get; set; }

    [JsonPropertyName("willSaveWaitUntil")]
    public bool? WillSaveWaitUntil { get; set; }

    [JsonPropertyName("didSave")]
    public bool? DidSave { get; set; }
}

public class GDCompletionClientCapabilities
{
    [JsonPropertyName("dynamicRegistration")]
    public bool? DynamicRegistration { get; set; }
}

public class GDHoverClientCapabilities
{
    [JsonPropertyName("dynamicRegistration")]
    public bool? DynamicRegistration { get; set; }

    [JsonPropertyName("contentFormat")]
    public string[]? ContentFormat { get; set; }
}

public class GDDefinitionClientCapabilities
{
    [JsonPropertyName("dynamicRegistration")]
    public bool? DynamicRegistration { get; set; }
}

public class GDReferencesClientCapabilities
{
    [JsonPropertyName("dynamicRegistration")]
    public bool? DynamicRegistration { get; set; }
}

public class GDDocumentSymbolClientCapabilities
{
    [JsonPropertyName("dynamicRegistration")]
    public bool? DynamicRegistration { get; set; }

    [JsonPropertyName("hierarchicalDocumentSymbolSupport")]
    public bool? HierarchicalDocumentSymbolSupport { get; set; }
}

/// <summary>
/// Workspace client capabilities.
/// </summary>
public class GDWorkspaceClientCapabilities
{
    [JsonPropertyName("applyEdit")]
    public bool? ApplyEdit { get; set; }

    [JsonPropertyName("workspaceEdit")]
    public GDWorkspaceEditClientCapabilities? WorkspaceEdit { get; set; }
}

public class GDWorkspaceEditClientCapabilities
{
    [JsonPropertyName("documentChanges")]
    public bool? DocumentChanges { get; set; }
}

/// <summary>
/// Initialize result.
/// </summary>
public class GDInitializeResult
{
    /// <summary>
    /// The capabilities the language server provides.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public GDServerCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// Information about the server.
    /// </summary>
    [JsonPropertyName("serverInfo")]
    public GDServerInfo? ServerInfo { get; set; }
}

/// <summary>
/// Server capabilities.
/// </summary>
public class GDServerCapabilities
{
    /// <summary>
    /// Defines how text documents are synced.
    /// </summary>
    [JsonPropertyName("textDocumentSync")]
    public GDTextDocumentSyncOptions? TextDocumentSync { get; set; }

    /// <summary>
    /// The server provides hover support.
    /// </summary>
    [JsonPropertyName("hoverProvider")]
    public bool? HoverProvider { get; set; }

    /// <summary>
    /// The server provides go to definition support.
    /// </summary>
    [JsonPropertyName("definitionProvider")]
    public bool? DefinitionProvider { get; set; }

    /// <summary>
    /// The server provides find references support.
    /// </summary>
    [JsonPropertyName("referencesProvider")]
    public bool? ReferencesProvider { get; set; }

    /// <summary>
    /// The server provides document symbol support.
    /// </summary>
    [JsonPropertyName("documentSymbolProvider")]
    public bool? DocumentSymbolProvider { get; set; }

    /// <summary>
    /// The server provides rename support.
    /// </summary>
    [JsonPropertyName("renameProvider")]
    public GDRenameOptions? RenameProvider { get; set; }

    /// <summary>
    /// The server provides document highlight support.
    /// </summary>
    [JsonPropertyName("documentHighlightProvider")]
    public bool? DocumentHighlightProvider { get; set; }

    /// <summary>
    /// The server provides folding range support.
    /// </summary>
    [JsonPropertyName("foldingRangeProvider")]
    public bool? FoldingRangeProvider { get; set; }

    /// <summary>
    /// The server provides document formatting support.
    /// </summary>
    [JsonPropertyName("documentFormattingProvider")]
    public bool? DocumentFormattingProvider { get; set; }

    /// <summary>
    /// The server provides completion support.
    /// </summary>
    [JsonPropertyName("completionProvider")]
    public GDCompletionOptions? CompletionProvider { get; set; }

    /// <summary>
    /// The server provides code action support.
    /// </summary>
    [JsonPropertyName("codeActionProvider")]
    public bool? CodeActionProvider { get; set; }

    /// <summary>
    /// The server provides signature help support.
    /// </summary>
    [JsonPropertyName("signatureHelpProvider")]
    public GDSignatureHelpOptions? SignatureHelpProvider { get; set; }

    /// <summary>
    /// The server provides inlay hint support.
    /// </summary>
    [JsonPropertyName("inlayHintProvider")]
    public GDInlayHintOptions? InlayHintProvider { get; set; }

    /// <summary>
    /// The server provides workspace symbol support.
    /// </summary>
    [JsonPropertyName("workspaceSymbolProvider")]
    public bool? WorkspaceSymbolProvider { get; set; }
}

/// <summary>
/// Completion options.
/// </summary>
public class GDCompletionOptions
{
    /// <summary>
    /// The characters that trigger completion automatically.
    /// </summary>
    [JsonPropertyName("triggerCharacters")]
    public string[]? TriggerCharacters { get; set; }

    /// <summary>
    /// The server provides support to resolve additional information for a completion item.
    /// </summary>
    [JsonPropertyName("resolveProvider")]
    public bool? ResolveProvider { get; set; }
}

/// <summary>
/// Text document sync options.
/// </summary>
public class GDTextDocumentSyncOptions
{
    /// <summary>
    /// Open and close notifications are sent to the server.
    /// </summary>
    [JsonPropertyName("openClose")]
    public bool? OpenClose { get; set; }

    /// <summary>
    /// Change notifications are sent to the server.
    /// </summary>
    [JsonPropertyName("change")]
    public GDTextDocumentSyncKind? Change { get; set; }

    /// <summary>
    /// Save notifications are sent to the server.
    /// </summary>
    [JsonPropertyName("save")]
    public GDSaveOptions? Save { get; set; }
}

/// <summary>
/// Text document sync kind.
/// </summary>
public enum GDTextDocumentSyncKind
{
    None = 0,
    Full = 1,
    Incremental = 2
}

/// <summary>
/// Save options.
/// </summary>
public class GDSaveOptions
{
    [JsonPropertyName("includeText")]
    public bool? IncludeText { get; set; }
}

/// <summary>
/// Server info.
/// </summary>
public class GDServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string? Version { get; set; }
}
