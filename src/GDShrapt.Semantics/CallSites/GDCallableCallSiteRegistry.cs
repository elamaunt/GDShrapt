using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Registry for Callable call sites across files.
/// Maps lambda definitions to their call sites for parameter type inference.
/// </summary>
internal class GDCallableCallSiteRegistry
{
    private readonly Dictionary<string, GDCallableDefinition> _definitions = new();
    private readonly Dictionary<string, List<GDCallableCallSiteInfo>> _callSitesByDefinition = new();
    private readonly Dictionary<string, List<GDCallableCallSiteInfo>> _callSitesByVariable = new();
    private readonly Dictionary<string, GDCallableCallSiteCollector> _collectorsByFile = new();

    // Phase 2: Inter-procedural tracking
    private readonly Dictionary<string, GDMethodCallableProfile> _methodProfiles = new();
    private readonly List<GDCallableArgumentBinding> _argumentBindings = new();
    private readonly Dictionary<string, List<GDCallableCallSiteInfo>> _callSitesByMethodParam = new();

    /// <summary>
    /// Registers a Callable definition.
    /// </summary>
    public void RegisterDefinition(GDCallableDefinition definition)
    {
        if (definition == null)
            return;

        _definitions[definition.UniqueId] = definition;
    }

    /// <summary>
    /// Registers a call site.
    /// </summary>
    public void RegisterCallSite(GDCallableCallSiteInfo callSite)
    {
        if (callSite == null)
            return;

        // Register by definition
        if (callSite.ResolvedDefinition != null)
        {
            var defId = callSite.ResolvedDefinition.UniqueId;
            if (!_callSitesByDefinition.TryGetValue(defId, out var sites))
            {
                sites = new List<GDCallableCallSiteInfo>();
                _callSitesByDefinition[defId] = sites;
            }
            sites.Add(callSite);
        }

        // Register by variable name
        if (!string.IsNullOrEmpty(callSite.CallableVariableName))
        {
            if (!_callSitesByVariable.TryGetValue(callSite.CallableVariableName, out var varSites))
            {
                varSites = new List<GDCallableCallSiteInfo>();
                _callSitesByVariable[callSite.CallableVariableName] = varSites;
            }
            varSites.Add(callSite);
        }
    }

    /// <summary>
    /// Registers a collector for a file.
    /// </summary>
    public void RegisterCollector(string filePath, GDCallableCallSiteCollector collector)
    {
        if (string.IsNullOrEmpty(filePath) || collector == null)
            return;

        _collectorsByFile[filePath] = collector;

        // Register all definitions and call sites
        foreach (var def in collector.Tracker.AllDefinitions)
        {
            RegisterDefinition(def);
        }

        foreach (var callSite in collector.CallSites)
        {
            RegisterCallSite(callSite);
        }
    }

    /// <summary>
    /// Gets call sites for a specific Callable definition.
    /// </summary>
    public IReadOnlyList<GDCallableCallSiteInfo> GetCallSitesFor(GDCallableDefinition definition)
    {
        if (definition == null)
            return Array.Empty<GDCallableCallSiteInfo>();

        if (_callSitesByDefinition.TryGetValue(definition.UniqueId, out var sites))
            return sites;

        return Array.Empty<GDCallableCallSiteInfo>();
    }

    /// <summary>
    /// Gets call sites for a specific lambda expression.
    /// </summary>
    public IReadOnlyList<GDCallableCallSiteInfo> GetCallSitesFor(GDMethodExpression lambda, GDScriptFile? sourceFile)
    {
        if (lambda == null)
            return Array.Empty<GDCallableCallSiteInfo>();

        var definition = GDCallableDefinition.FromLambda(lambda, sourceFile);
        return GetCallSitesFor(definition);
    }

    /// <summary>
    /// Gets call sites for a specific variable name.
    /// </summary>
    public IReadOnlyList<GDCallableCallSiteInfo> GetCallSitesForVariable(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return Array.Empty<GDCallableCallSiteInfo>();

        if (_callSitesByVariable.TryGetValue(variableName, out var sites))
            return sites;

        return Array.Empty<GDCallableCallSiteInfo>();
    }

    /// <summary>
    /// Gets all registered definitions.
    /// </summary>
    public IEnumerable<GDCallableDefinition> AllDefinitions => _definitions.Values;

    /// <summary>
    /// Gets all registered call sites.
    /// </summary>
    public IEnumerable<GDCallableCallSiteInfo> AllCallSites =>
        _callSitesByDefinition.Values.SelectMany(s => s);

    /// <summary>
    /// Invalidates data for a specific file.
    /// </summary>
    public void InvalidateFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        // Remove collector
        if (_collectorsByFile.TryGetValue(filePath, out var collector))
        {
            // Remove definitions from this file
            foreach (var def in collector.Tracker.AllDefinitions)
            {
                _definitions.Remove(def.UniqueId);
                _callSitesByDefinition.Remove(def.UniqueId);
            }

            // Remove call sites from this file
            foreach (var sites in _callSitesByVariable.Values.ToList())
            {
                sites.RemoveAll(cs => cs.SourceFile?.Reference?.FullPath == filePath);
            }

            foreach (var sites in _callSitesByDefinition.Values.ToList())
            {
                sites.RemoveAll(cs => cs.SourceFile?.Reference?.FullPath == filePath);
            }

            _collectorsByFile.Remove(filePath);
        }
    }

    /// <summary>
    /// Clears all registered data.
    /// </summary>
    public void Clear()
    {
        _definitions.Clear();
        _callSitesByDefinition.Clear();
        _callSitesByVariable.Clear();
        _collectorsByFile.Clear();
    }

    /// <summary>
    /// Infers parameter types for a lambda from its call sites.
    /// </summary>
    public IReadOnlyDictionary<int, GDUnionType> InferParameterTypes(GDMethodExpression lambda, GDScriptFile? sourceFile)
    {
        var result = new Dictionary<int, GDUnionType>();

        var callSites = GetCallSitesFor(lambda, sourceFile);
        if (callSites.Count == 0)
            return result;

        // Collect argument types for each parameter position
        foreach (var callSite in callSites)
        {
            foreach (var arg in callSite.Arguments)
            {
                if (string.IsNullOrEmpty(arg.InferredType) || arg.InferredType == "Variant")
                    continue;

                if (!result.TryGetValue(arg.Index, out var unionType))
                {
                    unionType = new GDUnionType();
                    result[arg.Index] = unionType;
                }

                unionType.AddType(arg.InferredType);
            }
        }

        return result;
    }

    /// <summary>
    /// Infers parameter type for a specific parameter index.
    /// </summary>
    public string? InferParameterType(GDMethodExpression lambda, GDScriptFile? sourceFile, int parameterIndex)
    {
        var types = InferParameterTypes(lambda, sourceFile);

        if (types.TryGetValue(parameterIndex, out var unionType))
        {
            return unionType.EffectiveType;
        }

        return null;
    }

    #region Phase 2: Inter-procedural tracking

    /// <summary>
    /// Registers a method Callable profile.
    /// </summary>
    public void RegisterMethodProfile(GDMethodCallableProfile profile)
    {
        if (profile == null)
            return;

        _methodProfiles[profile.MethodKey] = profile;

        // Register call sites on parameters
        foreach (var kvp in profile.ParameterCallSites)
        {
            var paramName = kvp.Key;
            var paramIndex = profile.GetParameterIndex(paramName);
            var key = CreateMethodParamKey(profile.MethodKey, paramIndex);

            if (!_callSitesByMethodParam.TryGetValue(key, out var sites))
            {
                sites = new List<GDCallableCallSiteInfo>();
                _callSitesByMethodParam[key] = sites;
            }

            sites.AddRange(kvp.Value);
        }
    }

    /// <summary>
    /// Registers an argument binding.
    /// </summary>
    public void RegisterArgumentBinding(GDCallableArgumentBinding binding)
    {
        if (binding == null)
            return;

        _argumentBindings.Add(binding);
    }

    /// <summary>
    /// Registers a flow collector for a file.
    /// </summary>
    public void RegisterFlowCollector(string filePath, GDCallableFlowCollector collector)
    {
        if (string.IsNullOrEmpty(filePath) || collector == null)
            return;

        foreach (var profile in collector.MethodProfiles)
        {
            RegisterMethodProfile(profile);
        }

        foreach (var binding in collector.ArgumentBindings)
        {
            RegisterArgumentBinding(binding);
        }
    }

    /// <summary>
    /// Gets a method profile by key.
    /// </summary>
    public GDMethodCallableProfile? GetMethodProfile(string methodKey)
    {
        if (string.IsNullOrEmpty(methodKey))
            return null;

        return _methodProfiles.TryGetValue(methodKey, out var profile) ? profile : null;
    }

    /// <summary>
    /// Gets call sites for a method parameter.
    /// </summary>
    public IReadOnlyList<GDCallableCallSiteInfo> GetCallSitesForMethodParameter(string methodKey, int paramIndex)
    {
        var key = CreateMethodParamKey(methodKey, paramIndex);

        if (_callSitesByMethodParam.TryGetValue(key, out var sites))
            return sites;

        return Array.Empty<GDCallableCallSiteInfo>();
    }

    /// <summary>
    /// Gets argument bindings for a lambda.
    /// </summary>
    public IReadOnlyList<GDCallableArgumentBinding> GetBindingsForLambda(GDCallableDefinition lambda)
    {
        if (lambda == null)
            return Array.Empty<GDCallableArgumentBinding>();

        return _argumentBindings
            .Where(b => b.LambdaDefinition?.UniqueId == lambda.UniqueId)
            .ToList();
    }

    /// <summary>
    /// Gets argument bindings for a lambda expression.
    /// </summary>
    public IReadOnlyList<GDCallableArgumentBinding> GetBindingsForLambda(GDMethodExpression lambda, GDScriptFile? sourceFile)
    {
        if (lambda == null)
            return Array.Empty<GDCallableArgumentBinding>();

        var definition = GDCallableDefinition.FromLambda(lambda, sourceFile);
        return GetBindingsForLambda(definition);
    }

    /// <summary>
    /// Infers parameter types for a lambda including inter-procedural analysis.
    /// </summary>
    public IReadOnlyDictionary<int, GDUnionType> InferParameterTypesWithFlow(GDMethodExpression lambda, GDScriptFile? sourceFile)
    {
        var result = new Dictionary<int, GDUnionType>();

        // 1. Direct call sites (existing logic)
        var directCallSites = GetCallSitesFor(lambda, sourceFile);
        CollectTypesFromCallSites(directCallSites, result);

        // 2. Inter-procedural: lambda passed to method parameter
        var definition = GDCallableDefinition.FromLambda(lambda, sourceFile);
        var bindings = GetBindingsForLambda(definition);

        foreach (var binding in bindings)
        {
            // Get call sites on the target method parameter
            var paramCallSites = GetCallSitesForMethodParameter(
                binding.TargetMethodKey,
                binding.TargetParameterIndex);

            CollectTypesFromCallSites(paramCallSites, result);
        }

        return result;
    }

    /// <summary>
    /// Infers parameter type for a specific parameter index with inter-procedural analysis.
    /// </summary>
    public string? InferParameterTypeWithFlow(GDMethodExpression lambda, GDScriptFile? sourceFile, int parameterIndex)
    {
        var types = InferParameterTypesWithFlow(lambda, sourceFile);

        if (types.TryGetValue(parameterIndex, out var unionType))
        {
            return unionType.EffectiveType;
        }

        return null;
    }

    /// <summary>
    /// All registered method profiles.
    /// </summary>
    public IEnumerable<GDMethodCallableProfile> AllMethodProfiles => _methodProfiles.Values;

    /// <summary>
    /// All registered argument bindings.
    /// </summary>
    public IEnumerable<GDCallableArgumentBinding> AllArgumentBindings => _argumentBindings;

    private static string CreateMethodParamKey(string methodKey, int paramIndex)
    {
        return $"{methodKey}#{paramIndex}";
    }

    private static void CollectTypesFromCallSites(
        IReadOnlyList<GDCallableCallSiteInfo> callSites,
        Dictionary<int, GDUnionType> result)
    {
        foreach (var callSite in callSites)
        {
            foreach (var arg in callSite.Arguments)
            {
                if (string.IsNullOrEmpty(arg.InferredType) || arg.InferredType == "Variant")
                    continue;

                if (!result.TryGetValue(arg.Index, out var unionType))
                {
                    unionType = new GDUnionType();
                    result[arg.Index] = unionType;
                }

                unionType.AddType(arg.InferredType);
            }
        }
    }

    #endregion
}
