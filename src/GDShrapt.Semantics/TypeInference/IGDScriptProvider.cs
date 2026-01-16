using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Interface for providing script information to the type system.
/// Abstraction over project script map to avoid circular dependencies.
/// </summary>
public interface IGDScriptProvider
{
    /// <summary>
    /// Gets all scripts in the project.
    /// </summary>
    IEnumerable<IGDScriptInfo> Scripts { get; }

    /// <summary>
    /// Gets a script by its type name (class_name).
    /// </summary>
    IGDScriptInfo? GetScriptByTypeName(string typeName);

    /// <summary>
    /// Gets a script by its file path.
    /// </summary>
    IGDScriptInfo? GetScriptByPath(string path);
}

/// <summary>
/// Information about a script for type resolution.
/// </summary>
public interface IGDScriptInfo
{
    /// <summary>
    /// The type name (from class_name declaration) or null if none.
    /// </summary>
    string? TypeName { get; }

    /// <summary>
    /// The full path to the script file.
    /// </summary>
    string? FullPath { get; }

    /// <summary>
    /// The res:// path to the script (Godot resource path).
    /// Used for path-based extends: extends "res://path/to/script.gd"
    /// </summary>
    string? ResPath { get; }

    /// <summary>
    /// The parsed class declaration.
    /// </summary>
    GDClassDeclaration? Class { get; }

    /// <summary>
    /// Whether this script is global (uses class_name).
    /// </summary>
    bool IsGlobal { get; }
}
