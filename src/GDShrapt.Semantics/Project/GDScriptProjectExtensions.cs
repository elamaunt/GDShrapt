using System.Linq;
using System.Threading;

namespace GDShrapt.Semantics;

/// <summary>
/// Extension methods for <see cref="GDScriptProject"/>.
/// </summary>
public static class GDScriptProjectExtensions
{
    /// <summary>
    /// Analyzes all files (if not already analyzed) and builds a project-wide semantic model.
    /// </summary>
    public static GDProjectSemanticModel AnalyzeAndBuildProjectModel(
        this GDScriptProject project,
        CancellationToken cancellationToken = default)
    {
        if (project.ScriptFiles.Any(s => s.SemanticModel == null && s.Class != null))
            project.AnalyzeAll(cancellationToken);

        return new GDProjectSemanticModel(project);
    }
}
