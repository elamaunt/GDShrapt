using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Registry for flow analyzers and flow-sensitive type information.
/// Extracted from GDSemanticModel to provide cleaner separation of concerns.
/// </summary>
internal class GDFlowAnalysisRegistry
{
    // Flow-sensitive type analysis (SSA-style) — keyed by method-like scope (method or accessor body)
    private readonly Dictionary<GDNode, GDFlowAnalyzer> _methodFlowAnalyzers = new();

    /// <summary>
    /// Gets the flow analyzer for a method-like scope.
    /// </summary>
    public GDFlowAnalyzer? GetFlowAnalyzer(GDNode methodScope)
    {
        return _methodFlowAnalyzers.TryGetValue(methodScope, out var analyzer) ? analyzer : null;
    }

    /// <summary>
    /// Gets all registered flow analyzers.
    /// </summary>
    public IEnumerable<KeyValuePair<GDNode, GDFlowAnalyzer>> GetAllFlowAnalyzers()
    {
        return _methodFlowAnalyzers;
    }

    /// <summary>
    /// Gets the narrowed type for a variable at a specific location.
    /// </summary>
    public string? GetNarrowedType(string variableName, GDNode atLocation)
    {
        if (string.IsNullOrEmpty(variableName) || atLocation == null)
            return null;

        var scope = atLocation?.GetContainingMethodScope();
        if (scope == null)
            return null;

        // Get the flow analyzer for this scope
        if (!_methodFlowAnalyzers.TryGetValue(scope, out var analyzer))
            return null;

        // Get flow state at the specific location and check narrowing
        var flowState = analyzer.GetStateAtLocation(atLocation);
        var varType = flowState?.GetVariableType(variableName);
        if (varType != null && varType.IsNarrowed && varType.NarrowedFromType != null)
            return varType.NarrowedFromType.DisplayName;

        return null;
    }

    // ========================================
    // Registration Methods (internal)
    // ========================================

    /// <summary>
    /// Registers a flow analyzer for a method-like scope.
    /// </summary>
    internal void RegisterFlowAnalyzer(GDNode methodScope, GDFlowAnalyzer analyzer)
    {
        if (methodScope != null && analyzer != null)
        {
            _methodFlowAnalyzers[methodScope] = analyzer;
        }
    }

    /// <summary>
    /// Gets or creates a flow analyzer for a method-like scope (method or accessor body).
    /// </summary>
    internal GDFlowAnalyzer GetOrCreateFlowAnalyzer(
        GDNode methodScope,
        GDTypeInferenceEngine? typeEngine,
        System.Func<GDExpression, string?>? expressionTypeGetter = null,
        System.Func<IEnumerable<string>>? onreadyVarsGetter = null,
        string? filePath = null)
    {
        if (_methodFlowAnalyzers.TryGetValue(methodScope, out var existing))
        {
            return existing;
        }

        var analyzer = new GDFlowAnalyzer(typeEngine, expressionTypeGetter, onreadyVarsGetter);
        if (filePath != null)
            analyzer.SetFilePath(filePath);
        // Cache BEFORE Analyze to prevent infinite recursion
        _methodFlowAnalyzers[methodScope] = analyzer;
        analyzer.AnalyzeScope(methodScope);
        return analyzer;
    }

    /// <summary>
    /// Checks if an analyzer exists for a method-like scope.
    /// </summary>
    internal bool HasFlowAnalyzer(GDNode methodScope)
    {
        return _methodFlowAnalyzers.ContainsKey(methodScope);
    }

    /// <summary>
    /// Clears all data in the registry.
    /// </summary>
    internal void Clear()
    {
        _methodFlowAnalyzers.Clear();
    }

}
