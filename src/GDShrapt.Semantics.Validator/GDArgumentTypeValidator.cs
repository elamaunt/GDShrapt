using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Validator
{
    /// <summary>
    /// Validates argument types at function/method call sites.
    /// Uses the semantic model to infer expected parameter types and compare with actual arguments.
    /// Reports ArgumentTypeMismatch diagnostics when types are incompatible.
    /// </summary>
    public class GDArgumentTypeValidator : GDValidationVisitor
    {
        private readonly GDSemanticModel _semanticModel;
        private readonly GDDiagnosticSeverity _severity;

        /// <summary>
        /// Creates a new argument type validator.
        /// </summary>
        /// <param name="context">The validation context.</param>
        /// <param name="semanticModel">The semantic model for the file.</param>
        /// <param name="severity">The severity level for type mismatch diagnostics.</param>
        public GDArgumentTypeValidator(
            GDValidationContext context,
            GDSemanticModel semanticModel,
            GDDiagnosticSeverity severity = GDDiagnosticSeverity.Warning)
            : base(context)
        {
            _semanticModel = semanticModel;
            _severity = severity;
        }

        /// <summary>
        /// Validates the given AST node for argument type mismatches.
        /// </summary>
        public void Validate(GDNode node)
        {
            if (node == null || _semanticModel == null)
                return;

            node.WalkIn(this);
        }

        public override void Visit(GDCallExpression callExpression)
        {
            var caller = callExpression.CallerExpression;
            var arguments = callExpression.Parameters?.ToList() ?? new List<GDExpression>();

            if (arguments.Count == 0)
                return;

            // Direct function call: func_name()
            if (caller is GDIdentifierExpression identifierExpr)
            {
                var funcName = identifierExpr.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(funcName))
                {
                    ValidateUserFunctionArguments(funcName, arguments, callExpression);
                }
            }
            // Method call: self.method() or obj.method()
            else if (caller is GDMemberOperatorExpression memberExpr)
            {
                var callerExpr = memberExpr.CallerExpression;
                var methodName = memberExpr.Identifier?.Sequence;

                if (string.IsNullOrEmpty(methodName))
                    return;

                // self.method() - validate against user-defined methods
                if (callerExpr is GDIdentifierExpression selfExpr && selfExpr.Identifier?.Sequence == "self")
                {
                    ValidateUserFunctionArguments(methodName, arguments, callExpression);
                }
            }
        }

        /// <summary>
        /// Validates argument types for a user-defined function call.
        /// </summary>
        private void ValidateUserFunctionArguments(
            string funcName,
            List<GDExpression> arguments,
            GDCallExpression callExpr)
        {
            // Get the method symbol to access parameter info
            var methodSymbol = _semanticModel.FindSymbol(funcName);
            if (methodSymbol?.DeclarationNode is not GDMethodDeclaration method)
                return;

            var parameters = method.Parameters?.ToList() ?? new List<GDParameterDeclaration>();
            var argCount = System.Math.Min(arguments.Count, parameters.Count);

            for (int i = 0; i < argCount; i++)
            {
                var param = parameters[i];
                var arg = arguments[i];
                var paramName = param.Identifier?.Sequence;

                if (string.IsNullOrEmpty(paramName))
                    continue;

                // Get expected type from parameter (annotation, type guards, or usage)
                var expectedUnion = _semanticModel.GetUnionType(paramName);
                var explicitType = param.Type?.BuildName();

                string expectedType = null;
                if (!string.IsNullOrEmpty(explicitType))
                {
                    expectedType = explicitType;
                }
                else if (expectedUnion != null && !expectedUnion.IsEmpty)
                {
                    // For union types, we'll be more lenient - only report if no types match
                    expectedType = expectedUnion.EffectiveType;
                }

                if (string.IsNullOrEmpty(expectedType) || expectedType == "Variant")
                    continue;

                // Parameter with null type (inferred from default = null) accepts any value
                if (expectedType == "null")
                    continue;

                // Get actual type of argument
                var actualType = InferArgumentType(arg);
                if (string.IsNullOrEmpty(actualType) || actualType == "Unknown" || actualType == "Variant")
                    continue;

                // Check type compatibility
                bool isCompatible = IsTypeCompatible(actualType, expectedType, expectedUnion);

                if (!isCompatible)
                {
                    ReportTypeMismatch(funcName, paramName, expectedType, actualType, arg);
                }
            }
        }

        /// <summary>
        /// Infers the type of an argument expression.
        /// </summary>
        private string InferArgumentType(GDExpression arg)
        {
            // Use semantic model for type inference
            var type = _semanticModel?.GetExpressionType(arg);
            return !string.IsNullOrEmpty(type) ? type : "Unknown";
        }

        /// <summary>
        /// Checks if the actual type is compatible with the expected type.
        /// </summary>
        private bool IsTypeCompatible(string actualType, string expectedType, GDUnionType expectedUnion)
        {
            // Exact match
            if (actualType == expectedType)
                return true;

            // null is compatible with anything (nullable type)
            if (actualType == "null")
                return true;

            // Variant accepts anything
            if (expectedType == "Variant")
                return true;

            // Handle 'self' - resolve to actual class type
            if (actualType == "self")
            {
                var currentClassType = _semanticModel?.ScriptFile?.TypeName;
                if (!string.IsNullOrEmpty(currentClassType))
                {
                    // Replace 'self' with actual class type for inheritance check
                    actualType = currentClassType;
                }
                else
                {
                    // If we can't determine type, assume compatible
                    return true;
                }
            }

            // Check union type compatibility
            if (expectedUnion != null && !expectedUnion.IsEmpty)
            {
                // If actual type is in the union, it's compatible
                if (expectedUnion.Types.Contains(actualType))
                    return true;

                // Check inheritance for each type in union
                foreach (var unionType in expectedUnion.Types)
                {
                    if (IsInheritanceCompatible(actualType, unionType))
                        return true;
                }
            }

            // Check inheritance
            if (IsInheritanceCompatible(actualType, expectedType))
                return true;

            // Numeric compatibility
            if (IsNumericCompatible(actualType, expectedType))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if actualType inherits from expectedType.
        /// </summary>
        private bool IsInheritanceCompatible(string actualType, string expectedType)
        {
            if (Context.RuntimeProvider == null)
                return false;

            return Context.RuntimeProvider.IsAssignableTo(actualType, expectedType);
        }

        /// <summary>
        /// Checks numeric type compatibility (int -> float is OK).
        /// </summary>
        private static bool IsNumericCompatible(string actualType, string expectedType)
        {
            if (actualType == "int" && expectedType == "float")
                return true;

            return false;
        }

        /// <summary>
        /// Reports a type mismatch diagnostic.
        /// </summary>
        private void ReportTypeMismatch(
            string funcName,
            string paramName,
            string expectedType,
            string actualType,
            GDExpression argument)
        {
            var message = $"Argument type mismatch for '{paramName}' in call to '{funcName}': " +
                          $"expected '{expectedType}', got '{actualType}'";

            switch (_severity)
            {
                case GDDiagnosticSeverity.Error:
                    ReportError(GDDiagnosticCode.ArgumentTypeMismatch, message, argument);
                    break;
                case GDDiagnosticSeverity.Warning:
                    ReportWarning(GDDiagnosticCode.ArgumentTypeMismatch, message, argument);
                    break;
                case GDDiagnosticSeverity.Hint:
                    ReportHint(GDDiagnosticCode.ArgumentTypeMismatch, message, argument);
                    break;
            }
        }
    }
}
