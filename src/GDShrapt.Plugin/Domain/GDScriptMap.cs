using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Represents a GDScript file with its parsed AST and analysis information.
/// This is the data-only class implementing IGDScriptInfo.
/// UI-specific state (TabController, async waiters) is in GDScriptFileUIBinding.
/// </summary>
/*
internal class GDScriptFile : IGDScriptInfo
{
    private static readonly GDScriptReader Reader = new GDScriptReader();

    private string _fullPath;
    private bool _referencesBuilt;

    private SemaphoreSlim _readingSlim = new (1, 1);

    /// <summary>
    /// Creates a script map with owner and reference.
    /// </summary>
    public GDScriptFile(GDScriptProject? owner, string fullPath)
    {
        Owner = owner;
        _fullPath = fullPath;
    }

    /// <summary>
    /// Creates a standalone script map without owner.
    /// </summary>
    public GDScriptFile(string fullPath)
    {
        _fullPath = fullPath;
    }

    /// <summary>
    /// The project map that owns this script (if any).
    /// </summary>
    public GDScriptProject? Owner { get; }

    /// <summary>
    /// The parsed class declaration.
    /// </summary>
    public GDClassDeclaration? Class { get; private set; }

    /// <summary>
    /// Script analyzer for type inference and reference collection.
    /// </summary>
    public GDScriptAnalyzer? Analyzer { get; private set; }

    public TabController? TabController { get; set; }

    /// <summary>
    /// Whether this script is global (has class_name declaration).
    /// </summary>
    public bool IsGlobal { get; private set; }

    /// <summary>
    /// The type name (from class_name or filename).
    /// </summary>
    public string? TypeName { get; private set; }

    /// <summary>
    /// Whether a read error occurred during parsing.
    /// </summary>
    public bool WasReadError { get; private set; }

    public string FullPath => _fullPath;

    public string ResourcePath => ProjectSettings.LocalizePath(_fullPath);

    /// <summary>
    /// Reloads the script from file or editor content.
    /// This is the synchronous parsing part - async coordination is handled by UIBinding.
    /// </summary>
    /// <param name="editorContent">Optional editor content to parse instead of file.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public async Task<bool> Reload(string? editorContent = null)
    {
        try
        {
            await _readingSlim.WaitAsync();

            Logger.Debug($"Parsing: {Path.GetFileName(_fullPath)}");

            if (editorContent != null)
                Class = Reader.ParseFileContent(editorContent);
            else
                Class = Reader.ParseFile(_fullPath);

            TypeName = Class?.ClassName?.Identifier?.Sequence
                ?? Path.GetFileNameWithoutExtension(_fullPath);
            IsGlobal = Class?.ClassName?.Identifier?.Sequence != null;

            Analyzer = null;

            Logger.Debug($"Loaded: {TypeName}");
            return true;
        }
        catch (Exception ex)
        {
            WasReadError = true;
            Logger.Warning($"Parse error in {Path.GetFileName(_fullPath)}: {ex.Message}");
            _readingSlim.Release();
            return false;
        }
    }

    /// <summary>
    /// Builds analyzer if not already built.
    /// </summary>
    /// <returns>The analyzer, or null if build failed.</returns>
    public GDScriptAnalyzer? BuildAnalyzerIfNeeded()
    {
        if (_referencesBuilt && Analyzer != null)
            return Analyzer;

        Analyzer = null;

        try
        {
            var analyzer = new GDScriptAnalyzer(this);
            var runtimeProvider = Owner?.CreateRuntimeProvider();
            analyzer.Analyze(runtimeProvider);
            Analyzer = analyzer;
        }
        catch (Exception ex)
        {
            Logger.Warning($"Analysis failed: {ex.Message}");
        }

        _referencesBuilt = true;
        return Analyzer;
    }


    public override string ToString()
    {
        return $"{TypeName} ({_fullPath})";
    }
}
*/