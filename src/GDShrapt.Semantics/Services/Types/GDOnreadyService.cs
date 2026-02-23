using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for @onready and _ready() variable analysis.
/// </summary>
internal class GDOnreadyService
{
    /// <summary>
    /// Delegate for finding symbol by name.
    /// </summary>
    public delegate GDSymbolInfo? FindSymbolDelegate(string name);

    /// <summary>
    /// Delegate for getting all variable symbols.
    /// </summary>
    public delegate IReadOnlyList<GDSymbolInfo> GetVariablesDelegate();

    /// <summary>
    /// Delegate for getting all method symbols.
    /// </summary>
    public delegate IReadOnlyList<GDSymbolInfo> GetMethodsDelegate();

    private readonly FindSymbolDelegate? _findSymbol;
    private readonly GetVariablesDelegate? _getVariables;
    private readonly GetMethodsDelegate? _getMethods;

    /// <summary>
    /// Initializes a new instance of the <see cref="GDOnreadyService"/> class.
    /// </summary>
    public GDOnreadyService(
        FindSymbolDelegate? findSymbol = null,
        GetVariablesDelegate? getVariables = null,
        GetMethodsDelegate? getMethods = null)
    {
        _findSymbol = findSymbol;
        _getVariables = getVariables;
        _getMethods = getMethods;
    }

    /// <summary>
    /// Checks if a variable has the @onready attribute.
    /// </summary>
    public bool IsOnreadyVariable(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return false;

        var symbol = _findSymbol?.Invoke(variableName);
        if (symbol?.DeclarationNode is not GDVariableDeclaration varDecl)
            return false;

        return varDecl.AttributesDeclaredBefore.Any(attr => attr.Attribute?.IsOnready() == true);
    }

    /// <summary>
    /// Checks if a variable is initialized in _ready() method.
    /// </summary>
    public bool IsReadyInitializedVariable(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return false;

        var symbol = _findSymbol?.Invoke(variableName);
        if (symbol?.DeclarationNode is not GDVariableDeclaration varDecl)
            return false;

        // Has initializer at class level — not a _ready() initialized variable
        if (varDecl.Initializer != null)
            return false;

        return HasAssignmentInReadyMethod(variableName);
    }

    /// <summary>
    /// Checks if a variable is either @onready or initialized in _ready().
    /// </summary>
    public bool IsOnreadyOrReadyInitializedVariable(string variableName)
    {
        return IsOnreadyVariable(variableName) || IsReadyInitializedVariable(variableName);
    }

    /// <summary>
    /// Gets all @onready variable names in the current class.
    /// </summary>
    public IEnumerable<string> GetOnreadyVariables()
    {
        var variables = _getVariables?.Invoke();
        if (variables == null)
            yield break;

        foreach (var varSymbol in variables)
        {
            if (IsOnreadyVariable(varSymbol.Name))
                yield return varSymbol.Name;
        }
    }

    /// <summary>
    /// Gets the _ready() method declaration if it exists.
    /// </summary>
    public GDMethodDeclaration? GetReadyMethod()
    {
        var methods = _getMethods?.Invoke();
        if (methods == null)
            return null;

        foreach (var methodSymbol in methods)
        {
            if (methodSymbol.DeclarationNode is GDMethodDeclaration method && method.IsReady())
                return method;
        }

        return null;
    }

    /// <summary>
    /// Checks if there's an assignment to a variable in the _ready() method.
    /// </summary>
    private bool HasAssignmentInReadyMethod(string variableName)
    {
        var readyMethod = GetReadyMethod();
        if (readyMethod == null)
            return false;

        var visitor = new AssignmentFinder(variableName);
        readyMethod.WalkIn(visitor);
        return visitor.Found;
    }

    /// <summary>
    /// Helper visitor to find assignments to a specific variable.
    /// </summary>
    private class AssignmentFinder : GDVisitor
    {
        private readonly string _targetVariable;
        public bool Found { get; private set; }

        public AssignmentFinder(string targetVariable)
        {
            _targetVariable = targetVariable;
        }

        public override void Visit(GDExpressionStatement statement)
        {
            if (Found)
                return;

            base.Visit(statement);

            if (statement.Expression is GDDualOperatorExpression dualOp)
            {
                if (dualOp.OperatorType == GDDualOperatorType.Assignment ||
                    dualOp.OperatorType == GDDualOperatorType.AddAndAssign ||
                    dualOp.OperatorType == GDDualOperatorType.SubtractAndAssign ||
                    dualOp.OperatorType == GDDualOperatorType.MultiplyAndAssign ||
                    dualOp.OperatorType == GDDualOperatorType.DivideAndAssign)
                {
                    if (dualOp.LeftExpression is GDIdentifierExpression leftIdent &&
                        leftIdent.Identifier?.Sequence == _targetVariable)
                    {
                        Found = true;
                    }
                }
            }
        }
    }
}
