using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.Semantics;

/// <summary>
/// Project-level semantic model. THE unified entry point for all GDScript semantic operations.
///
/// <para>
/// Provides access to:
/// <list type="bullet">
///   <item><description>File-level semantic models via <see cref="GetSemanticModel(GDScriptFile)"/></description></item>
///   <item><description>Cross-file symbol resolution via <see cref="FindSymbolsInProject"/></description></item>
///   <item><description>Refactoring services via <see cref="Services"/></description></item>
///   <item><description>Diagnostics and validation via <see cref="Diagnostics"/></description></item>
///   <item><description>Type inference queries via <see cref="InferMethodReturnType"/> and <see cref="InferParameterTypesInProject"/></description></item>
/// </list>
/// </para>
///
/// <example>
/// <code>
/// // Load a project
/// var model = GDProjectSemanticModel.Load("/path/to/godot/project");
///
/// // Get semantic model for a file
/// var fileModel = model.GetSemanticModel("res://scripts/player.gd");
/// var type = fileModel.GetTypeForNode(someExpression);
///
/// // Use refactoring services
/// var refs = model.Services.FindReferences.FindReferences(context);
/// var plan = model.Services.Rename.PlanRename(context, "newName");
///
/// // Validate the project
/// var result = model.Diagnostics.ValidateProject();
/// </code>
/// </example>
/// </summary>
public class GDProjectSemanticModel : IDisposable
{
    private readonly GDScriptProject _project;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, GDSemanticModel> _fileModels = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource> _pendingInvalidations = new();
    private readonly Lazy<GDRefactoringServices> _services;
    private readonly Lazy<GDDiagnosticsServices> _diagnostics;
    private readonly Lazy<GDSignalConnectionRegistry> _signalRegistry;
    private readonly Lazy<GDClassContainerRegistry> _containerRegistry;
    private readonly Lazy<GDTypeDependencyGraph> _dependencyGraph;
    private readonly TimeSpan _debounceInterval;
    private readonly bool _enableIncrementalAnalysis;
    private readonly bool _subscribeToChanges;
    private bool _disposed;
    private readonly Lazy<IGDProjectTypeSystem> _typeSystem;
    private readonly Lazy<GDDeadCodeService> _deadCode;
    private readonly Lazy<GDMetricsService> _metrics;
    private readonly Lazy<GDTypeCoverageService> _typeCoverage;
    private readonly Lazy<GDDependencyService> _dependencies;
    private readonly Lazy<GDSceneFlowService> _sceneFlow;
    private readonly Lazy<GDResourceFlowService> _resourceFlow;
    private readonly Lazy<GDDuplicateDetectionService> _duplicates;
    private readonly Lazy<GDSecurityScanningService> _security;

    /// <summary>
    /// The underlying project.
    /// </summary>
    public GDScriptProject Project => _project;

    /// <summary>
    /// Project-level type system for cross-file type resolution.
    /// Provides unified access to type queries across all files.
    /// </summary>
    public IGDProjectTypeSystem TypeSystem => _typeSystem.Value;

    /// <summary>
    /// Refactoring and code action services.
    /// Provides access to rename, find references, extract method, and other refactorings.
    /// </summary>
    public GDRefactoringServices Services => _services.Value;

    /// <summary>
    /// Diagnostics and validation services.
    /// Provides unified access to syntax checking, validation, and linting.
    /// </summary>
    public GDDiagnosticsServices Diagnostics => _diagnostics.Value;

    /// <summary>
    /// Signal connection registry for inter-procedural analysis.
    /// Thread-safe lazy initialization.
    /// </summary>
    public GDSignalConnectionRegistry SignalConnectionRegistry => _signalRegistry.Value;

    /// <summary>
    /// Class-level container registry for cross-file type inference.
    /// Thread-safe lazy initialization.
    /// </summary>
    internal GDClassContainerRegistry ContainerRegistry => _containerRegistry.Value;

    /// <summary>
    /// Type dependency graph for incremental invalidation.
    /// Tracks which files depend on which other files.
    /// </summary>
    public GDTypeDependencyGraph DependencyGraph => _dependencyGraph.Value;

    /// <summary>
    /// Dead code analysis service.
    /// </summary>
    public GDDeadCodeService DeadCode => _deadCode.Value;

    /// <summary>
    /// Code metrics analysis service.
    /// </summary>
    public GDMetricsService Metrics => _metrics.Value;

    /// <summary>
    /// Type annotation coverage analysis service.
    /// </summary>
    public GDTypeCoverageService TypeCoverage => _typeCoverage.Value;

    /// <summary>
    /// Dependency analysis service.
    /// </summary>
    public GDDependencyService Dependencies => _dependencies.Value;

    /// <summary>
    /// Scene composition and flow analysis service.
    /// </summary>
    public GDSceneFlowService SceneFlow => _sceneFlow.Value;

    /// <summary>
    /// Resource usage analysis service.
    /// </summary>
    public GDResourceFlowService ResourceFlow => _resourceFlow.Value;

    /// <summary>
    /// Duplicate code detection service.
    /// </summary>
    public GDDuplicateDetectionService Duplicates => _duplicates.Value;

    /// <summary>
    /// Security vulnerability scanning service.
    /// </summary>
    public GDSecurityScanningService Security => _security.Value;

    /// <summary>
    /// Fired when a file is invalidated in the semantic model.
    /// </summary>
    public event EventHandler<string>? FileInvalidated;

    /// <summary>
    /// Creates a new project-level semantic model.
    /// </summary>
    /// <param name="project">The GDScript project to analyze.</param>
    /// <param name="subscribeToChanges">Whether to subscribe to project's incremental change events.</param>
    /// <param name="config">Optional semantics configuration. If null, uses project options or defaults.</param>
    public GDProjectSemanticModel(GDScriptProject project, bool subscribeToChanges = false, GDSemanticsConfig? config = null)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _subscribeToChanges = subscribeToChanges;

        var semanticsConfig = config ?? project.Options?.SemanticsConfig ?? new GDSemanticsConfig();
        _debounceInterval = TimeSpan.FromMilliseconds(semanticsConfig.FileChangeDebounceMs);
        _enableIncrementalAnalysis = semanticsConfig.EnableIncrementalAnalysis;

        _typeSystem = new Lazy<IGDProjectTypeSystem>(() => new GDProjectTypeSystem(this), LazyThreadSafetyMode.ExecutionAndPublication);
        _services = new Lazy<GDRefactoringServices>(() => new GDRefactoringServices(_project, this), LazyThreadSafetyMode.ExecutionAndPublication);
        _diagnostics = new Lazy<GDDiagnosticsServices>(() => new GDDiagnosticsServices(_project), LazyThreadSafetyMode.ExecutionAndPublication);
        _deadCode = new Lazy<GDDeadCodeService>(() => new GDDeadCodeService(this), LazyThreadSafetyMode.ExecutionAndPublication);
        _metrics = new Lazy<GDMetricsService>(() => new GDMetricsService(_project), LazyThreadSafetyMode.ExecutionAndPublication);
        _typeCoverage = new Lazy<GDTypeCoverageService>(() => new GDTypeCoverageService(_project), LazyThreadSafetyMode.ExecutionAndPublication);
        _dependencies = new Lazy<GDDependencyService>(() => new GDDependencyService(_project, SignalConnectionRegistry), LazyThreadSafetyMode.ExecutionAndPublication);
        _sceneFlow = new Lazy<GDSceneFlowService>(() => new GDSceneFlowService(this), LazyThreadSafetyMode.ExecutionAndPublication);
        _resourceFlow = new Lazy<GDResourceFlowService>(() => new GDResourceFlowService(this), LazyThreadSafetyMode.ExecutionAndPublication);
        _duplicates = new Lazy<GDDuplicateDetectionService>(() => new GDDuplicateDetectionService(this), LazyThreadSafetyMode.ExecutionAndPublication);
        _security = new Lazy<GDSecurityScanningService>(() => new GDSecurityScanningService(this), LazyThreadSafetyMode.ExecutionAndPublication);

        _signalRegistry = new Lazy<GDSignalConnectionRegistry>(InitializeSignalRegistry, LazyThreadSafetyMode.ExecutionAndPublication);
        _containerRegistry = new Lazy<GDClassContainerRegistry>(InitializeContainerRegistry, LazyThreadSafetyMode.ExecutionAndPublication);
        _dependencyGraph = new Lazy<GDTypeDependencyGraph>(InitializeDependencyGraph, LazyThreadSafetyMode.ExecutionAndPublication);

        if (_subscribeToChanges)
        {
            _project.IncrementalChange += OnIncrementalChange;
        }
    }

    /// <summary>
    /// Initializes the signal connection registry by collecting all connections.
    /// </summary>
    private GDSignalConnectionRegistry InitializeSignalRegistry()
    {
        var registry = new GDSignalConnectionRegistry();
        var collector = new GDSignalConnectionCollector(_project);
        var connections = collector.CollectAllConnections();

        foreach (var connection in connections)
        {
            registry.Register(connection);
        }

        var sceneProvider = _project.SceneTypesProvider;
        if (sceneProvider != null)
        {
            foreach (var sceneInfo in sceneProvider.AllScenes)
            {
                foreach (var conn in sceneInfo.SignalConnections)
                {
                    var fromNode = sceneInfo.Nodes.FirstOrDefault(n => n.Path == conn.FromNode);
                    var toNode = sceneInfo.Nodes.FirstOrDefault(n => n.Path == conn.ToNode);

                    registry.Register(GDSignalConnectionEntry.FromScene(
                        sceneInfo.FullPath,
                        conn.LineNumber,
                        fromNode?.ScriptTypeName ?? fromNode?.NodeType ?? conn.SourceNodeType ?? "",
                        conn.SignalName,
                        toNode?.ScriptTypeName ?? toNode?.NodeType ?? "",
                        conn.Method));
                }
            }
        }

        return registry;
    }

    /// <summary>
    /// Initializes the class container registry by collecting all class-level container profiles.
    /// Also performs cross-file analysis to merge usages from external files.
    /// </summary>
    private GDClassContainerRegistry InitializeContainerRegistry()
    {
        var registry = new GDClassContainerRegistry();

        foreach (var scriptFile in _project.ScriptFiles)
        {
            var model = GetSemanticModel(scriptFile);
            if (model == null)
                continue;

            var className = scriptFile.TypeName ?? "";
            foreach (var kv in model.ClassContainerProfiles)
            {
                // kv.Key is composite "className.variableName" from GDContainerTypeService
                // Extract just the variable name to avoid double-prefixing in the registry
                var containerName = kv.Key;
                var prefix = className + ".";
                if (!string.IsNullOrEmpty(className) && containerName.StartsWith(prefix))
                {
                    containerName = containerName.Substring(prefix.Length);
                }

                registry.Register(className, containerName, kv.Value, scriptFile.FullPath);
            }
        }

        var crossFileCollector = new GDCrossFileContainerUsageCollector(_project);

        foreach (var profileEntry in registry.AllProfiles)
        {
            var parts = profileEntry.Key.Split(new[] { '.' }, 2);
            if (parts.Length != 2)
                continue;

            var className = parts[0];
            var containerName = parts[1];

            var crossFileUsages = crossFileCollector.CollectUsages(className, containerName);
            if (crossFileUsages.Count > 0)
            {
                registry.MergeCrossFileUsages(className, containerName, crossFileUsages);
            }
        }

        return registry;
    }

    /// <summary>
    /// Initializes the dependency graph by analyzing all scripts for cross-file references.
    /// </summary>
    private GDTypeDependencyGraph InitializeDependencyGraph()
    {
        var graph = new GDTypeDependencyGraph();

        foreach (var scriptFile in _project.ScriptFiles)
        {
            if (scriptFile.FullPath == null || scriptFile.Class == null)
                continue;

            var dependencies = CollectFileDependencies(scriptFile);
            graph.UpdateDependencies(scriptFile.FullPath, dependencies);
        }

        return graph;
    }

    /// <summary>
    /// Collects all file paths that the given script depends on.
    /// </summary>
    private IEnumerable<string> CollectFileDependencies(GDScriptFile scriptFile)
    {
        var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (scriptFile.Class == null)
            return dependencies;

        var extendsType = scriptFile.Class.Extends?.Type;
        if (extendsType != null)
        {
            if (extendsType is GDStringTypeNode stringTypeNode)
            {
                var path = stringTypeNode.Path?.Sequence;
                if (!string.IsNullOrEmpty(path))
                {
                    var extendedScript = _project.GetScriptByResourcePath(path);
                    if (extendedScript?.FullPath != null)
                    {
                        dependencies.Add(extendedScript.FullPath);
                    }
                }
            }
            else
            {
                var typeName = extendsType.BuildName();
                if (!string.IsNullOrEmpty(typeName))
                {
                    var extendedScript = _project.GetScriptByTypeName(typeName);
                    if (extendedScript?.FullPath != null)
                    {
                        dependencies.Add(extendedScript.FullPath);
                    }
                }
            }
        }

        foreach (var preload in scriptFile.Class.AllNodes.OfType<GDCallExpression>())
        {
            if (preload.CallerExpression is GDIdentifierExpression idExpr &&
                idExpr.Identifier?.Sequence == GDWellKnownFunctions.Preload &&
                preload.Parameters?.Count > 0 &&
                preload.Parameters[0] is GDStringExpression strExpr)
            {
                var path = strExpr.String?.Sequence;
                if (!string.IsNullOrEmpty(path) && path.EndsWith(".gd"))
                {
                    var referencedScript = _project.GetScriptByResourcePath(path);
                    if (referencedScript?.FullPath != null)
                    {
                        dependencies.Add(referencedScript.FullPath);
                    }
                }
            }
        }

        foreach (var member in scriptFile.Class.Members)
        {
            if (member is GDVariableDeclaration varDecl && varDecl.Type != null)
            {
                var typeName = varDecl.Type.BuildName();
                var typeScript = _project.GetScriptByTypeName(typeName);
                if (typeScript?.FullPath != null)
                {
                    dependencies.Add(typeScript.FullPath);
                }
            }

            if (member is GDMethodDeclaration methodDecl)
            {
                if (methodDecl.ReturnType != null)
                {
                    var returnTypeName = methodDecl.ReturnType.BuildName();
                    var returnTypeScript = _project.GetScriptByTypeName(returnTypeName);
                    if (returnTypeScript?.FullPath != null)
                    {
                        dependencies.Add(returnTypeScript.FullPath);
                    }
                }

                if (methodDecl.Parameters != null)
                {
                    foreach (var param in methodDecl.Parameters)
                    {
                        if (param.Type != null)
                        {
                            var paramTypeName = param.Type.BuildName();
                            var paramTypeScript = _project.GetScriptByTypeName(paramTypeName);
                            if (paramTypeScript?.FullPath != null)
                            {
                                dependencies.Add(paramTypeScript.FullPath);
                            }
                        }
                    }
                }
            }
        }

        return dependencies;
    }

    #region Factory Methods

    /// <summary>
    /// Creates a project semantic model from a directory path.
    /// This is the recommended entry point for external consumers.
    /// </summary>
    /// <param name="projectPath">Path to the Godot project directory (containing project.godot).</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <param name="enableSceneTypes">Enable scene types provider for autoloads.</param>
    /// <returns>A fully initialized project semantic model.</returns>
    /// <example>
    /// <code>
    /// var model = GDProjectSemanticModel.Load("/path/to/godot/project");
    /// var file = model.GetSemanticModel("res://scripts/player.gd");
    /// </code>
    /// </example>
    public static GDProjectSemanticModel Load(string projectPath, IGDLogger? logger = null, bool enableSceneTypes = true)
    {
        if (string.IsNullOrEmpty(projectPath))
            throw new ArgumentNullException(nameof(projectPath));

        var project = GDProjectLoader.LoadProject(projectPath, logger, enableSceneTypes);
        return new GDProjectSemanticModel(project);
    }

    /// <summary>
    /// Creates a project semantic model from a directory path asynchronously.
    /// Loads scripts and performs parallel analysis.
    /// </summary>
    /// <param name="projectPath">Path to the Godot project directory (containing project.godot).</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <param name="enableSceneTypes">Enable scene types provider for autoloads.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>A fully initialized project semantic model.</returns>
    public static Task<GDProjectSemanticModel> LoadAsync(
        string projectPath,
        IGDLogger? logger = null,
        bool enableSceneTypes = true,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(projectPath))
                throw new ArgumentNullException(nameof(projectPath));

            var project = GDProjectLoader.LoadProject(projectPath, logger, enableSceneTypes);
            cancellationToken.ThrowIfCancellationRequested();

            project.AnalyzeAll(cancellationToken);

            return new GDProjectSemanticModel(project);
        }, cancellationToken);
    }

    #endregion

    #region Semantic Model Access

    /// <summary>
    /// Gets or creates the semantic model for a script file.
    /// Thread-safe via ConcurrentDictionary.
    /// </summary>
    public GDSemanticModel? GetSemanticModel(GDScriptFile scriptFile)
    {
        if (scriptFile == null)
            return null;

        var path = scriptFile.FullPath ?? scriptFile.Reference?.FullPath ?? "";
        if (string.IsNullOrEmpty(path))
            return null;

        return _fileModels.GetOrAdd(path, _ =>
        {
            if (scriptFile.SemanticModel == null)
            {
                var runtimeProvider = _project.CreateRuntimeProvider();
                scriptFile.Analyze(runtimeProvider);
            }
            // Analyze() guarantees SemanticModel is set (even on parse failure, a minimal model is created)
            return scriptFile.SemanticModel!;
        });
    }

    /// <summary>
    /// Gets the semantic model for a file by path.
    /// </summary>
    public GDSemanticModel? GetSemanticModel(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        if (_fileModels.TryGetValue(filePath, out var cached))
            return cached;

        var scriptFile = _project.ScriptFiles
            .FirstOrDefault(f => f.FullPath != null &&
                f.FullPath.Equals(filePath, System.StringComparison.OrdinalIgnoreCase));

        return scriptFile != null ? GetSemanticModel(scriptFile) : null;
    }

    #endregion

    #region File-Level Delegation

    /// <summary>
    /// Finds a symbol by name in a specific file. Delegates to file-level semantic model.
    /// </summary>
    internal GDSymbolInfo? FindSymbolInFile(GDScriptFile file, string name)
    {
        if (file == null || string.IsNullOrEmpty(name))
            return null;

        var model = GetSemanticModel(file);
        return model?.FindSymbol(name);
    }

    /// <summary>
    /// Gets the symbol at a specific position in a file. Delegates to file-level semantic model.
    /// </summary>
    internal GDSymbolInfo? GetSymbolAtPosition(GDScriptFile file, int line, int column)
    {
        if (file == null)
            return null;

        var model = GetSemanticModel(file);
        return model?.GetSymbolAtPosition(line, column);
    }

    /// <summary>
    /// Gets the inferred type for an expression. Auto-finds the containing file.
    /// </summary>
    internal string? GetExpressionType(GDExpression expr)
    {
        if (expr == null)
            return null;

        var file = FindFileContaining(expr);
        if (file == null)
            return null;

        var model = GetSemanticModel(file);
        return model?.GetExpressionType(expr);
    }

    /// <summary>
    /// Gets the inferred type for any AST node. Auto-finds the containing file.
    /// </summary>
    internal string? GetTypeForNode(GDNode node)
    {
        if (node == null)
            return null;

        var file = FindFileContaining(node);
        if (file == null)
            return null;

        var model = GetSemanticModel(file);
        return model?.GetTypeForNode(node);
    }

    /// <summary>
    /// Gets the inferred type for any AST node in a specific file.
    /// </summary>
    internal string? GetTypeForNode(GDScriptFile file, GDNode node)
    {
        if (file == null || node == null)
            return null;

        var model = GetSemanticModel(file);
        return model?.GetTypeForNode(node);
    }

    /// <summary>
    /// Gets the symbol for an AST node. Auto-finds the containing file.
    /// </summary>
    internal GDSymbolInfo? GetSymbolForNode(GDNode node)
    {
        if (node == null)
            return null;

        var file = FindFileContaining(node);
        if (file == null)
            return null;

        var model = GetSemanticModel(file);
        return model?.GetSymbolForNode(node);
    }

    #endregion

    #region Cross-File Symbol Resolution

    /// <summary>
    /// Finds a symbol by name across the entire project.
    /// </summary>
    internal IEnumerable<(GDScriptFile File, GDSymbolInfo Symbol)> FindSymbolsInProject(string name)
    {
        if (string.IsNullOrEmpty(name))
            yield break;

        foreach (var scriptFile in _project.ScriptFiles)
        {
            var model = GetSemanticModel(scriptFile);
            if (model == null)
                continue;

            foreach (var symbol in model.FindSymbols(name))
            {
                yield return (scriptFile, symbol);
            }
        }
    }

    /// <summary>
    /// Gets all declarations of a type/class across the project.
    /// </summary>
    internal IEnumerable<(GDScriptFile File, GDSymbolInfo Symbol)> FindTypeDeclarations(string typeName)
    {
        return FindSymbolsInProject(typeName)
            .Where(x => x.Symbol.Kind == GDSymbolKind.Class);
    }

    #endregion

    #region Container Profiles

    /// <summary>
    /// Gets a merged container profile for a class-level variable.
    /// Uses the project-wide container registry that includes cross-file usages.
    /// </summary>
    /// <param name="className">The class containing the container.</param>
    /// <param name="variableName">The container variable name.</param>
    /// <returns>The merged profile including cross-file usages, or null if not found.</returns>
    internal GDContainerUsageProfile? GetMergedContainerProfile(string className, string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return null;

        return ContainerRegistry.GetProfile(className ?? "", variableName);
    }

    /// <summary>
    /// Gets all container profiles for a class.
    /// </summary>
    /// <param name="className">The class name.</param>
    /// <returns>Dictionary of container name to profile.</returns>
    internal IReadOnlyDictionary<string, GDContainerUsageProfile> GetContainerProfilesForClass(string className)
    {
        return ContainerRegistry.GetProfilesForClass(className ?? "");
    }

    #endregion

    #region Project-Wide References

    /// <summary>
    /// Gets all references to a symbol across the entire project.
    /// </summary>
    /// <param name="symbol">The symbol to find references for.</param>
    /// <returns>Enumerable of file and reference pairs.</returns>
    public IEnumerable<(GDScriptFile File, GDReference Reference)> GetReferencesInProject(GDSymbolInfo symbol)
    {
        if (symbol == null)
            yield break;

        foreach (var scriptFile in _project.ScriptFiles)
        {
            var model = GetSemanticModel(scriptFile);
            if (model == null)
                continue;

            var refs = model.GetReferencesTo(symbol);
            if (refs == null)
                continue;

            foreach (var reference in refs)
            {
                yield return (scriptFile, reference);
            }
        }
    }

    /// <summary>
    /// Gets all member accesses to a type's member across the project.
    /// </summary>
    /// <param name="typeName">The type name (e.g., "OS", "Node").</param>
    /// <param name="memberName">The member name (e.g., "execute", "add_child").</param>
    /// <returns>Enumerable of file and reference pairs.</returns>
    public IEnumerable<(GDScriptFile File, GDReference Reference)> GetMemberAccessesInProject(
        string typeName,
        string memberName)
    {
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(memberName))
            yield break;

        foreach (var scriptFile in _project.ScriptFiles)
        {
            var model = GetSemanticModel(scriptFile);
            if (model == null)
                continue;

            var accesses = model.GetMemberAccesses(typeName, memberName);
            if (accesses == null)
                continue;

            foreach (var access in accesses)
            {
                yield return (scriptFile, access);
            }
        }
    }

    /// <summary>
    /// Gets all member accesses for a given member name across the entire project,
    /// regardless of caller type.
    /// </summary>
    internal IEnumerable<(GDScriptFile File, GDReference Reference)> GetAllMemberAccessesForMemberInProject(string memberName)
    {
        if (string.IsNullOrEmpty(memberName))
            yield break;

        foreach (var scriptFile in _project.ScriptFiles)
        {
            var model = GetSemanticModel(scriptFile);
            if (model == null)
                continue;

            foreach (var (_, references) in model.GetAllMemberAccessesForMember(memberName))
            {
                foreach (var reference in references)
                {
                    yield return (scriptFile, reference);
                }
            }
        }
    }

    /// <summary>
    /// Gets references to a symbol in a specific file. Delegates to file-level semantic model.
    /// </summary>
    public IReadOnlyList<GDReference> GetReferencesInFile(GDScriptFile file, GDSymbolInfo symbol)
    {
        if (file == null || symbol == null)
            return Array.Empty<GDReference>();

        var model = GetSemanticModel(file);
        return model?.GetReferencesTo(symbol) ?? Array.Empty<GDReference>();
    }

    #endregion

    #region Call Site Queries

    /// <summary>
    /// Gets all call sites for a method.
    /// Requires call site tracking to be enabled on the project.
    /// </summary>
    public IReadOnlyList<GDCallSiteEntry> GetCallSitesForMethod(string className, string methodName)
    {
        var registry = _project.CallSiteRegistry;
        if (registry == null)
            return System.Array.Empty<GDCallSiteEntry>();

        return registry.GetCallersOf(className, methodName);
    }

    /// <summary>
    /// Gets all call sites from a specific file.
    /// </summary>
    internal IReadOnlyList<GDCallSiteEntry> GetCallSitesInFile(string filePath)
    {
        var registry = _project.CallSiteRegistry;
        if (registry == null)
            return System.Array.Empty<GDCallSiteEntry>();

        return registry.GetCallSitesInFile(filePath);
    }

    #endregion

    #region Inference Order

    /// <summary>
    /// Gets the optimal order for type inference (handles cycles).
    /// Useful for batch type inference operations.
    /// </summary>
    internal IEnumerable<(string MethodKey, bool InCycle)> GetInferenceOrder()
    {
        var cycleDetector = new GDInferenceCycleDetector(_project);
        cycleDetector.BuildDependencyGraph();
        return cycleDetector.GetInferenceOrder();
    }

    /// <summary>
    /// Detects all cycles in the type inference dependency graph.
    /// </summary>
    internal IEnumerable<IReadOnlyList<string>> DetectInferenceCycles()
    {
        var cycleDetector = new GDInferenceCycleDetector(_project);
        cycleDetector.BuildDependencyGraph();
        return cycleDetector.DetectCycles();
    }

    #endregion

    #region Project-Wide Type Resolution

    /// <summary>
    /// Gets the inferred type for a method across the project.
    /// </summary>
    internal GDInferredType? InferMethodReturnType(string className, string methodName)
    {
        var typeDecl = FindTypeDeclarations(className).FirstOrDefault();
        if (typeDecl.File == null)
            return null;

        var model = GetSemanticModel(typeDecl.File);
        if (model == null)
            return null;

        var classDecl = typeDecl.File.Class;
        if (classDecl == null)
            return null;

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == methodName);

        if (method == null)
            return null;

        var type = model.GetTypeForNode(method);
        return string.IsNullOrEmpty(type) || type == GDWellKnownTypes.Variant
            ? GDInferredType.Unknown()
            : GDInferredType.High(type, $"inferred from {className}.{methodName}");
    }

    /// <summary>
    /// Infers parameter types for a method using local usage analysis.
    /// Project-level entry point that locates the method and delegates to file-level analysis.
    /// </summary>
    internal IReadOnlyDictionary<string, GDInferredParameterType> InferParameterTypesInProject(
        string className,
        string methodName)
    {
        var result = new Dictionary<string, GDInferredParameterType>();

        var typeDecl = FindTypeDeclarations(className).FirstOrDefault();
        if (typeDecl.File == null)
            return result;

        var model = GetSemanticModel(typeDecl.File);
        if (model == null)
            return result;

        var classDecl = typeDecl.File.Class;
        if (classDecl == null)
            return result;

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == methodName);

        if (method == null)
            return result;

        return model.InferParameterTypes(method);
    }

    /// <summary>
    /// Infers parameter types for a method using both local usage analysis and cross-file call site analysis.
    /// This provides the most accurate inference by combining:
    /// 1. Local duck typing (how parameters are used within the method)
    /// 2. Call site analysis (what types are passed when the method is called)
    /// </summary>
    /// <param name="className">The class name containing the method.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="preferCallSites">If true, prefer call site types over local duck types when both are available.</param>
    /// <returns>Dictionary of parameter name to inferred type.</returns>
    internal IReadOnlyDictionary<string, GDInferredParameterType> InferParameterTypesWithCallSites(
        string className,
        string methodName,
        bool preferCallSites = true)
    {
        var result = new Dictionary<string, GDInferredParameterType>();

        var typeDecl = FindTypeDeclarations(className).FirstOrDefault();
        if (typeDecl.File == null)
            return result;

        var model = GetSemanticModel(typeDecl.File);
        if (model == null)
            return result;

        var classDecl = typeDecl.File.Class;
        if (classDecl == null)
            return result;

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == methodName);

        if (method == null)
            return result;

        var localTypes = model.InferParameterTypes(method);

        var callSiteRegistry = _project.CallSiteRegistry;
        if (callSiteRegistry != null)
        {
            var callSiteTypes = GetParameterTypesFromCallSites(className, methodName, method, callSiteRegistry);
            return MergeParameterTypes(localTypes, callSiteTypes, preferCallSites);
        }

        return localTypes;
    }

    /// <summary>
    /// Gets parameter types from call site analysis.
    /// </summary>
    private IReadOnlyDictionary<string, GDInferredParameterType> GetParameterTypesFromCallSites(
        string className,
        string methodName,
        GDMethodDeclaration method,
        GDCallSiteRegistry callSiteRegistry)
    {
        var result = new Dictionary<string, GDInferredParameterType>();

        var paramNames = method.Parameters?
            .Select(p => p.Identifier?.Sequence)
            .Where(n => !string.IsNullOrEmpty(n))
            .Cast<string>()
            .ToList() ?? new List<string>();

        if (paramNames.Count == 0)
            return result;

        var analyzer = new GDCallSiteTypeAnalyzer(
            callSiteRegistry,
            file => GetSemanticModel(file));

        var callSiteResults = analyzer.AnalyzeCallSites(
            className,
            methodName,
            paramNames,
            GetFileByPath);

        // Convert to inferred types
        foreach (var (paramName, callSiteResult) in callSiteResults)
        {
            result[paramName] = GDCallSiteTypeAnalyzer.ToInferredParameterType(callSiteResult);
        }

        return result;
    }

    /// <summary>
    /// Gets a script file by its path.
    /// </summary>
    private GDScriptFile? GetFileByPath(string filePath)
    {
        return _project.ScriptFiles?.FirstOrDefault(f =>
            f.FullPath != null &&
            f.FullPath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Merges local duck typing results with call site analysis results.
    /// </summary>
    private static IReadOnlyDictionary<string, GDInferredParameterType> MergeParameterTypes(
        IReadOnlyDictionary<string, GDInferredParameterType> localTypes,
        IReadOnlyDictionary<string, GDInferredParameterType> callSiteTypes,
        bool preferCallSites)
    {
        var result = new Dictionary<string, GDInferredParameterType>();

        var allParams = new HashSet<string>(localTypes.Keys);
        foreach (var key in callSiteTypes.Keys)
            allParams.Add(key);

        foreach (var paramName in allParams)
        {
            var hasLocal = localTypes.TryGetValue(paramName, out var local);
            var hasCallSite = callSiteTypes.TryGetValue(paramName, out var callSite);

            if (hasLocal && hasCallSite)
                result[paramName] = MergeSingleParameterType(local!, callSite!, preferCallSites, paramName);
            else if (hasLocal)
                result[paramName] = local!;
            else if (hasCallSite)
                result[paramName] = callSite!;
        }

        return result;
    }

    /// <summary>
    /// Merges a single parameter's type from two sources.
    /// </summary>
    private static GDInferredParameterType MergeSingleParameterType(
        GDInferredParameterType local,
        GDInferredParameterType callSite,
        bool preferCallSites,
        string paramName)
    {
        if (local.IsUnknown && !callSite.IsUnknown)
            return callSite;
        if (callSite.IsUnknown && !local.IsUnknown)
            return local;
        if (local.IsUnknown && callSite.IsUnknown)
            return local;

        if (local.TypeName.Equals(callSite.TypeName))
            return local.Confidence >= callSite.Confidence ? local : callSite;

        if (preferCallSites && callSite.Confidence >= GDTypeConfidence.Medium)
            return callSite;

        if (!preferCallSites && local.Confidence >= GDTypeConfidence.Medium)
            return local;

        var typeNames = new List<string>();
        if (local.UnionTypes != null)
            typeNames.AddRange(local.UnionTypes.Select(t => t.DisplayName));
        else if (!local.TypeName.IsVariant)
            typeNames.Add(local.TypeName.DisplayName);

        if (callSite.UnionTypes != null)
            typeNames.AddRange(callSite.UnionTypes.Select(t => t.DisplayName));
        else if (!callSite.TypeName.IsVariant)
            typeNames.Add(callSite.TypeName.DisplayName);

        var types = typeNames.Distinct().ToList();

        if (types.Count == 0)
            return GDInferredParameterType.Unknown(paramName);

        var confidence = local.Confidence < callSite.Confidence ? local.Confidence : callSite.Confidence;

        return GDInferredParameterType.Union(
            paramName,
            types,
            confidence,
            "merged from local usage and call sites");
    }

    #endregion

    #region Invalidation

    /// <summary>
    /// Invalidates the cached semantic model for a file.
    /// Call this when a file has changed.
    /// Thread-safe via ConcurrentDictionary.
    /// </summary>
    public void InvalidateFile(string filePath)
    {
        if (!string.IsNullOrEmpty(filePath))
        {
            _fileModels.TryRemove(filePath, out _);

            if (_signalRegistry.IsValueCreated)
            {
                _signalRegistry.Value.UnregisterFile(filePath);
                var collector = new GDSignalConnectionCollector(_project);
                var file = GetFileByPath(filePath);
                if (file != null)
                {
                    var connections = collector.CollectConnectionsInFile(file);
                    foreach (var connection in connections)
                        _signalRegistry.Value.Register(connection);
                }
            }

            if (_containerRegistry.IsValueCreated)
                _containerRegistry.Value.InvalidateFile(filePath);
        }
    }

    /// <summary>
    /// Clears all cached semantic models.
    /// Warning: Lazy registries will need to be re-initialized on next access.
    /// </summary>
    public void InvalidateAll()
    {
        _fileModels.Clear();
        if (_signalRegistry.IsValueCreated)
            _signalRegistry.Value.Clear();
        if (_containerRegistry.IsValueCreated)
            _containerRegistry.Value.Clear();
        if (_dependencyGraph.IsValueCreated)
            _dependencyGraph.Value.Clear();
    }

    /// <summary>
    /// Handles incremental changes from the project.
    /// Uses debouncing to coalesce rapid changes.
    /// </summary>
    private void OnIncrementalChange(object? sender, GDScriptIncrementalChangeEventArgs e)
    {
        if (_disposed || string.IsNullOrEmpty(e.FilePath))
            return;

        if (_pendingInvalidations.TryRemove(e.FilePath, out var oldCts))
        {
            try { oldCts.Cancel(); }
            catch { /* Ignore cancellation exceptions */ }
            oldCts.Dispose();
        }

        var cts = new CancellationTokenSource();
        _pendingInvalidations[e.FilePath] = cts;

        Task.Delay(_debounceInterval, cts.Token).ContinueWith(t =>
        {
            if (t.IsCanceled || _disposed)
                return;

            _pendingInvalidations.TryRemove(e.FilePath, out _);
            ProcessIncrementalChange(e);
        }, TaskScheduler.Default);
    }

    /// <summary>
    /// Processes an incremental change after debouncing.
    /// </summary>
    private void ProcessIncrementalChange(GDScriptIncrementalChangeEventArgs e)
    {
        if (!_enableIncrementalAnalysis)
        {
            InvalidateAll();
            FileInvalidated?.Invoke(this, e.FilePath);
            return;
        }

        InvalidateFile(e.FilePath);

        if (e.ChangeKind == GDIncrementalChangeKind.Renamed && !string.IsNullOrEmpty(e.OldFilePath))
        {
            InvalidateFile(e.OldFilePath);
            if (_dependencyGraph.IsValueCreated)
            {
                _dependencyGraph.Value.RemoveFile(e.OldFilePath);
            }
        }

        if (_dependencyGraph.IsValueCreated && e.Script != null)
        {
            if (e.ChangeKind == GDIncrementalChangeKind.Deleted)
                _dependencyGraph.Value.RemoveFile(e.FilePath);
            else
            {
                var dependencies = CollectFileDependencies(e.Script);
                _dependencyGraph.Value.UpdateDependencies(e.FilePath, dependencies);
            }

            var dependents = _dependencyGraph.Value.GetDependents(e.FilePath);
            foreach (var dependent in dependents)
            {
                InvalidateFile(dependent);
                FileInvalidated?.Invoke(this, dependent);
            }
        }

        var callSiteRegistry = _project.CallSiteRegistry;
        if (callSiteRegistry != null && e.OldTree != null && e.NewTree != null)
        {
            var updater = new GDIncrementalCallSiteUpdater();
            updater.UpdateSemanticModel(
                _project,
                e.FilePath,
                e.OldTree,
                e.NewTree,
                e.TextChanges,
                default);
        }

        FileInvalidated?.Invoke(this, e.FilePath);
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Disposes resources and unsubscribes from events.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                if (_subscribeToChanges)
                    _project.IncrementalChange -= OnIncrementalChange;

                foreach (var cts in _pendingInvalidations.Values)
                {
                    try { cts.Cancel(); cts.Dispose(); }
                    catch { }
                }
                _pendingInvalidations.Clear();
                _fileModels.Clear();
            }
            _disposed = true;
        }
    }

    #endregion

    #region AST Overloads

    /// <summary>
    /// Gets the inferred return type for a method. Overload with AST node for precise identification.
    /// </summary>
    internal GDInferredType? InferMethodReturnType(GDMethodDeclaration method)
    {
        if (method == null)
            return null;

        var file = FindFileContaining(method);
        if (file == null)
            return null;

        var model = GetSemanticModel(file);
        if (model == null)
            return null;

        var type = model.GetTypeForNode(method);
        return string.IsNullOrEmpty(type) || type == GDWellKnownTypes.Variant
            ? GDInferredType.Unknown()
            : GDInferredType.High(type, $"inferred from {method.Identifier?.Sequence}");
    }

    /// <summary>
    /// Infers parameter types for a method (local analysis only). Overload with AST node for precise identification.
    /// </summary>
    internal IReadOnlyDictionary<string, GDInferredParameterType> InferParameterTypesInProject(GDMethodDeclaration method)
    {
        if (method == null)
            return new Dictionary<string, GDInferredParameterType>();

        var file = FindFileContaining(method);
        if (file == null)
            return new Dictionary<string, GDInferredParameterType>();

        var model = GetSemanticModel(file);
        return model?.InferParameterTypes(method) ?? new Dictionary<string, GDInferredParameterType>();
    }

    /// <summary>
    /// Infers parameter types with call site analysis. Overload with AST node for precise identification.
    /// </summary>
    internal IReadOnlyDictionary<string, GDInferredParameterType> InferParameterTypesWithCallSites(
        GDMethodDeclaration method,
        bool preferCallSites = true)
    {
        if (method == null)
            return new Dictionary<string, GDInferredParameterType>();

        var file = FindFileContaining(method);
        if (file == null)
            return new Dictionary<string, GDInferredParameterType>();

        var className = file.TypeName ?? "";
        var methodName = method.Identifier?.Sequence ?? "";
        return InferParameterTypesWithCallSites(className, methodName, preferCallSites);
    }

    /// <summary>
    /// Gets all call sites for a method. Overload with AST node for precise identification.
    /// </summary>
    internal IReadOnlyList<GDCallSiteEntry> GetCallSitesForMethod(GDMethodDeclaration method)
    {
        if (method == null)
            return Array.Empty<GDCallSiteEntry>();

        var file = FindFileContaining(method);
        if (file == null)
            return Array.Empty<GDCallSiteEntry>();

        var className = file.TypeName ?? "";
        var methodName = method.Identifier?.Sequence ?? "";
        return GetCallSitesForMethod(className, methodName);
    }

    /// <summary>
    /// Gets all signals that call a method. Overload with AST node for precise identification.
    /// </summary>
    internal IReadOnlyList<GDSignalConnectionEntry> GetSignalsCallingMethod(GDMethodDeclaration method)
    {
        if (method == null)
            return Array.Empty<GDSignalConnectionEntry>();

        var file = FindFileContaining(method);
        var className = file?.TypeName;
        var methodName = method.Identifier?.Sequence ?? "";
        return GetSignalsCallingMethod(className, methodName);
    }

    /// <summary>
    /// Infers callback parameter types from signals. Overload with AST node for precise identification.
    /// </summary>
    internal IReadOnlyDictionary<string, GDInferredParameterType> InferCallbackParameterTypes(GDMethodDeclaration method)
    {
        if (method == null)
            return new Dictionary<string, GDInferredParameterType>();

        var file = FindFileContaining(method);
        var className = file?.TypeName;
        var methodName = method.Identifier?.Sequence ?? "";
        return InferCallbackParameterTypes(className, methodName);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Finds the script file containing a given AST node.
    /// Walks up the tree to find the root and matches it against project files.
    /// </summary>
    public GDScriptFile? FindFileContaining(GDNode? node)
    {
        if (node == null)
            return null;

        var current = node;
        while (current.Parent != null)
        {
            if (current.Parent is GDNode parentNode)
                current = parentNode;
            else
                break;
        }

        return _project.ScriptFiles.FirstOrDefault(f => f.Class == current);
    }

    #endregion

    #region Signal Connection Queries

    /// <summary>
    /// Gets all signals that call a specific callback method.
    /// Useful for understanding what events trigger a method.
    /// </summary>
    internal IReadOnlyList<GDSignalConnectionEntry> GetSignalsCallingMethod(string? className, string methodName)
    {
        return SignalConnectionRegistry.GetSignalsCallingMethod(className, methodName);
    }

    /// <summary>
    /// Gets all callbacks connected to a specific signal.
    /// Useful for finding all handlers of a signal.
    /// </summary>
    internal IReadOnlyList<GDSignalConnectionEntry> GetCallbacksForSignal(string? emitterType, string signalName)
    {
        return SignalConnectionRegistry.GetCallbacksForSignal(emitterType, signalName);
    }

    /// <summary>
    /// Checks if a method is used as a signal callback.
    /// </summary>
    internal bool IsMethodUsedAsCallback(string? className, string methodName)
    {
        var connections = SignalConnectionRegistry.GetSignalsCallingMethod(className, methodName);
        return connections.Count > 0;
    }

    /// <summary>
    /// Gets parameter types for a callback method based on the signals it's connected to.
    /// Analyzes the signal definitions to infer parameter types.
    /// </summary>
    internal IReadOnlyDictionary<string, GDInferredParameterType> InferCallbackParameterTypes(
        string? className,
        string methodName)
    {
        var result = new Dictionary<string, GDInferredParameterType>();
        var connections = SignalConnectionRegistry.GetSignalsCallingMethod(className, methodName);

        if (connections.Count == 0)
            return result;

        GDMethodDeclaration? method = null;
        foreach (var file in _project.ScriptFiles)
        {
            if (file.Class == null)
                continue;

            var fileTypeName = file.TypeName;
            if (className != null && fileTypeName != className)
                continue;

            method = file.Class.Members
                .OfType<GDMethodDeclaration>()
                .FirstOrDefault(m => m.Identifier?.Sequence == methodName);

            if (method != null)
                break;
        }

        if (method == null)
            return result;

        var paramNames = method.Parameters?
            .Select(p => p.Identifier?.Sequence)
            .Where(n => !string.IsNullOrEmpty(n))
            .Cast<string>()
            .ToList() ?? new List<string>();

        if (paramNames.Count == 0)
            return result;

        var runtimeProvider = _project.CreateRuntimeProvider();
        foreach (var connection in connections)
        {
            if (string.IsNullOrEmpty(connection.EmitterType))
                continue;

            var signalMember = runtimeProvider?.GetMember(connection.EmitterType, connection.SignalName);
            if (signalMember == null || signalMember.Kind != GDRuntimeMemberKind.Signal)
                continue;

            for (int i = 0; i < paramNames.Count; i++)
            {
                var paramName = paramNames[i];
                if (!result.ContainsKey(paramName))
                {
                    result[paramName] = GDInferredParameterType.Create(
                        paramName,
                        GDWellKnownTypes.Variant,
                        GDTypeConfidence.Low,
                        $"inferred from signal {connection.EmitterType}.{connection.SignalName}");
                }
            }
        }

        return result;
    }

    #endregion
}
