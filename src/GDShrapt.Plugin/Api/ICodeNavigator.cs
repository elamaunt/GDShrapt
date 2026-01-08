using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.Plugin.Api;

/// <summary>
/// Provides code navigation functionality.
/// </summary>
public interface ICodeNavigator
{
    /// <summary>
    /// Finds the definition of a symbol at the given location.
    /// </summary>
    Task<ILocationInfo?> FindDefinitionAsync(
        string filePath,
        int line,
        int column,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the definition of a symbol by name.
    /// </summary>
    Task<ILocationInfo?> FindDefinitionByNameAsync(
        string symbolName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a location in code.
/// </summary>
public interface ILocationInfo
{
    /// <summary>
    /// File path.
    /// </summary>
    string FilePath { get; }

    /// <summary>
    /// Line number (0-based).
    /// </summary>
    int Line { get; }

    /// <summary>
    /// Column number (0-based).
    /// </summary>
    int Column { get; }
}
