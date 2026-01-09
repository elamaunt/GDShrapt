using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Checks that class members are ordered according to GDScript style guide.
    /// Recommended order: signals, enums, constants, @export variables,
    /// public variables, private variables, @onready variables, and functions.
    /// Functions can be further ordered by: static, abstract, lifecycle, public, private.
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
            ClassAttribute,     // tool, extends, class_name
            Signal,             // signal declarations
            Enum,               // enum declarations
            Constant,           // const declarations
            ExportVariable,     // @export var
            PublicVariable,     // var without _ prefix
            PrivateVariable,    // var with _ prefix
            OnReadyVariable,    // @onready var
            InnerClass,         // class declarations
            StaticMethod,       // static func
            AbstractMethod,     // @abstract func
            LifecycleMethod,    // _init, _ready, _process, etc.
            PublicMethod,       // func without _ prefix
            PrivateMethod       // func with _ prefix
        }

        private static readonly HashSet<string> LifecycleMethods = new HashSet<string>
        {
            "_init", "_ready", "_enter_tree", "_exit_tree",
            "_process", "_physics_process", "_input", "_unhandled_input",
            "_unhandled_key_input", "_notification", "_draw",
            "_gui_input", "_get_configuration_warnings"
        };

        public override void Visit(GDClassDeclaration classDeclaration)
        {
            if (Options?.EnforceMemberOrdering != true)
                return;

            CheckMemberOrdering(classDeclaration.Members, classDeclaration.ClassName?.Identifier);
            CheckMethodOrdering(classDeclaration.Methods.ToList(), classDeclaration.ClassName?.Identifier);
        }

        public override void Visit(GDInnerClassDeclaration innerClass)
        {
            if (Options?.EnforceMemberOrdering != true)
                return;

            CheckMemberOrdering(innerClass.Members, innerClass.Identifier);
            CheckMethodOrdering(innerClass.Methods.ToList(), innerClass.Identifier);
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

                // For methods, use the base Function category for general ordering
                var compareCategory = IsMethodCategory(currentCategory) ? MemberCategory.PublicMethod : currentCategory;
                var lastCompareCategory = lastCategory.HasValue && IsMethodCategory(lastCategory.Value)
                    ? MemberCategory.PublicMethod
                    : lastCategory;

                if (lastCompareCategory.HasValue && !IsMethodCategory(currentCategory) &&
                    compareCategory < lastCompareCategory.Value)
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

        private void CheckMethodOrdering(List<GDMethodDeclaration> methods, GDIdentifier classIdentifier)
        {
            if (methods == null || methods.Count < 2)
                return;

            var abstractPosition = Options?.AbstractMethodPosition ?? "none";
            var privatePosition = Options?.PrivateMethodPosition ?? "after_public";
            var staticPosition = Options?.StaticMethodPosition ?? "none";

            // Check abstract method positioning
            if (abstractPosition != "none")
            {
                CheckMethodGroupPosition(methods, IsAbstractMethod, abstractPosition, "abstract method");
            }

            // Check static method positioning
            if (staticPosition != "none")
            {
                CheckMethodGroupPosition(methods, IsStaticMethod, staticPosition, "static method");
            }

            // Check private method positioning
            if (privatePosition != "none")
            {
                CheckPrivateMethodPosition(methods, privatePosition);
            }
        }

        private void CheckMethodGroupPosition(List<GDMethodDeclaration> methods,
            System.Func<GDMethodDeclaration, bool> predicate, string position, string methodType)
        {
            var matchingMethods = methods.Where(predicate).ToList();
            var otherMethods = methods.Where(m => !predicate(m)).ToList();

            if (matchingMethods.Count == 0 || otherMethods.Count == 0)
                return;

            if (position == "first")
            {
                // All matching methods should come before non-matching
                var lastMatchingIndex = methods.FindLastIndex(m => predicate(m));
                var firstOtherIndex = methods.FindIndex(m => !predicate(m));

                if (firstOtherIndex < lastMatchingIndex)
                {
                    var outOfOrder = matchingMethods.Where(m => methods.IndexOf(m) > firstOtherIndex).FirstOrDefault();
                    if (outOfOrder != null)
                    {
                        ReportIssue(
                            $"'{outOfOrder.Identifier?.Sequence}' ({methodType}) should appear before other methods",
                            outOfOrder.Identifier,
                            $"Move {methodType}s to the beginning of the methods section");
                    }
                }
            }
            else if (position == "last")
            {
                // All matching methods should come after non-matching
                var firstMatchingIndex = methods.FindIndex(m => predicate(m));
                var lastOtherIndex = methods.FindLastIndex(m => !predicate(m));

                if (firstMatchingIndex < lastOtherIndex)
                {
                    var outOfOrder = matchingMethods.Where(m => methods.IndexOf(m) < lastOtherIndex).FirstOrDefault();
                    if (outOfOrder != null)
                    {
                        ReportIssue(
                            $"'{outOfOrder.Identifier?.Sequence}' ({methodType}) should appear after other methods",
                            outOfOrder.Identifier,
                            $"Move {methodType}s to the end of the methods section");
                    }
                }
            }
        }

        private void CheckPrivateMethodPosition(List<GDMethodDeclaration> methods, string position)
        {
            var privateMethods = methods.Where(IsPrivateMethod).ToList();
            var publicMethods = methods.Where(m => !IsPrivateMethod(m) && !IsLifecycleMethod(m)).ToList();

            if (privateMethods.Count == 0 || publicMethods.Count == 0)
                return;

            if (position == "after_public")
            {
                // Private methods should come after public methods
                var lastPublicIndex = methods.FindLastIndex(m => !IsPrivateMethod(m) && !IsLifecycleMethod(m));
                var firstPrivateIndex = methods.FindIndex(m => IsPrivateMethod(m));

                if (firstPrivateIndex < lastPublicIndex)
                {
                    var outOfOrder = privateMethods.Where(m => methods.IndexOf(m) < lastPublicIndex).FirstOrDefault();
                    if (outOfOrder != null)
                    {
                        ReportIssue(
                            $"'{outOfOrder.Identifier?.Sequence}' (private method) should appear after public methods",
                            outOfOrder.Identifier,
                            "Move private methods after public methods");
                    }
                }
            }
            else if (position == "before_public")
            {
                // Private methods should come before public methods
                var lastPrivateIndex = methods.FindLastIndex(m => IsPrivateMethod(m));
                var firstPublicIndex = methods.FindIndex(m => !IsPrivateMethod(m) && !IsLifecycleMethod(m));

                if (firstPublicIndex >= 0 && lastPrivateIndex > firstPublicIndex)
                {
                    var outOfOrder = privateMethods.Where(m => methods.IndexOf(m) > firstPublicIndex).FirstOrDefault();
                    if (outOfOrder != null)
                    {
                        ReportIssue(
                            $"'{outOfOrder.Identifier?.Sequence}' (private method) should appear before public methods",
                            outOfOrder.Identifier,
                            "Move private methods before public methods");
                    }
                }
            }
        }

        private bool IsMethodCategory(MemberCategory category)
        {
            return category == MemberCategory.StaticMethod ||
                   category == MemberCategory.AbstractMethod ||
                   category == MemberCategory.LifecycleMethod ||
                   category == MemberCategory.PublicMethod ||
                   category == MemberCategory.PrivateMethod;
        }

        private bool IsAbstractMethod(GDMethodDeclaration method)
        {
            return method.AttributesDeclaredBefore
                .OfType<GDCustomAttribute>()
                .Any(a => a.Attribute?.Name?.Sequence == "abstract");
        }

        private bool IsStaticMethod(GDMethodDeclaration method)
        {
            return method.StaticKeyword != null;
        }

        private bool IsPrivateMethod(GDMethodDeclaration method)
        {
            var name = method.Identifier?.Sequence;
            return name?.StartsWith("_") == true && !LifecycleMethods.Contains(name);
        }

        private bool IsLifecycleMethod(GDMethodDeclaration method)
        {
            var name = method.Identifier?.Sequence;
            return name != null && LifecycleMethods.Contains(name);
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

                case GDMethodDeclaration method:
                    return GetMethodCategory(method);

                default:
                    return MemberCategory.PublicMethod; // Default to last
            }
        }

        private MemberCategory GetMethodCategory(GDMethodDeclaration method)
        {
            // Check static first
            if (method.StaticKeyword != null)
                return MemberCategory.StaticMethod;

            // Check abstract
            var hasAbstract = method.AttributesDeclaredBefore
                .OfType<GDCustomAttribute>()
                .Any(a => a.Attribute?.Name?.Sequence == "abstract");
            if (hasAbstract)
                return MemberCategory.AbstractMethod;

            // Check lifecycle
            var methodName = method.Identifier?.Sequence;
            if (methodName != null && LifecycleMethods.Contains(methodName))
                return MemberCategory.LifecycleMethod;

            // Check private (starts with _ but not lifecycle)
            if (methodName?.StartsWith("_") == true)
                return MemberCategory.PrivateMethod;

            return MemberCategory.PublicMethod;
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
                case MemberCategory.StaticMethod: return "static method";
                case MemberCategory.AbstractMethod: return "abstract method";
                case MemberCategory.LifecycleMethod: return "lifecycle method";
                case MemberCategory.PublicMethod: return "public method";
                case MemberCategory.PrivateMethod: return "private method";
                default: return "member";
            }
        }
    }
}
