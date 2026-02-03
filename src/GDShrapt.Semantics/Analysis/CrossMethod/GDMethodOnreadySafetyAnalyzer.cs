using GDShrapt.Reader;

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
        private readonly Dictionary<string, HashSet<string>> _callerGraph = new();

        private const int MaxIterations = 100;

        public GDMethodOnreadySafetyAnalyzer(GDSemanticModel semanticModel, GDMethodFlowSummaryRegistry registry)
        {
            _semanticModel = semanticModel;
            _registry = registry;
        }

        /// <summary>
        /// Analyzes all methods and computes their @onready safety levels.
        /// </summary>
        public Dictionary<string, GDMethodOnreadySafety> Analyze()
        {
            var safety = new Dictionary<string, GDMethodOnreadySafety>();

            // Build caller graph from summaries
            BuildCallerGraph();

            // Phase 1: Initialize safety based on method characteristics
            foreach (var summary in _registry.GetAllSummaries())
            {
                safety[summary.MethodName] = summary.OnreadySafety;
            }

            // Phase 2: Fixed-point iteration
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
                        // No external callers → may be called externally → unsafe
                        safety[methodName] = GDMethodOnreadySafety.Unsafe;
                        changed = true;
                    }
                    else if (callers.All(c => safety.TryGetValue(c, out var s) && s == GDMethodOnreadySafety.Safe))
                    {
                        // All callers are safe → method is safe
                        safety[methodName] = GDMethodOnreadySafety.Safe;
                        changed = true;
                    }
                    else if (callers.Any(c => safety.TryGetValue(c, out var s) && s == GDMethodOnreadySafety.Unsafe))
                    {
                        // At least one unsafe caller → method is unsafe
                        safety[methodName] = GDMethodOnreadySafety.Unsafe;
                        changed = true;
                    }
                    // Otherwise, keep Unknown and wait for callers to be determined
                }
            }

            // Phase 3: Mark remaining Unknown methods as Unsafe
            // (circular dependencies without any safe entry point)
            foreach (var methodName in safety.Keys.ToList())
            {
                if (safety[methodName] == GDMethodOnreadySafety.Unknown)
                {
                    safety[methodName] = GDMethodOnreadySafety.Unsafe;
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
