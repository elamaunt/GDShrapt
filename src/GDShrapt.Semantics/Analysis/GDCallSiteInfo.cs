using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Contains information about a method call site.
/// Used for parameter type inference from usage.
/// </summary>
public class GDCallSiteInfo
{
    /// <summary>
    /// The call expression node.
    /// </summary>
    public GDCallExpression CallExpression { get; }

    /// <summary>
    /// The source script where this call occurs.
    /// </summary>
    public GDScriptFile SourceScript { get; }

    /// <summary>
    /// The arguments at this call site with their inferred types.
    /// </summary>
    public IReadOnlyList<GDArgumentInfo> Arguments { get; }

    /// <summary>
    /// The type of the receiver expression (if method call on object).
    /// Null for direct function calls.
    /// </summary>
    public string? ReceiverType { get; }

    /// <summary>
    /// Whether this call site comes from a duck-typed receiver.
    /// </summary>
    public bool IsDuckTyped { get; }

    /// <summary>
    /// If duck-typed, the name of the receiver variable.
    /// </summary>
    public string? ReceiverVariableName { get; }

    /// <summary>
    /// If the receiver has a Union type, the full Union type string.
    /// </summary>
    public string? UnionReceiverType { get; }

    /// <summary>
    /// Confidence level of this call site resolution.
    /// </summary>
    public GDReferenceConfidence Confidence { get; }

    /// <summary>
    /// Whether this call site comes from a dynamic call (obj.call("method", args)).
    /// </summary>
    public bool IsDynamicCall { get; set; }

    /// <summary>
    /// Line number of the call site.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Column number of the call site.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Full file path of the source script.
    /// </summary>
    public string FilePath => SourceScript.FullPath ?? "";

    /// <summary>
    /// Resource path of the source script (res://...).
    /// </summary>
    public string? ResPath => SourceScript.ResPath;

    /// <summary>
    /// Creates a new call site info.
    /// </summary>
    public GDCallSiteInfo(
        GDCallExpression callExpression,
        GDScriptFile sourceScript,
        IReadOnlyList<GDArgumentInfo> arguments,
        string? receiverType,
        GDReferenceConfidence confidence)
    {
        CallExpression = callExpression;
        SourceScript = sourceScript;
        Arguments = arguments;
        ReceiverType = receiverType;
        Confidence = confidence;
        IsDuckTyped = false;

        var token = callExpression.AllTokens.FirstOrDefault();
        Line = token?.StartLine ?? 0;
        Column = token?.StartColumn ?? 0;
    }

    /// <summary>
    /// Creates a duck-typed call site info.
    /// </summary>
    public static GDCallSiteInfo CreateDuckTyped(
        GDCallExpression callExpression,
        GDScriptFile sourceScript,
        IReadOnlyList<GDArgumentInfo> arguments,
        string receiverVariableName)
    {
        return new GDCallSiteInfo(
            callExpression,
            sourceScript,
            arguments,
            receiverType: null,
            GDReferenceConfidence.Potential,
            isDuckTyped: true,
            receiverVariableName: receiverVariableName,
            unionReceiverType: null);
    }

    /// <summary>
    /// Creates a call site info with Union receiver type.
    /// </summary>
    public static GDCallSiteInfo CreateWithUnionReceiver(
        GDCallExpression callExpression,
        GDScriptFile sourceScript,
        IReadOnlyList<GDArgumentInfo> arguments,
        string receiverType,
        string unionReceiverType,
        GDReferenceConfidence confidence)
    {
        return new GDCallSiteInfo(
            callExpression,
            sourceScript,
            arguments,
            receiverType,
            confidence,
            isDuckTyped: false,
            receiverVariableName: null,
            unionReceiverType: unionReceiverType);
    }

    // Private constructor for factory methods
    private GDCallSiteInfo(
        GDCallExpression callExpression,
        GDScriptFile sourceScript,
        IReadOnlyList<GDArgumentInfo> arguments,
        string? receiverType,
        GDReferenceConfidence confidence,
        bool isDuckTyped,
        string? receiverVariableName,
        string? unionReceiverType)
    {
        CallExpression = callExpression;
        SourceScript = sourceScript;
        Arguments = arguments;
        ReceiverType = receiverType;
        Confidence = confidence;
        IsDuckTyped = isDuckTyped;
        ReceiverVariableName = receiverVariableName;
        UnionReceiverType = unionReceiverType;

        var token = callExpression.AllTokens.FirstOrDefault();
        Line = token?.StartLine ?? 0;
        Column = token?.StartColumn ?? 0;
    }

    /// <summary>
    /// Gets the argument at the specified index, or null if out of range.
    /// </summary>
    public GDArgumentInfo? GetArgument(int index)
    {
        return index >= 0 && index < Arguments.Count ? Arguments[index] : null;
    }

    public override string ToString()
    {
        var location = $"{System.IO.Path.GetFileName(FilePath)}:{Line}:{Column}";
        var argTypes = string.Join(", ", Arguments.Select(a => a.InferredType ?? "?"));
        var confidence = Confidence.ToString().ToLower();

        if (IsDuckTyped)
            return $"[duck] {ReceiverVariableName}.call({argTypes}) @ {location}";

        if (!string.IsNullOrEmpty(UnionReceiverType))
            return $"[union:{UnionReceiverType}].call({argTypes}) @ {location} ({confidence})";

        return $"{ReceiverType ?? "?"}.call({argTypes}) @ {location} ({confidence})";
    }
}
