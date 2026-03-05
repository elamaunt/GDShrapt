using System.Collections.Generic;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for "Find Implementations" navigation.
/// </summary>
public interface IGDImplementationHandler
{
    /// <summary>
    /// Finds all implementations (overrides/subclasses) of the symbol at the given position.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="line">Line number (1-based).</param>
    /// <param name="column">Column number (1-based).</param>
    /// <returns>List of implementation locations.</returns>
    IReadOnlyList<GDDefinitionLocation> FindImplementations(string filePath, int line, int column);
}
