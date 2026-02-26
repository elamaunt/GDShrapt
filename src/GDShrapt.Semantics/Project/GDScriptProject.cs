using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
    private readonly IGDLogger _logger;
    private readonly GDSceneTypesProvider? _sceneTypesProvider;
    private readonly GDTresResourceProvider? _tresResourceProvider;
    private readonly GDCallSiteRegistry? _callSiteRegistry;
    private readonly GDSceneChangeReanalysisService? _sceneChangeService;
    private readonly bool _enableFileWatcher;
    private readonly GDScriptProjectOptions? _options;
    private FileSystemWatcher? _scriptsWatcher;
    private bool _disposed;
    private IReadOnlyList<GDAutoloadEntry>? _autoloadEntries;
    private Version? _godotVersion;
    private bool _godotVersionParsed;

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

    /// <summary>
    /// Fired when a script file changes incrementally.
    /// Contains old and new AST for delta analysis.
    /// Use this for semantic model invalidation and incremental updates.
    /// </summary>
    public event EventHandler<GDScriptIncrementalChangeEventArgs>? IncrementalChange;

    /// <summary>
    /// Fired when scene changes affect scripts that need reanalysis.
    /// Subscribe to this to update diagnostics when .tscn files change.
    /// </summary>
    public event EventHandler<GDSceneAffectedScriptsEventArgs>? SceneScriptsChanged;

    #endregion

    /// <summary>
    /// Emits an incremental change event. Used by LSP and external integrations.
    /// </summary>
    /// <param name="script">The script that changed.</param>
    /// <param name="oldTree">The AST before the change.</param>
    /// <param name="newTree">The AST after the change.</param>
    /// <param name="changes">The text changes that were applied.</param>
    /// <param name="kind">The kind of change.</param>
    public void EmitIncrementalChange(
        GDScriptFile script,
        GDClassDeclaration? oldTree,
        GDClassDeclaration? newTree,
        IReadOnlyList<GDTextChange> changes,
        GDIncrementalChangeKind kind = GDIncrementalChangeKind.Modified)
    {
        if (script.FullPath == null)
            return;

        IncrementalChange?.Invoke(this, new GDScriptIncrementalChangeEventArgs(
            script.FullPath,
            script,
            oldTree,
            newTree,
            kind,
            textChanges: changes));
    }

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
    /// Resource provider for .tres file analysis.
    /// </summary>
    internal GDTresResourceProvider? TresResourceProvider => _tresResourceProvider;

    /// <summary>
    /// Call site registry for incremental updates.
    /// May be null if call site tracking is not enabled.
    /// </summary>
    public GDCallSiteRegistry? CallSiteRegistry => _callSiteRegistry;

    /// <summary>
    /// Autoload entries parsed from project.godot.
    /// Lazy-loaded on first access.
    /// </summary>
    public IReadOnlyList<GDAutoloadEntry> AutoloadEntries
    {
        get
        {
            if (_autoloadEntries == null)
            {
                var projectGodotPath = Path.Combine(_context.ProjectPath, "project.godot");
                _autoloadEntries = GDGodotProjectParser.ParseAutoloads(projectGodotPath, _fileSystem);
            }
            return _autoloadEntries;
        }
    }

    /// <summary>
    /// Godot engine version detected from project.godot config/features.
    /// Null if version cannot be determined.
    /// </summary>
    public Version? GodotVersion
    {
        get
        {
            if (!_godotVersionParsed)
            {
                var projectGodotPath = Path.Combine(_context.ProjectPath, "project.godot");
                _godotVersion = GDGodotProjectParser.ParseGodotVersion(projectGodotPath, _fileSystem);
                _godotVersionParsed = true;
            }
            return _godotVersion;
        }
    }

    /// <summary>
    /// File system abstraction for the project.
    /// </summary>
    internal IGDFileSystem FileSystem => _fileSystem;

    /// <summary>
    /// Logger for diagnostic output.
    /// </summary>
    public IGDLogger Logger => _logger;

    /// <summary>
    /// Project options (may be null if created without options).
    /// </summary>
    public GDScriptProjectOptions? Options => _options;

    // IGDScriptProvider implementation
    IEnumerable<IGDScriptInfo> IGDScriptProvider.Scripts => _scripts.Values;

    public GDScriptProject(IGDProjectContext context, GDScriptProjectOptions? options = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _options = options;
        _fileSystem = options?.FileSystem ?? new GDDefaultFileSystem();
        _logger = options?.Logger ?? GDNullLogger.Instance;
        _enableFileWatcher = options?.EnableFileWatcher ?? false;

        if (options?.EnableSceneTypesProvider == true)
        {
            _sceneTypesProvider = new GDSceneTypesProvider(_context.ProjectPath, _fileSystem, _logger);
            _tresResourceProvider = new GDTresResourceProvider(_context.ProjectPath, _fileSystem, _logger);
        }

        if (options?.EnableCallSiteRegistry == true)
        {
            _callSiteRegistry = new GDCallSiteRegistry();
        }

        if (options?.EnableSceneChangeReanalysis == true && _sceneTypesProvider != null)
        {
            _sceneChangeService = new GDSceneChangeReanalysisService(this, _sceneTypesProvider, _logger);
            _sceneChangeService.ScriptsNeedReanalysis += OnSceneScriptsNeedReanalysis;
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

            var reference = new GDScriptReference(scriptFile, _context);
            var script = CreateScriptFile(reference);
            _scripts.TryAdd(reference, script);
            script.Reload();
        }

        _logger.Debug($"Project loaded: {_scripts.Count} scripts");

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
    /// Loads .tres resource files from the project directory.
    /// </summary>
    public void LoadResources()
    {
        _tresResourceProvider?.ReloadAllResources();
    }

    /// <summary>
    /// Resolves .tres resource class names using loaded scripts.
    /// Should be called after LoadScripts + LoadResources but before dead code analysis.
    /// </summary>
    internal void ResolveTresClassNames()
    {
        _tresResourceProvider?.ResolveClassNames(this);
    }

    /// <summary>
    /// Analyzes all scripts with type resolution.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    public void AnalyzeAll(CancellationToken cancellationToken = default)
    {
        var config = _options?.SemanticsConfig ?? new GDSemanticsConfig();
        var runtimeProvider = CreateRuntimeProvider();
        var nodeTypeInjector = CreateNodeTypeInjector();

        // Sequential fallback when parallel is disabled or degree is 0
        if (!config.EnableParallelAnalysis || config.MaxDegreeOfParallelism == 0)
        {
            foreach (var script in _scripts.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                script.Analyze(runtimeProvider, nodeTypeInjector);
            }
            return;
        }

        // Parallel analysis
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = ResolveParallelism(config.MaxDegreeOfParallelism),
            CancellationToken = cancellationToken
        };

        var exceptions = new ConcurrentBag<Exception>();

        try
        {
            Parallel.ForEach(_scripts.Values, options, script =>
            {
                try
                {
                    script.Analyze(runtimeProvider, nodeTypeInjector);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    exceptions.Add(ex);
                    _logger.Error($"Error analyzing {script.FullPath}: {ex.Message}");
                }
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }

        if (!exceptions.IsEmpty)
        {
            throw new AggregateException("Errors during parallel analysis", exceptions);
        }
    }

    /// <summary>
    /// Analyzes all scripts asynchronously with type resolution.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task representing the analysis operation.</returns>
    public Task AnalyzeAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() => AnalyzeAll(cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Enriches semantic models with cross-method call-site analysis.
    /// Must be called after AnalyzeAll() and before parameter type inference.
    /// </summary>
    public void EnrichWithCallSiteAnalysis()
    {
        var engine = new GDMethodSignatureInferenceEngine(this);
        engine.BuildAll();

        var projectReport = engine.GetProjectReport();

        var filesByType = new Dictionary<string, GDScriptFile>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in ScriptFiles)
        {
            if (!string.IsNullOrEmpty(file.TypeName))
                filesByType[file.TypeName] = file;
        }

        foreach (var (methodKey, report) in projectReport.Methods)
        {
            if (filesByType.TryGetValue(report.ClassName, out var file))
            {
                file.SemanticModel?.SetCallSiteTypesFromReport(report);
            }
        }
    }

    /// <summary>
    /// Resolves the effective parallelism degree from configuration.
    /// </summary>
    private static int ResolveParallelism(int configValue)
    {
        return configValue < 0 ? Environment.ProcessorCount :
               configValue == 0 ? 1 : configValue;
    }

    /// <summary>
    /// Creates a new GDScriptFile with project configuration applied.
    /// </summary>
    private GDScriptFile CreateScriptFile(GDScriptReference reference)
    {
        var config = _options?.SemanticsConfig;
        var enableIncrementalParsing = config?.EnableIncrementalParsing ?? true;
        var script = new GDScriptFile(reference, _fileSystem, _logger, enableIncrementalParsing);

        // Apply incremental parsing thresholds from config
        if (config != null)
        {
            script.ConfigureIncremental(
                config.IncrementalFullReparseThreshold,
                config.IncrementalMaxAffectedMembers);
        }

        return script;
    }

    /// <summary>
    /// Applies semantic configuration to all scripts in the project.
    /// Call this if you change SemanticsConfig after scripts are loaded.
    /// </summary>
    /// <param name="config">The configuration to apply.</param>
    public void ApplySemanticsConfig(GDSemanticsConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        foreach (var script in _scripts.Values)
        {
            script.ConfigureIncremental(
                config.IncrementalFullReparseThreshold,
                config.IncrementalMaxAffectedMembers);
        }

        _logger.Debug($"Applied semantics config: threshold={config.IncrementalFullReparseThreshold}, maxMembers={config.IncrementalMaxAffectedMembers}");
    }

    /// <summary>
    /// Creates a node type injector for scene-based node type inference.
    /// </summary>
    private GDNodeTypeInjector? CreateNodeTypeInjector()
    {
        if (_sceneTypesProvider == null)
            return null;

        var godotTypesProvider = new GDGodotTypesProvider();
        return new GDNodeTypeInjector(
            _sceneTypesProvider,
            this, // IGDScriptProvider
            godotTypesProvider,
            _logger);
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

        // Load autoloads from project.godot
        var projectGodotPath = Path.Combine(_context.ProjectPath, "project.godot");
        var autoloads = GDGodotProjectParser.ParseAutoloads(projectGodotPath, _fileSystem);
        var autoloadsProvider = new GDAutoloadsProvider(autoloads, this, _sceneTypesProvider);

        return new GDCompositeRuntimeProvider(
            godotTypesProvider,
            projectTypesProvider,
            autoloadsProvider,
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

        // Load autoloads from project.godot
        var projectGodotPath = Path.Combine(_context.ProjectPath, "project.godot");
        var autoloads = GDGodotProjectParser.ParseAutoloads(projectGodotPath, _fileSystem);
        var autoloadsProvider = new GDAutoloadsProvider(autoloads, this, _sceneTypesProvider);

        return new GDTypeResolver(
            godotTypesProvider,
            projectTypesProvider,
            autoloadsProvider,
            _sceneTypesProvider,
            this, // IGDScriptProvider for preload type inference
            _logger);
    }

    /// <summary>
    /// Adds a script to the project.
    /// </summary>
    public GDScriptFile AddScript(string fullPath)
    {
        var reference = new GDScriptReference(fullPath, _context);
        var script = CreateScriptFile(reference);
        _scripts.TryAdd(reference, script);
        script.Reload();
        return script;
    }

    /// <summary>
    /// Adds a script from content.
    /// </summary>
    public GDScriptFile AddScript(string fullPath, string content)
    {
        var reference = new GDScriptReference(fullPath, _context);
        var script = CreateScriptFile(reference);
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

    #region Call Site Registry

    /// <summary>
    /// Builds or rebuilds the call site registry from all loaded scripts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public void BuildCallSiteRegistry(CancellationToken cancellationToken = default)
    {
        if (_callSiteRegistry == null)
            return;

        _callSiteRegistry.Clear();

        var updater = new GDIncrementalCallSiteUpdater(CreateRuntimeProvider());

        foreach (var scriptFile in _scripts.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (scriptFile.Class == null || scriptFile.FullPath == null)
                continue;

            // Register all call sites from this file
            updater.UpdateSemanticModel(
                this,
                scriptFile.FullPath,
                null,  // No old tree (initial build)
                scriptFile.Class,
                Array.Empty<GDTextChange>(),
                cancellationToken);
        }

        _logger.Debug($"Call site registry built: {_callSiteRegistry.Count} entries");
    }

    /// <summary>
    /// Handles incremental update when a file changes.
    /// </summary>
    /// <param name="filePath">Path of the changed file.</param>
    /// <param name="oldTree">Old AST (may be null for new files).</param>
    /// <param name="newTree">New AST.</param>
    /// <param name="changes">List of text changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public void OnFileChanged(
        string filePath,
        GDClassDeclaration? oldTree,
        GDClassDeclaration newTree,
        IReadOnlyList<GDTextChange> changes,
        CancellationToken cancellationToken = default)
    {
        if (_callSiteRegistry == null || string.IsNullOrEmpty(filePath))
            return;

        var updater = new GDIncrementalCallSiteUpdater(CreateRuntimeProvider());
        updater.UpdateSemanticModel(this, filePath, oldTree, newTree, changes, cancellationToken);
    }

    #endregion

    private void OnSceneScriptsNeedReanalysis(object? sender, GDSceneAffectedScriptsEventArgs e)
    {
        SceneScriptsChanged?.Invoke(this, e);
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
        _scriptsWatcher.Error += OnWatcherError;

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
        _scriptsWatcher.Error -= OnWatcherError;
        _scriptsWatcher.Dispose();
        _scriptsWatcher = null;

        _logger.Debug("FileSystemWatcher disabled");
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        _logger.Warning($"FileSystemWatcher error: {e.GetException().Message}");
        try
        {
            DisableFileWatcher();
            EnableFileWatcher();
            _logger.Info("FileSystemWatcher restarted after error");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to restart FileSystemWatcher: {ex.Message}");
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var reference = new GDScriptReference(e.FullPath, _context);
        if (_scripts.TryGetValue(reference, out var script))
        {
            var oldTree = script.Class;

            // Read new content
            string newContent;
            try
            {
                newContent = _fileSystem.ReadAllText(e.FullPath);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to read changed file: {ex.Message}");
                return;
            }

            // Compute diff if we have previous content
            IReadOnlyList<GDTextChange> changes = Array.Empty<GDTextChange>();
            if (script.LastContent != null && GDTextDiffComputer.TextsDiffer(script.LastContent, newContent))
            {
                changes = GDTextDiffComputer.ComputeChanges(script.LastContent, newContent);
            }

            // Incremental reload
            GDIncrementalReloadResult result;
            if (changes.Count > 0)
            {
                result = script.Reload(newContent, changes);
                _logger.Debug($"Script changed (incremental={result.WasIncremental}): {e.Name}");
            }
            else
            {
                script.Reload(newContent);
                result = new GDIncrementalReloadResult(oldTree, script.Class, changes, false);
                _logger.Debug($"Script changed (full reparse): {e.Name}");
            }

            ScriptChanged?.Invoke(this, new GDScriptFileEventArgs(e.FullPath, script));

            // Emit incremental change event with actual changes
            IncrementalChange?.Invoke(this, new GDScriptIncrementalChangeEventArgs(
                e.FullPath,
                script,
                result.OldTree,
                result.NewTree,
                GDIncrementalChangeKind.Modified,
                textChanges: changes));
        }
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        var reference = new GDScriptReference(e.FullPath, _context);
        var script = CreateScriptFile(reference);
        if (_scripts.TryAdd(reference, script))
        {
            script.Reload();
            var newTree = script.Class;

            _logger.Debug($"Script created: {e.Name}");
            ScriptCreated?.Invoke(this, new GDScriptFileEventArgs(e.FullPath, script));

            // Emit incremental change event
            IncrementalChange?.Invoke(this, new GDScriptIncrementalChangeEventArgs(
                e.FullPath,
                script,
                null, // No old tree for new files
                newTree,
                GDIncrementalChangeKind.Created));
        }
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        var reference = new GDScriptReference(e.FullPath, _context);
        if (_scripts.TryRemove(reference, out var script))
        {
            var oldTree = script.Class;

            _logger.Debug($"Script deleted: {e.Name}");
            ScriptDeleted?.Invoke(this, new GDScriptFileEventArgs(e.FullPath, script));

            // Emit incremental change event
            IncrementalChange?.Invoke(this, new GDScriptIncrementalChangeEventArgs(
                e.FullPath,
                script,
                oldTree,
                null, // No new tree for deleted files
                GDIncrementalChangeKind.Deleted));
        }
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        var oldReference = new GDScriptReference(e.OldFullPath, _context);
        var newReference = new GDScriptReference(e.FullPath, _context);

        if (_scripts.TryRemove(oldReference, out var script))
        {
            var oldTree = script.Class;

            // Create new script file at new location
            var newScript = CreateScriptFile(newReference);
            _scripts.TryAdd(newReference, newScript);
            newScript.Reload();
            var newTree = newScript.Class;

            _logger.Debug($"Script renamed: {e.OldName} -> {e.Name}");
            ScriptRenamed?.Invoke(this, new GDScriptRenamedEventArgs(e.OldFullPath, e.FullPath, newScript));

            // Emit incremental change event
            IncrementalChange?.Invoke(this, new GDScriptIncrementalChangeEventArgs(
                e.FullPath,
                newScript,
                oldTree,
                newTree,
                GDIncrementalChangeKind.Renamed,
                oldFilePath: e.OldFullPath));
        }
    }

    #endregion

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                if (_sceneChangeService != null)
                {
                    _sceneChangeService.ScriptsNeedReanalysis -= OnSceneScriptsNeedReanalysis;
                    _sceneChangeService.Dispose();
                }

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
    public IGDLogger? Logger { get; set; }

    /// <summary>
    /// Whether to enable scene types provider for node path resolution.
    /// </summary>
    public bool EnableSceneTypesProvider { get; set; } = true;

    /// <summary>
    /// Whether to enable file system watcher for automatic script reload.
    /// </summary>
    public bool EnableFileWatcher { get; set; } = false;

    /// <summary>
    /// Whether to enable call site registry for incremental updates.
    /// </summary>
    public bool EnableCallSiteRegistry { get; set; } = false;

    /// <summary>
    /// Whether to enable scene change reanalysis service.
    /// When true, scripts affected by .tscn file changes will be automatically detected.
    /// Requires EnableSceneTypesProvider to be true.
    /// </summary>
    public bool EnableSceneChangeReanalysis { get; set; } = false;

    /// <summary>
    /// Semantic analysis configuration.
    /// If null, uses defaults from GDSemanticsConfig.
    /// </summary>
    public GDSemanticsConfig? SemanticsConfig { get; set; }
}
