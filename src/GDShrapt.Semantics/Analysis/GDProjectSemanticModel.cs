using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GDShrapt.Semantics;

/// <summary>
/// Project-level semantic model. THE unified entry point for all GDScript semantic operations.
///
/// <para>
/// Provides access to:
/// <list type="bullet">
///   <item><description>File-level semantic models via <see cref="GetSemanticModel(GDScriptFile)"/></description></item>
///   <item><description>Cross-file symbol resolution via <see cref="FindSymbolAcrossProject"/></description></item>
///   <item><description>Refactoring services via <see cref="Services"/></description></item>
///   <item><description>Diagnostics and validation via <see cref="Diagnostics"/></description></item>
///   <item><description>Type inference queries via <see cref="InferMethodReturnType"/> and <see cref="InferParameterTypes"/></description></item>
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
public class GDProjectSemanticModel
{
    private readonly GDScriptProject _project;
    private readonly Dictionary<string, GDSemanticModel> _fileModels = new();
    private GDRefactoringServices? _services;
    private GDDiagnosticsServices? _diagnostics;
    private GDSignalConnectionRegistry? _signalRegistry;
    private bool _signalRegistryInitialized;

    /// <summary>
    /// The underlying project.
    /// </summary>
    public GDScriptProject Project => _project;

    /// <summary>
    /// Refactoring and code action services.
    /// Provides access to rename, find references, extract method, and other refactorings.
    /// </summary>
    public GDRefactoringServices Services => _services ??= new GDRefactoringServices(_project);

    /// <summary>
    /// Diagnostics and validation services.
    /// Provides unified access to syntax checking, validation, and linting.
    /// </summary>
    public GDDiagnosticsServices Diagnostics => _diagnostics ??= new GDDiagnosticsServices(_project);

    /// <summary>
    /// Signal connection registry for inter-procedural analysis.
    /// Lazy initialized on first access.
    /// </summary>
    public GDSignalConnectionRegistry SignalConnectionRegistry
    {
        get
        {
            if (!_signalRegistryInitialized)
            {
                _signalRegistry = new GDSignalConnectionRegistry();
                InitializeSignalRegistry();
                _signalRegistryInitialized = true;
            }
            return _signalRegistry!;
        }
    }

    /// <summary>
    /// Creates a new project-level semantic model.
    /// </summary>
    /// <param name="project">The GDScript project to analyze.</param>
    public GDProjectSemanticModel(GDScriptProject project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
    }

    /// <summary>
    /// Initializes the signal connection registry by collecting all connections.
    /// </summary>
    private void InitializeSignalRegistry()
    {
        if (_signalRegistry == null)
            return;

        var collector = new GDSignalConnectionCollector(_project);
        var connections = collector.CollectAllConnections();

        foreach (var connection in connections)
        {
            _signalRegistry.Register(connection);
        }
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
    public static GDProjectSemanticModel Load(string projectPath, IGDSemanticLogger? logger = null, bool enableSceneTypes = true)
    {
        if (string.IsNullOrEmpty(projectPath))
            throw new ArgumentNullException(nameof(projectPath));

        var project = GDProjectLoader.LoadProject(projectPath, logger, enableSceneTypes);
        return new GDProjectSemanticModel(project);
    }

    /// <summary>
    /// Creates a project semantic model from a directory path asynchronously.
    /// </summary>
    /// <param name="projectPath">Path to the Godot project directory (containing project.godot).</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <param name="enableSceneTypes">Enable scene types provider for autoloads.</param>
    /// <returns>A fully initialized project semantic model.</returns>
    public static Task<GDProjectSemanticModel> LoadAsync(string projectPath, IGDSemanticLogger? logger = null, bool enableSceneTypes = true)
    {
        // Currently synchronous, but provides async signature for future optimization
        return Task.FromResult(Load(projectPath, logger, enableSceneTypes));
    }

    #endregion

    #region Semantic Model Access

    /// <summary>
    /// Gets or creates the semantic model for a script file.
    /// </summary>
    public GDSemanticModel? GetSemanticModel(GDScriptFile scriptFile)
    {
        if (scriptFile == null)
            return null;

        var path = scriptFile.FullPath ?? scriptFile.Reference?.FullPath ?? "";
        if (string.IsNullOrEmpty(path))
            return null;

        if (_fileModels.TryGetValue(path, out var model))
            return model;

        // Ensure the file is analyzed
        if (scriptFile.Analyzer?.SemanticModel == null)
        {
            var runtimeProvider = _project.CreateRuntimeProvider();
            scriptFile.Analyze(runtimeProvider);
        }

        model = scriptFile.Analyzer?.SemanticModel;
        if (model != null)
            _fileModels[path] = model;

        return model;
    }

    /// <summary>
    /// Gets the semantic model for a file by path.
    /// </summary>
    public GDSemanticModel? GetSemanticModel(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        // Try cache first
        if (_fileModels.TryGetValue(filePath, out var cached))
            return cached;

        // Find the script file
        var scriptFile = _project.ScriptFiles
            .FirstOrDefault(f => f.FullPath != null &&
                f.FullPath.Equals(filePath, System.StringComparison.OrdinalIgnoreCase));

        return scriptFile != null ? GetSemanticModel(scriptFile) : null;
    }

    #endregion

    #region Cross-File Symbol Resolution

    /// <summary>
    /// Finds a symbol by name across the entire project.
    /// </summary>
    public IEnumerable<(GDScriptFile File, GDSymbolInfo Symbol)> FindSymbolAcrossProject(string name)
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
    public IEnumerable<(GDScriptFile File, GDSymbolInfo Symbol)> FindTypeDeclarations(string typeName)
    {
        return FindSymbolAcrossProject(typeName)
            .Where(x => x.Symbol.Kind == GDSymbolKind.Class);
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
    public IReadOnlyList<GDCallSiteEntry> GetCallSitesInFile(string filePath)
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
    public IEnumerable<(string MethodKey, bool InCycle)> GetInferenceOrder()
    {
        var cycleDetector = new GDInferenceCycleDetector(_project);
        cycleDetector.BuildDependencyGraph();
        return cycleDetector.GetInferenceOrder();
    }

    /// <summary>
    /// Detects all cycles in the type inference dependency graph.
    /// </summary>
    public IEnumerable<IReadOnlyList<string>> DetectInferenceCycles()
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
    public GDInferredType? InferMethodReturnType(string className, string methodName)
    {
        // Find the method declaration
        var typeDecl = FindTypeDeclarations(className).FirstOrDefault();
        if (typeDecl.File == null)
            return null;

        var model = GetSemanticModel(typeDecl.File);
        if (model == null)
            return null;

        // Find the method in the class
        var classDecl = typeDecl.File.Class;
        if (classDecl == null)
            return null;

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == methodName);

        if (method == null)
            return null;

        // Use the model to infer type
        var type = model.GetTypeForNode(method);
        return string.IsNullOrEmpty(type) || type == "Variant"
            ? GDInferredType.Unknown()
            : GDInferredType.High(type, $"inferred from {className}.{methodName}");
    }

    /// <summary>
    /// Infers parameter types for a method using local usage analysis.
    /// </summary>
    public IReadOnlyDictionary<string, GDInferredParameterType> InferParameterTypes(
        string className,
        string methodName)
    {
        var result = new Dictionary<string, GDInferredParameterType>();

        // Find the method declaration
        var typeDecl = FindTypeDeclarations(className).FirstOrDefault();
        if (typeDecl.File == null)
            return result;

        var model = GetSemanticModel(typeDecl.File);
        if (model == null)
            return result;

        // Find the method
        var classDecl = typeDecl.File.Class;
        if (classDecl == null)
            return result;

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == methodName);

        if (method == null)
            return result;

        // Use local analysis
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
    public IReadOnlyDictionary<string, GDInferredParameterType> InferParameterTypesWithCallSites(
        string className,
        string methodName,
        bool preferCallSites = true)
    {
        var result = new Dictionary<string, GDInferredParameterType>();

        // Find the method declaration
        var typeDecl = FindTypeDeclarations(className).FirstOrDefault();
        if (typeDecl.File == null)
            return result;

        var model = GetSemanticModel(typeDecl.File);
        if (model == null)
            return result;

        // Find the method
        var classDecl = typeDecl.File.Class;
        if (classDecl == null)
            return result;

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == methodName);

        if (method == null)
            return result;

        // Get local usage analysis
        var localTypes = model.InferParameterTypes(method);

        // Get call site analysis if registry is available
        var callSiteRegistry = _project.CallSiteRegistry;
        if (callSiteRegistry != null)
        {
            var callSiteTypes = GetParameterTypesFromCallSites(className, methodName, method, callSiteRegistry);

            // Merge local and call site results
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

        // Get parameter names
        var paramNames = method.Parameters?
            .Select(p => p.Identifier?.Sequence)
            .Where(n => !string.IsNullOrEmpty(n))
            .Cast<string>()
            .ToList() ?? new List<string>();

        if (paramNames.Count == 0)
            return result;

        // Create the analyzer
        var analyzer = new GDCallSiteTypeAnalyzer(
            callSiteRegistry,
            file => GetSemanticModel(file));

        // Analyze call sites
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

        // Get all parameter names
        var allParams = new HashSet<string>(localTypes.Keys);
        foreach (var key in callSiteTypes.Keys)
            allParams.Add(key);

        foreach (var paramName in allParams)
        {
            var hasLocal = localTypes.TryGetValue(paramName, out var local);
            var hasCallSite = callSiteTypes.TryGetValue(paramName, out var callSite);

            if (hasLocal && hasCallSite)
            {
                // Both sources available - merge or prefer one
                result[paramName] = MergeSingleParameterType(local!, callSite!, preferCallSites, paramName);
            }
            else if (hasLocal)
            {
                result[paramName] = local!;
            }
            else if (hasCallSite)
            {
                result[paramName] = callSite!;
            }
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
        // If one is unknown, use the other
        if (local.IsUnknown && !callSite.IsUnknown)
            return callSite;
        if (callSite.IsUnknown && !local.IsUnknown)
            return local;
        if (local.IsUnknown && callSite.IsUnknown)
            return local;

        // If types match, use the one with higher confidence
        if (local.TypeName == callSite.TypeName)
        {
            return local.Confidence >= callSite.Confidence ? local : callSite;
        }

        // Types differ - preference-based selection or union
        if (preferCallSites && callSite.Confidence >= GDTypeConfidence.Medium)
        {
            // Call site types are more reliable as they show actual usage
            return callSite;
        }

        if (!preferCallSites && local.Confidence >= GDTypeConfidence.Medium)
        {
            // Local duck typing preferred
            return local;
        }

        // Create a union of both types
        var types = new List<string>();
        if (local.UnionTypes != null)
            types.AddRange(local.UnionTypes);
        else if (!string.IsNullOrEmpty(local.TypeName) && local.TypeName != "Variant")
            types.Add(local.TypeName);

        if (callSite.UnionTypes != null)
            types.AddRange(callSite.UnionTypes);
        else if (!string.IsNullOrEmpty(callSite.TypeName) && callSite.TypeName != "Variant")
            types.Add(callSite.TypeName);

        // Deduplicate
        types = types.Distinct().ToList();

        if (types.Count == 0)
            return GDInferredParameterType.Unknown(paramName);

        // Determine confidence (lower of the two sources)
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
    /// </summary>
    public void InvalidateFile(string filePath)
    {
        if (!string.IsNullOrEmpty(filePath))
        {
            _fileModels.Remove(filePath);

            // Also invalidate signal connections from this file
            if (_signalRegistryInitialized && _signalRegistry != null)
            {
                _signalRegistry.UnregisterFile(filePath);
                // Re-collect connections for this file
                var collector = new GDSignalConnectionCollector(_project);
                var file = GetFileByPath(filePath);
                if (file != null)
                {
                    var connections = collector.CollectConnectionsInFile(file);
                    foreach (var connection in connections)
                    {
                        _signalRegistry.Register(connection);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Clears all cached semantic models.
    /// </summary>
    public void InvalidateAll()
    {
        _fileModels.Clear();
        _signalRegistry?.Clear();
        _signalRegistryInitialized = false;
    }

    #endregion

    #region Signal Connection Queries

    /// <summary>
    /// Gets all signals that call a specific callback method.
    /// Useful for understanding what events trigger a method.
    /// </summary>
    public IReadOnlyList<GDSignalConnectionEntry> GetSignalsCallingMethod(string? className, string methodName)
    {
        return SignalConnectionRegistry.GetSignalsCallingMethod(className, methodName);
    }

    /// <summary>
    /// Gets all callbacks connected to a specific signal.
    /// Useful for finding all handlers of a signal.
    /// </summary>
    public IReadOnlyList<GDSignalConnectionEntry> GetCallbacksForSignal(string? emitterType, string signalName)
    {
        return SignalConnectionRegistry.GetCallbacksForSignal(emitterType, signalName);
    }

    /// <summary>
    /// Checks if a method is used as a signal callback.
    /// </summary>
    public bool IsMethodUsedAsCallback(string? className, string methodName)
    {
        var connections = SignalConnectionRegistry.GetSignalsCallingMethod(className, methodName);
        return connections.Count > 0;
    }

    /// <summary>
    /// Gets parameter types for a callback method based on the signals it's connected to.
    /// Analyzes the signal definitions to infer parameter types.
    /// </summary>
    public IReadOnlyDictionary<string, GDInferredParameterType> InferCallbackParameterTypes(
        string? className,
        string methodName)
    {
        var result = new Dictionary<string, GDInferredParameterType>();
        var connections = SignalConnectionRegistry.GetSignalsCallingMethod(className, methodName);

        if (connections.Count == 0)
            return result;

        // Find the method to get parameter names
        GDMethodDeclaration? method = null;
        foreach (var file in _project.ScriptFiles)
        {
            if (file.Class == null)
                continue;

            // Check if this file contains the callback
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

        // Try to get signal parameter types from the emitters
        var runtimeProvider = _project.CreateRuntimeProvider();
        foreach (var connection in connections)
        {
            if (string.IsNullOrEmpty(connection.EmitterType))
                continue;

            // Try to get signal info
            var signalMember = runtimeProvider?.GetMember(connection.EmitterType, connection.SignalName);
            if (signalMember == null || signalMember.Kind != GDRuntimeMemberKind.Signal)
                continue;

            // Signal parameters should match callback parameters
            // For now, mark them as inferred from signal
            for (int i = 0; i < paramNames.Count; i++)
            {
                var paramName = paramNames[i];
                if (!result.ContainsKey(paramName))
                {
                    // Mark as inferred from signal (Variant for now, could be improved)
                    result[paramName] = GDInferredParameterType.Create(
                        paramName,
                        "Variant",
                        GDTypeConfidence.Low,
                        $"inferred from signal {connection.EmitterType}.{connection.SignalName}");
                }
            }
        }

        return result;
    }

    #endregion
}
