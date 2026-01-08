using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.Plugin.Api;

/// <summary>
/// Provides type resolution functionality.
/// </summary>
public interface ITypeResolver
{
    /// <summary>
    /// Gets the inferred type for an identifier at a location.
    /// </summary>
    Task<ITypeInfo?> GetTypeAtAsync(
        string filePath,
        int line,
        int column,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets members of a type.
    /// </summary>
    IReadOnlyList<ISymbolInfo> GetTypeMembers(string typeName);

    /// <summary>
    /// Checks if a type exists in the project or Godot API.
    /// </summary>
    bool TypeExists(string typeName);
}

/// <summary>
/// Information about an inferred type.
/// </summary>
public interface ITypeInfo
{
    /// <summary>
    /// Type name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether this is a built-in Godot type.
    /// </summary>
    bool IsBuiltin { get; }

    /// <summary>
    /// Whether this is a script-defined type.
    /// </summary>
    bool IsScriptType { get; }

    /// <summary>
    /// Base type if known.
    /// </summary>
    string? BaseType { get; }
}
