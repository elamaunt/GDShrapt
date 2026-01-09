using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.Plugin;

/// <summary>
/// Adapter that wraps GDScriptMap to implement IGDScriptInfo interface from Semantics.
/// This allows the Plugin's script maps to be used with the Semantics type inference system.
/// </summary>
internal class GDScriptMapAdapter : IGDScriptInfo
{
    private readonly GDScriptMap _scriptMap;

    public GDScriptMapAdapter(GDScriptMap scriptMap)
        => _scriptMap = scriptMap ?? throw new System.ArgumentNullException(nameof(scriptMap));

    public string? TypeName => _scriptMap.TypeName;
    public string? FullPath => _scriptMap.Reference?.FullPath;
    public GDClassDeclaration? Class => _scriptMap.Class;
    public bool IsGlobal => _scriptMap.IsGlobal;

    /// <summary>
    /// Gets the underlying GDScriptMap.
    /// </summary>
    public GDScriptMap ScriptMap => _scriptMap;
}
