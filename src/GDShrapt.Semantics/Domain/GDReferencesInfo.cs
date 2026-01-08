using System.Collections.Generic;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Tracks references to an identifier within a script.
/// </summary>
public class GDReferencesInfo
{
    private List<GDIdentifier>? _references;

    /// <summary>
    /// The original declaration identifier.
    /// </summary>
    public GDIdentifier? Origin { get; }

    /// <summary>
    /// External name for built-in or cross-file references.
    /// </summary>
    public string? ExternalName { get; }

    /// <summary>
    /// All references to this identifier.
    /// </summary>
    public IReadOnlyList<GDIdentifier> References => _references ?? (IReadOnlyList<GDIdentifier>)System.Array.Empty<GDIdentifier>();

    /// <summary>
    /// Creates reference info for a declaration.
    /// </summary>
    public GDReferencesInfo(GDIdentifier origin)
    {
        Origin = origin;
    }

    /// <summary>
    /// Creates reference info for an external name.
    /// </summary>
    public GDReferencesInfo(string externalName)
    {
        ExternalName = externalName;
    }

    /// <summary>
    /// Adds a reference to this identifier.
    /// </summary>
    public GDReferencesInfo AddReference(GDIdentifier reference)
    {
        _references ??= new List<GDIdentifier>();
        _references.Add(reference);
        return this;
    }

    /// <summary>
    /// Gets total reference count.
    /// </summary>
    public int ReferenceCount => _references?.Count ?? 0;

    public override string ToString()
    {
        var name = Origin?.Sequence ?? ExternalName ?? "(unknown)";
        return $"{name} ({ReferenceCount} references)";
    }
}
