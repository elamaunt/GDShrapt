using GDShrapt.Reader;
using System.Collections.Generic;

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
        private readonly IReadOnlyList<GDSignalConnectionEntry>? _externalSignalConnections;

        public GDCrossMethodFlowAnalyzer(
            GDSemanticModel semanticModel,
            GDMethodFlowSummaryRegistry registry,
            IReadOnlyList<GDSignalConnectionEntry>? externalSignalConnections = null)
        {
            _semanticModel = semanticModel;
            _registry = registry;
            _externalSignalConnections = externalSignalConnections;
        }

        /// <summary>
        /// Performs full cross-method flow analysis and returns the result state.
        /// </summary>
        public GDCrossMethodFlowState Analyze()
        {
            var state = new GDCrossMethodFlowState();

            BuildMethodSummaries();

            var signalConnections = CollectSignalConnections();
            var safetyAnalyzer = new GDMethodOnreadySafetyAnalyzer(_semanticModel, _registry, signalConnections);
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

            state.MethodSafetyCache = safetyAnalyzer.Analyze();

            AnalyzeReadyMethod(state);

            foreach (var summary in _registry.GetAllSummaries())
            {
                state.MethodSummaries[summary.MethodName] = summary;
            }

            return state;
        }

        private IReadOnlyList<GDSignalConnectionEntry> CollectSignalConnections()
        {
            var scriptFile = _semanticModel.ScriptFile;
            var local = scriptFile?.Class != null
                ? GDSignalConnectionCollector.CollectConnectionsInFile(scriptFile, _semanticModel.RuntimeProvider)
                : System.Array.Empty<GDSignalConnectionEntry>();

            if (_externalSignalConnections == null || _externalSignalConnections.Count == 0)
                return local;

            var merged = new List<GDSignalConnectionEntry>(local);
            merged.AddRange(_externalSignalConnections);
            return merged;
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
                else if (member is GDVariableDeclaration varDecl)
                {
                    // Inline setter body: set(value): ...
                    var setter = varDecl.FirstAccessorDeclarationNode as GDSetAccessorBodyDeclaration
                              ?? varDecl.SecondAccessorDeclarationNode as GDSetAccessorBodyDeclaration;

                    if (setter != null)
                    {
                        var summary = summaryBuilder.BuildForSetter(varDecl, setter);
                        _registry.Register(summary, _semanticModel.ScriptFile?.FullPath);
                    }
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
