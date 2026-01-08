using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Reader;

namespace GDShrapt.Plugin.Api.Internal;

/// <summary>
/// Implementation of ICodeNavigator that uses GDProjectMap.
/// </summary>
internal class CodeNavigatorImpl : ICodeNavigator
{
    private readonly GDProjectMap _projectMap;

    public CodeNavigatorImpl(GDProjectMap projectMap)
    {
        _projectMap = projectMap;
    }

    public Task<ILocationInfo?> FindDefinitionAsync(
        string filePath,
        int line,
        int column,
        CancellationToken cancellationToken = default)
    {
        var script = _projectMap.GetScriptMap(filePath);
        if (script?.Class == null)
            return Task.FromResult<ILocationInfo?>(null);

        // Find the identifier at the given position
        GDIdentifier? targetIdentifier = null;
        foreach (var token in script.Class.AllTokens)
        {
            if (token is GDIdentifier identifier &&
                identifier.StartLine == line &&
                identifier.StartColumn <= column &&
                identifier.EndColumn >= column)
            {
                targetIdentifier = identifier;
                break;
            }
        }

        if (targetIdentifier == null)
            return Task.FromResult<ILocationInfo?>(null);

        return FindDefinitionByNameAsync(targetIdentifier.Sequence ?? string.Empty, cancellationToken);
    }

    public Task<ILocationInfo?> FindDefinitionByNameAsync(
        string symbolName,
        CancellationToken cancellationToken = default)
    {
        // Search in all scripts for the declaration
        foreach (var script in _projectMap.Scripts)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (script.Class == null)
                continue;

            // Check class name
            if (script.TypeName == symbolName && script.Class.ClassName?.Identifier != null)
            {
                var id = script.Class.ClassName.Identifier;
                return Task.FromResult<ILocationInfo?>(new LocationInfoImpl(
                    script.Reference?.FullPath ?? string.Empty,
                    id.StartLine,
                    id.StartColumn
                ));
            }

            // Check members
            foreach (var member in script.Class.Members)
            {
                if (member is GDIdentifiableClassMember identifiable &&
                    identifiable.Identifier?.Sequence == symbolName)
                {
                    return Task.FromResult<ILocationInfo?>(new LocationInfoImpl(
                        script.Reference?.FullPath ?? string.Empty,
                        identifiable.Identifier.StartLine,
                        identifiable.Identifier.StartColumn
                    ));
                }
            }
        }

        return Task.FromResult<ILocationInfo?>(null);
    }
}

internal class LocationInfoImpl : ILocationInfo
{
    public LocationInfoImpl(string filePath, int line, int column)
    {
        FilePath = filePath;
        Line = line;
        Column = column;
    }

    public string FilePath { get; }
    public int Line { get; }
    public int Column { get; }
}
