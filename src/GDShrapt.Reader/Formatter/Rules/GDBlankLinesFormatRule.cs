using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Formats blank lines between functions, classes, and member types.
    /// </summary>
    public class GDBlankLinesFormatRule : GDFormatRule
    {
        public override string RuleId => "GDF002";
        public override string Name => "blank-lines";
        public override string Description => "Format blank lines between functions and member groups";

        private GDClassMember _previousMember;
        private bool _afterClassDeclaration;

        public override void Visit(GDClassDeclaration classDeclaration)
        {
            if (classDeclaration == null)
                return;

            _previousMember = null;
            _afterClassDeclaration = false;

            // Check if there's an extends or class_name
            if (classDeclaration.Extends != null || classDeclaration.ClassName != null)
            {
                _afterClassDeclaration = true;
            }
        }

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            ProcessMember(methodDeclaration, MemberType.Function);
        }

        public override void Visit(GDVariableDeclaration variableDeclaration)
        {
            ProcessMember(variableDeclaration, MemberType.Variable);
        }

        public override void Visit(GDSignalDeclaration signalDeclaration)
        {
            ProcessMember(signalDeclaration, MemberType.Signal);
        }

        public override void Visit(GDEnumDeclaration enumDeclaration)
        {
            ProcessMember(enumDeclaration, MemberType.Enum);
        }

        public override void Visit(GDClassNameAttribute classNameAttribute)
        {
            ProcessMember(classNameAttribute, MemberType.ClassAttribute);
        }

        public override void Visit(GDExtendsAttribute extendsAttribute)
        {
            ProcessMember(extendsAttribute, MemberType.ClassAttribute);
        }

        public override void Visit(GDToolAttribute toolAttribute)
        {
            ProcessMember(toolAttribute, MemberType.ClassAttribute);
        }

        public override void Visit(GDCustomAttribute customAttribute)
        {
            // Custom attributes (like @onready, @export) should be tracked as previous member
            // but NOT trigger blank line insertion - they stay with their following declaration
            ProcessMember(customAttribute, MemberType.CustomAttribute);
        }

        public override void Visit(GDInnerClassDeclaration innerClassDeclaration)
        {
            ProcessMember(innerClassDeclaration, MemberType.InnerClass);
        }

        private void ProcessMember(GDClassMember member, MemberType currentType)
        {
            if (member?.Parent == null)
                return;

            var parent = member.Parent as GDNode;
            if (parent == null)
                return;

            var form = parent.Form;
            if (form == null)
                return;

            // Custom attributes (like @onready, @export) should NOT have blank lines inserted
            // They are part of the following declaration and should stay attached
            if (currentType == MemberType.CustomAttribute)
            {
                _previousMember = member;
                return;
            }

            // Class attributes (extends, class_name, tool) are part of class declaration
            // They should NOT trigger blank line insertion and should be skipped
            if (currentType == MemberType.ClassAttribute)
            {
                _previousMember = member;
                return;
            }

            // Check if this member has custom attributes declared before it
            // If so, we should insert blank lines before the FIRST attribute, not before this member
            var attributesBefore = member.AttributesDeclaredBefore.ToList();
            GDSyntaxToken targetToken = attributesBefore.Count > 0
                ? (GDSyntaxToken)attributesBefore.Last() // Last in the enumeration is first in source order
                : (GDSyntaxToken)member;

            int targetBlankLines = 0;

            // Check if previous member was a class attribute - if so, this is the first real member
            var prevType = _previousMember != null ? GetMemberType(_previousMember) : MemberType.Other;
            bool afterClassAttrs = _afterClassDeclaration &&
                                   (_previousMember == null || prevType == MemberType.ClassAttribute);

            if (afterClassAttrs)
            {
                // First member after class declaration (extends, class_name, tool)
                targetBlankLines = Options.BlankLinesAfterClassDeclaration;
                _afterClassDeclaration = false;
            }
            else if (_previousMember != null)
            {
                // Skip class attributes when determining previous type for spacing
                if (prevType == MemberType.ClassAttribute)
                {
                    // Already handled above
                    targetBlankLines = 0;
                }
                // Skip custom attributes when determining previous type for spacing
                // since they are part of the current declaration group
                else if (prevType == MemberType.CustomAttribute)
                {
                    // Don't add blank lines after custom attributes - they stay with their declaration
                    targetBlankLines = 0;
                }
                else if (currentType == MemberType.Function || prevType == MemberType.Function)
                {
                    // Function to function or function to other
                    targetBlankLines = Options.BlankLinesBetweenFunctions;
                }
                else if (currentType != prevType)
                {
                    // Different member types
                    targetBlankLines = Options.BlankLinesBetweenMemberTypes;
                }
            }

            if (targetBlankLines > 0)
            {
                EnsureBlankLinesBefore(targetToken, parent, targetBlankLines);
            }

            _previousMember = member;
        }

        private MemberType GetMemberType(GDClassMember member)
        {
            switch (member)
            {
                case GDMethodDeclaration _:
                    return MemberType.Function;
                case GDVariableDeclaration _:
                    return MemberType.Variable;
                case GDSignalDeclaration _:
                    return MemberType.Signal;
                case GDEnumDeclaration _:
                    return MemberType.Enum;
                case GDCustomAttribute _:
                    return MemberType.CustomAttribute;
                case GDClassAttribute _:
                    return MemberType.ClassAttribute;
                case GDInnerClassDeclaration _:
                    return MemberType.InnerClass;
                default:
                    return MemberType.Other;
            }
        }

        public override void Left(GDClassDeclaration classDeclaration)
        {
            // Reset state after leaving class
            _previousMember = null;
            _afterClassDeclaration = false;
        }

        private enum MemberType
        {
            Function,
            Variable,
            Signal,
            Enum,
            ClassAttribute,
            CustomAttribute,
            InnerClass,
            Other
        }
    }
}
