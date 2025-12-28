namespace GDShrapt.Reader
{
    /// <summary>
    /// Checks argument counts for built-in GDScript functions.
    /// </summary>
    public class GDCallValidator : GDValidationVisitor
    {
        public GDCallValidator(GDValidationContext context) : base(context)
        {
        }

        public void Validate(GDNode node)
        {
            node?.WalkIn(this);
        }

        public override void Visit(GDCallExpression callExpression)
        {
            var caller = callExpression.CallerExpression;
            var parameters = callExpression.Parameters;
            var argCount = parameters?.Count ?? 0;

            if (caller is GDIdentifierExpression identifierExpr)
            {
                var name = identifierExpr.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(name))
                {
                    ValidateBuiltInCall(name, argCount, callExpression);
                }
            }
        }

        private void ValidateBuiltInCall(string name, int argCount, GDCallExpression call)
        {
            switch (name)
            {
                // Variadic - any count
                case "print":
                case "prints":
                case "printt":
                case "printraw":
                case "print_rich":
                case "print_debug":
                    break;

                // At least 1
                case "printerr":
                case "push_error":
                case "push_warning":
                case "str":
                case "min":
                case "max":
                    if (argCount < 1)
                    {
                        ReportError(
                            GDDiagnosticCode.WrongArgumentCount,
                            $"'{name}' requires at least 1 argument, got {argCount}",
                            call);
                    }
                    break;

                // Exactly 1
                case "load":
                case "preload":
                case "abs":
                case "absf":
                case "absi":
                case "ceil":
                case "ceilf":
                case "ceili":
                case "floor":
                case "floorf":
                case "floori":
                case "round":
                case "roundf":
                case "roundi":
                case "sign":
                case "signf":
                case "signi":
                case "sqrt":
                case "log":
                case "exp":
                case "sin":
                case "cos":
                case "tan":
                case "sinh":
                case "cosh":
                case "tanh":
                case "asin":
                case "acos":
                case "atan":
                case "asinh":
                case "acosh":
                case "atanh":
                case "typeof":
                case "weakref":
                case "hash":
                case "len":
                case "deg_to_rad":
                case "rad_to_deg":
                case "is_nan":
                case "is_inf":
                case "is_finite":
                case "is_zero_approx":
                case "instance_from_id":
                case "is_instance_valid":
                case "is_instance_id_valid":
                case "type_string":
                case "var_to_str":
                case "str_to_var":
                case "get_stack":
                    if (argCount != 1)
                    {
                        ReportError(
                            GDDiagnosticCode.WrongArgumentCount,
                            $"'{name}' requires exactly 1 argument, got {argCount}",
                            call);
                    }
                    break;

                // Exactly 0
                case "randomize":
                case "randi":
                case "randf":
                    if (argCount != 0)
                    {
                        ReportError(
                            GDDiagnosticCode.WrongArgumentCount,
                            $"'randomize' requires no arguments, got {argCount}",
                            call);
                    }
                    break;

                // Exactly 2
                case "atan2":
                case "pow":
                case "fmod":
                case "fposmod":
                case "posmod":
                case "is_equal_approx":
                case "snappedf":
                case "snappedi":
                case "wrapf":
                case "wrapi":
                case "randi_range":
                case "randf_range":
                case "seed":
                case "var_to_bytes":
                case "bytes_to_var":
                    if (argCount != 2)
                    {
                        ReportError(
                            GDDiagnosticCode.WrongArgumentCount,
                            $"'{name}' requires exactly 2 arguments, got {argCount}",
                            call);
                    }
                    break;

                // Exactly 3
                case "clamp":
                case "clampf":
                case "clampi":
                case "lerp":
                case "lerpf":
                case "lerp_angle":
                case "inverse_lerp":
                case "smoothstep":
                case "move_toward":
                case "rotate_toward":
                    if (argCount != 3)
                    {
                        ReportError(
                            GDDiagnosticCode.WrongArgumentCount,
                            $"'{name}' requires exactly 3 arguments, got {argCount}",
                            call);
                    }
                    break;

                // Exactly 4
                case "remap":
                case "bezier_interpolate":
                case "cubic_interpolate":
                    if (argCount != 4)
                    {
                        ReportError(
                            GDDiagnosticCode.WrongArgumentCount,
                            $"'{name}' requires exactly 4 arguments, got {argCount}",
                            call);
                    }
                    break;

                // Special cases
                case "assert":
                    if (argCount < 1 || argCount > 2)
                    {
                        ReportError(
                            GDDiagnosticCode.WrongArgumentCount,
                            $"'assert' requires 1 or 2 arguments, got {argCount}",
                            call);
                    }
                    break;

                case "range":
                    if (argCount < 1 || argCount > 3)
                    {
                        ReportError(
                            GDDiagnosticCode.WrongArgumentCount,
                            $"'range' requires 1 to 3 arguments, got {argCount}",
                            call);
                    }
                    break;
            }
        }
    }
}
