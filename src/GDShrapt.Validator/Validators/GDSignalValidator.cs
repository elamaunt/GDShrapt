using System.Linq;
using GDShrapt.Abstractions;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Validates signal operations (emit_signal, connect) in GDScript.
    /// Checks signal existence and argument counts.
    /// </summary>
    public class GDSignalValidator : GDValidationVisitor
    {
        public GDSignalValidator(GDValidationContext context) : base(context)
        {
        }

        public void Validate(GDNode node)
        {
            if (node == null)
                return;

            node.WalkIn(this);
        }

        public override void Visit(GDCallExpression callExpression)
        {
            var caller = callExpression.CallerExpression;

            // Check for emit_signal and connect calls
            if (caller is GDMemberOperatorExpression memberExpr)
            {
                var methodName = memberExpr.Identifier?.Sequence;
                if (methodName == "emit_signal")
                {
                    ValidateEmitSignal(callExpression, memberExpr);
                }
                else if (methodName == "connect")
                {
                    ValidateConnect(callExpression, memberExpr);
                }
            }
            else if (caller is GDIdentifierExpression identExpr)
            {
                // Direct call to emit_signal/connect (implicit self)
                var name = identExpr.Identifier?.Sequence;
                if (name == "emit_signal")
                {
                    ValidateEmitSignal(callExpression, callerExpression: null);
                }
                else if (name == "connect")
                {
                    ValidateConnect(callExpression, callerExpression: null);
                }
            }
        }

        private void ValidateEmitSignal(GDCallExpression callExpr, GDMemberOperatorExpression callerExpression)
        {
            var args = callExpr.Parameters;
            if (args == null || args.Count == 0)
            {
                ReportError(
                    GDDiagnosticCode.WrongArgumentCount,
                    "emit_signal requires at least signal name as argument",
                    callExpr);
                return;
            }

            // Get signal name from first argument
            var signalNameArg = args.FirstOrDefault();
            var signalName = ExtractStaticString(signalNameArg);

            if (signalName == null)
                return; // Dynamic signal name - cannot validate

            // Determine the type of the object emitting the signal
            var callerType = GetCallerType(callerExpression);

            // Try to find the signal
            var signalInfo = FindSignal(callerType, signalName);

            if (signalInfo == null)
            {
                // Only report when caller type is known or it's a bare self call.
                // Skip when there's a caller expression but type is unresolvable (e.g., call chain).
                if (callerExpression == null || callerType != null)
                {
                    ReportWarning(
                        GDDiagnosticCode.UndefinedSignalEmit,
                        $"Signal '{signalName}' not found on type '{callerType ?? "self"}'",
                        callExpr);
                }
                return;
            }

            // Validate argument count (emit_signal args are: signal_name, ...signal_args)
            var expectedParamCount = signalInfo.Parameters?.Count ?? 0;
            var actualArgCount = args.Count - 1; // Minus signal name

            if (actualArgCount != expectedParamCount)
            {
                ReportError(
                    GDDiagnosticCode.EmitSignalWrongArgCount,
                    $"Signal '{signalName}' expects {expectedParamCount} argument(s), got {actualArgCount}",
                    callExpr);
            }
        }

        private void ValidateConnect(GDCallExpression callExpr, GDMemberOperatorExpression callerExpression)
        {
            var args = callExpr.Parameters;
            if (args == null || args.Count < 2)
            {
                // connect requires at least signal_name and callable
                return;
            }

            // Get signal name from first argument
            var signalNameArg = args.FirstOrDefault();
            var signalName = ExtractStaticString(signalNameArg);

            if (signalName == null)
                return; // Dynamic signal name - cannot validate

            // Determine the type of the object with the signal
            var callerType = GetCallerType(callerExpression);

            // Try to find the signal
            var signalInfo = FindSignal(callerType, signalName);

            if (signalInfo == null)
            {
                // Only report when caller type is known or it's a bare self call.
                // Skip when there's a caller expression but type is unresolvable (e.g., call chain).
                if (callerExpression == null || callerType != null)
                {
                    ReportWarning(
                        GDDiagnosticCode.UndefinedSignalEmit,
                        $"Signal '{signalName}' not found on type '{callerType ?? "self"}'",
                        callExpr);
                }
                return;
            }

            // Check callback signature if we can determine it
            var callableArg = args.Skip(1).FirstOrDefault();
            ValidateConnectCallback(callExpr, signalInfo, callableArg);
        }

        private void ValidateConnectCallback(GDCallExpression callExpr, GDSignalInfo signalInfo, GDExpression callableArg)
        {
            if (callableArg == null)
                return;

            // Check for Callable(self, "method_name") pattern
            if (callableArg is GDCallExpression callableCall)
            {
                if (callableCall.CallerExpression is GDIdentifierExpression callableIdent &&
                    callableIdent.Identifier?.Sequence == "Callable")
                {
                    var callableParams = callableCall.Parameters;
                    if (callableParams?.Count >= 2)
                    {
                        var methodNameArg = callableParams.Skip(1).FirstOrDefault();
                        var methodName = ExtractStaticString(methodNameArg);

                        if (methodName != null)
                        {
                            ValidateCallbackMethodSignature(callExpr, signalInfo, methodName);
                        }
                    }
                }
            }
            // Check for direct method reference (self.method_name)
            else if (callableArg is GDMemberOperatorExpression methodRef)
            {
                var methodName = methodRef.Identifier?.Sequence;
                if (methodName != null)
                {
                    ValidateCallbackMethodSignature(callExpr, signalInfo, methodName);
                }
            }
        }

        private void ValidateCallbackMethodSignature(GDCallExpression callExpr, GDSignalInfo signalInfo, string methodName)
        {
            // Get method signature from user functions
            var methodSignature = Context.GetUserFunction(methodName);
            if (methodSignature == null)
                return; // Method not found in user functions, skip validation

            var signalParamCount = signalInfo.Parameters?.Count ?? 0;

            // Callback should accept at least the signal parameters
            if (methodSignature.MinParameters > signalParamCount)
            {
                ReportWarning(
                    GDDiagnosticCode.ConnectCallbackMismatch,
                    $"Callback '{methodName}' requires {methodSignature.MinParameters} parameter(s), but signal '{signalInfo.Name}' emits {signalParamCount}",
                    callExpr);
            }
            else if (!methodSignature.HasVarArgs && methodSignature.MaxParameters < signalParamCount)
            {
                ReportWarning(
                    GDDiagnosticCode.ConnectCallbackMismatch,
                    $"Callback '{methodName}' accepts at most {methodSignature.MaxParameters} parameter(s), but signal '{signalInfo.Name}' emits {signalParamCount}",
                    callExpr);
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

            // Check for StringName(&"signal_name")
            if (expr is GDCallExpression callExpr &&
                callExpr.CallerExpression is GDIdentifierExpression identExpr &&
                identExpr.Identifier?.Sequence == "StringName")
            {
                var firstArg = callExpr.Parameters?.FirstOrDefault();
                return ExtractStaticString(firstArg);
            }

            return null;
        }

        /// <summary>
        /// Gets the type name of the caller expression.
        /// </summary>
        private string GetCallerType(GDMemberOperatorExpression memberExpr)
        {
            if (memberExpr?.CallerExpression == null)
                return null;

            // self.emit_signal - type is current class
            if (memberExpr.CallerExpression is GDIdentifierExpression identExpr)
            {
                var name = identExpr.Identifier?.Sequence;
                if (name == "self")
                    return null; // Use current class signals

                // Check if it's a known type
                if (Context.RuntimeProvider.IsKnownType(name))
                    return name;

                // Check if it's a variable with known type
                var symbol = LookupSymbol(name);
                return symbol?.TypeName;
            }

            return null;
        }

        /// <summary>
        /// Finds signal information for a type.
        /// </summary>
        private GDSignalInfo FindSignal(string typeName, string signalName)
        {
            // First check user-defined signals in the class scope
            if (string.IsNullOrEmpty(typeName))
            {
                // Check class-level signal declarations
                var symbol = Context.Scopes.Lookup(signalName);
                if (symbol?.Kind == GDSymbolKind.Signal && symbol.Declaration is GDSignalDeclaration signalDecl)
                {
                    return new GDSignalInfo
                    {
                        Name = signalName,
                        Parameters = signalDecl.Parameters?
                            .OfType<GDParameterDeclaration>()
                            .Select(p => new GDRuntimeParameterInfo(
                                p.Identifier?.Sequence ?? "",
                                p.Type?.BuildName() ?? "Variant"))
                            .ToList()
                    };
                }
            }

            // Check through project runtime provider if available
            if (Context.RuntimeProvider is IGDProjectRuntimeProvider projectProvider)
            {
                var signal = projectProvider.GetSignal(typeName ?? "self", signalName);
                if (signal != null)
                    return signal;
            }

            // Check built-in signals from runtime provider
            if (!string.IsNullOrEmpty(typeName))
            {
                var typeInfo = Context.RuntimeProvider.GetTypeInfo(typeName);
                var signalMember = typeInfo?.Members?.FirstOrDefault(m =>
                    m.Kind == GDRuntimeMemberKind.Signal && m.Name == signalName);

                if (signalMember != null)
                {
                    return new GDSignalInfo
                    {
                        Name = signalName,
                        Parameters = signalMember.Parameters
                    };
                }
            }

            return null;
        }
    }
}
