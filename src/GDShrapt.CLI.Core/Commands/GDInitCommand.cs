using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Creates a .gdshrapt.json configuration file with optional presets.
/// Delegates to GDConfigInitCommand for backward compatibility with 'gdshrapt init'.
/// </summary>
public class GDInitCommand : IGDCommand
{
    private readonly GDConfigInitCommand _inner;

    public string Name => "init";
    public string Description => "Create a .gdshrapt.json configuration file";

    public GDInitCommand(string path, string? preset, bool force)
    {
        _inner = new GDConfigInitCommand(path, preset, force);
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _inner.ExecuteAsync(cancellationToken);
    }
}
