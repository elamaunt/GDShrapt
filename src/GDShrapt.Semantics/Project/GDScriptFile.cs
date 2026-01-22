using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents a GDScript file with its parsed AST and analysis information.
/// Godot-independent version of script map for semantic analysis.
/// </summary>
public class GDScriptFile : IGDScriptInfo
{
    private readonly IGDFileSystem _fileSystem;
    private readonly IGDSemanticLogger _logger;
    private GDScriptReference _reference;
    private static readonly GDScriptReader Reader = new();

    /// <summary>
    /// Reference to the script file.
    /// </summary>
    public GDScriptReference Reference => _reference;

    /// <summary>
    /// The parsed class declaration.
    /// </summary>
    public GDClassDeclaration? Class { get; private set; }

    /// <summary>
    /// The semantic model for this script.
    /// Provides unified access to symbols, references, type inference, and semantic analysis.
    /// </summary>
    public GDSemanticModel? SemanticModel { get; private set; }

    /// <summary>
    /// Whether this script is global (has class_name).
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

    /// <summary>
    /// The full path to the script file.
    /// </summary>
    public string? FullPath => _reference.FullPath;

    /// <summary>
    /// The res:// path to the script (Godot resource path).
    /// </summary>
    public string? ResPath => _reference.ResourcePath;

    // IGDScriptInfo implementation
    string? IGDScriptInfo.TypeName => TypeName;
    string? IGDScriptInfo.FullPath => FullPath;
    string? IGDScriptInfo.ResPath => ResPath;
    GDClassDeclaration? IGDScriptInfo.Class => Class;
    bool IGDScriptInfo.IsGlobal => IsGlobal;

    public GDScriptFile(GDScriptReference reference, IGDFileSystem? fileSystem = null, IGDSemanticLogger? logger = null)
    {
        _reference = reference;
        _fileSystem = fileSystem ?? new GDDefaultFileSystem();
        _logger = logger ?? GDNullLogger.Instance;
    }

    /// <summary>
    /// Reloads the script from the file system.
    /// </summary>
    public void Reload()
    {
        try
        {
            var content = _fileSystem.ReadAllText(_reference.FullPath);
            Reload(content);
        }
        catch (Exception ex)
        {
            WasReadError = true;
            _logger.Error($"Failed to read script {Path.GetFileName(_reference.FullPath)}: {ex.Message}");
        }
    }

    /// <summary>
    /// Reloads the script from the provided content.
    /// </summary>
    public void Reload(string content)
    {
        WasReadError = false;
        SemanticModel = null;

        try
        {
            _logger.Debug($"Parsing: {Path.GetFileName(_reference.FullPath)}");

            Class = Reader.ParseFileContent(content);

            TypeName = Class?.ClassName?.Identifier?.Sequence ?? Path.GetFileNameWithoutExtension(_reference.FullPath);
            IsGlobal = Class?.ClassName?.Identifier?.Sequence != null;

            _logger.Debug($"Loaded: {TypeName}");
        }
        catch (Exception ex)
        {
            WasReadError = true;
            _logger.Warning($"Parse error in {Path.GetFileName(_reference.FullPath)}: {ex.Message}");
        }
    }

    /// <summary>
    /// Analyzes the script with the provided runtime provider.
    /// </summary>
    public void Analyze(IGDRuntimeProvider? runtimeProvider = null)
    {
        if (Class == null)
            return;

        try
        {
            SemanticModel = GDSemanticModel.Create(this, runtimeProvider);
            _logger.Debug($"Analysis complete: {SemanticModel.Symbols.Count()} symbols found");
        }
        catch (Exception ex)
        {
            _logger.Warning($"Analysis failed for {TypeName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Changes the reference to this script.
    /// </summary>
    internal void ChangeReference(GDScriptReference newReference)
    {
        _reference = newReference;
    }

    /// <summary>
    /// Validates the script and returns all diagnostics.
    /// </summary>
    /// <param name="options">Optional validation options. Defaults to all checks enabled.</param>
    /// <returns>Validation result with diagnostics, or null if class is not parsed.</returns>
    public GDValidationResult? Validate(GDValidationOptions? options = null)
    {
        if (Class == null)
            return null;

        try
        {
            var validator = new GDValidator();
            options ??= new GDValidationOptions
            {
                CheckSyntax = true,
                CheckScope = true,
                CheckTypes = true,
                CheckCalls = true,
                CheckControlFlow = true,
                RuntimeProvider = SemanticModel?.RuntimeProvider
            };

            return validator.Validate(Class, options);
        }
        catch (Exception ex)
        {
            _logger.Warning($"Validation failed for {TypeName}: {ex.Message}");
            return null;
        }
    }

    public override string ToString()
    {
        return $"{TypeName} ({_reference.FullPath})";
    }
}
