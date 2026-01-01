namespace GDShrapt.Reader
{
    /// <summary>
    /// Suggests adding type hints to variables and function parameters.
    /// Based on GDScript style guide: "Use static typing where possible."
    /// </summary>
    public class GDTypeHintRule : GDLintRule
    {
        public override string RuleId => "GDL204";
        public override string Name => "type-hint";
        public override string Description => "Suggest adding type hints for better code clarity";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Hint;
        public override bool EnabledByDefault => false; // Disabled by default

        public override void Visit(GDVariableDeclaration variableDeclaration)
        {
            if (Options?.SuggestTypeHints != true)
                return;

            // Skip constants - they often have inferred types
            if (variableDeclaration.ConstKeyword != null)
                return;

            var varName = variableDeclaration.Identifier?.Sequence;
            if (string.IsNullOrEmpty(varName))
                return;

            // Check if variable has a type hint
            if (variableDeclaration.Type == null)
            {
                ReportIssue(
                    $"Variable '{varName}' has no type hint",
                    variableDeclaration.Identifier,
                    $"Add a type hint, e.g., 'var {varName}: Type = ...'");
            }
        }

        public override void Visit(GDVariableDeclarationStatement localVariable)
        {
            if (Options?.SuggestTypeHints != true)
                return;

            var varName = localVariable.Identifier?.Sequence;
            if (string.IsNullOrEmpty(varName))
                return;

            // Check if variable has a type hint
            if (localVariable.Type == null)
            {
                ReportIssue(
                    $"Local variable '{varName}' has no type hint",
                    localVariable.Identifier,
                    $"Add a type hint, e.g., 'var {varName}: Type = ...'");
            }
        }

        public override void Visit(GDParameterDeclaration parameter)
        {
            if (Options?.SuggestTypeHints != true)
                return;

            var paramName = parameter.Identifier?.Sequence;
            if (string.IsNullOrEmpty(paramName))
                return;

            // Check if parameter has a type hint
            if (parameter.Type == null)
            {
                ReportIssue(
                    $"Parameter '{paramName}' has no type hint",
                    parameter.Identifier,
                    $"Add a type hint, e.g., '{paramName}: Type'");
            }
        }

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            if (Options?.SuggestTypeHints != true)
                return;

            var methodName = methodDeclaration.Identifier?.Sequence;
            if (string.IsNullOrEmpty(methodName))
                return;

            // Skip virtual methods
            if (GDSpecialMethodHelper.IsKnownVirtualMethod(methodName))
                return;

            // Check if method has a return type hint
            if (methodDeclaration.ReturnType == null)
            {
                ReportIssue(
                    $"Function '{methodName}' has no return type hint",
                    methodDeclaration.Identifier,
                    $"Add a return type, e.g., 'func {methodName}() -> Type:'");
            }
        }
    }
}
