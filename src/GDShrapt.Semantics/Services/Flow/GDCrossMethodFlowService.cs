using GDShrapt.Abstractions;
using System;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for cross-method flow analysis.
/// Analyzes @onready variable safety across method boundaries.
/// </summary>
public class GDCrossMethodFlowService
{
    private GDCrossMethodFlowState? _crossMethodState;
    private GDMethodFlowSummaryRegistry? _flowSummaryRegistry;

    /// <summary>
    /// Delegate for performing analysis and returning state and registry.
    /// </summary>
    public delegate (GDCrossMethodFlowState state, GDMethodFlowSummaryRegistry registry) PerformAnalysisDelegate();

    /// <summary>
    /// Delegate for checking if a variable is @onready or _ready() initialized.
    /// </summary>
    public delegate bool IsOnreadyOrReadyInitializedVariableDelegate(string varName);

    /// <summary>
    /// Delegate for getting script type name.
    /// </summary>
    public delegate string GetTypeNameDelegate();

    private readonly PerformAnalysisDelegate _performAnalysis;
    private readonly IsOnreadyOrReadyInitializedVariableDelegate? _isOnreadyOrReadyInitializedVariable;
    private readonly GetTypeNameDelegate? _getTypeName;

    /// <summary>
    /// Initializes a new instance of the <see cref="GDCrossMethodFlowService"/> class.
    /// </summary>
    public GDCrossMethodFlowService(
        PerformAnalysisDelegate performAnalysis,
        IsOnreadyOrReadyInitializedVariableDelegate? isOnreadyOrReadyInitializedVariable = null,
        GetTypeNameDelegate? getTypeName = null)
    {
        _performAnalysis = performAnalysis ?? throw new ArgumentNullException(nameof(performAnalysis));
        _isOnreadyOrReadyInitializedVariable = isOnreadyOrReadyInitializedVariable;
        _getTypeName = getTypeName;
    }

    /// <summary>
    /// Checks if a variable is safe to access at a given method, considering cross-method analysis.
    /// </summary>
    public bool IsVariableSafeAtMethod(string varName, string methodName)
    {
        EnsureCrossMethodAnalysis();

        if (_crossMethodState == null)
            return false;

        // If variable is not @onready or _ready() initialized, use regular flow analysis
        if (_isOnreadyOrReadyInitializedVariable?.Invoke(varName) != true)
            return false;

        // Check method safety
        var safety = GetMethodOnreadySafety(methodName);
        if (safety != GDMethodOnreadySafety.Safe)
            return false;

        // Check if variable is guaranteed after ready and not conditionally initialized
        return _crossMethodState.GuaranteedAfterReady.Contains(varName) &&
               !_crossMethodState.MayBeNullAfterReady.Contains(varName);
    }

    /// <summary>
    /// Gets the @onready safety level for a method.
    /// </summary>
    public GDMethodOnreadySafety GetMethodOnreadySafety(string methodName)
    {
        EnsureCrossMethodAnalysis();

        if (_crossMethodState?.MethodSafetyCache.TryGetValue(methodName, out var safety) == true)
            return safety;

        return GDMethodOnreadySafety.Unknown;
    }

    /// <summary>
    /// Checks if a variable has conditional initialization in _ready().
    /// </summary>
    public bool HasConditionalReadyInitialization(string varName)
    {
        EnsureCrossMethodAnalysis();
        return _crossMethodState?.MayBeNullAfterReady.Contains(varName) ?? false;
    }

    /// <summary>
    /// Gets the flow summary for a method.
    /// </summary>
    public GDMethodFlowSummary? GetMethodFlowSummary(string methodName)
    {
        EnsureCrossMethodAnalysis();
        var typeName = _getTypeName?.Invoke() ?? "";
        return _flowSummaryRegistry?.GetSummary(typeName, methodName);
    }

    /// <summary>
    /// Gets the cross-method flow state.
    /// </summary>
    public GDCrossMethodFlowState? GetCrossMethodFlowState()
    {
        EnsureCrossMethodAnalysis();
        return _crossMethodState;
    }

    /// <summary>
    /// Ensures cross-method analysis has been performed.
    /// </summary>
    private void EnsureCrossMethodAnalysis()
    {
        if (_crossMethodState != null)
            return;

        var (state, registry) = _performAnalysis();
        _crossMethodState = state;
        _flowSummaryRegistry = registry;
    }
}
