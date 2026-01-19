using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Analyzes how parameters are used within a method body.
/// Builds constraints that can be resolved to concrete types via duck typing.
/// </summary>
internal class GDParameterUsageAnalyzer : GDVisitor
{
    private readonly Dictionary<string, GDParameterConstraints> _paramConstraints = new();
    private readonly HashSet<string> _parameterNames;
    private readonly IGDRuntimeProvider? _runtimeProvider;

    // Alias tracking: maps local variable name to the parameter it was assigned from
    // e.g., "var current = data" maps "current" → "data"
    private readonly Dictionary<string, string> _aliasToParameter = new();

    // Iterator tracking: maps iterator variable name to the parameter being iterated
    // e.g., "for key in path" maps "key" → "path"
    private readonly Dictionary<string, string> _iteratorToParameter = new();

    // Tracks which parameters use an iterator as a key (for deferred type propagation)
    // e.g., "current[key]" where key is iterator → "data" uses key from "path"
    // Maps iterator variable name → list of parameters that use it as key
    private readonly Dictionary<string, List<string>> _iteratorKeyUsers = new();

    /// <summary>
    /// The collected constraints for each parameter.
    /// </summary>
    public IReadOnlyDictionary<string, GDParameterConstraints> Constraints => _paramConstraints;

    /// <summary>
    /// Creates a new parameter usage analyzer.
    /// </summary>
    /// <param name="parameterNames">Names of parameters to analyze.</param>
    /// <param name="runtimeProvider">Optional runtime provider for type resolution.</param>
    public GDParameterUsageAnalyzer(
        IEnumerable<string> parameterNames,
        IGDRuntimeProvider? runtimeProvider = null)
    {
        _parameterNames = new HashSet<string>(parameterNames);
        _runtimeProvider = runtimeProvider;

        // Initialize constraints for each parameter
        foreach (var name in _parameterNames)
            _paramConstraints[name] = new GDParameterConstraints(name);
    }

    #region Alias Tracking

    /// <summary>
    /// Tracks variable declarations that alias parameters.
    /// E.g., "var current = data" makes current an alias of data.
    /// </summary>
    public override void Visit(GDVariableDeclarationStatement varDecl)
    {
        base.Visit(varDecl);

        var initVar = GetRootVariable(varDecl.Initializer);
        if (initVar == null)
            return;

        // Resolve through existing aliases
        initVar = ResolveAlias(initVar);

        if (_parameterNames.Contains(initVar))
        {
            var localName = varDecl.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(localName))
            {
                _aliasToParameter[localName] = initVar;
            }
        }
    }

    /// <summary>
    /// Resolves a variable name through the alias chain to get the original parameter.
    /// </summary>
    private string ResolveAlias(string varName)
    {
        while (_aliasToParameter.TryGetValue(varName, out var aliased))
            varName = aliased;
        return varName;
    }

    #endregion

    #region Member Access

    /// <summary>
    /// Tracks member access on parameters (property or method calls).
    /// </summary>
    public override void Visit(GDMemberOperatorExpression memberOp)
    {
        base.Visit(memberOp);

        var rootVar = GetRootVariable(memberOp.CallerExpression);
        if (rootVar == null)
            return;

        // Resolve alias to original parameter
        rootVar = ResolveAlias(rootVar);

        if (_parameterNames.Contains(rootVar))
        {
            var memberName = memberOp.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(memberName))
            {
                // Check if this is a method call or property access
                if (memberOp.Parent is GDCallExpression)
                    _paramConstraints[rootVar].AddRequiredMethod(memberName);
                else
                    _paramConstraints[rootVar].AddRequiredProperty(memberName);
            }
        }
    }

    #endregion

    #region Iteration and Indexing

    /// <summary>
    /// Tracks for loop usage - parameter must be iterable.
    /// Also tracks iterator variable for element type inference.
    /// </summary>
    public override void Visit(GDForStatement forStmt)
    {
        base.Visit(forStmt);

        // for x in param → param is iterable
        var collectionVar = GetRootVariable(forStmt.Collection);
        if (collectionVar == null)
            return;

        // Resolve alias
        collectionVar = ResolveAlias(collectionVar);

        if (_parameterNames.Contains(collectionVar))
        {
            _paramConstraints[collectionVar].AddIterableConstraint();

            // Track iterator → parameter mapping for element type inference
            var iteratorName = forStmt.Variable?.Sequence;
            if (!string.IsNullOrEmpty(iteratorName))
            {
                _iteratorToParameter[iteratorName] = collectionVar;
            }
        }
    }

    /// <summary>
    /// Tracks indexer access - parameter must be indexable.
    /// Also collects key types from the index expression.
    /// </summary>
    public override void Visit(GDIndexerExpression indexer)
    {
        base.Visit(indexer);

        var rootVar = GetRootVariable(indexer.CallerExpression);
        if (rootVar == null)
            return;

        // Resolve alias
        rootVar = ResolveAlias(rootVar);

        if (_parameterNames.Contains(rootVar))
        {
            _paramConstraints[rootVar].AddIndexableConstraint();

            // Track key types from the index expression
            TrackKeyType(rootVar, indexer.InnerExpression, indexer);
        }
    }

    /// <summary>
    /// Tracks key/index type from an expression used to access a container parameter.
    /// </summary>
    private void TrackKeyType(string paramName, GDExpression? keyExpr, GDNode? sourceNode = null)
    {
        if (keyExpr == null)
            return;

        // If key is an iterator variable, register this parameter as a key user
        // Element types will be propagated when discovered via type checks
        var keyVar = GetRootVariable(keyExpr);
        if (keyVar != null && _iteratorToParameter.TryGetValue(keyVar, out var iteratorSource))
        {
            // Register this parameter as using iterator as key
            if (!_iteratorKeyUsers.TryGetValue(keyVar, out var users))
            {
                users = new List<string>();
                _iteratorKeyUsers[keyVar] = users;
            }
            if (!users.Contains(paramName))
                users.Add(paramName);

            // Also copy any already-discovered element types from the iterator's source
            foreach (var elemType in _paramConstraints[iteratorSource].ElementTypes)
            {
                _paramConstraints[paramName].AddKeyType(elemType);

                // Add to per-type constraints
                var source = sourceNode != null
                    ? GDTypeInferenceSource.FromIndexer(sourceNode)
                    : null;
                _paramConstraints[paramName].AddKeyTypeForType("Dictionary", elemType, source);
                _paramConstraints[paramName].AddKeyTypeForType("Array", elemType, source);
            }
            return;
        }

        // Infer key type from expression type
        var keyType = InferSimpleType(keyExpr);
        if (!string.IsNullOrEmpty(keyType))
        {
            _paramConstraints[paramName].AddKeyType(keyType);

            // Add to per-type constraints
            var source = sourceNode != null
                ? GDTypeInferenceSource.FromIndexer(sourceNode)
                : null;
            _paramConstraints[paramName].AddKeyTypeForType("Dictionary", keyType, source);
            _paramConstraints[paramName].AddKeyTypeForType("Array", keyType, source);
        }
    }

    /// <summary>
    /// Infers a simple type from an expression (literals, type checks).
    /// </summary>
    private string? InferSimpleType(GDExpression? expr)
    {
        return expr switch
        {
            GDNumberExpression num => InferNumberType(num),
            GDStringExpression => "String",
            GDBoolExpression => "bool",
            GDIdentifierExpression ident => CheckIteratorType(ident.Identifier?.Sequence),
            _ => null
        };
    }

    /// <summary>
    /// Infers type from a number expression.
    /// </summary>
    private static string InferNumberType(GDNumberExpression num)
    {
        if (num.Number == null)
            return "int";

        return num.Number.ResolveNumberType() switch
        {
            GDNumberType.LongDecimal or GDNumberType.LongBinary or GDNumberType.LongHexadecimal => "int",
            GDNumberType.Double => "float",
            _ => "int"
        };
    }

    /// <summary>
    /// If the identifier is an iterator, return its tracked element types.
    /// </summary>
    private string? CheckIteratorType(string? varName)
    {
        if (string.IsNullOrEmpty(varName))
            return null;

        // If this is an iterator, we don't know its type yet - it will be inferred later
        // Return null so we don't add incorrect types
        if (_iteratorToParameter.ContainsKey(varName))
            return null;

        return null;
    }

    #endregion

    #region Call Sites

    /// <summary>
    /// Tracks when parameters are passed to other methods.
    /// Also tracks .get(key) calls on aliased parameters to collect key types.
    /// </summary>
    public override void Visit(GDCallExpression call)
    {
        base.Visit(call);

        // Track .get(key) calls on aliased parameters
        if (call.CallerExpression is GDMemberOperatorExpression memberOp)
        {
            var methodName = memberOp.Identifier?.Sequence;
            var objVar = GetRootVariable(memberOp.CallerExpression);

            if (objVar != null && methodName == "get")
            {
                // Resolve alias to original parameter
                var paramName = ResolveAlias(objVar);

                if (_parameterNames.Contains(paramName))
                {
                    // current.get(key) → data (via alias) has KeyType = typeof(key)
                    var args = call.Parameters?.ToList();
                    if (args != null && args.Count >= 1)
                    {
                        TrackKeyType(paramName, args[0], call);
                    }
                }
            }
        }

        // Track when parameter is passed to another method
        var callArgs = call.Parameters?.ToList() ?? new List<GDExpression>();
        for (int i = 0; i < callArgs.Count; i++)
        {
            var argVar = GetRootVariable(callArgs[i]);
            if (argVar == null)
                continue;

            // Resolve alias
            argVar = ResolveAlias(argVar);

            if (_parameterNames.Contains(argVar))
            {
                _paramConstraints[argVar].AddPassedToCall(call, i);
            }
        }
    }

    #endregion

    #region Type Checks

    /// <summary>
    /// Tracks 'is' type checks to narrow possible types.
    /// Also tracks element types when type checks are on iterator variables.
    /// </summary>
    public override void Visit(GDDualOperatorExpression dualOp)
    {
        base.Visit(dualOp);

        var op = dualOp.Operator?.OperatorType;
        if (op != GDDualOperatorType.Is)
            return;

        var leftVar = GetRootVariable(dualOp.LeftExpression);
        if (leftVar == null)
            return;

        // Get the type being checked
        var typeName = GetTypeFromExpression(dualOp.RightExpression);
        if (string.IsNullOrEmpty(typeName))
            return;

        // Case 1: Type check on iterator → element type of the container parameter
        if (_iteratorToParameter.TryGetValue(leftVar, out var containerParam))
        {
            // "key is int" where "for key in path" → path has element type int
            _paramConstraints[containerParam].AddElementType(typeName);

            // Also add to per-type constraints for Array (iterators come from Array/String/etc)
            var source = GDTypeInferenceSource.FromTypeCheck(dualOp, typeName);
            _paramConstraints[containerParam].AddElementTypeForType("Array", typeName, source);

            // Also propagate to any parameter that uses this iterator as key
            PropagateIteratorTypeToKeyUsers(leftVar, typeName, dualOp);
            return;
        }

        // Case 2: Resolve alias to original parameter
        var paramName = ResolveAlias(leftVar);
        if (!_parameterNames.Contains(paramName))
            return;

        // Check if this is a negative check (not param is Type)
        var isNegative = IsNegativeCheck(dualOp);

        if (isNegative)
        {
            _paramConstraints[paramName].ExcludeType(typeName);
        }
        else
        {
            // Add possible type with source for navigation
            var source = GDTypeInferenceSource.FromTypeCheck(dualOp, typeName);
            _paramConstraints[paramName].AddPossibleTypeWithSource(typeName, source);

            // For Dictionary and Array, mark value as derivable if not already known
            if (typeName == "Dictionary" || typeName == "Array")
            {
                _paramConstraints[paramName].MarkValueDerivable(
                    typeName,
                    dualOp,
                    "value type can be inferred from return statements or further usage");
            }
        }
    }

    /// <summary>
    /// When an iterator's type is discovered, propagate it as key type to any
    /// parameter that uses this iterator for indexing or .get() calls.
    /// </summary>
    private void PropagateIteratorTypeToKeyUsers(string iteratorVar, string typeName, GDNode? sourceNode = null)
    {
        // Find all parameters that use this iterator as a key
        if (_iteratorKeyUsers.TryGetValue(iteratorVar, out var users))
        {
            var source = sourceNode != null
                ? GDTypeInferenceSource.FromTypeCheck(sourceNode, typeName)
                : null;

            foreach (var paramName in users)
            {
                _paramConstraints[paramName].AddKeyType(typeName);

                // Add to per-type constraints for Dictionary (since .get() and [] use keys)
                _paramConstraints[paramName].AddKeyTypeForType("Dictionary", typeName, source);
                // Array also uses keys for indexing
                _paramConstraints[paramName].AddKeyTypeForType("Array", typeName, source);
            }
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the root variable name from an expression chain.
    /// E.g., player.weapon.damage → "player"
    /// </summary>
    private string? GetRootVariable(GDExpression? expr)
    {
        while (expr is GDMemberOperatorExpression member)
            expr = member.CallerExpression;
        while (expr is GDIndexerExpression indexer)
            expr = indexer.CallerExpression;

        return (expr as GDIdentifierExpression)?.Identifier?.Sequence;
    }

    /// <summary>
    /// Gets the type name from a type expression (used in 'is' checks).
    /// </summary>
    private string? GetTypeFromExpression(GDExpression? expr)
    {
        if (expr is GDIdentifierExpression identExpr)
            return identExpr.Identifier?.Sequence;

        // Handle dotted type names like Node2D.SomeClass
        if (expr is GDMemberOperatorExpression memberOp)
        {
            var parts = new List<string>();
            GDExpression? current = expr;
            while (current is GDMemberOperatorExpression mo)
            {
                var memberName = mo.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(memberName))
                    parts.Insert(0, memberName);
                current = mo.CallerExpression;
            }
            if (current is GDIdentifierExpression rootIdent)
            {
                var rootName = rootIdent.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(rootName))
                    parts.Insert(0, rootName);
            }
            return parts.Count > 0 ? string.Join(".", parts) : null;
        }

        return null;
    }

    /// <summary>
    /// Checks if an 'is' expression is inside a negation.
    /// </summary>
    private bool IsNegativeCheck(GDDualOperatorExpression dualOp)
    {
        // Check if parent is a 'not' operator
        var parent = dualOp.Parent;
        if (parent is GDSingleOperatorExpression singleOp)
        {
            var opType = singleOp.OperatorType;
            return opType == GDSingleOperatorType.Not || opType == GDSingleOperatorType.Not2;
        }

        // Check if used in "if not x is Type" pattern
        // This is harder to detect, skip for now
        return false;
    }

    #endregion

    #region Static Factory

    /// <summary>
    /// Analyzes a method and returns constraints for all its parameters.
    /// </summary>
    public static IReadOnlyDictionary<string, GDParameterConstraints> AnalyzeMethod(
        GDMethodDeclaration method,
        IGDRuntimeProvider? runtimeProvider = null)
    {
        var paramNames = method.Parameters?
            .Select(p => p.Identifier?.Sequence)
            .Where(n => !string.IsNullOrEmpty(n))
            .Cast<string>()
            .ToList() ?? new List<string>();

        if (paramNames.Count == 0)
            return new Dictionary<string, GDParameterConstraints>();

        var analyzer = new GDParameterUsageAnalyzer(paramNames, runtimeProvider);
        method.Statements?.WalkIn(analyzer);
        return analyzer.Constraints;
    }

    /// <summary>
    /// Analyzes a lambda and returns constraints for all its parameters.
    /// </summary>
    public static IReadOnlyDictionary<string, GDParameterConstraints> AnalyzeLambda(
        GDMethodExpression lambda,
        IGDRuntimeProvider? runtimeProvider = null)
    {
        var paramNames = lambda.Parameters?
            .Select(p => p.Identifier?.Sequence)
            .Where(n => !string.IsNullOrEmpty(n))
            .Cast<string>()
            .ToList() ?? new List<string>();

        if (paramNames.Count == 0)
            return new Dictionary<string, GDParameterConstraints>();

        var analyzer = new GDParameterUsageAnalyzer(paramNames, runtimeProvider);
        lambda.Expression?.WalkIn(analyzer);
        lambda.Statements?.WalkIn(analyzer);
        return analyzer.Constraints;
    }

    #endregion
}
