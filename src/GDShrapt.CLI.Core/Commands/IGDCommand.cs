using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Interface for CLI commands.
/// </summary>
public interface IGDCommand
{
    /// <summary>
    /// Gets the command name used in CLI.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the command description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code: 0 for success, 1 for errors in code, 2 for CLI errors.</returns>
    Task<int> ExecuteAsync(CancellationToken cancellationToken = default);
}
