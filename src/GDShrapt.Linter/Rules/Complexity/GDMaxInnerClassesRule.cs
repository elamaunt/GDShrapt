using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Warns when a file has too many inner classes.
    /// Too many inner classes can make a file hard to navigate and understand.
    /// </summary>
    public class GDMaxInnerClassesRule : GDLintRule
    {
        public override string RuleId => "GDL232";
        public override string Name => "max-inner-classes";
        public override string Description => "Warn when file has too many inner classes";
        public override GDLintCategory Category => GDLintCategory.Complexity;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public const int DefaultMaxInnerClasses = 5;

        public override void Visit(GDClassDeclaration classDecl)
        {
            var maxInnerClasses = Options?.MaxInnerClasses ?? DefaultMaxInnerClasses;
            if (maxInnerClasses <= 0)
                return; // Disabled

            var innerClassCount = classDecl.InnerClasses?.Count() ?? 0;

            if (innerClassCount > maxInnerClasses)
            {
                var className = classDecl.ClassName?.Identifier?.Sequence ?? "File";
                ReportIssue(
                    $"'{className}' has {innerClassCount} inner classes (max {maxInnerClasses})",
                    classDecl.ClassName?.Identifier,
                    "Consider moving some inner classes to separate files");
            }

            base.Visit(classDecl);
        }
    }
}
