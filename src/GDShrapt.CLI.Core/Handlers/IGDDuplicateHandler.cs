using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler interface for duplicate code detection.
/// </summary>
public interface IGDDuplicateHandler
{
    GDDuplicateReport AnalyzeProject(GDDuplicateOptions options);
    GDDuplicateReport AnalyzeProjectWithBaseline(GDDuplicateOptions options, GDDuplicateReport? baseline);
}
