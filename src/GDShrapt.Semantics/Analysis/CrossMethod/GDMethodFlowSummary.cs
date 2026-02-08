namespace GDShrapt.Semantics
{
    /// <summary>
    /// Summary of flow state for a method. Used for cross-method analysis.
    /// </summary>
    internal class GDMethodFlowSummary
    {
        /// <summary>
        /// Method key in format "ClassName.MethodName"
        /// </summary>
        public string MethodKey { get; init; } = string.Empty;

        /// <summary>
        /// Method name without class prefix
        /// </summary>
        public string MethodName { get; init; } = string.Empty;

        /// <summary>
        /// Variables guaranteed non-null on exit from this method.
        /// </summary>
        public Dictionary<string, GDExitVariableState> ExitGuarantees { get; } = new();

        /// <summary>
        /// Variables unconditionally initialized by this method (on ALL paths).
        /// </summary>
        public HashSet<string> UnconditionalInitializations { get; } = new();

        /// <summary>
        /// Variables conditionally initialized by this method (on SOME paths only).
        /// </summary>
        public HashSet<string> ConditionalInitializations { get; } = new();

        /// <summary>
        /// Final merged flow state after all paths.
        /// </summary>
        public GDFlowState? ExitState { get; set; }

        /// <summary>
        /// Safety level for @onready variables.
        /// </summary>
        public GDMethodOnreadySafety OnreadySafety { get; set; } = GDMethodOnreadySafety.Unknown;

        /// <summary>
        /// Methods called by this method (for call graph building).
        /// </summary>
        public HashSet<string> CalledMethods { get; } = new();
    }

    /// <summary>
    /// State of a variable at method exit.
    /// </summary>
    internal class GDExitVariableState
    {
        /// <summary>
        /// Whether the variable is guaranteed non-null at exit.
        /// </summary>
        public bool IsGuaranteedNonNull { get; set; }

        /// <summary>
        /// Whether the variable may be null at exit.
        /// </summary>
        public bool IsPotentiallyNull { get; set; }

        /// <summary>
        /// Type of the variable at exit.
        /// </summary>
        public GDUnionType? Type { get; set; }

        /// <summary>
        /// Which branches initialize this variable.
        /// </summary>
        public GDInitializationBranches InitializedIn { get; set; }
    }

    /// <summary>
    /// Describes which branches of code initialize a variable.
    /// </summary>
    internal enum GDInitializationBranches
    {
        /// <summary>
        /// Variable is not initialized in this method.
        /// </summary>
        None,

        /// <summary>
        /// Variable is initialized unconditionally (on all paths).
        /// </summary>
        Unconditional,

        /// <summary>
        /// Variable is initialized conditionally (on some paths only).
        /// </summary>
        Conditional
    }

    /// <summary>
    /// Safety level of a method for accessing @onready variables.
    /// </summary>
    public enum GDMethodOnreadySafety
    {
        /// <summary>
        /// Method has not been analyzed yet.
        /// </summary>
        Unknown,

        /// <summary>
        /// Method is safe - it's a lifecycle method or all callers are safe.
        /// </summary>
        Safe,

        /// <summary>
        /// Method is unsafe - may be called before _ready().
        /// </summary>
        Unsafe,

        /// <summary>
        /// Method requires is_node_ready() guard for safety.
        /// </summary>
        ConditionalSafe
    }
}
