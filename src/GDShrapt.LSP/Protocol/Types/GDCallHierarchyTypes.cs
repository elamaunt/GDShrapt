using System.Text.Json.Serialization;

namespace GDShrapt.LSP;

/// <summary>
/// Parameters for textDocument/prepareCallHierarchy.
/// </summary>
public class GDCallHierarchyPrepareParams
{
    [JsonPropertyName("textDocument")]
    public GDLspTextDocumentIdentifier TextDocument { get; set; } = new();

    [JsonPropertyName("position")]
    public GDLspPosition Position { get; set; } = new();
}

/// <summary>
/// A call hierarchy item representing a method.
/// </summary>
public class GDLspCallHierarchyItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public int Kind { get; set; } = 12; // SymbolKind.Function

    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("range")]
    public GDLspRange Range { get; set; } = new();

    [JsonPropertyName("selectionRange")]
    public GDLspRange SelectionRange { get; set; } = new();

    [JsonPropertyName("detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; set; }

    /// <summary>
    /// Round-trip data for incoming/outgoing call resolution.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GDCallHierarchyItemData? Data { get; set; }
}

/// <summary>
/// Round-trip data stored in call hierarchy items.
/// </summary>
public class GDCallHierarchyItemData
{
    [JsonPropertyName("filePath")]
    public string? FilePath { get; set; }

    [JsonPropertyName("className")]
    public string? ClassName { get; set; }

    [JsonPropertyName("methodName")]
    public string? MethodName { get; set; }

    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("column")]
    public int Column { get; set; }
}

/// <summary>
/// Parameters for callHierarchy/incomingCalls.
/// </summary>
public class GDCallHierarchyIncomingCallsParams
{
    [JsonPropertyName("item")]
    public GDLspCallHierarchyItem Item { get; set; } = new();
}

/// <summary>
/// Parameters for callHierarchy/outgoingCalls.
/// </summary>
public class GDCallHierarchyOutgoingCallsParams
{
    [JsonPropertyName("item")]
    public GDLspCallHierarchyItem Item { get; set; } = new();
}

/// <summary>
/// An incoming call result.
/// </summary>
public class GDLspCallHierarchyIncomingCall
{
    [JsonPropertyName("from")]
    public GDLspCallHierarchyItem From { get; set; } = new();

    [JsonPropertyName("fromRanges")]
    public GDLspRange[] FromRanges { get; set; } = [];
}

/// <summary>
/// An outgoing call result.
/// </summary>
public class GDLspCallHierarchyOutgoingCall
{
    [JsonPropertyName("to")]
    public GDLspCallHierarchyItem To { get; set; } = new();

    [JsonPropertyName("fromRanges")]
    public GDLspRange[] FromRanges { get; set; } = [];
}
