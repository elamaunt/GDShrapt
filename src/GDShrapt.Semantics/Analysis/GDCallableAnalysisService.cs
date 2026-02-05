using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Unified service for Callable analysis.
/// Provides a single entry point for:
/// - Tracking lambda definitions and method references
/// - Collecting .call() call sites
/// - Inter-procedural parameter type inference
/// - Argument binding analysis
/// </summary>
internal class GDCallableAnalysisService
{
    private readonly GDCallableCallSiteRegistry _registry;
    private readonly Dictionary<string, GDCallableCallSiteCollector> _collectorsByFile = new();
    private readonly Dictionary<string, GDCallableFlowCollector> _flowCollectorsByFile = new();

    /// <summary>
    /// Creates a new Callable analysis service.
    /// </summary>
    public GDCallableAnalysisService()
    {
        _registry = new GDCallableCallSiteRegistry();
    }

    /// <summary>
    /// Creates a new Callable analysis service with an existing registry.
    /// </summary>
    public GDCallableAnalysisService(GDCallableCallSiteRegistry registry)
    {
        _registry = registry ?? new GDCallableCallSiteRegistry();
    }

    /// <summary>
    /// Gets the underlying call site registry.
    /// </summary>
    public GDCallableCallSiteRegistry Registry => _registry;

    // ========================================
    // Analysis Methods
    // ========================================

    /// <summary>
    /// Analyzes a script file for Callable definitions and call sites.
    /// </summary>
    /// <param name="scriptFile">The script file to analyze.</param>
    /// <param name="typeInferrer">Optional type inferrer for argument types.</param>
    /// <param name="methodResolver">Optional method resolver for inter-procedural analysis.</param>
    public void AnalyzeFile(
        GDScriptFile scriptFile,
        Func<GDExpression, string?>? typeInferrer = null,
        Func<string, GDMethodDeclaration?>? methodResolver = null)
    {
        if (scriptFile?.Class == null)
            return;

        var filePath = scriptFile.Reference?.FullPath ?? string.Empty;

        var collector = new GDCallableCallSiteCollector(scriptFile, typeInferrer);
        collector.Collect(scriptFile.Class);
        _registry.RegisterCollector(filePath, collector);
        _collectorsByFile[filePath] = collector;

        var flowCollector = new GDCallableFlowCollector(scriptFile, typeInferrer, methodResolver);
        flowCollector.Collect(scriptFile.Class);
        _registry.RegisterFlowCollector(filePath, flowCollector);
        _flowCollectorsByFile[filePath] = flowCollector;
    }

    /// <summary>
    /// Analyzes a single method for Callable definitions and call sites.
    /// </summary>
    public void AnalyzeMethod(
        GDMethodDeclaration method,
        GDScriptFile? sourceFile = null,
        Func<GDExpression, string?>? typeInferrer = null)
    {
        if (method == null)
            return;

        var collector = new GDCallableCallSiteCollector(sourceFile, typeInferrer);
        collector.Collect(method);

        foreach (var def in collector.Tracker.AllDefinitions)
        {
            _registry.RegisterDefinition(def);
        }

        foreach (var callSite in collector.CallSites)
        {
            _registry.RegisterCallSite(callSite);
        }
    }

    // ========================================
    // Query Methods - Definitions
    // ========================================

    /// <summary>
    /// Gets all registered Callable definitions.
    /// </summary>
    public IEnumerable<GDCallableDefinition> AllDefinitions => _registry.AllDefinitions;

    /// <summary>
    /// Gets all lambda definitions.
    /// </summary>
    public IEnumerable<GDCallableDefinition> LambdaDefinitions =>
        _registry.AllDefinitions.Where(d => d.Kind == GDCallableDefinitionKind.Lambda);

    /// <summary>
    /// Gets all method reference definitions.
    /// </summary>
    public IEnumerable<GDCallableDefinition> MethodReferenceDefinitions =>
        _registry.AllDefinitions.Where(d => d.Kind == GDCallableDefinitionKind.MethodReference);

    /// <summary>
    /// Creates a definition from a lambda expression.
    /// </summary>
    public GDCallableDefinition CreateDefinition(GDMethodExpression lambda, GDScriptFile? sourceFile)
    {
        return GDCallableDefinition.FromLambda(lambda, sourceFile);
    }

    /// <summary>
    /// Creates a definition from a method reference.
    /// </summary>
    public GDCallableDefinition CreateMethodReference(
        string methodName,
        string? declaringClassName,
        GDScriptFile? sourceFile,
        IReadOnlyList<string>? parameterNames = null)
    {
        return GDCallableDefinition.FromMethodReference(methodName, declaringClassName, sourceFile, parameterNames);
    }

    // ========================================
    // Query Methods - Call Sites
    // ========================================

    /// <summary>
    /// Gets all registered call sites.
    /// </summary>
    public IEnumerable<GDCallableCallSiteInfo> AllCallSites => _registry.AllCallSites;

    /// <summary>
    /// Gets call sites for a specific Callable definition.
    /// </summary>
    public IReadOnlyList<GDCallableCallSiteInfo> GetCallSites(GDCallableDefinition definition)
    {
        return _registry.GetCallSitesFor(definition);
    }

    /// <summary>
    /// Gets call sites for a specific lambda expression.
    /// </summary>
    public IReadOnlyList<GDCallableCallSiteInfo> GetCallSites(GDMethodExpression lambda, GDScriptFile? sourceFile)
    {
        return _registry.GetCallSitesFor(lambda, sourceFile);
    }

    /// <summary>
    /// Gets call sites for a specific variable name.
    /// </summary>
    public IReadOnlyList<GDCallableCallSiteInfo> GetCallSitesForVariable(string variableName)
    {
        return _registry.GetCallSitesForVariable(variableName);
    }

    // ========================================
    // Type Inference
    // ========================================

    /// <summary>
    /// Infers parameter types for a lambda from its call sites.
    /// Uses direct call site analysis only.
    /// </summary>
    public IReadOnlyDictionary<int, GDUnionType> InferParameterTypes(
        GDMethodExpression lambda,
        GDScriptFile? sourceFile)
    {
        return _registry.InferParameterTypes(lambda, sourceFile);
    }

    /// <summary>
    /// Infers parameter types for a lambda with inter-procedural analysis.
    /// Considers both direct calls and calls via method parameters.
    /// </summary>
    public IReadOnlyDictionary<int, GDUnionType> InferParameterTypesWithFlow(
        GDMethodExpression lambda,
        GDScriptFile? sourceFile)
    {
        return _registry.InferParameterTypesWithFlow(lambda, sourceFile);
    }

    /// <summary>
    /// Infers the type for a specific parameter.
    /// </summary>
    public string? InferParameterType(
        GDMethodExpression lambda,
        GDScriptFile? sourceFile,
        int parameterIndex)
    {
        return _registry.InferParameterType(lambda, sourceFile, parameterIndex);
    }

    /// <summary>
    /// Infers the type for a specific parameter with inter-procedural analysis.
    /// </summary>
    public string? InferParameterTypeWithFlow(
        GDMethodExpression lambda,
        GDScriptFile? sourceFile,
        int parameterIndex)
    {
        return _registry.InferParameterTypeWithFlow(lambda, sourceFile, parameterIndex);
    }

    /// <summary>
    /// Creates a GDCallableSemanticType from inferred parameter types.
    /// </summary>
    public GDCallableSemanticType? InferCallableType(
        GDMethodExpression lambda,
        GDScriptFile? sourceFile,
        Func<GDExpression, GDSemanticType?>? returnTypeInferrer = null)
    {
        var paramTypes = InferParameterTypesWithFlow(lambda, sourceFile);
        if (paramTypes.Count == 0 && lambda.Parameters?.Count() == 0)
            return new GDCallableSemanticType();

        var paramCount = lambda.Parameters?.Count() ?? 0;
        var parameterTypes = new List<GDSemanticType>();

        for (int i = 0; i < paramCount; i++)
        {
            if (paramTypes.TryGetValue(i, out var unionType))
            {
                var typeName = unionType.EffectiveType;
                parameterTypes.Add(GDSemanticType.FromTypeName(typeName));
            }
            else
            {
                parameterTypes.Add(GDVariantSemanticType.Instance);
            }
        }

        GDSemanticType? returnType = null;

        return new GDCallableSemanticType(returnType, parameterTypes);
    }

    // ========================================
    // Inter-procedural Analysis
    // ========================================

    /// <summary>
    /// Gets all method profiles.
    /// </summary>
    public IEnumerable<GDMethodCallableProfile> AllMethodProfiles => _registry.AllMethodProfiles;

    /// <summary>
    /// Gets a method profile by key.
    /// </summary>
    public GDMethodCallableProfile? GetMethodProfile(string methodKey)
    {
        return _registry.GetMethodProfile(methodKey);
    }

    /// <summary>
    /// Gets call sites for a method parameter.
    /// </summary>
    public IReadOnlyList<GDCallableCallSiteInfo> GetCallSitesForMethodParameter(
        string methodKey,
        int paramIndex)
    {
        return _registry.GetCallSitesForMethodParameter(methodKey, paramIndex);
    }

    /// <summary>
    /// Gets argument bindings for a lambda.
    /// </summary>
    public IReadOnlyList<GDCallableArgumentBinding> GetBindingsForLambda(GDCallableDefinition lambda)
    {
        return _registry.GetBindingsForLambda(lambda);
    }

    /// <summary>
    /// Gets argument bindings for a lambda expression.
    /// </summary>
    public IReadOnlyList<GDCallableArgumentBinding> GetBindingsForLambda(
        GDMethodExpression lambda,
        GDScriptFile? sourceFile)
    {
        return _registry.GetBindingsForLambda(lambda, sourceFile);
    }

    /// <summary>
    /// Gets all argument bindings.
    /// </summary>
    public IEnumerable<GDCallableArgumentBinding> AllArgumentBindings => _registry.AllArgumentBindings;

    // ========================================
    // File Management
    // ========================================

    /// <summary>
    /// Invalidates analysis data for a file.
    /// </summary>
    public void InvalidateFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        _registry.InvalidateFile(filePath);
        _collectorsByFile.Remove(filePath);
        _flowCollectorsByFile.Remove(filePath);
    }

    /// <summary>
    /// Clears all analysis data.
    /// </summary>
    public void Clear()
    {
        _registry.Clear();
        _collectorsByFile.Clear();
        _flowCollectorsByFile.Clear();
    }

    /// <summary>
    /// Gets the collector for a specific file.
    /// </summary>
    public GDCallableCallSiteCollector? GetCollector(string filePath)
    {
        return _collectorsByFile.TryGetValue(filePath, out var collector) ? collector : null;
    }

    /// <summary>
    /// Gets the flow collector for a specific file.
    /// </summary>
    public GDCallableFlowCollector? GetFlowCollector(string filePath)
    {
        return _flowCollectorsByFile.TryGetValue(filePath, out var collector) ? collector : null;
    }
}
