using GDShrapt.Abstractions;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

public class GDResourceFlowOptions
{
    public GDTypeConfidence MinimumConfidence { get; init; } = GDTypeConfidence.Low;
    public bool IncludeSceneResources { get; init; } = true;
    public bool IncludeCodeResources { get; init; } = true;
    public bool DetectUnused { get; init; } = false;
    public HashSet<GDResourceCategory>? FilterCategories { get; init; }
}
