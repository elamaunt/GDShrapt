using GDShrapt.Reader;

namespace GDShrapt.Semantics
{
    /// <summary>
    /// Builds GDMethodFlowSummary from method analysis.
    /// </summary>
    internal class GDMethodFlowSummaryBuilder
    {
        private readonly GDSemanticModel _semanticModel;
        private readonly string _className;

        public GDMethodFlowSummaryBuilder(GDSemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
            _className = semanticModel.ScriptFile?.TypeName ?? "Unknown";
        }

        /// <summary>
        /// Builds a flow summary for a method.
        /// </summary>
        public GDMethodFlowSummary Build(GDMethodDeclaration method)
        {
            var methodName = method.Identifier?.Sequence ?? "";
            var methodKey = $"{_className}.{methodName}";

            var summary = new GDMethodFlowSummary
            {
                MethodKey = methodKey,
                MethodName = methodName
            };

            // Determine initial safety from lifecycle status
            summary.OnreadySafety = DetermineInitialSafety(method);

            // Get class variables for assignment analysis
            var classVariables = GetClassVariableNames();

            // Analyze assignment paths
            var pathAnalyzer = new GDAssignmentPathAnalyzer(classVariables);
            var assignmentResult = pathAnalyzer.Analyze(method);

            foreach (var varName in assignmentResult.Unconditional)
            {
                summary.UnconditionalInitializations.Add(varName);
            }

            foreach (var varName in assignmentResult.Conditional)
            {
                summary.ConditionalInitializations.Add(varName);
            }

            // Collect called methods
            var callCollector = new MethodCallCollector();
            method.WalkIn(callCollector);
            foreach (var calledMethod in callCollector.CalledMethods)
            {
                summary.CalledMethods.Add(calledMethod);
            }

            // Build exit guarantees from flow state
            BuildExitGuarantees(summary, method, classVariables);

            return summary;
        }

        private GDMethodOnreadySafety DetermineInitialSafety(GDMethodDeclaration method)
        {
            // Lifecycle methods are inherently safe for @onready access
            if (method.IsReady() || method.IsProcessMethod() ||
                method.IsInputMethod() || method.IsDraw())
            {
                return GDMethodOnreadySafety.Safe;
            }

            // Other methods start as unknown
            return GDMethodOnreadySafety.Unknown;
        }

        private IEnumerable<string> GetClassVariableNames()
        {
            var classDecl = _semanticModel.ScriptFile?.Class;
            if (classDecl == null)
                yield break;

            foreach (var member in classDecl.Members)
            {
                if (member is GDVariableDeclaration varDecl)
                {
                    var name = varDecl.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(name))
                        yield return name;
                }
            }
        }

        private void BuildExitGuarantees(
            GDMethodFlowSummary summary,
            GDMethodDeclaration method,
            IEnumerable<string> classVariables)
        {
            // Get flow state at the end of the method
            // Use the last statement if available, otherwise the method itself
            GDFlowState? flowState = null;
            var lastStatement = method.Statements?.LastOrDefault();
            if (lastStatement != null)
            {
                flowState = _semanticModel.GetFlowStateAtLocation(lastStatement);
            }

            foreach (var varName in classVariables)
            {
                var exitState = new GDExitVariableState();

                // Check if assigned in this method
                if (summary.UnconditionalInitializations.Contains(varName))
                {
                    exitState.InitializedIn = GDInitializationBranches.Unconditional;
                    exitState.IsGuaranteedNonNull = true;
                    exitState.IsPotentiallyNull = false;
                }
                else if (summary.ConditionalInitializations.Contains(varName))
                {
                    exitState.InitializedIn = GDInitializationBranches.Conditional;
                    exitState.IsGuaranteedNonNull = false;
                    exitState.IsPotentiallyNull = true;
                }
                else
                {
                    exitState.InitializedIn = GDInitializationBranches.None;

                    // Check flow state if available
                    if (flowState != null)
                    {
                        var varType = flowState.GetVariableType(varName);
                        if (varType != null)
                        {
                            exitState.IsGuaranteedNonNull = varType.IsGuaranteedNonNull;
                            exitState.IsPotentiallyNull = varType.IsPotentiallyNull;
                        }
                    }
                }

                summary.ExitGuarantees[varName] = exitState;
            }
        }

        /// <summary>
        /// Helper visitor to collect method calls within a method.
        /// </summary>
        private class MethodCallCollector : GDVisitor
        {
            public HashSet<string> CalledMethods { get; } = new();

            public override void Visit(GDCallExpression callExpr)
            {
                var callerExpr = callExpr.CallerExpression;

                // Direct function call: func_name()
                if (callerExpr is GDIdentifierExpression ident)
                {
                    var name = ident.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(name))
                        CalledMethods.Add(name);
                }
                // Method call on self: self.method() or just method()
                else if (callerExpr is GDMemberOperatorExpression memberExpr)
                {
                    // Only track self.method() calls as internal
                    if (memberExpr.CallerExpression is GDIdentifierExpression selfIdent &&
                        selfIdent.Identifier?.Sequence == "self")
                    {
                        var name = memberExpr.Identifier?.Sequence;
                        if (!string.IsNullOrEmpty(name))
                            CalledMethods.Add(name);
                    }
                }
            }
        }
    }
}
