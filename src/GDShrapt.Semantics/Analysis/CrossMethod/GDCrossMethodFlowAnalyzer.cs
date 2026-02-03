using GDShrapt.Reader;

namespace GDShrapt.Semantics
{
    /// <summary>
    /// Main orchestrator for cross-method flow analysis.
    /// Coordinates building of method summaries, call graphs, and safety analysis.
    /// </summary>
    internal class GDCrossMethodFlowAnalyzer
    {
        private readonly GDSemanticModel _semanticModel;
        private readonly GDMethodFlowSummaryRegistry _registry;

        public GDCrossMethodFlowAnalyzer(GDSemanticModel semanticModel, GDMethodFlowSummaryRegistry registry)
        {
            _semanticModel = semanticModel;
            _registry = registry;
        }

        /// <summary>
        /// Performs full cross-method flow analysis and returns the result state.
        /// </summary>
        public GDCrossMethodFlowState Analyze()
        {
            var state = new GDCrossMethodFlowState();

            // Phase 1: Build method flow summaries for all methods
            BuildMethodSummaries();

            // Phase 2: Build call graphs
            var safetyAnalyzer = new GDMethodOnreadySafetyAnalyzer(_semanticModel, _registry);
            state.CallGraph.Clear();
            foreach (var kvp in safetyAnalyzer.GetCallGraph())
            {
                state.CallGraph[kvp.Key] = kvp.Value;
            }

            state.CallerGraph.Clear();
            foreach (var kvp in safetyAnalyzer.GetCallerGraph())
            {
                state.CallerGraph[kvp.Key] = kvp.Value;
            }

            // Phase 3: Compute method safety
            state.MethodSafetyCache = safetyAnalyzer.Analyze();

            // Phase 4: Analyze _ready() for variable initialization
            AnalyzeReadyMethod(state);

            // Phase 5: Store method summaries
            foreach (var summary in _registry.GetAllSummaries())
            {
                state.MethodSummaries[summary.MethodName] = summary;
            }

            return state;
        }

        private void BuildMethodSummaries()
        {
            var summaryBuilder = new GDMethodFlowSummaryBuilder(_semanticModel);
            var classDecl = _semanticModel.ScriptFile?.Class;
            if (classDecl == null)
                return;

            foreach (var member in classDecl.Members)
            {
                if (member is GDMethodDeclaration method)
                {
                    var summary = summaryBuilder.Build(method);
                    _registry.Register(summary, _semanticModel.ScriptFile?.FullPath);
                }
            }
        }

        private void AnalyzeReadyMethod(GDCrossMethodFlowState state)
        {
            var readySummary = _registry.GetSummary(_semanticModel.ScriptFile?.TypeName ?? "", GDSpecialMethodHelper.Ready);

            // Add @onready variables - they're always guaranteed after ready
            foreach (var varName in _semanticModel.GetOnreadyVariables())
            {
                state.GuaranteedAfterReady.Add(varName);
            }

            if (readySummary == null)
                return;

            // Unconditional initializations in _ready() are guaranteed
            foreach (var varName in readySummary.UnconditionalInitializations)
            {
                state.GuaranteedAfterReady.Add(varName);
            }

            // Conditional initializations may still be null
            foreach (var varName in readySummary.ConditionalInitializations)
            {
                state.MayBeNullAfterReady.Add(varName);
            }
        }
    }
}
