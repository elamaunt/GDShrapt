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
    /// Creates a new project-level semantic model.
    /// </summary>
    /// <param name="project">The GDScript project to analyze.</param>
    public GDProjectSemanticModel(GDScriptProject project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
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

    #endregion

    #region Invalidation

    /// <summary>
    /// Invalidates the cached semantic model for a file.
    /// Call this when a file has changed.
    /// </summary>
    public void InvalidateFile(string filePath)
    {
        if (!string.IsNullOrEmpty(filePath))
            _fileModels.Remove(filePath);
    }

    /// <summary>
    /// Clears all cached semantic models.
    /// </summary>
    public void InvalidateAll()
    {
        _fileModels.Clear();
    }

    #endregion
}
