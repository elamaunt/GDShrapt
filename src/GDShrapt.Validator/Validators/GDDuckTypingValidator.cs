using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Validates duck typing safety - checks that member access on untyped variables
    /// has proper type guards (is, has_method, has_signal, etc.).
    /// </summary>
    public class GDDuckTypingValidator : GDValidationVisitor
    {
        private readonly GDDiagnosticSeverity _severity;
        private readonly GDTypeInferenceEngine _typeInference;
        private readonly GDTypeNarrowingAnalyzer _narrowingAnalyzer;

        // Methods on Object that are always available (type guards)
        private static readonly HashSet<string> ObjectMethods = new HashSet<string>
        {
            "has_method", "has_signal", "has", "get", "set", "call", "callv",
            "connect", "disconnect", "emit_signal", "is_connected",
            "get_class", "is_class", "get_property_list", "get_method_list",
            "notification", "to_string", "free", "queue_free"
        };

        // Track current narrowing context through if/elif/else branches
        private readonly Stack<GDTypeNarrowingContext> _narrowingStack = new Stack<GDTypeNarrowingContext>();
        private GDTypeNarrowingContext CurrentNarrowing => _narrowingStack.Count > 0 ? _narrowingStack.Peek() : null;

        public GDDuckTypingValidator(GDValidationContext context, GDDiagnosticSeverity severity)
            : base(context)
        {
            _severity = severity;
            _typeInference = new GDTypeInferenceEngine(context.RuntimeProvider, context.Scopes);
            _narrowingAnalyzer = new GDTypeNarrowingAnalyzer(context.RuntimeProvider);
        }

        public void Validate(GDNode node)
        {
            // Start with empty narrowing context
            _narrowingStack.Push(new GDTypeNarrowingContext());
            node?.WalkIn(this);
            _narrowingStack.Pop();
        }

        #region Scope Management

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            // Enter method scope and register parameters
            EnterScope(GDScopeType.Method, methodDeclaration);

            var parameters = methodDeclaration.Parameters;
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    var paramName = param.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(paramName))
                    {
                        var typeNode = param.Type;
                        var typeName = typeNode?.BuildName();
                        TryDeclareSymbol(GDSymbol.Parameter(paramName, param, typeName: typeName, typeNode: typeNode));
                    }
                }
            }
        }

        public override void Left(GDMethodDeclaration methodDeclaration)
        {
            ExitScope();
        }

        public override void Visit(GDVariableDeclarationStatement variableDeclaration)
        {
            var varName = variableDeclaration.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                var typeNode = variableDeclaration.Type;
                var typeName = typeNode?.BuildName();
                TryDeclareSymbol(GDSymbol.Variable(varName, variableDeclaration, typeName: typeName, typeNode: typeNode));
            }
        }

        #endregion

        #region If Statement Handling

        // Track whether we pushed a narrowing context for each branch to pop in Left
        private readonly Stack<bool> _pushedNarrowingForBranch = new Stack<bool>();

        public override void Visit(GDIfBranch ifBranch)
        {
            var condition = ifBranch.Condition;
            bool pushed = false;

            if (condition != null)
            {
                // Analyze condition for type narrowing
                var narrowingContext = _narrowingAnalyzer.AnalyzeCondition(condition, isNegated: false);
                var merged = CurrentNarrowing?.CreateChild() ?? new GDTypeNarrowingContext();
                MergeNarrowing(merged, narrowingContext);

                _narrowingStack.Push(merged);
                pushed = true;
            }

            _pushedNarrowingForBranch.Push(pushed);
            // Let WalkIn continue to visit children (condition and statements)
        }

        public override void Left(GDIfBranch ifBranch)
        {
            if (_pushedNarrowingForBranch.Pop())
            {
                _narrowingStack.Pop();
            }
        }

        public override void Visit(GDElifBranch elifBranch)
        {
            var condition = elifBranch.Condition;
            bool pushed = false;

            if (condition != null)
            {
                var narrowingContext = _narrowingAnalyzer.AnalyzeCondition(condition, isNegated: false);
                var merged = CurrentNarrowing?.CreateChild() ?? new GDTypeNarrowingContext();
                MergeNarrowing(merged, narrowingContext);

                _narrowingStack.Push(merged);
                pushed = true;
            }

            _pushedNarrowingForBranch.Push(pushed);
        }

        public override void Left(GDElifBranch elifBranch)
        {
            if (_pushedNarrowingForBranch.Pop())
            {
                _narrowingStack.Pop();
            }
        }

        public override void Visit(GDElseBranch elseBranch)
        {
            // In else branch, we know the if/elif conditions were false
            // Could add inverted narrowing here in the future
        }

        public override void Left(GDElseBranch elseBranch)
        {
            // Nothing to pop for else branches
        }

        #endregion

        #region Member Access Validation

        public override void Visit(GDMemberOperatorExpression memberAccess)
        {
            ValidateMemberAccess(memberAccess);
            base.Visit(memberAccess);
        }

        public override void Visit(GDCallExpression callExpression)
        {
            // Check if this is a method call on an object
            if (callExpression.CallerExpression is GDMemberOperatorExpression memberExpr)
            {
                ValidateMethodCall(memberExpr, callExpression);
            }
            base.Visit(callExpression);
        }

        private void ValidateMemberAccess(GDMemberOperatorExpression memberAccess)
        {
            var callerExpr = memberAccess.CallerExpression;
            var memberName = memberAccess.Identifier?.Sequence;

            if (callerExpr == null || string.IsNullOrEmpty(memberName))
                return;

            // Get the variable name if accessing member on a variable
            var varName = GetRootVariableName(callerExpr);
            if (string.IsNullOrEmpty(varName))
                return;

            // Skip if variable is 'self' or known global
            if (varName == "self" || Context.RuntimeProvider.GetGlobalClass(varName) != null)
                return;

            // Check if this is a built-in Object method (always available)
            if (ObjectMethods.Contains(memberName))
                return;

            // Check if variable has a declared type in scope
            if (IsVariableTyped(varName))
                return;

            // Get the type of the caller expression
            var callerType = _typeInference.InferType(callerExpr);

            // If type is known and it's a concrete type, check if member exists
            if (!string.IsNullOrEmpty(callerType) && callerType != "Variant" && !callerType.StartsWith("Unknown"))
            {
                // Type is known - standard type checking applies
                return;
            }

            // Variable is untyped or Variant - check for type guards
            var narrowedType = CurrentNarrowing?.GetNarrowedType(varName);

            // Check if narrowed to a concrete type via 'is' check
            if (narrowedType != null && narrowedType.PossibleTypes.Count > 0)
            {
                // User verified type via 'is' check - trust that access is valid
                // We don't need to verify member exists because:
                // 1. The runtime provider may not know user-defined types
                // 2. The 'is' check is explicit user intention
                return;
            }

            // Check if member is required by duck type (has_method, has_signal, has checks)
            if (narrowedType != null)
            {
                if (narrowedType.RequiredProperties.ContainsKey(memberName) ||
                    narrowedType.RequiredMethods.ContainsKey(memberName))
                {
                    return; // Member is guaranteed by has_method/property guard
                }
            }

            // No type guard found - report diagnostic
            ReportDuckTypingIssue(
                GDDiagnosticCode.UnguardedPropertyAccess,
                $"Accessing '{memberName}' on untyped variable '{varName}' without type guard",
                memberAccess);
        }

        private void ValidateMethodCall(GDMemberOperatorExpression memberExpr, GDCallExpression callExpression)
        {
            var callerExpr = memberExpr.CallerExpression;
            var methodName = memberExpr.Identifier?.Sequence;

            if (callerExpr == null || string.IsNullOrEmpty(methodName))
                return;

            var varName = GetRootVariableName(callerExpr);
            if (string.IsNullOrEmpty(varName))
                return;

            // Skip if variable is 'self' or known global
            if (varName == "self" || Context.RuntimeProvider.GetGlobalClass(varName) != null)
                return;

            // Skip built-in Object methods (has_method, get, set, etc.)
            if (ObjectMethods.Contains(methodName))
                return;

            // Check if variable has a declared type in scope
            if (IsVariableTyped(varName))
                return;

            // Get the type of the caller expression
            var callerType = _typeInference.InferType(callerExpr);

            // If type is known and it's a concrete type, standard type checking applies
            if (!string.IsNullOrEmpty(callerType) && callerType != "Variant" && !callerType.StartsWith("Unknown"))
                return;

            // Variable is untyped - check for type guards
            var narrowedType = CurrentNarrowing?.GetNarrowedType(varName);

            // Check if narrowed to a concrete type via 'is' check
            if (narrowedType != null && narrowedType.PossibleTypes.Count > 0)
            {
                // User verified type via 'is' check - trust that call is valid
                return;
            }

            // Check if method is guaranteed by has_method guard
            if (narrowedType != null && narrowedType.RequiredMethods.ContainsKey(methodName))
                return;

            // No type guard found
            ReportDuckTypingIssue(
                GDDiagnosticCode.UnguardedMethodCall,
                $"Calling method '{methodName}' on untyped variable '{varName}' without type guard",
                callExpression);
        }

        /// <summary>
        /// Checks if a variable has an explicit type declaration in scope.
        /// </summary>
        private bool IsVariableTyped(string varName)
        {
            var symbol = Context.Lookup(varName);
            if (symbol == null)
                return false;

            // Check if symbol has a declared type (via TypeName string or Type object)
            if (!string.IsNullOrEmpty(symbol.TypeName) && symbol.TypeName != "Variant")
                return true;

            if (symbol.Type != null)
                return true;

            return false;
        }

        #endregion

        #region Helper Methods

        private string GetRootVariableName(GDExpression expr)
        {
            switch (expr)
            {
                case GDIdentifierExpression idExpr:
                    return idExpr.Identifier?.Sequence;

                case GDMemberOperatorExpression memberExpr:
                    return GetRootVariableName(memberExpr.CallerExpression);

                case GDIndexerExpression indexerExpr:
                    return GetRootVariableName(indexerExpr.CallerExpression);

                default:
                    return null;
            }
        }

        private void MergeNarrowing(GDTypeNarrowingContext target, GDTypeNarrowingContext source)
        {
            if (source == null)
                return;

            // Copy all narrowing info from source to target
            foreach (var varName in source.GetNarrowedVariables())
            {
                var narrowed = source.GetNarrowedType(varName);
                if (narrowed == null)
                    continue;

                foreach (var type in narrowed.PossibleTypes)
                    target.NarrowType(varName, type);

                foreach (var type in narrowed.ExcludedTypes)
                    target.ExcludeType(varName, type);

                foreach (var kv in narrowed.RequiredMethods)
                    target.RequireMethod(varName, kv.Key);

                foreach (var kv in narrowed.RequiredProperties)
                    target.RequireProperty(varName, kv.Key);

                foreach (var signal in narrowed.RequiredSignals)
                    target.RequireSignal(varName, signal);
            }
        }

        private void ReportDuckTypingIssue(GDDiagnosticCode code, string message, GDNode node)
        {
            switch (_severity)
            {
                case GDDiagnosticSeverity.Error:
                    ReportError(code, message, node);
                    break;
                case GDDiagnosticSeverity.Warning:
                    ReportWarning(code, message, node);
                    break;
                case GDDiagnosticSeverity.Hint:
                    ReportHint(code, message, node);
                    break;
            }
        }

        #endregion
    }
}
