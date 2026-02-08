using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents a single call site entry in the registry.
/// Contains information about where a method is called from and what it calls.
/// </summary>
public class GDCallSiteEntry : IEquatable<GDCallSiteEntry>
{
    /// <summary>
    /// Full path of the source file containing the call.
    /// </summary>
    public string SourceFilePath { get; }

    /// <summary>
    /// Name of the method containing the call.
    /// May be null for calls at class level (e.g., in variable initializers).
    /// </summary>
    public string? SourceMethodName { get; }

    /// <summary>
    /// Line number of the call (1-based).
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Column number of the call (1-based).
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Name of the class that declares the target method.
    /// For duck-typed calls, this may be inferred or null.
    /// </summary>
    public string TargetClassName { get; }

    /// <summary>
    /// Name of the called method.
    /// </summary>
    public string TargetMethodName { get; }

    /// <summary>
    /// The call expression AST node.
    /// May be null if the AST is no longer available.
    /// </summary>
    public GDCallExpression? CallExpression { get; }

    /// <summary>
    /// Confidence level of the call site resolution.
    /// </summary>
    public GDReferenceConfidence Confidence { get; }

    /// <summary>
    /// Whether this is a duck-typed call (receiver type unknown).
    /// </summary>
    public bool IsDuckTyped { get; }

    /// <summary>
    /// Creates a new call site entry.
    /// </summary>
    public GDCallSiteEntry(
        string sourceFilePath,
        string? sourceMethodName,
        int line,
        int column,
        string targetClassName,
        string targetMethodName,
        GDCallExpression? callExpression = null,
        GDReferenceConfidence confidence = GDReferenceConfidence.Strict,
        bool isDuckTyped = false)
    {
        SourceFilePath = sourceFilePath ?? throw new ArgumentNullException(nameof(sourceFilePath));
        SourceMethodName = sourceMethodName;
        Line = line;
        Column = column;
        TargetClassName = targetClassName ?? throw new ArgumentNullException(nameof(targetClassName));
        TargetMethodName = targetMethodName ?? throw new ArgumentNullException(nameof(targetMethodName));
        CallExpression = callExpression;
        Confidence = confidence;
        IsDuckTyped = isDuckTyped;
    }

    /// <summary>
    /// Creates a call site entry from a GDCallSiteInfo.
    /// </summary>
    /// <param name="info">The call site info from the collector.</param>
    /// <param name="targetClassName">The target class name.</param>
    /// <param name="targetMethodName">The target method name.</param>
    /// <param name="sourceMethodName">The name of the method containing the call.</param>
    internal static GDCallSiteEntry FromCallSiteInfo(
        GDCallSiteInfo info,
        string targetClassName,
        string targetMethodName,
        string? sourceMethodName)
    {
        return new GDCallSiteEntry(
            sourceFilePath: info.FilePath,
            sourceMethodName: sourceMethodName,
            line: info.Line,
            column: info.Column,
            targetClassName: targetClassName,
            targetMethodName: targetMethodName,
            callExpression: info.CallExpression,
            confidence: info.Confidence,
            isDuckTyped: info.IsDuckTyped);
    }

    /// <summary>
    /// Creates a new entry with updated position.
    /// </summary>
    public GDCallSiteEntry WithPosition(int line, int column)
    {
        return new GDCallSiteEntry(
            SourceFilePath,
            SourceMethodName,
            line,
            column,
            TargetClassName,
            TargetMethodName,
            CallExpression,
            Confidence,
            IsDuckTyped);
    }

    public bool Equals(GDCallSiteEntry? other)
    {
        if (other is null)
            return false;

        return string.Equals(SourceFilePath, other.SourceFilePath, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(SourceMethodName, other.SourceMethodName, StringComparison.OrdinalIgnoreCase) &&
               Line == other.Line &&
               Column == other.Column &&
               string.Equals(TargetClassName, other.TargetClassName, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(TargetMethodName, other.TargetMethodName, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as GDCallSiteEntry);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (SourceFilePath?.ToUpperInvariant().GetHashCode() ?? 0);
            hash = hash * 31 + (SourceMethodName?.ToUpperInvariant().GetHashCode() ?? 0);
            hash = hash * 31 + Line;
            hash = hash * 31 + Column;
            hash = hash * 31 + (TargetClassName?.ToUpperInvariant().GetHashCode() ?? 0);
            hash = hash * 31 + (TargetMethodName?.ToUpperInvariant().GetHashCode() ?? 0);
            return hash;
        }
    }

    public override string ToString()
    {
        var source = string.IsNullOrEmpty(SourceMethodName)
            ? $"{System.IO.Path.GetFileName(SourceFilePath)}:{Line}:{Column}"
            : $"{System.IO.Path.GetFileName(SourceFilePath)}.{SourceMethodName}:{Line}:{Column}";

        var target = $"{TargetClassName}.{TargetMethodName}";
        var marker = IsDuckTyped ? "[duck] " : "";

        return $"{marker}{source} → {target}";
    }
}
