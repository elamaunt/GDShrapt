using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Collects all call sites for a method across the project.
/// Used for parameter type inference from usage patterns.
/// </summary>
internal class GDCallSiteCollector
{
    private readonly GDScriptProject _project;
    private readonly IGDRuntimeProvider? _runtimeProvider;
    private readonly GDProjectTypesProvider? _projectTypesProvider;

    public GDCallSiteCollector(GDScriptProject project)
    {
        _project = project;
        _runtimeProvider = project.CreateRuntimeProvider();

        if (_runtimeProvider is GDCompositeRuntimeProvider composite)
        {
            _projectTypesProvider = composite.ProjectTypesProvider;
        }
    }

    /// <summary>
    /// Collects all call sites for a method across the project.
    /// </summary>
    /// <param name="declaringTypeName">The type where the method is declared (class_name or script path).</param>
    /// <param name="methodName">The method name to find call sites for.</param>
    /// <returns>List of call site information with argument types.</returns>
    public IReadOnlyList<GDCallSiteInfo> CollectCallSites(string declaringTypeName, string methodName)
    {
        var callSites = new List<GDCallSiteInfo>();
        var visited = new HashSet<string>();

        CollectCallSitesInternal(declaringTypeName, methodName, callSites, visited);

        return callSites;
    }

    /// <summary>
    /// Collects call sites for a method, including from Union receiver types.
    /// </summary>
    private void CollectCallSitesInternal(
        string declaringTypeName,
        string methodName,
        List<GDCallSiteInfo> callSites,
        HashSet<string> visited)
    {
        var key = $"{declaringTypeName}.{methodName}";
        if (visited.Contains(key))
            return;
        visited.Add(key);

        foreach (var scriptFile in _project.ScriptFiles)
        {
            var collector = new ScriptCallSiteVisitor(
                scriptFile,
                declaringTypeName,
                methodName,
                _runtimeProvider);

            if (scriptFile.Class != null)
            {
                scriptFile.Class.WalkIn(collector);
            }

            callSites.AddRange(collector.CallSites);
        }
    }

    /// <summary>
    /// Collects call sites from a Union receiver type.
    /// Checks all types in the Union for the method.
    /// </summary>
    /// <param name="unionType">The Union type of the receiver.</param>
    /// <param name="methodName">The method name to find call sites for.</param>
    /// <param name="visited">Set of already visited type.method combinations to prevent cycles.</param>
    /// <returns>List of call site information.</returns>
    public IReadOnlyList<GDCallSiteInfo> CollectCallSitesForUnionReceiver(
        GDUnionType unionType,
        string methodName,
        HashSet<string>? visited = null)
    {
        if (unionType == null || unionType.IsEmpty)
            return new List<GDCallSiteInfo>();

        visited ??= new HashSet<string>();
        var callSites = new List<GDCallSiteInfo>();

        foreach (var typeName in unionType.Types)
        {
            var key = $"{typeName}.{methodName}";
            if (visited.Contains(key))
                continue;
            visited.Add(key);

            if (_runtimeProvider != null)
            {
                var member = _runtimeProvider.GetMember(typeName, methodName);
                if (member == null || member.Kind != GDRuntimeMemberKind.Method)
                    continue;
            }

            CollectCallSitesInternal(typeName, methodName, callSites, visited);
        }

        return callSites;
    }

    /// <summary>
    /// Gets the parameter count for a method.
    /// </summary>
    public int? GetParameterCount(string declaringTypeName, string methodName)
    {
        if (_projectTypesProvider == null)
            return null;

        var methodInfo = _projectTypesProvider.GetMethodInfo(declaringTypeName, methodName);
        return methodInfo?.Parameters.Count;
    }

    /// <summary>
    /// Gets parameter names for a method.
    /// </summary>
    public IReadOnlyList<string>? GetParameterNames(string declaringTypeName, string methodName)
    {
        if (_projectTypesProvider == null)
            return null;

        var methodInfo = _projectTypesProvider.GetMethodInfo(declaringTypeName, methodName);
        return methodInfo?.Parameters.Select(p => p.Name).ToList();
    }

    /// <summary>
    /// Gets parameter info for a method.
    /// </summary>
    public IReadOnlyList<GDProjectParameterInfo>? GetParameterInfos(string declaringTypeName, string methodName)
    {
        if (_projectTypesProvider == null)
            return null;

        var methodInfo = _projectTypesProvider.GetMethodInfo(declaringTypeName, methodName);
        return methodInfo?.Parameters;
    }

    /// <summary>
    /// Visitor that collects call sites within a single script.
    /// </summary>
    private class ScriptCallSiteVisitor : GDVisitor
    {
        private readonly GDScriptFile _scriptFile;
        private readonly string _targetTypeName;
        private readonly string _targetMethodName;
        private readonly IGDRuntimeProvider? _runtimeProvider;
        private readonly GDTypeInferenceEngine? _typeEngine;
        private readonly List<GDCallSiteInfo> _callSites = new();

        public IReadOnlyList<GDCallSiteInfo> CallSites => _callSites;

        public ScriptCallSiteVisitor(
            GDScriptFile scriptFile,
            string targetTypeName,
            string targetMethodName,
            IGDRuntimeProvider? runtimeProvider)
        {
            _scriptFile = scriptFile;
            _targetTypeName = targetTypeName;
            _targetMethodName = targetMethodName;
            _runtimeProvider = runtimeProvider;

            // Create type engine for argument type inference
            if (runtimeProvider != null && scriptFile.Class != null)
            {
                var scopeStack = new GDScopeStack();
                scopeStack.Push(GDScopeType.Global);
                scopeStack.Push(GDScopeType.Class, scriptFile.Class);
                _typeEngine = new GDTypeInferenceEngine(runtimeProvider, scopeStack);
            }
        }

        public override void Visit(GDCallExpression callExpr)
        {
            base.Visit(callExpr);

            // First check for direct method call
            var callSiteInfo = TryCreateCallSiteInfo(callExpr);
            if (callSiteInfo != null)
            {
                _callSites.Add(callSiteInfo);
                return;
            }

            // Then check for dynamic call/callv
            var dynamicCallSiteInfo = TryCreateDynamicCallSiteInfo(callExpr);
            if (dynamicCallSiteInfo != null)
            {
                _callSites.Add(dynamicCallSiteInfo);
            }
        }

        /// <summary>
        /// Tries to create a call site info from a dynamic call/callv expression.
        /// </summary>
        private GDCallSiteInfo? TryCreateDynamicCallSiteInfo(GDCallExpression callExpr)
        {
            if (callExpr.CallerExpression is not GDMemberOperatorExpression memberExpr)
                return null;

            var methodName = memberExpr.Identifier?.Sequence;
            if (methodName != "call" && methodName != "callv")
                return null;

            var args = callExpr.Parameters?.ToList();
            if (args == null || args.Count == 0)
                return null;

            // Try to extract static method name
            var resolver = GDStaticStringExtractor.CreateClassResolver(callExpr.RootClassDeclaration);
            var targetMethodName = GDStaticStringExtractor.TryExtractString(args[0] as GDExpression, resolver);

            if (string.IsNullOrEmpty(targetMethodName) || targetMethodName != _targetMethodName)
                return null;

            // Get receiver type
            var receiverType = _typeEngine?.InferType(memberExpr.CallerExpression);
            var receiverVariableName = GetRootVariableName(memberExpr.CallerExpression);
            var isDuckTyped = string.IsNullOrEmpty(receiverType) || receiverType == "Variant";

            // Check type compatibility for non-duck-typed calls
            if (!isDuckTyped)
            {
                if (receiverType!.Contains("|"))
                {
                    // Union type - check if any matches
                    var types = receiverType.Split('|').Select(t => t.Trim()).ToList();
                    if (!types.Any(t => IsTypeCompatible(t, _targetTypeName)))
                        return null;
                }
                else if (!IsTypeCompatible(receiverType, _targetTypeName))
                {
                    return null;
                }
            }

            // For "call", args[1..] are the method arguments
            // For "callv", args[1] is an Array of arguments (harder to analyze, skip for now)
            if (methodName == "callv")
                return null;

            // Skip first argument (method name string)
            var methodArgs = new List<GDArgumentInfo>();
            for (int i = 1; i < args.Count; i++)
            {
                var expr = args[i] as GDExpression;
                if (expr == null)
                {
                    methodArgs.Add(GDArgumentInfo.Unknown(i - 1));
                }
                else
                {
                    var argType = _typeEngine?.InferType(expr);
                    var isHighConfidence = !string.IsNullOrEmpty(argType) && argType != "Variant";
                    methodArgs.Add(new GDArgumentInfo(i - 1, expr, argType, isHighConfidence));
                }
            }

            // Create call site info with dynamic call marker
            var confidence = isDuckTyped ? GDReferenceConfidence.Potential : GDReferenceConfidence.Strict;

            if (isDuckTyped && receiverVariableName != null)
            {
                var info = GDCallSiteInfo.CreateDuckTyped(
                    callExpr,
                    _scriptFile,
                    methodArgs,
                    receiverVariableName);
                info.IsDynamicCall = true;
                return info;
            }
            else
            {
                var info = new GDCallSiteInfo(
                    callExpr,
                    _scriptFile,
                    methodArgs,
                    receiverType,
                    confidence);
                info.IsDynamicCall = true;
                return info;
            }
        }

        private GDCallSiteInfo? TryCreateCallSiteInfo(GDCallExpression callExpr)
        {
            string? methodName = null;
            GDExpression? receiverExpr = null;

            if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
            {
                // obj.method() form
                methodName = memberOp.Identifier?.Sequence;
                receiverExpr = memberOp.CallerExpression;
            }
            else if (callExpr.CallerExpression is GDIdentifierExpression identExpr)
            {
                // Direct method() call (self or global)
                methodName = identExpr.Identifier?.Sequence;
                receiverExpr = null; // Implicit self
            }

            if (methodName != _targetMethodName)
                return null;

            // Determine receiver type and confidence
            string? receiverType = null;
            string? unionReceiverType = null;
            string? receiverVariableName = null;
            bool isDuckTyped = false;
            var confidence = GDReferenceConfidence.NameMatch;

            if (receiverExpr == null)
            {
                // Direct call - check if this is the declaring type or inherits from it
                var thisTypeName = _scriptFile.TypeName;
                if (IsTypeCompatible(thisTypeName, _targetTypeName))
                {
                    receiverType = thisTypeName;
                    confidence = GDReferenceConfidence.Strict;
                }
                else
                {
                    // Could be a global function with same name - skip
                    return null;
                }
            }
            else
            {
                // Member call - infer receiver type
                receiverType = _typeEngine?.InferType(receiverExpr);
                receiverVariableName = GetRootVariableName(receiverExpr);

                if (!string.IsNullOrEmpty(receiverType) && receiverType != "Variant")
                {
                    if (receiverType.Contains("|"))
                    {
                        unionReceiverType = receiverType;
                        // For Union types, check if ANY type in the union is compatible
                        var types = receiverType.Split('|').Select(t => t.Trim()).ToList();
                        var hasMatch = types.Any(t => IsTypeCompatible(t, _targetTypeName));
                        if (hasMatch)
                        {
                            receiverType = types.FirstOrDefault(t => IsTypeCompatible(t, _targetTypeName)) ?? types[0];
                            confidence = types.All(t => IsTypeCompatible(t, _targetTypeName))
                                ? GDReferenceConfidence.Strict
                                : GDReferenceConfidence.Potential;
                        }
                        else
                        {
                            return null; // No matching type in Union
                        }
                    }
                    else if (IsTypeCompatible(receiverType, _targetTypeName))
                    {
                        confidence = GDReferenceConfidence.Strict;
                    }
                    else
                    {
                        // Type is known but doesn't match - skip
                        return null;
                    }
                }
                else
                {
                    // Duck-typed call (receiver type unknown)
                    isDuckTyped = true;
                    confidence = GDReferenceConfidence.Potential;
                }
            }

            var arguments = CollectArguments(callExpr);

            if (isDuckTyped && receiverVariableName != null)
            {
                return GDCallSiteInfo.CreateDuckTyped(
                    callExpr,
                    _scriptFile,
                    arguments,
                    receiverVariableName);
            }
            else if (!string.IsNullOrEmpty(unionReceiverType))
            {
                return GDCallSiteInfo.CreateWithUnionReceiver(
                    callExpr,
                    _scriptFile,
                    arguments,
                    receiverType!,
                    unionReceiverType,
                    confidence);
            }
            else
            {
                return new GDCallSiteInfo(
                    callExpr,
                    _scriptFile,
                    arguments,
                    receiverType,
                    confidence);
            }
        }

        private IReadOnlyList<GDArgumentInfo> CollectArguments(GDCallExpression callExpr)
        {
            var args = new List<GDArgumentInfo>();

            if (callExpr.Parameters == null)
                return args;

            int index = 0;
            foreach (var param in callExpr.Parameters)
            {
                var expr = param as GDExpression;
                if (expr == null)
                {
                    args.Add(GDArgumentInfo.Unknown(index));
                }
                else
                {
                    var argType = _typeEngine?.InferType(expr);
                    var isHighConfidence = !string.IsNullOrEmpty(argType) && argType != "Variant";
                    args.Add(new GDArgumentInfo(index, expr, argType, isHighConfidence));
                }
                index++;
            }

            return args;
        }

        private bool IsTypeCompatible(string? sourceType, string targetType)
        {
            if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
                return false;

            if (sourceType == targetType)
                return true;

            // Check inheritance
            if (_runtimeProvider != null)
            {
                return _runtimeProvider.IsAssignableTo(sourceType, targetType);
            }

            return false;
        }

        private string? GetRootVariableName(GDExpression? expr)
        {
            while (expr is GDMemberOperatorExpression member)
                expr = member.CallerExpression;
            while (expr is GDIndexerExpression indexer)
                expr = indexer.CallerExpression;

            return (expr as GDIdentifierExpression)?.Identifier?.Sequence;
        }
    }
}
