using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Checks that class members are ordered according to GDScript style guide.
    /// Recommended order: signals, enums, constants, @export variables,
    /// public variables, private variables, @onready variables, and functions.
    /// </summary>
    public class GDMemberOrderingRule : GDLintRule
    {
        public override string RuleId => "GDL301";
        public override string Name => "member-ordering";
        public override string Description => "Class members should be ordered according to GDScript style guide";
        public override GDLintCategory Category => GDLintCategory.Style;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Info;
        public override bool EnabledByDefault => false; // Optional style preference

        private enum MemberCategory
        {
            ClassAttribute,   // tool, extends, class_name
            Signal,          // signal declarations
            Enum,            // enum declarations
            Constant,        // const declarations
            ExportVariable,  // @export var
            PublicVariable,  // var without _ prefix
            PrivateVariable, // var with _ prefix
            OnReadyVariable, // @onready var
            InnerClass,      // class declarations
            Function         // func declarations
        }

        public override void Visit(GDClassDeclaration classDeclaration)
        {
            if (Options?.EnforceMemberOrdering != true)
                return;

            // GDClassDeclaration doesn't have an identifier directly, use ClassName if available
            CheckMemberOrdering(classDeclaration.Members, classDeclaration.ClassName?.Identifier);
        }

        public override void Visit(GDInnerClassDeclaration innerClass)
        {
            if (Options?.EnforceMemberOrdering != true)
                return;

            CheckMemberOrdering(innerClass.Members, innerClass.Identifier);
        }

        private void CheckMemberOrdering(IEnumerable<GDClassMember> members, GDIdentifier classIdentifier)
        {
            if (members == null)
                return;

            MemberCategory? lastCategory = null;
            GDClassMember lastMember = null;

            foreach (var member in members)
            {
                // Skip custom attributes (they're attached to the next member)
                if (member is GDCustomAttribute)
                    continue;

                var currentCategory = GetMemberCategory(member);

                // Skip class attributes - they must be first
                if (currentCategory == MemberCategory.ClassAttribute)
                    continue;

                if (lastCategory.HasValue && currentCategory < lastCategory.Value)
                {
                    // Out of order
                    var currentName = GetMemberName(member);
                    var lastName = GetMemberName(lastMember);
                    var currentCategoryName = GetCategoryName(currentCategory);
                    var lastCategoryName = GetCategoryName(lastCategory.Value);

                    ReportIssue(
                        $"'{currentName}' ({currentCategoryName}) should appear before '{lastName}' ({lastCategoryName})",
                        GetMemberIdentifier(member),
                        $"Move {currentCategoryName} declarations before {lastCategoryName} declarations");
                }

                lastCategory = currentCategory;
                lastMember = member;
            }
        }

        private MemberCategory GetMemberCategory(GDClassMember member)
        {
            switch (member)
            {
                case GDToolAttribute _:
                case GDExtendsAttribute _:
                case GDClassNameAttribute _:
                    return MemberCategory.ClassAttribute;

                case GDSignalDeclaration _:
                    return MemberCategory.Signal;

                case GDEnumDeclaration _:
                    return MemberCategory.Enum;

                case GDVariableDeclaration varDecl:
                    if (varDecl.ConstKeyword != null)
                        return MemberCategory.Constant;

                    // Check for @onready
                    var hasOnReady = varDecl.AttributesDeclaredBefore
                        .OfType<GDCustomAttribute>()
                        .Any(a => a.Attribute?.Name?.Sequence == "onready");
                    if (hasOnReady)
                        return MemberCategory.OnReadyVariable;

                    // Check for @export
                    var hasExport = varDecl.AttributesDeclaredBefore
                        .OfType<GDCustomAttribute>()
                        .Any(a => a.Attribute?.Name?.Sequence == "export" ||
                                  a.Attribute?.Name?.Sequence?.StartsWith("export_") == true);
                    if (hasExport)
                        return MemberCategory.ExportVariable;

                    // Check if private (starts with _)
                    var varName = varDecl.Identifier?.Sequence;
                    if (varName?.StartsWith("_") == true)
                        return MemberCategory.PrivateVariable;

                    return MemberCategory.PublicVariable;

                case GDInnerClassDeclaration _:
                    return MemberCategory.InnerClass;

                case GDMethodDeclaration _:
                    return MemberCategory.Function;

                default:
                    return MemberCategory.Function; // Default to last
            }
        }

        private string GetMemberName(GDClassMember member)
        {
            switch (member)
            {
                case GDSignalDeclaration sig:
                    return sig.Identifier?.Sequence ?? "signal";
                case GDEnumDeclaration enm:
                    return enm.Identifier?.Sequence ?? "enum";
                case GDVariableDeclaration varDecl:
                    return varDecl.Identifier?.Sequence ?? "variable";
                case GDMethodDeclaration method:
                    return method.Identifier?.Sequence ?? "function";
                case GDInnerClassDeclaration cls:
                    return cls.Identifier?.Sequence ?? "class";
                default:
                    return "member";
            }
        }

        private GDSyntaxToken GetMemberIdentifier(GDClassMember member)
        {
            switch (member)
            {
                case GDSignalDeclaration sig:
                    return sig.Identifier;
                case GDEnumDeclaration enm:
                    return enm.Identifier;
                case GDVariableDeclaration varDecl:
                    return varDecl.Identifier;
                case GDMethodDeclaration method:
                    return method.Identifier;
                case GDInnerClassDeclaration cls:
                    return cls.Identifier;
                default:
                    return null;
            }
        }

        private string GetCategoryName(MemberCategory category)
        {
            switch (category)
            {
                case MemberCategory.Signal: return "signal";
                case MemberCategory.Enum: return "enum";
                case MemberCategory.Constant: return "constant";
                case MemberCategory.ExportVariable: return "@export variable";
                case MemberCategory.PublicVariable: return "public variable";
                case MemberCategory.PrivateVariable: return "private variable";
                case MemberCategory.OnReadyVariable: return "@onready variable";
                case MemberCategory.InnerClass: return "inner class";
                case MemberCategory.Function: return "function";
                default: return "member";
            }
        }
    }
}
