using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Warns when a class has too many public methods.
    /// Too many public methods indicate a class that may have too many responsibilities.
    /// </summary>
    public class GDMaxPublicMethodsRule : GDLintRule
    {
        public override string RuleId => "GDL222";
        public override string Name => "max-public-methods";
        public override string Description => "Warn when class has too many public methods";
        public override GDLintCategory Category => GDLintCategory.Complexity;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public const int DefaultMaxPublicMethods = 20;

        public override void Visit(GDClassDeclaration classDecl)
        {
            CheckPublicMethods(classDecl.Methods, classDecl.ClassName?.Identifier);
            base.Visit(classDecl);
        }

        public override void Visit(GDInnerClassDeclaration innerClass)
        {
            CheckPublicMethods(innerClass.Methods, innerClass.Identifier);
            base.Visit(innerClass);
        }

        private void CheckPublicMethods(System.Collections.Generic.IEnumerable<GDMethodDeclaration> methods, GDIdentifier classIdentifier)
        {
            var maxPublicMethods = Options?.MaxPublicMethods ?? DefaultMaxPublicMethods;
            if (maxPublicMethods <= 0)
                return; // Disabled

            if (methods == null)
                return;

            // Public methods are those that don't start with underscore
            var publicMethodCount = methods
                .Count(m => m.Identifier?.Sequence?.StartsWith("_") != true);

            if (publicMethodCount > maxPublicMethods)
            {
                var className = classIdentifier?.Sequence ?? "Class";
                ReportIssue(
                    $"'{className}' has {publicMethodCount} public methods (max {maxPublicMethods})",
                    classIdentifier,
                    "Consider splitting this class into smaller, more focused classes");
            }
        }
    }
}
