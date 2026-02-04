using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Collects inter-procedural Callable flow information:
/// 1. Method profiles - what call sites exist on Callable parameters
/// 2. Argument bindings - which lambdas are passed to which method parameters
/// </summary>
internal class GDCallableFlowCollector : GDVisitor
{
    private readonly GDScriptFile? _sourceFile;
    private readonly Func<GDExpression, string?>? _typeInferrer;
    private readonly Func<string, GDMethodDeclaration?>? _methodResolver;

    private readonly List<GDMethodCallableProfile> _methodProfiles = new();
    private readonly List<GDCallableArgumentBinding> _argumentBindings = new();

    // Current method context
    private GDMethodDeclaration? _currentMethod;
    private GDMethodCallableProfile? _currentProfile;
    private string? _currentClassName;

    // Callable parameters in current method
    private readonly HashSet<string> _callableParams = new();

    public GDCallableFlowCollector(
        GDScriptFile? sourceFile = null,
        Func<GDExpression, string?>? typeInferrer = null,
        Func<string, GDMethodDeclaration?>? methodResolver = null)
    {
        _sourceFile = sourceFile;
        _typeInferrer = typeInferrer;
        _methodResolver = methodResolver;
    }

    /// <summary>
    /// All collected method profiles.
    /// </summary>
    public IReadOnlyList<GDMethodCallableProfile> MethodProfiles => _methodProfiles;

    /// <summary>
    /// All collected argument bindings.
    /// </summary>
    public IReadOnlyList<GDCallableArgumentBinding> ArgumentBindings => _argumentBindings;

    /// <summary>
    /// Collects flow information from a class declaration.
    /// </summary>
    public void Collect(GDClassDeclaration classDecl)
    {
        if (classDecl == null)
            return;

        // Try to get class name
        _currentClassName = classDecl.ClassName?.Identifier?.Sequence;

        classDecl.WalkIn(this);
    }

    public override void Visit(GDMethodDeclaration methodDecl)
    {
        // Start new method context
        _currentMethod = methodDecl;
        _callableParams.Clear();

        var methodName = methodDecl.Identifier?.Sequence;
        if (string.IsNullOrEmpty(methodName))
            return;

        var methodKey = GDMethodCallableProfile.CreateMethodKey(_currentClassName, methodName);
        _currentProfile = new GDMethodCallableProfile(methodKey, _currentClassName, methodName, _sourceFile);

        // Find Callable parameters
        if (methodDecl.Parameters != null)
        {
            int index = 0;
            foreach (var param in methodDecl.Parameters)
            {
                var paramType = param.Type?.BuildName();
                var paramName = param.Identifier?.Sequence;

                if (!string.IsNullOrEmpty(paramName) &&
                    (paramType == "Callable" || paramType == null))
                {
                    // Track as potential Callable parameter
                    // Type == null means untyped, could be Callable
                    if (paramType == "Callable")
                    {
                        _callableParams.Add(paramName);
                        _currentProfile.RegisterCallableParameter(paramName, index);
                    }
                }
                index++;
            }
        }

        // Only add profile if it has Callable parameters
        if (_currentProfile.CallableParameterIndices.Count > 0)
        {
            _methodProfiles.Add(_currentProfile);
        }

        // Continue walking into method body
        // Note: Don't reset here - nested lambdas handled separately
    }

    public override void Visit(GDExpressionStatement exprStmt)
    {
        // Track: self._callback = callback (parameter to class var assignment)
        if (exprStmt.Expression is GDDualOperatorExpression dualOp &&
            dualOp.OperatorType == GDDualOperatorType.Assignment)
        {
            TrackParameterToClassVarAssignment(dualOp.LeftExpression, dualOp.RightExpression);
        }
    }

    public override void Visit(GDCallExpression callExpr)
    {
        // 1. Check for .call() on Callable parameter
        if (IsCallOnCallableParameter(callExpr, out var paramName))
        {
            var callSite = GDCallableCallSiteInfo.TryCreate(callExpr, _sourceFile, _typeInferrer);
            if (callSite != null && _currentProfile != null)
            {
                _currentProfile.AddParameterCallSite(paramName, callSite);
            }
        }

        // 2. Check for method call with lambda argument
        CheckForLambdaArgument(callExpr);
    }

    /// <summary>
    /// Checks if a call expression is a .call() on a Callable parameter.
    /// </summary>
    private bool IsCallOnCallableParameter(GDCallExpression callExpr, out string paramName)
    {
        paramName = null;

        if (callExpr.CallerExpression is not GDMemberOperatorExpression memberOp)
            return false;

        var methodName = memberOp.Identifier?.Sequence;
        if (methodName != "call" && methodName != "callv")
            return false;

        // Check if caller is a Callable parameter
        if (memberOp.CallerExpression is GDIdentifierExpression identExpr)
        {
            var name = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(name) && _callableParams.Contains(name))
            {
                paramName = name;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a call expression passes a lambda as argument.
    /// </summary>
    private void CheckForLambdaArgument(GDCallExpression callExpr)
    {
        if (callExpr.Parameters == null)
            return;

        // Get called method name
        string? calledMethodName = null;
        string? calledClassName = null;

        if (callExpr.CallerExpression is GDIdentifierExpression identExpr)
        {
            calledMethodName = identExpr.Identifier?.Sequence;
        }
        else if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
        {
            calledMethodName = memberOp.Identifier?.Sequence;
            // Could extract class from caller, but for now assume same class
        }

        if (string.IsNullOrEmpty(calledMethodName))
            return;

        // Skip .call()/.callv() - these are handled separately
        if (calledMethodName == "call" || calledMethodName == "callv")
            return;

        // Check each argument for lambda
        int argIndex = 0;
        foreach (var arg in callExpr.Parameters)
        {
            if (arg is GDMethodExpression lambda)
            {
                // Found lambda passed as argument
                var binding = CreateBinding(lambda, calledMethodName, calledClassName, argIndex, callExpr);
                if (binding != null)
                {
                    _argumentBindings.Add(binding);
                }
            }
            argIndex++;
        }
    }

    /// <summary>
    /// Creates a binding for a lambda passed to a method.
    /// </summary>
    private GDCallableArgumentBinding? CreateBinding(
        GDMethodExpression lambda,
        string methodName,
        string? className,
        int argIndex,
        GDCallExpression callExpr)
    {
        var lambdaDef = GDCallableDefinition.FromLambda(lambda, _sourceFile);
        var methodKey = GDMethodCallableProfile.CreateMethodKey(className ?? _currentClassName, methodName);

        // Try to get parameter name from method declaration
        string paramName = $"arg{argIndex}"; // Default
        var targetMethod = _methodResolver?.Invoke(methodName);
        if (targetMethod?.Parameters != null)
        {
            int idx = 0;
            foreach (var param in targetMethod.Parameters)
            {
                if (idx == argIndex)
                {
                    paramName = param.Identifier?.Sequence ?? paramName;
                    break;
                }
                idx++;
            }
        }

        var firstToken = callExpr.AllTokens.FirstOrDefault();
        var line = firstToken?.StartLine ?? 0;
        var column = firstToken?.StartColumn ?? 0;

        return new GDCallableArgumentBinding(
            lambdaDef,
            methodKey,
            argIndex,
            paramName,
            callExpr,
            _sourceFile,
            line,
            column);
    }

    /// <summary>
    /// Tracks assignment of a Callable parameter to a class variable.
    /// </summary>
    private void TrackParameterToClassVarAssignment(GDExpression? left, GDExpression? right)
    {
        if (left == null || right == null || _currentProfile == null)
            return;

        // Check if right side is a Callable parameter
        string? paramName = null;
        if (right is GDIdentifierExpression rightIdent)
        {
            var name = rightIdent.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(name) && _callableParams.Contains(name))
            {
                paramName = name;
            }
        }

        if (paramName == null)
            return;

        // Check if left side is self._var or just _var (class variable)
        string? classVarName = null;
        if (left is GDMemberOperatorExpression memberOp)
        {
            if (memberOp.CallerExpression is GDIdentifierExpression callerIdent &&
                callerIdent.Identifier?.Sequence == "self")
            {
                classVarName = memberOp.Identifier?.Sequence;
            }
        }
        else if (left is GDIdentifierExpression leftIdent)
        {
            var name = leftIdent.Identifier?.Sequence;
            // Heuristic: class variables often start with _
            if (!string.IsNullOrEmpty(name) && name.StartsWith("_"))
            {
                classVarName = name;
            }
        }

        if (!string.IsNullOrEmpty(classVarName))
        {
            _currentProfile.AddParameterToClassVarAssignment(paramName, classVarName);
        }
    }

    /// <summary>
    /// Gets a method profile by key.
    /// </summary>
    public GDMethodCallableProfile? GetProfileByKey(string methodKey)
    {
        return _methodProfiles.FirstOrDefault(p => p.MethodKey == methodKey);
    }

    /// <summary>
    /// Gets bindings for a specific lambda.
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
    /// Gets bindings for a specific method parameter.
    /// </summary>
    public IReadOnlyList<GDCallableArgumentBinding> GetBindingsForMethodParameter(string methodKey, int paramIndex)
    {
        return _argumentBindings
            .Where(b => b.TargetMethodKey == methodKey && b.TargetParameterIndex == paramIndex)
            .ToList();
    }

    /// <summary>
    /// Clears all collected data.
    /// </summary>
    public void Clear()
    {
        _methodProfiles.Clear();
        _argumentBindings.Clear();
        _currentMethod = null;
        _currentProfile = null;
        _callableParams.Clear();
    }
}
