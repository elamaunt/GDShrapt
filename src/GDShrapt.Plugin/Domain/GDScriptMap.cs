using GDShrapt.Reader;
using GDShrapt.Semantics;
using System;
using System.IO;

namespace GDShrapt.Plugin;

/// <summary>
/// Represents a GDScript file with its parsed AST and analysis information.
/// This is the data-only class implementing IGDScriptInfo.
/// UI-specific state (TabController, async waiters) is in GDScriptMapUIBinding.
/// </summary>
internal class GDScriptMap : IGDScriptInfo
{
    private static readonly GDScriptReader Reader = new GDScriptReader();

    private GDPluginScriptReference _reference;
    private bool _referencesBuilt;

    /// <summary>
    /// Creates a script map with owner and reference.
    /// </summary>
    public GDScriptMap(GDProjectMap? owner, GDPluginScriptReference reference)
    {
        Owner = owner;
        _reference = reference ?? throw new ArgumentNullException(nameof(reference));
    }

    /// <summary>
    /// Creates a standalone script map without owner.
    /// </summary>
    public GDScriptMap(GDPluginScriptReference reference)
    {
        _reference = reference ?? throw new ArgumentNullException(nameof(reference));
    }

    /// <summary>
    /// The project map that owns this script (if any).
    /// </summary>
    public GDProjectMap? Owner { get; }

    /// <summary>
    /// Reference to the script file.
    /// </summary>
    public GDPluginScriptReference Reference => _reference;

    /// <summary>
    /// The parsed class declaration.
    /// </summary>
    public GDClassDeclaration? Class { get; private set; }

    /// <summary>
    /// Script analyzer for type inference and reference collection.
    /// </summary>
    public GDScriptAnalyzer? Analyzer { get; private set; }

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

    // IGDScriptInfo implementation
    string? IGDScriptInfo.FullPath => _reference.FullPath;
    string? IGDScriptInfo.TypeName => TypeName;
    GDClassDeclaration? IGDScriptInfo.Class => Class;
    bool IGDScriptInfo.IsGlobal => IsGlobal;

    /// <summary>
    /// Reloads the script from file or editor content.
    /// This is the synchronous parsing part - async coordination is handled by UIBinding.
    /// </summary>
    /// <param name="editorContent">Optional editor content to parse instead of file.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public bool Reload(string? editorContent = null)
    {
        WasReadError = false;
        _referencesBuilt = false;
        Analyzer = null;

        try
        {
            Logger.Debug($"Parsing: {Path.GetFileName(_reference.FullPath)}");

            if (editorContent != null)
                Class = Reader.ParseFileContent(editorContent);
            else
                Class = Reader.ParseFile(_reference.FullPath);

            TypeName = Class?.ClassName?.Identifier?.Sequence
                ?? Path.GetFileNameWithoutExtension(_reference.FullPath);
            IsGlobal = Class?.ClassName?.Identifier?.Sequence != null;

            Logger.Debug($"Loaded: {TypeName}");
            return true;
        }
        catch (Exception ex)
        {
            WasReadError = true;
            Logger.Warning($"Parse error in {Path.GetFileName(_reference.FullPath)}: {ex.Message}");
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

    /// <summary>
    /// Changes the reference to this script.
    /// </summary>
    internal void ChangeReference(GDPluginScriptReference newReference)
    {
        _reference = newReference ?? throw new ArgumentNullException(nameof(newReference));
    }

    public override string ToString()
    {
        return $"{TypeName} ({_reference.FullPath})";
    }
}
