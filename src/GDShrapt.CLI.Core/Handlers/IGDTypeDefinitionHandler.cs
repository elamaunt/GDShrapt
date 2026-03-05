namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for "Go to Type Definition" navigation.
/// </summary>
public interface IGDTypeDefinitionHandler
{
    /// <summary>
    /// Finds the type definition for the symbol at the given position.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="line">Line number (1-based).</param>
    /// <param name="column">Column number (1-based).</param>
    /// <returns>Definition location of the type, or null.</returns>
    GDDefinitionLocation? FindTypeDefinition(string filePath, int line, int column);
}
