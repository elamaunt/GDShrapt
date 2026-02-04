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
    // Flow-sensitive type analysis (SSA-style)
    private readonly Dictionary<GDMethodDeclaration, GDFlowAnalyzer> _methodFlowAnalyzers = new();

    /// <summary>
    /// Gets the flow analyzer for a method.
    /// </summary>
    public GDFlowAnalyzer? GetFlowAnalyzer(GDMethodDeclaration method)
    {
        return _methodFlowAnalyzers.TryGetValue(method, out var analyzer) ? analyzer : null;
    }

    /// <summary>
    /// Gets all registered flow analyzers.
    /// </summary>
    public IEnumerable<KeyValuePair<GDMethodDeclaration, GDFlowAnalyzer>> GetAllFlowAnalyzers()
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

        // Find the containing method
        var method = FindContainingMethod(atLocation);
        if (method == null)
            return null;

        // Get the flow analyzer for this method
        if (!_methodFlowAnalyzers.TryGetValue(method, out var analyzer))
            return null;

        // Get flow state at the specific location and check narrowing
        var flowState = analyzer.GetStateAtLocation(atLocation);
        var varType = flowState?.GetVariableType(variableName);
        if (varType != null && varType.IsNarrowed && !string.IsNullOrEmpty(varType.NarrowedFromType))
            return varType.NarrowedFromType;

        return null;
    }

    // ========================================
    // Registration Methods (internal)
    // ========================================

    /// <summary>
    /// Registers a flow analyzer for a method.
    /// </summary>
    internal void RegisterFlowAnalyzer(GDMethodDeclaration method, GDFlowAnalyzer analyzer)
    {
        if (method != null && analyzer != null)
        {
            _methodFlowAnalyzers[method] = analyzer;
        }
    }

    /// <summary>
    /// Gets or creates a flow analyzer for a method.
    /// </summary>
    internal GDFlowAnalyzer GetOrCreateFlowAnalyzer(
        GDMethodDeclaration method,
        GDTypeInferenceEngine? typeEngine,
        System.Func<GDExpression, string?>? expressionTypeGetter = null,
        System.Func<IEnumerable<string>>? onreadyVarsGetter = null)
    {
        if (_methodFlowAnalyzers.TryGetValue(method, out var existing))
        {
            return existing;
        }

        var analyzer = new GDFlowAnalyzer(typeEngine, expressionTypeGetter, onreadyVarsGetter);
        // Cache BEFORE Analyze to prevent infinite recursion
        _methodFlowAnalyzers[method] = analyzer;
        analyzer.Analyze(method);
        return analyzer;
    }

    /// <summary>
    /// Checks if an analyzer exists for a method.
    /// </summary>
    internal bool HasFlowAnalyzer(GDMethodDeclaration method)
    {
        return _methodFlowAnalyzers.ContainsKey(method);
    }

    /// <summary>
    /// Clears all data in the registry.
    /// </summary>
    internal void Clear()
    {
        _methodFlowAnalyzers.Clear();
    }

    // ========================================
    // Helper Methods
    // ========================================

    /// <summary>
    /// Finds the containing method for a node.
    /// </summary>
    private static GDMethodDeclaration? FindContainingMethod(GDNode? node)
    {
        while (node != null)
        {
            if (node is GDMethodDeclaration method)
                return method;
            node = node.Parent;
        }
        return null;
    }
}
