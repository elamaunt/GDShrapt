using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Detects cycles in the type inference dependency graph.
/// Uses DFS to find strongly connected components (cycles).
/// </summary>
public class GDInferenceCycleDetector
{
    private readonly GDScriptProject _project;
    private readonly IGDRuntimeProvider? _customRuntimeProvider;

    // Adjacency list: method key -> list of dependencies
    private readonly Dictionary<string, List<GDInferenceDependency>> _adjacencyList = new();

    // All methods in the graph
    private readonly HashSet<string> _methods = new();

    // DFS state
    private readonly Dictionary<string, int> _discoveryTime = new();
    private readonly Dictionary<string, int> _lowLink = new();
    private readonly HashSet<string> _onStack = new();
    private readonly Stack<string> _stack = new();
    private int _time;

    // Results
    private readonly List<List<string>> _cycles = new();
    private readonly HashSet<string> _methodsInCycles = new();

    /// <summary>
    /// All detected cycles (each cycle is a list of method keys).
    /// </summary>
    public IReadOnlyList<List<string>> DetectedCycles => _cycles;

    /// <summary>
    /// All methods that are part of at least one cycle.
    /// </summary>
    public IReadOnlySet<string> MethodsInCycles => _methodsInCycles;

    /// <summary>
    /// Creates a new cycle detector.
    /// </summary>
    public GDInferenceCycleDetector(GDScriptProject project)
    {
        _project = project;
    }

    /// <summary>
    /// Creates a new cycle detector with a custom runtime provider.
    /// </summary>
    public GDInferenceCycleDetector(GDScriptProject project, IGDRuntimeProvider runtimeProvider)
    {
        _project = project;
        _customRuntimeProvider = runtimeProvider;
    }

    /// <summary>
    /// Builds the dependency graph from the project.
    /// </summary>
    public void BuildDependencyGraph()
    {
        _adjacencyList.Clear();
        _methods.Clear();

        var callSiteCollector = new GDCallSiteCollector(_project);
        var runtimeProvider = _customRuntimeProvider ?? _project.CreateRuntimeProvider();
        var projectTypesProvider = (runtimeProvider as GDCompositeRuntimeProvider)?.ProjectTypesProvider;

        if (projectTypesProvider == null)
            return;

        // For each script/type
        foreach (var typeName in projectTypesProvider.GetAllTypes())
        {
            var scriptInfo = projectTypesProvider.GetScriptInfoForType(typeName);
            if (scriptInfo?.Class == null)
                continue;

            // For each method
            foreach (var member in scriptInfo.Class.Members)
            {
                if (member is not GDMethodDeclaration method || method.Identifier == null)
                    continue;

                var methodKey = $"{typeName}.{method.Identifier.Sequence}";
                _methods.Add(methodKey);

                // Analyze method body for call sites
                if (method.Statements != null)
                {
                    var bodyAnalyzer = new MethodBodyAnalyzer(methodKey, typeName, runtimeProvider);
                    method.Statements.WalkIn(bodyAnalyzer);

                    foreach (var dep in bodyAnalyzer.Dependencies)
                    {
                        AddDependency(dep);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Adds a dependency to the graph.
    /// </summary>
    public void AddDependency(GDInferenceDependency dependency)
    {
        _methods.Add(dependency.FromMethod);
        _methods.Add(dependency.ToMethod);

        if (!_adjacencyList.TryGetValue(dependency.FromMethod, out var deps))
        {
            deps = new List<GDInferenceDependency>();
            _adjacencyList[dependency.FromMethod] = deps;
        }

        if (!deps.Contains(dependency))
        {
            deps.Add(dependency);
        }
    }

    /// <summary>
    /// Detects all cycles in the dependency graph using Tarjan's SCC algorithm.
    /// </summary>
    public IEnumerable<List<string>> DetectCycles()
    {
        _cycles.Clear();
        _methodsInCycles.Clear();
        _discoveryTime.Clear();
        _lowLink.Clear();
        _onStack.Clear();
        _stack.Clear();
        _time = 0;

        foreach (var method in _methods)
        {
            if (!_discoveryTime.ContainsKey(method))
            {
                TarjanDFS(method);
            }
        }

        // Mark dependencies that are part of cycles
        foreach (var cycle in _cycles)
        {
            var cycleSet = new HashSet<string>(cycle);
            foreach (var method in cycle)
            {
                _methodsInCycles.Add(method);

                if (_adjacencyList.TryGetValue(method, out var deps))
                {
                    foreach (var dep in deps)
                    {
                        if (cycleSet.Contains(dep.ToMethod))
                        {
                            dep.IsPartOfCycle = true;
                        }
                    }
                }
            }
        }

        return _cycles;
    }

    private void TarjanDFS(string method)
    {
        _discoveryTime[method] = _time;
        _lowLink[method] = _time;
        _time++;
        _stack.Push(method);
        _onStack.Add(method);

        if (_adjacencyList.TryGetValue(method, out var deps))
        {
            foreach (var dep in deps)
            {
                var next = dep.ToMethod;
                if (!_discoveryTime.ContainsKey(next))
                {
                    TarjanDFS(next);
                    _lowLink[method] = Math.Min(_lowLink[method], _lowLink[next]);
                }
                else if (_onStack.Contains(next))
                {
                    _lowLink[method] = Math.Min(_lowLink[method], _discoveryTime[next]);
                }
            }
        }

        // If method is root of an SCC
        if (_lowLink[method] == _discoveryTime[method])
        {
            var scc = new List<string>();
            string popped;
            do
            {
                popped = _stack.Pop();
                _onStack.Remove(popped);
                scc.Add(popped);
            } while (popped != method);

            // Only add cycles with more than one node (self-loops are also cycles)
            if (scc.Count > 1 || HasSelfLoop(method))
            {
                _cycles.Add(scc);
            }
        }
    }

    private bool HasSelfLoop(string method)
    {
        if (_adjacencyList.TryGetValue(method, out var deps))
        {
            return deps.Any(d => d.ToMethod == method);
        }
        return false;
    }

    /// <summary>
    /// Returns the inference order (topological sort with cycle handling).
    /// Methods in cycles are returned at the end with a marker.
    /// </summary>
    public IEnumerable<(string Method, bool InCycle)> GetInferenceOrder()
    {
        // First detect cycles
        DetectCycles();

        var visited = new HashSet<string>();
        var result = new List<(string Method, bool InCycle)>();
        var tempMarked = new HashSet<string>();

        // Process non-cycle methods first using topological sort
        foreach (var method in _methods.Where(m => !_methodsInCycles.Contains(m)))
        {
            if (!visited.Contains(method))
            {
                TopologicalSort(method, visited, tempMarked, result);
            }
        }

        // Add cycle methods at the end
        foreach (var method in _methodsInCycles)
        {
            if (!visited.Contains(method))
            {
                result.Add((method, true));
                visited.Add(method);
            }
        }

        return result;
    }

    private void TopologicalSort(
        string method,
        HashSet<string> visited,
        HashSet<string> tempMarked,
        List<(string Method, bool InCycle)> result)
    {
        if (visited.Contains(method) || _methodsInCycles.Contains(method))
            return;

        if (tempMarked.Contains(method))
            return; // Cycle detected during sort, skip

        tempMarked.Add(method);

        if (_adjacencyList.TryGetValue(method, out var deps))
        {
            foreach (var dep in deps)
            {
                if (!_methodsInCycles.Contains(dep.ToMethod))
                {
                    TopologicalSort(dep.ToMethod, visited, tempMarked, result);
                }
            }
        }

        tempMarked.Remove(method);
        visited.Add(method);
        result.Add((method, false));
    }

    /// <summary>
    /// Gets the dependencies for a method.
    /// </summary>
    public IReadOnlyList<GDInferenceDependency> GetDependencies(string methodKey)
    {
        return _adjacencyList.TryGetValue(methodKey, out var deps) ? deps : Array.Empty<GDInferenceDependency>();
    }

    /// <summary>
    /// Gets all dependencies.
    /// </summary>
    public IEnumerable<GDInferenceDependency> GetAllDependencies()
    {
        return _adjacencyList.Values.SelectMany(d => d);
    }

    /// <summary>
    /// Checks if a method is part of a cycle.
    /// </summary>
    public bool IsInCycle(string methodKey)
    {
        return _methodsInCycles.Contains(methodKey);
    }

    /// <summary>
    /// Gets the cycles that contain a specific method.
    /// </summary>
    public IEnumerable<List<string>> GetCyclesContaining(string methodKey)
    {
        return _cycles.Where(c => c.Contains(methodKey));
    }

    /// <summary>
    /// Analyzes a method body to find call dependencies.
    /// </summary>
    private class MethodBodyAnalyzer : GDVisitor
    {
        private readonly string _currentMethod;
        private readonly string _currentType;
        private readonly IGDRuntimeProvider? _runtimeProvider;
        private readonly List<GDInferenceDependency> _dependencies = new();

        public IReadOnlyList<GDInferenceDependency> Dependencies => _dependencies;

        public MethodBodyAnalyzer(string currentMethod, string currentType, IGDRuntimeProvider? runtimeProvider)
        {
            _currentMethod = currentMethod;
            _currentType = currentType;
            _runtimeProvider = runtimeProvider;
        }

        public override void Visit(GDCallExpression callExpr)
        {
            base.Visit(callExpr);

            var (targetType, targetMethod) = ExtractCallTarget(callExpr);
            if (targetMethod == null)
                return;

            var targetKey = !string.IsNullOrEmpty(targetType)
                ? $"{targetType}.{targetMethod}"
                : targetMethod; // For unresolved types, just use method name

            // Include all calls including self-calls (for self-loop detection)
            _dependencies.Add(GDInferenceDependency.CallSite(_currentMethod, targetKey));
        }

        private (string? Type, string? Method) ExtractCallTarget(GDCallExpression callExpr)
        {
            if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
            {
                var methodName = memberOp.Identifier?.Sequence;
                // Try to infer receiver type from the target expression
                var receiverType = InferReceiverType(memberOp.CallerExpression);
                return (receiverType, methodName);
            }
            else if (callExpr.CallerExpression is GDIdentifierExpression identExpr)
            {
                var methodName = identExpr.Identifier?.Sequence;
                // Direct call - likely self or global
                return (_currentType, methodName);
            }

            return (null, null);
        }

        private string? InferReceiverType(GDExpression? expression)
        {
            if (expression == null)
                return null;

            // Handle identifier expression - lookup variable type
            if (expression is GDIdentifierExpression identExpr)
            {
                var varName = identExpr.Identifier?.Sequence;
                if (string.IsNullOrEmpty(varName))
                    return null;

                // Try to find the type from the project types provider
                if (_runtimeProvider is GDCompositeRuntimeProvider composite)
                {
                    var projectTypes = composite.ProjectTypesProvider;
                    if (projectTypes != null)
                    {
                        // Check if it's a class member variable in current type
                        var memberType = projectTypes.GetMemberType(_currentType, varName);
                        if (!string.IsNullOrEmpty(memberType))
                            return memberType;
                    }
                }

                return null;
            }

            // Handle new expression - e.g., Enemy.new()
            if (expression is GDCallExpression callExpr &&
                callExpr.CallerExpression is GDMemberOperatorExpression memberOp &&
                memberOp.Identifier?.Sequence == "new" &&
                memberOp.CallerExpression is GDIdentifierExpression typeIdent)
            {
                return typeIdent.Identifier?.Sequence;
            }

            return null;
        }
    }
}
