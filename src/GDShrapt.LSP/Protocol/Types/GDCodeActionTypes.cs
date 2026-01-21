using System.Text.Json.Serialization;

namespace GDShrapt.LSP;

/// <summary>
/// CodeAction request parameters.
/// </summary>
public class GDCodeActionParams
{
    /// <summary>
    /// The document in which the command was invoked.
    /// </summary>
    [JsonPropertyName("textDocument")]
    public GDLspTextDocumentIdentifier TextDocument { get; set; } = new();

    /// <summary>
    /// The range for which the command was invoked.
    /// </summary>
    [JsonPropertyName("range")]
    public GDLspRange Range { get; set; } = new();

    /// <summary>
    /// Context carrying additional information.
    /// </summary>
    [JsonPropertyName("context")]
    public GDCodeActionContext Context { get; set; } = new();
}

/// <summary>
/// Contains additional diagnostic information about the context in which a code action is run.
/// </summary>
public class GDCodeActionContext
{
    /// <summary>
    /// An array of diagnostics known on the client side overlapping the range.
    /// </summary>
    [JsonPropertyName("diagnostics")]
    public GDLspDiagnostic[] Diagnostics { get; set; } = [];

    /// <summary>
    /// Requested kind of actions to return.
    /// </summary>
    [JsonPropertyName("only")]
    public string[]? Only { get; set; }

    /// <summary>
    /// The reason why code actions were requested.
    /// </summary>
    [JsonPropertyName("triggerKind")]
    public GDCodeActionTriggerKind? TriggerKind { get; set; }
}

/// <summary>
/// The reason why code actions were requested.
/// </summary>
public enum GDCodeActionTriggerKind
{
    /// <summary>
    /// Code actions were explicitly requested by the user or by an extension.
    /// </summary>
    Invoked = 1,

    /// <summary>
    /// Code actions were requested automatically.
    /// </summary>
    Automatic = 2
}

/// <summary>
/// A code action represents a change that can be performed in code.
/// </summary>
public class GDLspCodeAction
{
    /// <summary>
    /// A short, human-readable, title for this code action.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The kind of the code action.
    /// </summary>
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    /// <summary>
    /// The diagnostics that this code action resolves.
    /// </summary>
    [JsonPropertyName("diagnostics")]
    public GDLspDiagnostic[]? Diagnostics { get; set; }

    /// <summary>
    /// Marks this as a preferred action.
    /// </summary>
    [JsonPropertyName("isPreferred")]
    public bool? IsPreferred { get; set; }

    /// <summary>
    /// The workspace edit this code action performs.
    /// </summary>
    [JsonPropertyName("edit")]
    public GDWorkspaceEdit? Edit { get; set; }

    /// <summary>
    /// A command this code action executes.
    /// </summary>
    [JsonPropertyName("command")]
    public GDLspCommand? Command { get; set; }

    /// <summary>
    /// A data entry field that is preserved on a code action between
    /// a `textDocument/codeAction` and a `codeAction/resolve` request.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

/// <summary>
/// Code action options.
/// </summary>
public class GDCodeActionOptions
{
    /// <summary>
    /// CodeActionKinds that this server may return.
    /// </summary>
    [JsonPropertyName("codeActionKinds")]
    public string[]? CodeActionKinds { get; set; }

    /// <summary>
    /// The server provides support to resolve additional information for a code action.
    /// </summary>
    [JsonPropertyName("resolveProvider")]
    public bool? ResolveProvider { get; set; }
}

/// <summary>
/// Code action kind constants.
/// </summary>
public static class GDCodeActionKind
{
    /// <summary>
    /// Empty kind.
    /// </summary>
    public const string Empty = "";

    /// <summary>
    /// Base kind for quickfix actions.
    /// </summary>
    public const string QuickFix = "quickfix";

    /// <summary>
    /// Base kind for refactoring actions.
    /// </summary>
    public const string Refactor = "refactor";

    /// <summary>
    /// Base kind for refactoring extraction actions.
    /// </summary>
    public const string RefactorExtract = "refactor.extract";

    /// <summary>
    /// Base kind for refactoring inline actions.
    /// </summary>
    public const string RefactorInline = "refactor.inline";

    /// <summary>
    /// Base kind for refactoring rewrite actions.
    /// </summary>
    public const string RefactorRewrite = "refactor.rewrite";

    /// <summary>
    /// Base kind for source actions.
    /// </summary>
    public const string Source = "source";

    /// <summary>
    /// Base kind for an organize imports source action.
    /// </summary>
    public const string SourceOrganizeImports = "source.organizeImports";

    /// <summary>
    /// Base kind for a fix all source action.
    /// </summary>
    public const string SourceFixAll = "source.fixAll";
}
