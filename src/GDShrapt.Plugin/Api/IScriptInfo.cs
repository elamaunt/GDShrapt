using System.Collections.Generic;
using GDShrapt.Reader;

namespace GDShrapt.Plugin.Api;

/// <summary>
/// Provides information about a single GDScript file.
/// </summary>
public interface IScriptInfo
{
    /// <summary>
    /// Full file system path to the script.
    /// </summary>
    string FullPath { get; }

    /// <summary>
    /// Resource path (e.g., "res://scripts/player.gd").
    /// </summary>
    string ResourcePath { get; }

    /// <summary>
    /// Class/type name if defined with class_name.
    /// </summary>
    string? TypeName { get; }

    /// <summary>
    /// Whether this is a global class (autoload or class_name).
    /// </summary>
    bool IsGlobal { get; }

    /// <summary>
    /// Whether there was an error parsing this script.
    /// </summary>
    bool HasParseErrors { get; }

    /// <summary>
    /// Gets the AST root node (GDClassDeclaration from GDShrapt.Reader).
    /// For advanced use cases requiring direct AST access.
    /// </summary>
    GDClassDeclaration? AstRoot { get; }

    /// <summary>
    /// Gets all declarations in this script.
    /// </summary>
    IReadOnlyList<ISymbolInfo> GetDeclarations();

    /// <summary>
    /// Gets all identifiers in this script.
    /// </summary>
    IReadOnlyList<IIdentifierInfo> GetIdentifiers();
}
