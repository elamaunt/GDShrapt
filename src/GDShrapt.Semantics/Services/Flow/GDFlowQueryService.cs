using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for flow-sensitive type queries.
/// Provides methods to get flow-sensitive types and flow state at specific locations.
/// </summary>
internal class GDFlowQueryService
{
    /// <summary>
    /// Delegate for getting flow-sensitive type at a location.
    /// </summary>
    public delegate GDSemanticType? GetFlowTypeDelegate(GDNode methodScope, string variableName, GDNode atLocation);

    /// <summary>
    /// Delegate for getting flow variable type at a location.
    /// </summary>
    public delegate GDFlowVariableType? GetFlowVariableTypeDelegate(GDNode methodScope, string variableName, GDNode atLocation);

    /// <summary>
    /// Delegate for getting flow state at a location.
    /// </summary>
    public delegate GDFlowState? GetFlowStateDelegate(GDNode methodScope, GDNode atLocation);

    private readonly GetFlowTypeDelegate _getFlowType;
    private readonly GetFlowVariableTypeDelegate _getFlowVariableType;
    private readonly GetFlowStateDelegate _getFlowState;

    /// <summary>
    /// Initializes a new instance of the <see cref="GDFlowQueryService"/> class.
    /// </summary>
    public GDFlowQueryService(
        GetFlowTypeDelegate getFlowType,
        GetFlowVariableTypeDelegate getFlowVariableType,
        GetFlowStateDelegate getFlowState)
    {
        _getFlowType = getFlowType;
        _getFlowVariableType = getFlowVariableType;
        _getFlowState = getFlowState;
    }

    /// <summary>
    /// Gets the flow-sensitive type for a variable at a specific location.
    /// Returns null if flow analysis is not available.
    /// </summary>
    public GDSemanticType? GetFlowSensitiveType(string variableName, GDNode atLocation)
    {
        if (string.IsNullOrEmpty(variableName) || atLocation == null)
            return null;

        var scope = atLocation.GetContainingMethodScope();
        if (scope == null)
            return null;

        return _getFlowType(scope, variableName, atLocation);
    }

    /// <summary>
    /// Gets the full flow variable type info at a specific location.
    /// </summary>
    public GDFlowVariableType? GetFlowVariableType(string variableName, GDNode atLocation)
    {
        if (string.IsNullOrEmpty(variableName) || atLocation == null)
            return null;

        var scope = atLocation.GetContainingMethodScope();
        if (scope == null)
            return null;

        return _getFlowVariableType(scope, variableName, atLocation);
    }

    /// <summary>
    /// Gets the flow state at a specific location in the code.
    /// Returns null if flow analysis is not available.
    /// </summary>
    public GDFlowState? GetFlowStateAtLocation(GDNode atLocation)
    {
        if (atLocation == null)
            return null;

        var scope = atLocation.GetContainingMethodScope();
        if (scope == null)
            return null;

        return _getFlowState(scope, atLocation);
    }

    /// <summary>
    /// Finds a local variable declaration in the containing method that appears before the given expression.
    /// </summary>
    public static GDVariableDeclarationStatement? FindLocalVariableDeclaration(GDExpression expr, string varName)
    {
        var scope = expr.GetContainingMethodScope();
        if (scope == null) return null;

        var beforeLine = expr.StartLine;
        var scopeIndex = GDAstNodeIndex.Build(scope, typeof(GDVariableDeclarationStatement));
        return scopeIndex.GetNodes<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == varName && v.StartLine < beforeLine);
    }

    /// <summary>
    /// Finds a local variable declaration in the specified method that appears before the given line.
    /// </summary>
    public static GDVariableDeclarationStatement? FindLocalVariableDeclaration(GDMethodDeclaration method, string varName, int beforeLine)
    {
        if (method?.Statements == null) return null;

        var methodIndex = GDAstNodeIndex.Build(method, typeof(GDVariableDeclarationStatement));
        return methodIndex.GetNodes<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == varName && v.StartLine < beforeLine);
    }
}
