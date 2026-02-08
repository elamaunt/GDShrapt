namespace GDShrapt.Semantics
{
    /// <summary>
    /// Result of cross-method flow analysis for a class.
    /// </summary>
    internal class GDCrossMethodFlowState
    {
        /// <summary>
        /// Variables guaranteed non-null after _ready() completes.
        /// Includes @onready variables and variables unconditionally initialized in _ready().
        /// </summary>
        public HashSet<string> GuaranteedAfterReady { get; } = new();

        /// <summary>
        /// Variables that may still be null after _ready() (conditional initialization).
        /// </summary>
        public HashSet<string> MayBeNullAfterReady { get; } = new();

        /// <summary>
        /// Cache of method safety levels for @onready access.
        /// Key is method name.
        /// </summary>
        public Dictionary<string, GDMethodOnreadySafety> MethodSafetyCache { get; set; } = new();

        /// <summary>
        /// Call graph: maps method name to set of methods it calls.
        /// </summary>
        public Dictionary<string, HashSet<string>> CallGraph { get; } = new();

        /// <summary>
        /// Reverse call graph: maps method name to set of methods that call it.
        /// </summary>
        public Dictionary<string, HashSet<string>> CallerGraph { get; } = new();

        /// <summary>
        /// All method summaries indexed by method name.
        /// </summary>
        public Dictionary<string, GDMethodFlowSummary> MethodSummaries { get; } = new();

        /// <summary>
        /// Checks if a variable is safe to access at a given method.
        /// </summary>
        public bool IsVariableSafeInMethod(string varName, string methodName)
        {
            if (!MethodSafetyCache.TryGetValue(methodName, out var safety))
                return false;

            if (safety != GDMethodOnreadySafety.Safe)
                return false;

            return GuaranteedAfterReady.Contains(varName) && !MayBeNullAfterReady.Contains(varName);
        }

        /// <summary>
        /// Gets methods that call the specified method.
        /// </summary>
        public IEnumerable<string> GetCallers(string methodName)
        {
            if (CallerGraph.TryGetValue(methodName, out var callers))
                return callers;
            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Gets methods called by the specified method.
        /// </summary>
        public IEnumerable<string> GetCallees(string methodName)
        {
            if (CallGraph.TryGetValue(methodName, out var callees))
                return callees;
            return Enumerable.Empty<string>();
        }
    }
}
