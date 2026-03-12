using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Information about a node group: which types belong to it and from where.
/// </summary>
internal class GDGroupInfo
{
    public string GroupName { get; init; } = "";
    public List<GDGroupMembership> Members { get; } = new();
}

/// <summary>
/// A single membership record: a type belongs to a group.
/// </summary>
internal class GDGroupMembership
{
    public string TypeName { get; init; } = "";
    public GDGroupSource Source { get; init; }
    public string? SourcePath { get; init; }
}

internal enum GDGroupSource
{
    SceneFile,
    CodeAddToGroup
}
