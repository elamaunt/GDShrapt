using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Checks that abstract methods don't have implementation body.
    /// Abstract methods should end without colon or body.
    /// </summary>
    public class GDAbstractMethodBodyRule : GDLintRule
    {
        public override string RuleId => "GDL220";
        public override string Name => "abstract-method-body";
        public override string Description => "Abstract method should not have implementation body";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            // Check if method has @abstract attribute
            bool isAbstract = methodDeclaration.AttributesDeclaredBefore
                .Any(attr => attr.Attribute?.IsAbstract() == true);

            if (!isAbstract)
                return;

            // Check if abstract method has body
            bool hasColon = methodDeclaration.Colon != null;
            bool hasStatements = methodDeclaration.Statements?.Any() == true;
            bool hasExpression = methodDeclaration.Expression != null;

            if (hasColon || hasStatements || hasExpression)
            {
                var methodName = methodDeclaration.Identifier?.Sequence ?? "<unknown>";
                ReportIssue(
                    $"Abstract method '{methodName}' should not have implementation body",
                    methodDeclaration.Identifier ?? methodDeclaration.FuncKeyword,
                    "Remove the implementation body from abstract method");
            }
        }
    }
}
