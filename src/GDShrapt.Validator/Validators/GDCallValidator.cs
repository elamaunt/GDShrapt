namespace GDShrapt.Reader
{
    /// <summary>
    /// Validates function calls in GDScript.
    /// Uses function signatures collected by GDDeclarationCollector.
    /// </summary>
    public class GDCallValidator : GDValidationVisitor
    {
        public GDCallValidator(GDValidationContext context) : base(context)
        {
        }

        public void Validate(GDNode node)
        {
            if (node == null)
                return;

            // Function signatures are already collected by GDDeclarationCollector
            // Just walk the tree and validate calls
            node.WalkIn(this);
        }

        public override void Visit(GDCallExpression callExpression)
        {
            var caller = callExpression.CallerExpression;
            var parameters = callExpression.Parameters;
            var argCount = parameters?.Count ?? 0;

            // Direct function call: func_name()
            if (caller is GDIdentifierExpression identifierExpr)
            {
                var name = identifierExpr.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(name))
                {
                    // First check built-in via RuntimeProvider, then user-defined
                    if (!ValidateGlobalFunctionCall(name, argCount, callExpression))
                    {
                        ValidateUserFunctionCall(name, argCount, callExpression);
                    }
                }
            }
            // Method call on object: obj.method()
            else if (caller is GDMemberOperatorExpression memberExpr)
            {
                var callerExpr = memberExpr.CallerExpression;
                var methodName = memberExpr.Identifier?.Sequence;

                if (string.IsNullOrEmpty(methodName))
                    return;

                // self.method() - validate against user-defined methods
                if (callerExpr is GDIdentifierExpression selfExpr && selfExpr.Identifier?.Sequence == "self")
                {
                    ValidateUserFunctionCall(methodName, argCount, callExpression);
                }
                // GlobalClass.method() - validate against RuntimeProvider
                else if (callerExpr is GDIdentifierExpression identExpr)
                {
                    var typeName = identExpr.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        ValidateMemberCall(typeName, methodName, argCount, callExpression);
                    }
                }
            }
        }

        private void ValidateUserFunctionCall(string name, int argCount, GDCallExpression call)
        {
            // Use function signatures from context (collected by GDDeclarationCollector)
            var funcInfo = Context.GetUserFunction(name);
            if (funcInfo == null)
            {
                // Function not found - could be inherited or a method on another object
                // Don't report an error here - the scope validator handles undefined identifiers
                return;
            }

            if (argCount < funcInfo.MinParameters)
            {
                ReportError(
                    GDDiagnosticCode.WrongArgumentCount,
                    $"'{name}' requires at least {funcInfo.MinParameters} argument(s), got {argCount}",
                    call);
            }
            else if (argCount > funcInfo.MaxParameters && !funcInfo.HasVarArgs)
            {
                ReportError(
                    GDDiagnosticCode.WrongArgumentCount,
                    $"'{name}' takes at most {funcInfo.MaxParameters} argument(s), got {argCount}",
                    call);
            }
        }

        /// <summary>
        /// Validates a method call on a type using the RuntimeProvider.
        /// </summary>
        private void ValidateMemberCall(string typeName, string methodName, int argCount, GDCallExpression call)
        {
            // First check if it's a global class/singleton
            var globalClass = Context.RuntimeProvider.GetGlobalClass(typeName);
            if (globalClass != null)
            {
                // Get member info from the type
                var memberInfo = Context.RuntimeProvider.GetMember(typeName, methodName);
                if (memberInfo == null)
                {
                    // Method not found - could be inherited or unknown
                    // Don't report error for now, as we don't have full type hierarchy
                    return;
                }

                if (memberInfo.Kind != GDRuntimeMemberKind.Method)
                {
                    ReportError(
                        GDDiagnosticCode.NotCallable,
                        $"'{methodName}' on '{typeName}' is not a method",
                        call);
                    return;
                }

                // Validate argument count
                if (argCount < memberInfo.MinArgs)
                {
                    ReportError(
                        GDDiagnosticCode.WrongArgumentCount,
                        $"'{typeName}.{methodName}' requires at least {memberInfo.MinArgs} argument(s), got {argCount}",
                        call);
                }
                else if (!memberInfo.IsVarArgs && memberInfo.MaxArgs >= 0 && argCount > memberInfo.MaxArgs)
                {
                    ReportError(
                        GDDiagnosticCode.WrongArgumentCount,
                        $"'{typeName}.{methodName}' takes at most {memberInfo.MaxArgs} argument(s), got {argCount}",
                        call);
                }
            }
            // Also check if it's a known type (for static methods)
            else if (Context.RuntimeProvider.IsKnownType(typeName))
            {
                var memberInfo = Context.RuntimeProvider.GetMember(typeName, methodName);
                if (memberInfo != null && memberInfo.Kind == GDRuntimeMemberKind.Method)
                {
                    if (argCount < memberInfo.MinArgs)
                    {
                        ReportError(
                            GDDiagnosticCode.WrongArgumentCount,
                            $"'{typeName}.{methodName}' requires at least {memberInfo.MinArgs} argument(s), got {argCount}",
                            call);
                    }
                    else if (!memberInfo.IsVarArgs && memberInfo.MaxArgs >= 0 && argCount > memberInfo.MaxArgs)
                    {
                        ReportError(
                            GDDiagnosticCode.WrongArgumentCount,
                            $"'{typeName}.{methodName}' takes at most {memberInfo.MaxArgs} argument(s), got {argCount}",
                            call);
                    }
                }
            }
        }

        /// <summary>
        /// Validates a global function call using the RuntimeProvider.
        /// Returns true if the function is a known global function, false otherwise.
        /// </summary>
        private bool ValidateGlobalFunctionCall(string name, int argCount, GDCallExpression call)
        {
            var funcInfo = Context.RuntimeProvider.GetGlobalFunction(name);
            if (funcInfo == null)
                return false;

            // Validate argument count
            if (argCount < funcInfo.MinArgs)
            {
                ReportError(
                    GDDiagnosticCode.WrongArgumentCount,
                    $"'{name}' requires at least {funcInfo.MinArgs} argument(s), got {argCount}",
                    call);
            }
            else if (!funcInfo.IsVarArgs && funcInfo.MaxArgs >= 0 && argCount > funcInfo.MaxArgs)
            {
                if (funcInfo.MinArgs == funcInfo.MaxArgs)
                {
                    ReportError(
                        GDDiagnosticCode.WrongArgumentCount,
                        $"'{name}' requires exactly {funcInfo.MinArgs} argument(s), got {argCount}",
                        call);
                }
                else
                {
                    ReportError(
                        GDDiagnosticCode.WrongArgumentCount,
                        $"'{name}' takes {funcInfo.MinArgs} to {funcInfo.MaxArgs} argument(s), got {argCount}",
                        call);
                }
            }

            return true;
        }
    }
}
