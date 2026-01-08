using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents a GDScript project with all its scripts.
/// Godot-independent version for semantic analysis.
/// Implements IGDScriptProvider for type resolution.
/// </summary>
/// <remarks>
/// <para>
/// Thread Safety: This class is thread-safe. The internal scripts collection
/// uses <see cref="ConcurrentDictionary{TKey, TValue}"/> ensuring safe concurrent access.
/// Multiple threads can safely read and enumerate scripts while modifications occur.
/// </para>
/// <para>
/// The <see cref="ScriptFiles"/> property returns a snapshot of the values collection,
/// providing consistent iteration even during concurrent modifications.
/// </para>
/// </remarks>
public class GDScriptProject : IGDScriptProvider, IDisposable
{
    private readonly ConcurrentDictionary<GDScriptReference, GDScriptFile> _scripts = new();
    private readonly IGDProjectContext _context;
    private readonly IGDFileSystem _fileSystem;
    private readonly IGDSemanticLogger _logger;
    private readonly GDSceneTypesProvider? _sceneTypesProvider;
    private readonly bool _enableFileWatcher;
    private FileSystemWatcher? _scriptsWatcher;
    private bool _disposed;

    #region Events

    /// <summary>
    /// Fired when a script file is changed on disk.
    /// </summary>
    public event EventHandler<GDScriptFileEventArgs>? ScriptChanged;

    /// <summary>
    /// Fired when a new script file is created.
    /// </summary>
    public event EventHandler<GDScriptFileEventArgs>? ScriptCreated;

    /// <summary>
    /// Fired when a script file is deleted.
    /// </summary>
    public event EventHandler<GDScriptFileEventArgs>? ScriptDeleted;

    /// <summary>
    /// Fired when a script file is renamed.
    /// </summary>
    public event EventHandler<GDScriptRenamedEventArgs>? ScriptRenamed;

    #endregion

    /// <summary>
    /// The project path.
    /// </summary>
    public string ProjectPath => _context.ProjectPath;

    /// <summary>
    /// All scripts in the project.
    /// </summary>
    public IEnumerable<GDScriptFile> ScriptFiles => _scripts.Values;

    /// <summary>
    /// Scene types provider for node path type resolution.
    /// </summary>
    public GDSceneTypesProvider? SceneTypesProvider => _sceneTypesProvider;

    /// <summary>
    /// Logger for diagnostic output.
    /// </summary>
    public IGDSemanticLogger Logger => _logger;

    // IGDScriptProvider implementation
    IEnumerable<IGDScriptInfo> IGDScriptProvider.Scripts => _scripts.Values;

    public GDScriptProject(IGDProjectContext context, GDScriptProjectOptions? options = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _fileSystem = options?.FileSystem ?? new GDDefaultFileSystem();
        _logger = options?.Logger ?? GDNullLogger.Instance;
        _enableFileWatcher = options?.EnableFileWatcher ?? false;

        if (options?.EnableSceneTypesProvider == true)
        {
            _sceneTypesProvider = new GDSceneTypesProvider(_context.ProjectPath, _fileSystem, _logger);
        }

        _logger.Debug("Project created");
    }

    /// <summary>
    /// Creates a project from scripts content (for testing).
    /// </summary>
    public GDScriptProject(params string[] scriptContents)
    {
        _context = new GDDefaultProjectContext(".");
        _fileSystem = new GDDefaultFileSystem();
        _logger = GDNullLogger.Instance;

        for (int i = 0; i < scriptContents.Length; i++)
        {
            var reference = new GDScriptReference(i.ToString());
            var scriptFile = new GDScriptFile(reference, _fileSystem, _logger);
            _scripts.TryAdd(reference, scriptFile);
            scriptFile.Reload(scriptContents[i]);
        }

        _logger.Debug($"Project created with {scriptContents.Length} scripts");
    }

    /// <summary>
    /// Loads all scripts from the project directory.
    /// </summary>
    public void LoadScripts()
    {
        if (!_fileSystem.DirectoryExists(_context.ProjectPath))
        {
            _logger.Warning($"Project path does not exist: {_context.ProjectPath}");
            return;
        }

        var allScripts = _fileSystem.GetFiles(_context.ProjectPath, "*.gd", recursive: true);

        foreach (var scriptFile in allScripts)
        {
            _logger.Debug($"Loading script '{Path.GetFileName(scriptFile)}'");

            var reference = new GDScriptReference(scriptFile);
            var script = new GDScriptFile(reference, _fileSystem, _logger);
            _scripts.TryAdd(reference, script);
            script.Reload();
        }

        _logger.Info($"Project loaded: {_scripts.Count} scripts");

        if (_enableFileWatcher)
        {
            EnableFileWatcher();
        }
    }

    /// <summary>
    /// Reloads all scripts in the project.
    /// </summary>
    public void ReloadAll()
    {
        foreach (var script in _scripts.Values)
        {
            script.Reload();
        }
    }

    /// <summary>
    /// Loads scenes from the project directory.
    /// </summary>
    public void LoadScenes()
    {
        _sceneTypesProvider?.ReloadAllScenes();
    }

    /// <summary>
    /// Analyzes all scripts with type resolution.
    /// </summary>
    public void AnalyzeAll()
    {
        var runtimeProvider = CreateRuntimeProvider();

        foreach (var script in _scripts.Values)
        {
            script.Analyze(runtimeProvider);
        }
    }

    /// <summary>
    /// Gets a script by its full path.
    /// </summary>
    public GDScriptFile? GetScript(string fullPath)
    {
        return _scripts.GetValueOrDefault(new GDScriptReference(fullPath));
    }

    /// <summary>
    /// Gets a script by reference.
    /// </summary>
    public GDScriptFile? GetScript(GDScriptReference reference)
    {
        return _scripts.GetValueOrDefault(reference);
    }

    /// <summary>
    /// Gets a script by its type name (class_name).
    /// </summary>
    public GDScriptFile? GetScriptByTypeName(string typeName)
    {
        return _scripts.Values.FirstOrDefault(x => x.TypeName == typeName);
    }

    /// <summary>
    /// Gets a script by resource path (res://).
    /// </summary>
    public GDScriptFile? GetScriptByResourcePath(string resourcePath)
    {
        var globalPath = _context.GlobalizePath(resourcePath);
        return GetScript(new GDScriptReference(globalPath));
    }

    /// <summary>
    /// Gets a script by its class declaration.
    /// </summary>
    public GDScriptFile? GetScriptByClass(GDClassDeclaration classDecl)
    {
        if (classDecl == null) return null;
        return _scripts.Values.FirstOrDefault(x => x.Class == classDecl);
    }

    /// <summary>
    /// Creates a runtime provider for type resolution across the project.
    /// </summary>
    public IGDRuntimeProvider CreateRuntimeProvider()
    {
        var godotTypesProvider = new GDGodotTypesProvider();
        var projectTypesProvider = new GDProjectTypesProvider(this);
        projectTypesProvider.RebuildCache();

        return new GDCompositeRuntimeProvider(
            godotTypesProvider,
            projectTypesProvider,
            _sceneTypesProvider);
    }

    /// <summary>
    /// Creates a type resolver for this project.
    /// </summary>
    public GDTypeResolver CreateTypeResolver()
    {
        var godotTypesProvider = new GDGodotTypesProvider();
        var projectTypesProvider = new GDProjectTypesProvider(this);
        projectTypesProvider.RebuildCache();

        return new GDTypeResolver(
            godotTypesProvider,
            projectTypesProvider,
            _sceneTypesProvider,
            _logger);
    }

    /// <summary>
    /// Adds a script to the project.
    /// </summary>
    public GDScriptFile AddScript(string fullPath)
    {
        var reference = new GDScriptReference(fullPath);
        var script = new GDScriptFile(reference, _fileSystem, _logger);
        _scripts.TryAdd(reference, script);
        script.Reload();
        return script;
    }

    /// <summary>
    /// Adds a script from content.
    /// </summary>
    public GDScriptFile AddScript(string fullPath, string content)
    {
        var reference = new GDScriptReference(fullPath);
        var script = new GDScriptFile(reference, _fileSystem, _logger);
        _scripts.TryAdd(reference, script);
        script.Reload(content);
        return script;
    }

    /// <summary>
    /// Removes a script from the project.
    /// </summary>
    public bool RemoveScript(string fullPath)
    {
        return _scripts.TryRemove(new GDScriptReference(fullPath), out _);
    }

    /// <summary>
    /// Finds a static declaration by name across all scripts.
    /// </summary>
    public GDCodePointer? FindStaticDeclarationIdentifier(string name)
    {
        _logger.Debug($"FindStaticDeclarationIdentifier: {name}");

        foreach (var scriptFile in ScriptFiles)
        {
            if (scriptFile.TypeName == name)
            {
                return new GDCodePointer
                {
                    ScriptReference = scriptFile.Reference,
                    DeclarationIdentifier = scriptFile.Class?.ClassName?.Identifier
                };
            }
        }

        _logger.Debug($"Declaration not found: {name}");
        return null;
    }

    // IGDScriptProvider implementation
    IGDScriptInfo? IGDScriptProvider.GetScriptByTypeName(string typeName)
    {
        return GetScriptByTypeName(typeName);
    }

    IGDScriptInfo? IGDScriptProvider.GetScriptByPath(string path)
    {
        return GetScript(path);
    }

    #region FileSystemWatcher

    /// <summary>
    /// Enables file system watching for script changes.
    /// </summary>
    public void EnableFileWatcher()
    {
        if (_scriptsWatcher != null) return;
        if (!_fileSystem.DirectoryExists(_context.ProjectPath)) return;

        _scriptsWatcher = new FileSystemWatcher(_context.ProjectPath, "*.gd")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        _scriptsWatcher.Changed += OnFileChanged;
        _scriptsWatcher.Created += OnFileCreated;
        _scriptsWatcher.Deleted += OnFileDeleted;
        _scriptsWatcher.Renamed += OnFileRenamed;

        _logger.Debug("FileSystemWatcher enabled");
    }

    /// <summary>
    /// Disables file system watching.
    /// </summary>
    public void DisableFileWatcher()
    {
        if (_scriptsWatcher == null) return;

        _scriptsWatcher.EnableRaisingEvents = false;
        _scriptsWatcher.Changed -= OnFileChanged;
        _scriptsWatcher.Created -= OnFileCreated;
        _scriptsWatcher.Deleted -= OnFileDeleted;
        _scriptsWatcher.Renamed -= OnFileRenamed;
        _scriptsWatcher.Dispose();
        _scriptsWatcher = null;

        _logger.Debug("FileSystemWatcher disabled");
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var reference = new GDScriptReference(e.FullPath);
        if (_scripts.TryGetValue(reference, out var script))
        {
            script.Reload();
            _logger.Debug($"Script changed: {e.Name}");
            ScriptChanged?.Invoke(this, new GDScriptFileEventArgs(e.FullPath, script));
        }
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        var reference = new GDScriptReference(e.FullPath);
        var script = new GDScriptFile(reference, _fileSystem, _logger);
        if (_scripts.TryAdd(reference, script))
        {
            script.Reload();
            _logger.Debug($"Script created: {e.Name}");
            ScriptCreated?.Invoke(this, new GDScriptFileEventArgs(e.FullPath, script));
        }
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        var reference = new GDScriptReference(e.FullPath);
        if (_scripts.TryRemove(reference, out var script))
        {
            _logger.Debug($"Script deleted: {e.Name}");
            ScriptDeleted?.Invoke(this, new GDScriptFileEventArgs(e.FullPath, script));
        }
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        var oldReference = new GDScriptReference(e.OldFullPath);
        var newReference = new GDScriptReference(e.FullPath);

        if (_scripts.TryRemove(oldReference, out var script))
        {
            // Create new script file at new location
            var newScript = new GDScriptFile(newReference, _fileSystem, _logger);
            _scripts.TryAdd(newReference, newScript);
            newScript.Reload();

            _logger.Debug($"Script renamed: {e.OldName} -> {e.Name}");
            ScriptRenamed?.Invoke(this, new GDScriptRenamedEventArgs(e.OldFullPath, e.FullPath, newScript));
        }
    }

    #endregion

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                DisableFileWatcher();
                _scripts.Clear();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Options for creating a GDScriptProject.
/// </summary>
public class GDScriptProjectOptions
{
    /// <summary>
    /// Custom file system implementation.
    /// </summary>
    public IGDFileSystem? FileSystem { get; set; }

    /// <summary>
    /// Custom logger implementation.
    /// </summary>
    public IGDSemanticLogger? Logger { get; set; }

    /// <summary>
    /// Whether to enable scene types provider for node path resolution.
    /// </summary>
    public bool EnableSceneTypesProvider { get; set; } = true;

    /// <summary>
    /// Whether to enable file system watcher for automatic script reload.
    /// </summary>
    public bool EnableFileWatcher { get; set; } = false;
}
