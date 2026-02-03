using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Tracks Callable assignments to variables for resolving call sites to definitions.
/// Handles local variables, class variables, and aliases.
/// </summary>
public class GDCallableTracker
{
    private readonly Dictionary<string, List<GDCallableDefinition>> _localVariables = new();
    private readonly Dictionary<string, List<GDCallableDefinition>> _classVariables = new();
    private readonly Dictionary<string, string> _aliases = new();
    private readonly GDScriptFile? _sourceFile;
    private readonly System.Func<GDExpression, string?>? _typeInferrer;

    public GDCallableTracker(GDScriptFile? sourceFile = null, System.Func<GDExpression, string?>? typeInferrer = null)
    {
        _sourceFile = sourceFile;
        _typeInferrer = typeInferrer;
    }

    /// <summary>
    /// Tracks a lambda assignment to a local variable.
    /// </summary>
    public void TrackLambdaAssignment(string variableName, GDMethodExpression lambda)
    {
        if (string.IsNullOrEmpty(variableName))
            return;

        var definition = GDCallableDefinition.FromLambda(lambda, _sourceFile);

        if (!_localVariables.TryGetValue(variableName, out var definitions))
        {
            definitions = new List<GDCallableDefinition>();
            _localVariables[variableName] = definitions;
        }

        definitions.Add(definition);
    }

    /// <summary>
    /// Tracks a lambda assignment to a class variable.
    /// </summary>
    public void TrackClassVariableAssignment(string variableName, GDMethodExpression lambda)
    {
        if (string.IsNullOrEmpty(variableName))
            return;

        var definition = GDCallableDefinition.FromLambda(lambda, _sourceFile);

        if (!_classVariables.TryGetValue(variableName, out var definitions))
        {
            definitions = new List<GDCallableDefinition>();
            _classVariables[variableName] = definitions;
        }

        definitions.Add(definition);
    }

    /// <summary>
    /// Tracks an alias assignment (var alias = original).
    /// </summary>
    public void TrackAliasAssignment(string targetVariable, string sourceVariable)
    {
        if (string.IsNullOrEmpty(targetVariable) || string.IsNullOrEmpty(sourceVariable))
            return;

        _aliases[targetVariable] = sourceVariable;
    }

    /// <summary>
    /// Resolves a variable name to its Callable definitions.
    /// Follows aliases if needed.
    /// </summary>
    public IReadOnlyList<GDCallableDefinition> ResolveVariable(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return System.Array.Empty<GDCallableDefinition>();

        // Follow aliases
        var resolvedName = ResolveAlias(variableName);

        // Check local variables first
        if (_localVariables.TryGetValue(resolvedName, out var localDefs))
            return localDefs;

        // Check class variables
        if (_classVariables.TryGetValue(resolvedName, out var classDefs))
            return classDefs;

        return System.Array.Empty<GDCallableDefinition>();
    }

    /// <summary>
    /// Resolves an expression to its Callable definitions.
    /// </summary>
    public IReadOnlyList<GDCallableDefinition> ResolveExpression(GDExpression expression)
    {
        if (expression == null)
            return System.Array.Empty<GDCallableDefinition>();

        // Direct lambda
        if (expression is GDMethodExpression lambda)
        {
            return new[] { GDCallableDefinition.FromLambda(lambda, _sourceFile) };
        }

        // Identifier reference
        if (expression is GDIdentifierExpression identExpr)
        {
            var name = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(name))
                return ResolveVariable(name);
        }

        // Member access (self._callback, obj.callback)
        if (expression is GDMemberOperatorExpression memberOp)
        {
            var memberName = memberOp.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(memberName))
            {
                // Check if caller is "self"
                if (memberOp.CallerExpression is GDIdentifierExpression callerIdent &&
                    callerIdent.Identifier?.Sequence == "self")
                {
                    return ResolveVariable(memberName);
                }

                // For other callers, try class variable lookup
                if (_classVariables.TryGetValue(memberName, out var defs))
                    return defs;
            }
        }

        // Indexer (array[0])
        if (expression is GDIndexerExpression indexer)
        {
            // For indexed access into Callable arrays, we can't resolve to specific definition
            // but we can note that all lambdas in that array might be called
            return System.Array.Empty<GDCallableDefinition>();
        }

        return System.Array.Empty<GDCallableDefinition>();
    }

    /// <summary>
    /// Follows alias chain to find the original variable.
    /// </summary>
    private string ResolveAlias(string variableName)
    {
        var visited = new HashSet<string>();
        var current = variableName;

        while (_aliases.TryGetValue(current, out var source) && !visited.Contains(source))
        {
            visited.Add(current);
            current = source;
        }

        return current;
    }

    /// <summary>
    /// Gets all tracked local variable names.
    /// </summary>
    public IEnumerable<string> LocalVariableNames => _localVariables.Keys;

    /// <summary>
    /// Gets all tracked class variable names.
    /// </summary>
    public IEnumerable<string> ClassVariableNames => _classVariables.Keys;

    /// <summary>
    /// Gets all tracked definitions.
    /// </summary>
    public IEnumerable<GDCallableDefinition> AllDefinitions =>
        _localVariables.Values.SelectMany(d => d)
            .Concat(_classVariables.Values.SelectMany(d => d));

    /// <summary>
    /// Clears all tracked assignments.
    /// </summary>
    public void Clear()
    {
        _localVariables.Clear();
        _classVariables.Clear();
        _aliases.Clear();
    }

    /// <summary>
    /// Processes an assignment expression and tracks any Callable assignments.
    /// </summary>
    public void ProcessAssignment(GDExpression? target, GDExpression? value, bool isClassVariable = false)
    {
        if (target == null || value == null)
            return;

        // Get target variable name
        string? variableName = null;

        if (target is GDIdentifierExpression identTarget)
        {
            variableName = identTarget.Identifier?.Sequence;
        }
        else if (target is GDMemberOperatorExpression memberTarget)
        {
            // self._callback = ...
            if (memberTarget.CallerExpression is GDIdentifierExpression callerIdent &&
                callerIdent.Identifier?.Sequence == "self")
            {
                variableName = memberTarget.Identifier?.Sequence;
                isClassVariable = true;
            }
        }

        if (string.IsNullOrEmpty(variableName))
            return;

        ProcessAssignmentByName(variableName, value, isClassVariable);
    }

    /// <summary>
    /// Processes an assignment to a variable by name.
    /// </summary>
    public void ProcessAssignmentByName(string variableName, GDExpression? value, bool isClassVariable = false)
    {
        if (string.IsNullOrEmpty(variableName) || value == null)
            return;

        // Track lambda assignment
        if (value is GDMethodExpression lambda)
        {
            if (isClassVariable)
                TrackClassVariableAssignment(variableName, lambda);
            else
                TrackLambdaAssignment(variableName, lambda);
        }
        // Track alias
        else if (value is GDIdentifierExpression sourceIdent)
        {
            var sourceName = sourceIdent.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(sourceName))
            {
                TrackAliasAssignment(variableName, sourceName);
            }
        }
    }
}
