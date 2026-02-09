using System;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

public class GDResourceFlowReport
{
    public IReadOnlyList<GDResourceFlowEdge> AllEdges { get; init; } = Array.Empty<GDResourceFlowEdge>();
    public IReadOnlyDictionary<string, GDResourceNode> Resources { get; init; } = new Dictionary<string, GDResourceNode>();
    public IReadOnlyList<GDResourceFlowWarning> Warnings { get; init; } = Array.Empty<GDResourceFlowWarning>();
    public int TotalResources { get; init; }
    public int TotalEdges { get; init; }
    public IReadOnlyDictionary<GDResourceCategory, int> ResourcesByCategory { get; init; } = new Dictionary<GDResourceCategory, int>();
    public IReadOnlyList<string> UnusedResources { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MissingResources { get; init; } = Array.Empty<string>();
}

public class GDResourceFlowWarning
{
    public string Message { get; init; } = "";
    public string? ResourcePath { get; init; }
    public string? SourceFile { get; init; }
    public int? LineNumber { get; init; }
}
