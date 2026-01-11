using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Requires strict type hints on variables, parameters, and return types.
    /// Provides configurable severity per element type.
    /// </summary>
    public class GDStrictTypingRule : GDLintRule
    {
        public override string RuleId => "GDL215";
        public override string Name => "strict-typing";
        public override string Description => "Require explicit type hints for strict typing";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public override void Visit(GDVariableDeclaration variableDeclaration)
        {
            if (Options?.StrictTypingClassVariables == null)
                return;

            // Skip constants - they have inferred types
            if (variableDeclaration.ConstKeyword != null)
                return;

            // Skip if using inferred assignment (:=) - TypeColon exists but Type is null
            if (variableDeclaration.TypeColon != null && variableDeclaration.Type == null)
                return;

            var varName = variableDeclaration.Identifier?.Sequence;
            if (string.IsNullOrEmpty(varName))
                return;

            // Check if variable has a type hint
            if (variableDeclaration.Type == null)
            {
                ReportIssue(
                    Options.StrictTypingClassVariables.Value,
                    $"Class variable '{varName}' requires type hint",
                    variableDeclaration.Identifier,
                    $"Add a type hint: 'var {varName}: Type = ...'");
            }
        }

        public override void Visit(GDVariableDeclarationStatement localVariable)
        {
            if (Options?.StrictTypingLocalVariables == null)
                return;

            // Skip if using inferred assignment (:=) - Colon exists but Type is null
            if (localVariable.Colon != null && localVariable.Type == null)
                return;

            var varName = localVariable.Identifier?.Sequence;
            if (string.IsNullOrEmpty(varName))
                return;

            // Check if variable has a type hint
            if (localVariable.Type == null)
            {
                ReportIssue(
                    Options.StrictTypingLocalVariables.Value,
                    $"Local variable '{varName}' requires type hint",
                    localVariable.Identifier,
                    $"Add a type hint: 'var {varName}: Type = ...'");
            }
        }

        public override void Visit(GDParameterDeclaration parameter)
        {
            if (Options?.StrictTypingParameters == null)
                return;

            var paramName = parameter.Identifier?.Sequence;
            if (string.IsNullOrEmpty(paramName))
                return;

            // Check if parameter has a type hint
            if (parameter.Type == null)
            {
                ReportIssue(
                    Options.StrictTypingParameters.Value,
                    $"Parameter '{paramName}' requires type hint",
                    parameter.Identifier,
                    $"Add a type hint: '{paramName}: Type'");
            }
        }

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            if (Options?.StrictTypingReturnTypes == null)
                return;

            var methodName = methodDeclaration.Identifier?.Sequence;
            if (string.IsNullOrEmpty(methodName))
                return;

            // Skip virtual methods - they have predefined signatures
            if (GDSpecialMethodHelper.IsKnownVirtualMethod(methodName))
                return;

            // Check if method has a return type hint
            if (methodDeclaration.ReturnType == null)
            {
                ReportIssue(
                    Options.StrictTypingReturnTypes.Value,
                    $"Function '{methodName}' requires return type hint",
                    methodDeclaration.Identifier,
                    $"Add a return type: 'func {methodName}() -> Type:'");
            }
        }
    }
}
