using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Validates function calls in GDScript.
    /// Pass 1: Collects all user-defined function declarations.
    /// Pass 2: Validates calls against both built-in and user-defined functions.
    /// </summary>
    public class GDCallValidator : GDValidationVisitor
    {
        private readonly Dictionary<string, FunctionInfo> _userFunctions = new Dictionary<string, FunctionInfo>();
        private bool _isCollectionPass;

        private class FunctionInfo
        {
            public string Name { get; set; }
            public int MinParameters { get; set; }
            public int MaxParameters { get; set; }
            public bool HasVarArgs { get; set; }
            public GDMethodDeclaration Declaration { get; set; }
        }

        public GDCallValidator(GDValidationContext context) : base(context)
        {
        }

        public void Validate(GDNode node)
        {
            if (node == null)
                return;

            // Pass 1: Collect user-defined functions
            _isCollectionPass = true;
            _userFunctions.Clear();
            node.WalkIn(this);

            // Pass 2: Validate calls
            _isCollectionPass = false;
            node.WalkIn(this);
        }

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            if (!_isCollectionPass)
                return;

            var methodName = methodDeclaration.Identifier?.Sequence;
            if (string.IsNullOrEmpty(methodName))
                return;

            // Don't override if already declared (could be inherited/overridden)
            if (_userFunctions.ContainsKey(methodName))
                return;

            var parameters = methodDeclaration.Parameters?.ToList() ?? new List<GDParameterDeclaration>();

            // Count required and optional parameters
            int minParams = 0;
            int maxParams = parameters.Count;

            foreach (var param in parameters)
            {
                // If param has a default value, it's optional
                if (param.DefaultValue == null)
                    minParams++;
            }

            _userFunctions[methodName] = new FunctionInfo
            {
                Name = methodName,
                MinParameters = minParams,
                MaxParameters = maxParams,
                HasVarArgs = false, // GDScript doesn't have varargs for user functions
                Declaration = methodDeclaration
            };
        }

        public override void Visit(GDCallExpression callExpression)
        {
            if (_isCollectionPass)
                return;

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
            if (!_userFunctions.TryGetValue(name, out var funcInfo))
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
