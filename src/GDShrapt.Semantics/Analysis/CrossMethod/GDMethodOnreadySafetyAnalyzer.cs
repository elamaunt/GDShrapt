using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Semantics
{
    /// <summary>
    /// Computes @onready safety for methods using fixed-point iteration.
    /// A method is safe if:
    /// 1. It's a lifecycle method (_ready, _process, _input, _draw, etc.), OR
    /// 2. ALL of its callers are safe methods
    /// </summary>
    internal class GDMethodOnreadySafetyAnalyzer
    {
        private readonly GDSemanticModel _semanticModel;
        private readonly GDMethodFlowSummaryRegistry _registry;
        private readonly IReadOnlyList<GDSignalConnectionEntry>? _signalConnections;
        private readonly Dictionary<string, HashSet<string>> _callerGraph = new();

        private const int MaxIterations = 100;

        public GDMethodOnreadySafetyAnalyzer(
            GDSemanticModel semanticModel,
            GDMethodFlowSummaryRegistry registry,
            IReadOnlyList<GDSignalConnectionEntry>? signalConnections = null)
        {
            _semanticModel = semanticModel;
            _registry = registry;
            _signalConnections = signalConnections;
        }

        /// <summary>
        /// Analyzes all methods and computes their @onready safety levels.
        /// </summary>
        public Dictionary<string, GDMethodOnreadySafety> Analyze()
        {
            var safety = new Dictionary<string, GDMethodOnreadySafety>();

            BuildCallerGraph();

            foreach (var summary in _registry.GetAllSummaries())
            {
                safety[summary.MethodName] = summary.OnreadySafety;
            }

            bool changed = true;
            int iterations = 0;

            while (changed && iterations++ < MaxIterations)
            {
                changed = false;

                foreach (var summary in _registry.GetAllSummaries())
                {
                    var methodName = summary.MethodName;

                    // Skip already determined methods
                    if (safety[methodName] != GDMethodOnreadySafety.Unknown)
                        continue;

                    // Get callers, excluding self-calls (recursion)
                    var callers = GetCallers(methodName).Where(c => c != methodName).ToList();

                    if (callers.Count == 0)
                    {
                        // Setters with no callers are Unsafe — engine can call them before _ready()
                        // (e.g., @export property initialization)
                        if (methodName.StartsWith("@") && methodName.EndsWith(".set"))
                        {
                            safety[methodName] = GDMethodOnreadySafety.Unsafe;
                        }
                        else
                        {
                            // No in-file callers → likely called externally (from parent/sibling scripts)
                            safety[methodName] = GDMethodOnreadySafety.External;
                        }
                        changed = true;
                    }
                    else if (callers.All(c =>
                    {
                        var s = GetSafety(c, safety);
                        return s == GDMethodOnreadySafety.Safe;
                    }))
                    {
                        // All callers are safe → method is safe
                        safety[methodName] = GDMethodOnreadySafety.Safe;
                        changed = true;
                    }
                    else if (callers.Any(c => GetSafety(c, safety) == GDMethodOnreadySafety.Unsafe))
                    {
                        // At least one unsafe caller → method is unsafe
                        safety[methodName] = GDMethodOnreadySafety.Unsafe;
                        changed = true;
                    }
                    else if (callers.All(c =>
                    {
                        var s = GetSafety(c, safety);
                        return s == GDMethodOnreadySafety.Safe || s == GDMethodOnreadySafety.External;
                    }))
                    {
                        // All callers are safe or external → method is external (lower severity)
                        safety[methodName] = GDMethodOnreadySafety.External;
                        changed = true;
                    }
                    // Otherwise, keep Unknown and wait for callers to be determined
                }
            }

            foreach (var methodName in safety.Keys.ToList())
            {
                if (safety[methodName] == GDMethodOnreadySafety.Unknown)
                {
                    safety[methodName] = GDMethodOnreadySafety.External;
                }
            }

            return safety;
        }

        private void BuildCallerGraph()
        {
            _callerGraph.Clear();

            // Initialize all methods
            foreach (var summary in _registry.GetAllSummaries())
            {
                if (!_callerGraph.ContainsKey(summary.MethodName))
                    _callerGraph[summary.MethodName] = new HashSet<string>();
            }

            // Build reverse graph: for each method, find who calls it
            foreach (var summary in _registry.GetAllSummaries())
            {
                var callerName = summary.MethodName;

                foreach (var calledMethod in summary.CalledMethods)
                {
                    if (!_callerGraph.ContainsKey(calledMethod))
                        _callerGraph[calledMethod] = new HashSet<string>();

                    _callerGraph[calledMethod].Add(callerName);
                }
            }

            // Add property assignment → setter edges
            AddPropertySetterEdges();

            // Add signal connections as caller edges:
            // signal.connect(callback) in method X means callback is "called from" X
            if (_signalConnections != null)
            {
                var selfTypeName = _semanticModel.ScriptFile?.TypeName;

                foreach (var conn in _signalConnections)
                {
                    if (string.IsNullOrEmpty(conn.CallbackMethodName))
                        continue;

                    // Only self-connections (callback is in this class) or scene connections targeting this class
                    if (conn.CallbackClassName != null && conn.CallbackClassName != selfTypeName)
                        continue;

                    if (!_callerGraph.ContainsKey(conn.CallbackMethodName))
                        _callerGraph[conn.CallbackMethodName] = new HashSet<string>();

                    if (conn.IsSceneConnection)
                    {
                        // Scene connections always fire after _ready(), so add _ready as virtual caller
                        _callerGraph[conn.CallbackMethodName].Add(GDSpecialMethodHelper.Ready);
                    }
                    else if (!string.IsNullOrEmpty(conn.SourceMethodName))
                    {
                        _callerGraph[conn.CallbackMethodName].Add(conn.SourceMethodName);
                    }
                }
            }
        }

        private void AddPropertySetterEdges()
        {
            var classDecl = _semanticModel.ScriptFile?.Class;
            if (classDecl == null)
                return;

            foreach (var member in classDecl.Members)
            {
                if (member is not GDVariableDeclaration varDecl)
                    continue;

                var propName = varDecl.Identifier?.Sequence;
                if (string.IsNullOrEmpty(propName))
                    continue;

                // Handle "set = method_name" delegation
                var delegatedSetter =
                    varDecl.FirstAccessorDeclarationNode as GDSetAccessorMethodDeclaration ??
                    varDecl.SecondAccessorDeclarationNode as GDSetAccessorMethodDeclaration;

                if (delegatedSetter != null)
                {
                    var targetMethod = delegatedSetter.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(targetMethod))
                    {
                        if (!_callerGraph.ContainsKey(targetMethod))
                            _callerGraph[targetMethod] = new HashSet<string>();

                        foreach (var summary in _registry.GetAllSummaries())
                        {
                            if (summary.AssignedProperties.Contains(propName))
                                _callerGraph[targetMethod].Add(summary.MethodName);
                        }
                    }
                    continue;
                }

                // Handle inline setter — registered as "@prop.set"
                var syntheticName = $"@{propName}.set";
                if (!_callerGraph.ContainsKey(syntheticName))
                    continue;

                foreach (var summary in _registry.GetAllSummaries())
                {
                    if (summary.AssignedProperties.Contains(propName))
                        _callerGraph[syntheticName].Add(summary.MethodName);
                }
            }
        }

        private GDMethodOnreadySafety GetSafety(string methodName, Dictionary<string, GDMethodOnreadySafety> safety)
        {
            if (safety.TryGetValue(methodName, out var s))
                return s;

            // Lifecycle methods after _ready are implicitly Safe even when not defined in this script
            if (GDSpecialMethodHelper.IsLifecycleMethodAfterReady(methodName))
                return GDMethodOnreadySafety.Safe;

            return GDMethodOnreadySafety.Unknown;
        }

        private IEnumerable<string> GetCallers(string methodName)
        {
            if (_callerGraph.TryGetValue(methodName, out var callers))
                return callers;
            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Gets the caller graph (method -> callers).
        /// </summary>
        public Dictionary<string, HashSet<string>> GetCallerGraph()
        {
            if (_callerGraph.Count == 0)
                BuildCallerGraph();
            return _callerGraph;
        }

        /// <summary>
        /// Gets the call graph (method -> callees).
        /// </summary>
        public Dictionary<string, HashSet<string>> GetCallGraph()
        {
            var callGraph = new Dictionary<string, HashSet<string>>();

            foreach (var summary in _registry.GetAllSummaries())
            {
                callGraph[summary.MethodName] = new HashSet<string>(summary.CalledMethods);
            }

            return callGraph;
        }
    }
}
