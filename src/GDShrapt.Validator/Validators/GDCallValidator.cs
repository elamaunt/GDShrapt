using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Validates function calls in GDScript.
    /// Uses function signatures collected by GDDeclarationCollector.
    /// Optionally validates argument types when ArgumentTypeAnalyzer is provided.
    /// </summary>
    public class GDCallValidator : GDValidationVisitor
    {
        private readonly bool _checkResourcePaths;
        private readonly IGDArgumentTypeAnalyzer? _argumentTypeAnalyzer;
        private readonly bool _checkArgumentTypes;
        private readonly GDDiagnosticSeverity _argumentTypeSeverity;

        public GDCallValidator(GDValidationContext context, bool checkResourcePaths = true) : base(context)
        {
            _checkResourcePaths = checkResourcePaths;
            _argumentTypeAnalyzer = null;
            _checkArgumentTypes = false;
            _argumentTypeSeverity = GDDiagnosticSeverity.Warning;
        }

        public GDCallValidator(
            GDValidationContext context,
            bool checkResourcePaths,
            IGDArgumentTypeAnalyzer? argumentTypeAnalyzer,
            bool checkArgumentTypes,
            GDDiagnosticSeverity argumentTypeSeverity) : base(context)
        {
            _checkResourcePaths = checkResourcePaths;
            _argumentTypeAnalyzer = argumentTypeAnalyzer;
            _checkArgumentTypes = checkArgumentTypes && argumentTypeAnalyzer != null;
            _argumentTypeSeverity = argumentTypeSeverity;
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
                    // Check load/preload path validation
                    if (_checkResourcePaths && (name == "load" || name == "preload"))
                    {
                        ValidateLoadCall(callExpression, name);
                    }

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

            // Validate argument types if enabled
            if (_checkArgumentTypes && _argumentTypeAnalyzer != null)
            {
                ValidateArgumentTypes(callExpression);
            }
        }

        /// <summary>
        /// Validates argument types for a call expression using the semantic analyzer.
        /// </summary>
        private void ValidateArgumentTypes(GDCallExpression callExpression)
        {
            if (_argumentTypeAnalyzer == null)
                return;

            var diffs = _argumentTypeAnalyzer.GetAllArgumentTypeDiffs(callExpression);

            foreach (var diff in diffs)
            {
                // Skip if should be skipped (Variant without constraints)
                if (diff.ShouldSkip)
                    continue;

                // Skip if compatible
                if (diff.IsCompatible)
                    continue;

                // Report the mismatch with detailed message
                ReportArgumentTypeMismatch(callExpression, diff);
            }
        }

        /// <summary>
        /// Reports an argument type mismatch diagnostic.
        /// </summary>
        private void ReportArgumentTypeMismatch(GDCallExpression callExpression, GDArgumentTypeDiff diff)
        {
            // Use short message for inline display, detailed message for extended info
            var shortMessage = diff.FormatShortMessage();
            var detailedMessage = diff.FormatDetailedMessage();

            // Choose the appropriate severity
            switch (_argumentTypeSeverity)
            {
                case GDDiagnosticSeverity.Error:
                    ReportError(GDDiagnosticCode.ArgumentTypeMismatch, shortMessage, callExpression);
                    break;
                case GDDiagnosticSeverity.Warning:
                    ReportWarning(GDDiagnosticCode.ArgumentTypeMismatch, shortMessage, callExpression);
                    break;
                case GDDiagnosticSeverity.Hint:
                    ReportHint(GDDiagnosticCode.ArgumentTypeMismatch, shortMessage, callExpression);
                    break;
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
        /// Finds a member in a type, traversing the inheritance chain if necessary.
        /// </summary>
        private GDRuntimeMemberInfo FindMemberWithInheritance(string typeName, string memberName)
        {
            var visited = new HashSet<string>();
            var current = typeName;
            while (!string.IsNullOrEmpty(current))
            {
                // Prevent infinite loop on cyclic inheritance
                if (!visited.Add(current))
                    return null;

                var memberInfo = Context.RuntimeProvider.GetMember(current, memberName);
                if (memberInfo != null)
                    return memberInfo;

                current = Context.RuntimeProvider.GetBaseType(current);
            }
            return null;
        }

        /// <summary>
        /// Validates a method call on a type using the RuntimeProvider.
        /// </summary>
        private void ValidateMemberCall(string typeName, string methodName, int argCount, GDCallExpression call)
        {
            // Skip .new() constructor - it's a special built-in, not a regular method
            if (methodName == "new")
                return;

            // First check if it's a global class/singleton
            var globalClass = Context.RuntimeProvider.GetGlobalClass(typeName);
            if (globalClass != null)
            {
                // Get member info from the type, including inheritance chain
                var memberInfo = FindMemberWithInheritance(typeName, methodName);
                if (memberInfo == null)
                {
                    // Method not found on known global class - report warning
                    // Using warning since method could be inherited from types we don't fully know
                    ReportWarning(
                        GDDiagnosticCode.MethodNotFound,
                        $"Method '{methodName}' not found on '{typeName}'",
                        call);
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
                // Get member info from the type, including inheritance chain
                var memberInfo = FindMemberWithInheritance(typeName, methodName);
                if (memberInfo == null)
                {
                    // Method not found on known type - report warning
                    // Using warning since type hierarchy may be incomplete
                    ReportWarning(
                        GDDiagnosticCode.MethodNotFound,
                        $"Method '{methodName}' not found on type '{typeName}'",
                        call);
                    return;
                }

                if (memberInfo.Kind == GDRuntimeMemberKind.Method)
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

        /// <summary>
        /// Validates load/preload calls by checking if the resource path exists.
        /// </summary>
        private void ValidateLoadCall(GDCallExpression callExpr, string funcName)
        {
            var args = callExpr.Parameters;
            if (args == null || args.Count == 0)
                return;

            // Get path from first argument
            var pathArg = args.FirstOrDefault();
            var resourcePath = ExtractStaticString(pathArg);

            if (resourcePath == null)
                return; // Dynamic path - cannot validate

            // Check through project runtime provider if available
            if (Context.RuntimeProvider is IGDProjectRuntimeProvider projectProvider)
            {
                if (!projectProvider.ResourceExists(resourcePath))
                {
                    ReportWarning(
                        GDDiagnosticCode.ResourceNotFound,
                        $"Resource not found: '{resourcePath}'",
                        callExpr);
                }
            }
        }

        /// <summary>
        /// Extracts a static string value from an expression.
        /// Returns null if the string is dynamic or cannot be determined.
        /// </summary>
        private string ExtractStaticString(GDExpression expr)
        {
            if (expr is GDStringExpression stringExpr)
            {
                return stringExpr.String?.Sequence;
            }

            return null;
        }
    }
}
