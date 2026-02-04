using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for flow-sensitive type queries.
/// Provides methods to get flow-sensitive types and flow state at specific locations.
/// </summary>
public class GDFlowQueryService
{
    /// <summary>
    /// Delegate for getting flow-sensitive type at a location.
    /// </summary>
    public delegate string? GetFlowTypeDelegate(GDMethodDeclaration method, string variableName, GDNode atLocation);

    /// <summary>
    /// Delegate for getting flow variable type at a location.
    /// </summary>
    public delegate GDFlowVariableType? GetFlowVariableTypeDelegate(GDMethodDeclaration method, string variableName, GDNode atLocation);

    /// <summary>
    /// Delegate for getting flow state at a location.
    /// </summary>
    public delegate GDFlowState? GetFlowStateDelegate(GDMethodDeclaration method, GDNode atLocation);

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
    public string? GetFlowSensitiveType(string variableName, GDNode atLocation)
    {
        if (string.IsNullOrEmpty(variableName) || atLocation == null)
            return null;

        var method = FindContainingMethodNode(atLocation);
        if (method == null)
            return null;

        return _getFlowType(method, variableName, atLocation);
    }

    /// <summary>
    /// Gets the full flow variable type info at a specific location.
    /// </summary>
    public GDFlowVariableType? GetFlowVariableType(string variableName, GDNode atLocation)
    {
        if (string.IsNullOrEmpty(variableName) || atLocation == null)
            return null;

        var method = FindContainingMethodNode(atLocation);
        if (method == null)
            return null;

        return _getFlowVariableType(method, variableName, atLocation);
    }

    /// <summary>
    /// Gets the flow state at a specific location in the code.
    /// Returns null if flow analysis is not available.
    /// </summary>
    public GDFlowState? GetFlowStateAtLocation(GDNode atLocation)
    {
        if (atLocation == null)
            return null;

        var method = FindContainingMethodNode(atLocation);
        if (method == null)
            return null;

        return _getFlowState(method, atLocation);
    }

    /// <summary>
    /// Finds the containing method declaration for an AST node.
    /// </summary>
    public static GDMethodDeclaration? FindContainingMethodNode(GDNode node)
    {
        var current = node?.Parent;
        while (current != null)
        {
            if (current is GDMethodDeclaration method)
                return method;
            current = current.Parent;
        }
        return null;
    }

    /// <summary>
    /// Finds a local variable declaration in the containing method that appears before the given expression.
    /// </summary>
    public static GDVariableDeclarationStatement? FindLocalVariableDeclaration(GDExpression expr, string varName)
    {
        var method = FindContainingMethodNode(expr);
        if (method?.Statements == null) return null;

        var beforeLine = expr.StartLine;
        return method.AllNodes.OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == varName && v.StartLine < beforeLine);
    }

    /// <summary>
    /// Checks if a local variable has any reassignments within the method.
    /// Used to determine if we should fall back to initializer type inference.
    /// </summary>
    public static bool HasLocalReassignments(GDMethodDeclaration? method, string varName)
    {
        if (method?.Statements == null || string.IsNullOrEmpty(varName))
            return false;

        // Look for assignment expressions targeting this variable
        foreach (var node in method.AllNodes)
        {
            // Skip the initial declaration
            if (node is GDVariableDeclarationStatement)
                continue;

            // Check for reassignment: x = something
            if (node is GDDualOperatorExpression dualOp &&
                dualOp.Operator?.OperatorType == GDDualOperatorType.Assignment &&
                dualOp.LeftExpression is GDIdentifierExpression leftIdent &&
                leftIdent.Identifier?.Sequence == varName)
            {
                return true;
            }
        }

        return false;
    }
}
