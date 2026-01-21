using System.Collections.Generic;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for signature help (function parameter hints).
/// </summary>
public interface IGDSignatureHelpHandler
{
    /// <summary>
    /// Gets signature help for a function call at the given position.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="line">Line number (1-based).</param>
    /// <param name="column">Column number (1-based).</param>
    /// <returns>Signature help result or null if not in a function call.</returns>
    GDSignatureHelpResult? GetSignatureHelp(string filePath, int line, int column);
}

/// <summary>
/// Result of signature help request.
/// </summary>
public class GDSignatureHelpResult
{
    /// <summary>
    /// Available signatures for the function.
    /// </summary>
    public IReadOnlyList<GDSignatureInfo> Signatures { get; init; } = [];

    /// <summary>
    /// Index of the active signature.
    /// </summary>
    public int ActiveSignature { get; init; }

    /// <summary>
    /// Index of the active parameter within the active signature.
    /// </summary>
    public int ActiveParameter { get; init; }
}

/// <summary>
/// Information about a function signature.
/// </summary>
public class GDSignatureInfo
{
    /// <summary>
    /// The signature label (e.g., "func print(value: Variant) -> void").
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Documentation for the function.
    /// </summary>
    public string? Documentation { get; init; }

    /// <summary>
    /// Parameters of the function.
    /// </summary>
    public IReadOnlyList<GDParameterInfo> Parameters { get; init; } = [];
}

/// <summary>
/// Information about a function parameter.
/// </summary>
public class GDParameterInfo
{
    /// <summary>
    /// The parameter label (e.g., "value: Variant").
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Documentation for the parameter.
    /// </summary>
    public string? Documentation { get; init; }
}
