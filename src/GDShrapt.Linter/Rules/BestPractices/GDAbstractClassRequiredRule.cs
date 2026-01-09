using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Checks that classes containing abstract methods are marked with @abstract.
    /// </summary>
    public class GDAbstractClassRequiredRule : GDLintRule
    {
        public override string RuleId => "GDL221";
        public override string Name => "abstract-class-required";
        public override string Description => "Class with abstract methods must be marked @abstract";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;

        public override void Visit(GDClassDeclaration classDecl)
        {
            ValidateClass(classDecl.CustomAttributes, classDecl.Methods, classDecl);
        }

        public override void Visit(GDInnerClassDeclaration innerClass)
        {
            var classIsAbstract = innerClass.AttributesDeclaredBefore
                .Any(attr => attr.Attribute?.IsAbstract() == true);

            var abstractMethods = innerClass.Methods
                .Where(m => m.AttributesDeclaredBefore
                    .Any(attr => attr.Attribute?.IsAbstract() == true))
                .ToList();

            if (abstractMethods.Any() && !classIsAbstract)
            {
                var className = innerClass.Identifier?.Sequence ?? "<anonymous>";
                var firstAbstractMethod = abstractMethods.First();
                var methodName = firstAbstractMethod.Identifier?.Sequence ?? "<unknown>";

                ReportIssue(
                    $"Inner class '{className}' contains abstract method '{methodName}' but is not marked @abstract",
                    innerClass.Identifier ?? innerClass.ClassKeyword,
                    "Add @abstract annotation before the class declaration");
            }
        }

        private void ValidateClass(
            System.Collections.Generic.IEnumerable<GDCustomAttribute> customAttributes,
            System.Collections.Generic.IEnumerable<GDMethodDeclaration> methods,
            GDClassDeclaration classDecl)
        {
            var classIsAbstract = customAttributes
                .Any(attr => attr.Attribute?.IsAbstract() == true);

            var abstractMethods = methods
                .Where(m => m.AttributesDeclaredBefore
                    .Any(attr => attr.Attribute?.IsAbstract() == true))
                .ToList();

            if (abstractMethods.Any() && !classIsAbstract)
            {
                var firstAbstractMethod = abstractMethods.First();
                var methodName = firstAbstractMethod.Identifier?.Sequence ?? "<unknown>";

                // Get class name from class_name attribute if present
                var classNameAttr = classDecl.ClassNameAttribute;
                var className = classNameAttr?.Type?.Identifier?.Sequence ?? "<anonymous>";

                ReportIssue(
                    $"Class '{className}' contains abstract method '{methodName}' but is not marked @abstract",
                    firstAbstractMethod.Identifier ?? firstAbstractMethod.FuncKeyword,
                    "Add @abstract annotation before the class_name declaration");
            }
        }
    }
}
