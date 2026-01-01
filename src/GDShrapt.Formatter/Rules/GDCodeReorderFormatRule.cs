using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Reorders class members according to the specified category order.
    /// </summary>
    public class GDCodeReorderFormatRule : GDFormatRule
    {
        public override string RuleId => "GDF007";
        public override string Name => "code-reorder";
        public override string Description => "Reorder class members according to style guide";

        /// <summary>
        /// This rule is opt-in by default.
        /// </summary>
        public override bool EnabledByDefault => false;

        // Built-in Godot method names
        private static readonly HashSet<string> BuiltinMethodNames = new HashSet<string>
        {
            "_init",
            "_ready",
            "_process",
            "_physics_process",
            "_enter_tree",
            "_exit_tree",
            "_input",
            "_unhandled_input",
            "_unhandled_key_input",
            "_gui_input",
            "_draw",
            "_get",
            "_set",
            "_get_property_list",
            "_notification",
            "_to_string",
            "_get_configuration_warning",
            "_get_configuration_warnings"
        };

        public override void Visit(GDClassDeclaration classDeclaration)
        {
            if (!Options.ReorderCode)
                return;

            ReorderMembers(classDeclaration);
        }

        public override void Visit(GDInnerClassDeclaration innerClass)
        {
            if (!Options.ReorderCode)
                return;

            ReorderInnerClassMembers(innerClass);
        }

        private void ReorderMembers(GDClassDeclaration classDeclaration)
        {
            if (classDeclaration?.Members == null)
                return;

            var members = classDeclaration.Members.ToList();
            if (members.Count <= 1)
                return;

            var categorized = CategorizeMembersWithContext(members);
            var sorted = SortByCategory(categorized, Options.MemberOrder);

            if (!HasOrderChanged(members, sorted))
                return;

            ReorderInPlace(classDeclaration.Members, sorted);
        }

        private void ReorderInnerClassMembers(GDInnerClassDeclaration innerClass)
        {
            if (innerClass?.Members == null)
                return;

            var members = innerClass.Members.ToList();
            if (members.Count <= 1)
                return;

            var categorized = CategorizeMembersWithContext(members);
            var sorted = SortByCategory(categorized, Options.MemberOrder);

            if (!HasOrderChanged(members, sorted))
                return;

            ReorderInPlace(innerClass.Members, sorted);
        }

        private class MemberWithContext
        {
            public GDClassMember Member { get; set; }
            public GDMemberCategory Category { get; set; }
            public int OriginalIndex { get; set; }
            public List<GDSyntaxToken> LeadingTokens { get; set; } = new List<GDSyntaxToken>();
        }

        private List<MemberWithContext> CategorizeMembersWithContext(List<GDClassMember> members)
        {
            var result = new List<MemberWithContext>();

            for (int i = 0; i < members.Count; i++)
            {
                var member = members[i];
                result.Add(new MemberWithContext
                {
                    Member = member,
                    Category = GetCategory(member),
                    OriginalIndex = i
                });
            }

            return result;
        }

        private List<MemberWithContext> SortByCategory(List<MemberWithContext> members, List<GDMemberCategory> order)
        {
            var orderDict = new Dictionary<GDMemberCategory, int>();
            for (int i = 0; i < order.Count; i++)
            {
                orderDict[order[i]] = i;
            }

            // Use stable sort to preserve relative order within same category
            return members
                .OrderBy(m => orderDict.TryGetValue(m.Category, out var idx) ? idx : int.MaxValue)
                .ThenBy(m => m.OriginalIndex)
                .ToList();
        }

        private bool HasOrderChanged(List<GDClassMember> original, List<MemberWithContext> sorted)
        {
            for (int i = 0; i < original.Count; i++)
            {
                if (sorted[i].OriginalIndex != i)
                    return true;
            }
            return false;
        }

        private void ReorderInPlace(GDClassMembersList membersList, List<MemberWithContext> sorted)
        {
            if (membersList?.Form == null)
                return;

            var form = membersList.Form;

            // Collect all tokens associated with each member (including preceding whitespace/newlines)
            var memberTokenGroups = new List<List<GDSyntaxToken>>();
            var allTokens = form.ToList();

            GDClassMember currentMember = null;
            List<GDSyntaxToken> currentGroup = new List<GDSyntaxToken>();
            List<GDSyntaxToken> leadingTokens = new List<GDSyntaxToken>();

            foreach (var token in allTokens)
            {
                if (token is GDClassMember member)
                {
                    if (currentMember != null)
                    {
                        memberTokenGroups.Add(currentGroup);
                        currentGroup = new List<GDSyntaxToken>();
                    }

                    // Add any leading tokens (newlines, indentation) before this member
                    currentGroup.AddRange(leadingTokens);
                    leadingTokens.Clear();

                    currentGroup.Add(token);
                    currentMember = member;
                }
                else if (currentMember != null)
                {
                    // Token after a member - collect until next member
                    if (token is GDNewLine || token is GDIntendation || token is GDSpace)
                    {
                        leadingTokens.Add(token);
                    }
                    else
                    {
                        currentGroup.AddRange(leadingTokens);
                        leadingTokens.Clear();
                        currentGroup.Add(token);
                    }
                }
                else
                {
                    // Leading tokens before first member
                    leadingTokens.Add(token);
                }
            }

            // Add the last group
            if (currentMember != null)
            {
                memberTokenGroups.Add(currentGroup);
            }

            if (memberTokenGroups.Count != sorted.Count)
                return; // Safety check

            // Build the new order of token groups
            var reorderedGroups = sorted
                .Select(s => memberTokenGroups[s.OriginalIndex])
                .ToList();

            // Clear and rebuild the form
            var tokensToRemove = form.ToList();
            foreach (var token in tokensToRemove)
            {
                form.Remove(token);
            }

            // Add initial leading tokens
            foreach (var token in leadingTokens)
            {
                form.AddToEnd(token);
            }

            // Add reordered groups
            foreach (var group in reorderedGroups)
            {
                foreach (var token in group)
                {
                    form.AddToEnd(token);
                }
            }
        }

        /// <summary>
        /// Determines the category of a class member.
        /// </summary>
        public static GDMemberCategory GetCategory(GDClassMember member)
        {
            switch (member)
            {
                case GDClassNameAttribute _:
                case GDExtendsAttribute _:
                case GDToolAttribute _:
                    return GDMemberCategory.ClassAttribute;

                case GDSignalDeclaration _:
                    return GDMemberCategory.Signal;

                case GDEnumDeclaration _:
                    return GDMemberCategory.Enum;

                case GDVariableDeclaration varDecl:
                    return GetVariableCategory(varDecl);

                case GDMethodDeclaration methodDecl:
                    return GetMethodCategory(methodDecl);

                case GDInnerClassDeclaration _:
                    return GDMemberCategory.InnerClass;

                case GDCustomAttribute _:
                    // Custom attributes are typically attached to following declarations
                    // They should stay with their declaration, not be reordered separately
                    return GDMemberCategory.ClassAttribute;

                default:
                    return GDMemberCategory.PublicVariable;
            }
        }

        private static GDMemberCategory GetVariableCategory(GDVariableDeclaration varDecl)
        {
            // Check for const keyword
            if (varDecl.ConstKeyword != null)
                return GDMemberCategory.Constant;

            // Check attributes
            var attributes = varDecl.AttributesDeclaredBefore?.ToList() ?? new List<GDCustomAttribute>();

            bool hasExport = false;
            bool hasOnready = false;

            foreach (var attr in attributes)
            {
                var attrName = attr.Attribute?.Name?.Sequence?.ToLowerInvariant();
                if (attrName == "export" || attrName?.StartsWith("export_") == true)
                    hasExport = true;
                if (attrName == "onready")
                    hasOnready = true;
            }

            // @onready takes precedence for ordering purposes
            if (hasOnready)
                return GDMemberCategory.OnreadyVariable;

            if (hasExport)
                return GDMemberCategory.ExportVariable;

            // Check if private (starts with _)
            var name = varDecl.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(name) && name.StartsWith("_"))
                return GDMemberCategory.PrivateVariable;

            return GDMemberCategory.PublicVariable;
        }

        private static GDMemberCategory GetMethodCategory(GDMethodDeclaration methodDecl)
        {
            var name = methodDecl.Identifier?.Sequence;

            if (string.IsNullOrEmpty(name))
                return GDMemberCategory.PublicMethod;

            // Check for built-in methods
            if (BuiltinMethodNames.Contains(name))
                return GDMemberCategory.BuiltinMethod;

            // Check if private (starts with _ but not a built-in)
            if (name.StartsWith("_"))
                return GDMemberCategory.PrivateMethod;

            return GDMemberCategory.PublicMethod;
        }
    }
}
