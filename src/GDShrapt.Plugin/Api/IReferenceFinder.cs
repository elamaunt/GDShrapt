using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.Plugin.Api;

/// <summary>
/// Provides functionality for finding references across the project.
/// </summary>
public interface IReferenceFinder
{
    /// <summary>
    /// Finds all references to a symbol by name.
    /// </summary>
    Task<IReadOnlyList<IReferenceInfo>> FindReferencesAsync(
        string symbolName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all references to a symbol at a specific location.
    /// </summary>
    Task<IReadOnlyList<IReferenceInfo>> FindReferencesAtAsync(
        string filePath,
        int line,
        int column,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts references to a symbol (faster than full find).
    /// </summary>
    int CountReferences(string symbolName);

    /// <summary>
    /// Gets reference counts for all declarations in a script.
    /// </summary>
    IReadOnlyDictionary<string, int> GetReferenceCountsForScript(string filePath);
}
