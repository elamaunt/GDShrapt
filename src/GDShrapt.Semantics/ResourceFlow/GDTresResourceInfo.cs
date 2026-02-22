using System;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Parsed and enriched information from a .tres resource file.
/// </summary>
internal class GDTresResourceInfo
{
    /// <summary>Resource path (res://...)</summary>
    public string ResourcePath { get; init; } = "";

    /// <summary>Full filesystem path</summary>
    public string FullPath { get; init; } = "";

    /// <summary>Type from [gd_resource type="..."]</summary>
    public string? ResourceType { get; init; }

    /// <summary>script_class from header, if present</summary>
    public string? ScriptClass { get; init; }

    /// <summary>Resolved GDScript path (from script = ExtResource)</summary>
    public string? ScriptPath { get; init; }

    /// <summary>Resolved GDScript class_name (from script file)</summary>
    public string? ResolvedClassName { get; set; }

    /// <summary>
    /// Gets the effective class name: script_class takes priority,
    /// then resolved class_name from the script file.
    /// </summary>
    public string? EffectiveClassName => ScriptClass ?? ResolvedClassName;

    /// <summary>Property assignments from [resource] section</summary>
    public IReadOnlyList<GDTresProperty> Properties { get; init; } = Array.Empty<GDTresProperty>();

    /// <summary>External resources</summary>
    public IReadOnlyList<GDTresExtResource> ExtResources { get; init; } = Array.Empty<GDTresExtResource>();
}
